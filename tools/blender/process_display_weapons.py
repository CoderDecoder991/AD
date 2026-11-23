"""Prepare the C-RAM and 2K22 Tunguska models (CC-BY 4.0, Sketchfab; see docs/ASSET_REGISTER.md)
for display at the base: real-world scale, centred, on the ground, exported as .gltf with
separate textures, plus preview renders for checking orientation and markings.

Usage: blender -b --factory-startup --python process_display_weapons.py -- <SourceArt/Weapons> <Assets/.../Models> <preview_dir>
"""
import math
import os
import sys

import bpy
from mathutils import Vector

SRC, DST, PREV = sys.argv[sys.argv.index("--") + 1:][:3]

MODELS = [
    # name        source file      real size target (axis, metres)  why
    ("CRAM",      "c-ram.glb",     ("z", 4.7),   # Phalanx-type mount height
     ),
    ("Tunguska",  "tunguska.glb",  ("long", 7.93),  # 2K22 hull length
     ),
]


def bounds(objs):
    ws = [o.matrix_world @ Vector(c) for o in objs for c in o.bound_box]
    lo = Vector((min(w.x for w in ws), min(w.y for w in ws), min(w.z for w in ws)))
    hi = Vector((max(w.x for w in ws), max(w.y for w in ws), max(w.z for w in ws)))
    return lo, hi


for name, file, (axis, metres) in MODELS:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=os.path.join(SRC, file))
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    bpy.ops.object.select_all(action="DESELECT")
    for o in meshes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.make_single_user(object=True, obdata=True)
    bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    obj.name = obj.data.name = name
    for o in list(bpy.data.objects):
        if o is not obj:
            bpy.data.objects.remove(o, do_unlink=True)

    lo, hi = bounds([obj])
    size = hi - lo
    current = size.z if axis == "z" else max(size.x, size.y)
    s = metres / current
    obj.scale = (s, s, s)
    bpy.ops.object.transform_apply(scale=True)
    lo, hi = bounds([obj])
    obj.location -= Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z))
    bpy.ops.object.transform_apply(location=True)
    lo, hi = bounds([obj])
    print(f"[display] {name}: scale x{s:.4f} -> size ({hi.x - lo.x:.2f}, {hi.y - lo.y:.2f}, {hi.z - lo.z:.2f}) m, "
          f"tris {sum(len(p.vertices) - 2 for p in obj.data.polygons):,}")

    # Previews from four sides to check orientation and any markings.
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "TEXTURE"
    scene.render.resolution_x, scene.render.resolution_y = 800, 600
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    scene.collection.objects.link(cam)
    scene.camera = cam
    r = max(hi.x - lo.x, hi.y - lo.y, hi.z) * 1.6
    target = Vector((0, 0, hi.z * 0.45))
    os.makedirs(PREV, exist_ok=True)
    for label, ang in (("posX", 0), ("posY", 90), ("negX", 180), ("negY", 270)):
        a = math.radians(ang + 20)
        cam.location = target + Vector((math.cos(a) * r, math.sin(a) * r, r * 0.35))
        cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = os.path.join(PREV, f"{name}_{label}.png")
        bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam, do_unlink=True)

    out = os.path.join(DST, name)
    os.makedirs(out, exist_ok=True)
    bpy.ops.export_scene.gltf(filepath=os.path.join(out, f"{name}.gltf"), export_format="GLTF_SEPARATE",
                              export_yup=True, export_apply=True)
