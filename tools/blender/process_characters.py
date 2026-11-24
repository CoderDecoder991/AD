"""Prepare the user-supplied soldier models (CC-BY 4.0, Sketchfab; see docs/ASSET_REGISTER.md) as posed,
static crew figures for the base (no animations yet; re-run from the source .glb once animations exist).

Per model:
- removes the importer's bone-display sphere (it inflated the bounds to 2.8 m);
- makes materials opaque (the source marks whole bodies as alpha-blended, which makes overlapping clothing
  layers draw in the wrong order: the patchy look seen in the previews);
- full-gear soldier: paints out the national patches ("РОССИЯ" + flag, round emblem) on its gear texture
  with a plain velcro tone (project rule R-14: no real national flags or insignia);
- poses T-posed Mixamo rigs into a relaxed standing stance (arms down), then bakes the pose into the mesh
  and removes the skeleton;
- decimates to a crew-figure budget, stands the figure on the ground, exports .gltf + textures.

Usage: blender -b --factory-startup --python process_characters.py -- <SourceArt/Characters> <Assets/.../Characters> <preview_dir>
"""
import math
import os
import sys

import bpy
from mathutils import Matrix, Vector

SRC, DST, PREV = sys.argv[sys.argv.index("--") + 1:][:3]

MODELS = [
    # name               file                     target tris  pose
    ("soldier_a",        "soldier1.glb",          12000,       "tpose"),
    ("soldier_b",        "soldier2.glb",          12000,       "tpose"),
    ("soldier_gear",     "soldier_full_gear.glb", 24000,       "keep"),
]

# Patch areas on the full-gear texture "Image_2" (1024², pixel boxes with the origin at the TOP-left,
# as seen in an image viewer): two copies of the "РОССИЯ"/flag patch and a round emblem.
PATCHES = {"Image_2": [(916, 580, 1024, 710), (6, 832, 142, 980), (6, 750, 58, 802)]}
VELCRO = (0.105, 0.11, 0.1)  # dull olive-grey, like the bare loop panel


def tri_count(objs):
    return sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in objs)


def paint_out(img, boxes):
    w, h = img.size
    px = list(img.pixels)
    import random
    rnd = random.Random(7)
    for (x0, y0, x1, y1) in boxes:
        for yt in range(y0, min(y1, h)):
            y = h - 1 - yt  # Blender pixels start at the bottom row
            for x in range(x0, min(x1, w)):
                i = (y * w + x) * 4
                n = (rnd.random() - 0.5) * 0.025  # fabric noise
                px[i], px[i + 1], px[i + 2] = VELCRO[0] + n, VELCRO[1] + n, VELCRO[2] + n
    img.pixels = px
    img.update()
    img.pack()
    print(f"[chars] painted out {len(boxes)} patch areas on {img.name}")


def make_opaque(mat):
    """Unlink the alpha input so the exporter writes alphaMode OPAQUE (eyes/glasses keep theirs)."""
    if not mat.use_nodes or any(k in mat.name for k in ("Eye", "Glus", "Glass")):
        return
    for n in mat.node_tree.nodes:
        if n.type == "BSDF_PRINCIPLED":
            a = n.inputs["Alpha"]
            for link in list(a.links):
                mat.node_tree.links.remove(link)
            a.default_value = 1.0
    mat.surface_render_method = "DITHERED"


def lower_arms(arm):
    """T-pose -> relaxed stance: upper arms down along the body, slight elbow bend."""
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    pbs = arm.pose.bones

    def find(part):
        return next((b for b in pbs if b.name.split(":")[-1].split("_")[0] == part), None)

    for side, sign in (("Left", 1), ("Right", -1)):
        up = find(side + "Arm")
        fore = find(side + "ForeArm")
        if up is None:
            continue
        for pb, target in ((up, None), (fore, None)):
            if pb is None:
                continue
            bpy.context.view_layer.update()
            head = pb.head.copy()
            d = (pb.tail - pb.head).normalized()
            if pb is up:
                # Point the upper arm down and slightly out from the body.
                target = Vector((0.0, 0.0, -1.0)) + Vector((d.x, d.y, 0.0)).normalized() * 0.18
            else:
                # Forearm: mostly down, a little forward (relaxed elbow).
                target = Vector((0.0, 0.0, -1.0)) + Vector((d.x, d.y, 0.0)).normalized() * 0.08 + Vector((0.0, -0.18, 0.0))
            rot = d.rotation_difference(target.normalized()).to_matrix().to_4x4()
            pb.matrix = Matrix.Translation(head) @ rot @ Matrix.Translation(-head) @ pb.matrix
    bpy.context.view_layer.update()
    bpy.ops.object.mode_set(mode="OBJECT")


def bake_pose_and_strip(meshes, arm):
    """Apply the armature deformation to each mesh, then remove the skeleton."""
    for o in meshes:
        bpy.ops.object.select_all(action="DESELECT")
        o.select_set(True)
        bpy.context.view_layer.objects.active = o
        if o.data.users > 1:
            o.data = o.data.copy()
        for m in list(o.modifiers):
            if m.type == "ARMATURE":
                bpy.ops.object.modifier_apply(modifier=m.name)
        o.vertex_groups.clear()
    for o in meshes:
        mw = o.matrix_world.copy()
        o.parent = None
        o.matrix_world = mw
    if arm is not None:
        bpy.data.objects.remove(arm, do_unlink=True)


os.makedirs(PREV, exist_ok=True)
for name, file, target, pose in MODELS:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=os.path.join(SRC, file))
    for o in [o for o in bpy.data.objects if o.name.startswith("Icosphere")]:
        bpy.data.objects.remove(o, do_unlink=True)
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    arm = next((o for o in bpy.data.objects if o.type == "ARMATURE"), None)
    before = tri_count(meshes)

    for img_name, boxes in PATCHES.items():
        img = bpy.data.images.get(img_name)
        if img is not None and file == "soldier_full_gear.glb":
            paint_out(img, boxes)
    for m in bpy.data.materials:
        make_opaque(m)

    if arm is not None and pose == "tpose":
        lower_arms(arm)
    bake_pose_and_strip(meshes, arm)

    # Join, apply transforms, weld, decimate.
    bpy.ops.object.select_all(action="DESELECT")
    for o in meshes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    obj.name = obj.data.name = name
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.remove_doubles(threshold=0.0002)
    bpy.ops.object.mode_set(mode="OBJECT")
    t = tri_count([obj])
    if t > target:
        mod = obj.modifiers.new("Decimate", "DECIMATE")
        mod.decimate_type = "COLLAPSE"
        mod.ratio = target / t
        mod.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=mod.name)
    obj.data.validate(clean_customdata=False)

    # Stand on the ground at the origin, facing -Y (glTF +Z = Unity forward).
    ws = [obj.matrix_world @ v.co for v in obj.data.vertices]
    lo = Vector((min(w.x for w in ws), min(w.y for w in ws), min(w.z for w in ws)))
    hi = Vector((max(w.x for w in ws), max(w.y for w in ws), max(w.z for w in ws)))
    obj.location -= Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z))
    bpy.ops.object.transform_apply(location=True)
    for o in list(bpy.data.objects):
        if o is not obj:
            bpy.data.objects.remove(o, do_unlink=True)
    print(f"[chars] {name}: {before:,} -> {tri_count([obj]):,} tris, height {hi.z - lo.z:.2f} m, width {hi.x - lo.x:.2f} m, "
          f"mats {len(obj.data.materials)}")

    # Previews: front and back, to check the pose, the materials and that no patches remain.
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 600, 900
    world = bpy.data.worlds.new("W"); scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[1].default_value = 1.5
    sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN"))
    scene.collection.objects.link(sun); sun.data.energy = 3.5; sun.rotation_euler = (math.radians(55), 0, math.radians(25))
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); scene.collection.objects.link(cam); scene.camera = cam
    cam.data.lens = 60
    h = hi.z - lo.z
    tgt = Vector((0, 0, h * 0.55))
    for label, vec in (("front", Vector((0.3, -1, 0.1))), ("back", Vector((-0.3, 1, 0.1)))):
        cam.location = tgt + vec.normalized() * h * 2.0
        cam.rotation_euler = (tgt - cam.location).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = os.path.join(PREV, f"{name}_{label}.png")
        bpy.ops.render.render(write_still=True)
    for o in (sun, cam):
        bpy.data.objects.remove(o, do_unlink=True)

    out = os.path.join(DST, name)
    os.makedirs(out, exist_ok=True)
    bpy.ops.export_scene.gltf(filepath=os.path.join(out, f"{name}.gltf"), export_format="GLTF_SEPARATE",
                              export_yup=True, export_apply=True, export_skins=False, export_animations=False)
    print(f"[chars] exported {out}")
