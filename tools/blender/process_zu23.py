"""Prepare the ZU-23-2 Sketchfab model (CC-BY-4.0, LuckySkink9661) for mobile.

- Merges the 508 small meshes into three rig parts:
    ZU_Base  (chassis, wheels, chassis details)          - fixed
    ZU_Yaw   (turret, seats/rails, upper sight)           - traverses about vertical axis
    ZU_Pitch (twin barrels, side ammo boxes)              - elevates about the trunnion
- Decimates each group by a per-group ratio (wheels hardest; barrels lightest,
  because the operator camera sees them up close).
- Sets pivots, adds Muzzle_L / Muzzle_R empties, exports a .glb for Unity.
- Renders preview PNGs (rest pose + aimed pose) to check the rig.

Usage:
  blender -b --factory-startup --python process_zu23.py -- <in.glb> <out.glb> <preview_dir>
"""
import math
import os
import sys

import bpy
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
SRC, DST, PREVIEW_DIR = argv[0], argv[1], argv[2]

# Pivots in source coordinates (Blender Z-up, barrels point +X). Found by probe_glb.py.
YAW_PIVOT = Vector((0.0, 0.0, 0.40))
PITCH_PIVOT = Vector((0.05, 0.0, 1.08))

GROUPS = {
    # group name prefix         part        decimate ratio
    "Chassis_0":               ("ZU_Base",  0.45),
    "Wheels_51":               ("ZU_Base",  0.15),
    "Chassis_details_495":     ("ZU_Base",  0.40),
    "Turret_148":              ("ZU_Yaw",   0.50),
    "Seats_and_rails_387":     ("ZU_Yaw",   0.40),
    "Upper_sight_prop_435":    ("ZU_Yaw",   0.50),
    "Twin_barrel_visuals_244": ("ZU_Pitch", 0.70),
    "Side_boxes_328":          ("ZU_Pitch", 0.50),
}


def tri_count(obj):
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)


def select_only(objs, active=None):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = active or objs[0]


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=SRC)

# Parts removed on purpose. Real-world insignia are not used (DECISIONS R-14).
REMOVE = ("Ammo_star_",)


def removed(obj):
    o = obj
    while o is not None:
        if o.name.startswith(REMOVE):
            return True
        o = o.parent
    return False


root = next(o for o in bpy.data.objects if o.name.startswith("GLTF_SceneRootNode"))
group_objs = {name: [c for c in g.children_recursive if c.type == "MESH" and not removed(c)]
              for g in root.children for name in GROUPS if g.name.startswith(name)}
print("[process_zu23] removed parts:", sorted({o.name for o in bpy.data.objects if o.type == "MESH" and removed(o)}))

# Bake every mesh into world space and make data single-user (instanced bolts share meshes).
all_meshes = [o for objs in group_objs.values() for o in objs]
select_only(all_meshes)
bpy.ops.object.make_single_user(object=True, obdata=True)
bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

before = sum(tri_count(o) for o in all_meshes)
parts = {}
for name, objs in group_objs.items():
    part, ratio = GROUPS[name]
    select_only(objs)
    bpy.ops.object.join()
    joined = bpy.context.view_layer.objects.active
    joined.name = name
    # Weld coincident vertices first so decimation doesn't tear seams.
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.remove_doubles(threshold=0.0005)
    bpy.ops.object.mode_set(mode="OBJECT")
    mod = joined.modifiers.new("Decimate", "DECIMATE")
    mod.decimate_type = "COLLAPSE"
    mod.ratio = ratio
    mod.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.setdefault(part, []).append(joined)

rig = {}
for part, objs in parts.items():
    select_only(objs)
    bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    obj.name = part
    obj.data.name = part
    obj.rotation_mode = "XYZ"  # glTF import uses quaternions; previews set Euler angles
    rig[part] = obj

# Remove the now-empty import hierarchy.
for o in list(bpy.data.objects):
    if o.name not in rig:
        bpy.data.objects.remove(o, do_unlink=True)


def set_origin(obj, point):
    bpy.context.scene.cursor.location = point
    select_only([obj])
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")


set_origin(rig["ZU_Base"], Vector((0, 0, 0)))
set_origin(rig["ZU_Yaw"], YAW_PIVOT)
set_origin(rig["ZU_Pitch"], PITCH_PIVOT)

# Muzzle empties: average the barrel-tip vertices on each side.
pitch = rig["ZU_Pitch"]
xs = [(pitch.matrix_world @ v.co) for v in pitch.data.vertices]
max_x = max(p.x for p in xs)
tips = [p for p in xs if p.x > max_x - 0.03]
muzzles = {}
for label, sign in (("Muzzle_L", 1), ("Muzzle_R", -1)):
    side = [p for p in tips if p.y * sign > 0]
    c = sum(side, Vector()) / len(side)
    e = bpy.data.objects.new(label, None)
    bpy.context.collection.objects.link(e)
    e.location = c
    muzzles[label] = e


def parent_keep(child, parent):
    child.parent = parent
    child.matrix_parent_inverse = parent.matrix_world.inverted()


parent_keep(rig["ZU_Yaw"], rig["ZU_Base"])
parent_keep(rig["ZU_Pitch"], rig["ZU_Yaw"])
for e in muzzles.values():
    parent_keep(e, rig["ZU_Pitch"])

bpy.context.view_layer.update()
after = sum(tri_count(o) for o in rig.values())
print(f"[process_zu23] triangles {before:,} -> {after:,}")
for k, o in rig.items():
    print(f"[process_zu23]   {k}: {tri_count(o):,} tris, {len(o.data.materials)} materials, origin {tuple(round(v, 3) for v in o.matrix_world.translation)}")
for k, e in muzzles.items():
    print(f"[process_zu23]   {k}: {tuple(round(v, 3) for v in e.matrix_world.translation)}")

# ---- previews -------------------------------------------------------------
scene = bpy.context.scene
scene.render.engine = "BLENDER_WORKBENCH"
scene.display.shading.light = "STUDIO"
scene.display.shading.color_type = "TEXTURE"
scene.render.resolution_x, scene.render.resolution_y = 1200, 800
scene.render.film_transparent = False
world = bpy.data.worlds.new("W")
scene.world = world

cam_data = bpy.data.cameras.new("Cam")
cam = bpy.data.objects.new("Cam", cam_data)
scene.collection.objects.link(cam)
scene.camera = cam


def look_from(location, target=Vector((0.0, 0.0, 0.9))):
    cam.location = location
    cam.rotation_euler = (target - Vector(location)).to_track_quat("-Z", "Y").to_euler()


def render(name):
    os.makedirs(PREVIEW_DIR, exist_ok=True)
    scene.render.filepath = os.path.join(PREVIEW_DIR, name)
    bpy.ops.render.render(write_still=True)


look_from((1.5, -7.0, 2.5))
render("zu_rest_side.png")
# Aimed pose: traverse 40 deg, elevate 45 deg (rotate about -Y raises +X end).
rig["ZU_Yaw"].rotation_euler = (0, 0, math.radians(40))
rig["ZU_Pitch"].rotation_euler = (0, math.radians(-45), 0)
bpy.context.view_layer.update()
print("[process_zu23] aimed muzzle_L:", tuple(round(v, 3) for v in muzzles["Muzzle_L"].matrix_world.translation))
look_from((4.0, -6.0, 3.0))
render("zu_aimed.png")
look_from((-0.2, -2.6, 1.6), Vector((0.2, 0.0, 1.2)))
render("zu_aimed_trunnion_closeup.png")
rig["ZU_Yaw"].rotation_euler = (0, 0, 0)
rig["ZU_Pitch"].rotation_euler = (0, 0, 0)

# Remove preview-only objects before export.
bpy.data.objects.remove(cam, do_unlink=True)

os.makedirs(os.path.dirname(DST), exist_ok=True)
# .gltf + separate PNGs: Unity then imports the textures as normal assets and can
# compress them (ASTC on Android). A .glb would embed them uncompressed.
fmt = "GLTF_SEPARATE" if DST.lower().endswith(".gltf") else "GLB"
bpy.ops.export_scene.gltf(filepath=DST, export_format=fmt, export_yup=True, export_apply=True)
print(f"[process_zu23] exported {DST} ({fmt})")
