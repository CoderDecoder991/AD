"""Render orthographic front/side/top views of a model with a metre grid, to plan a rig.
Usage: blender -b --factory-startup --python probe_views.py -- <model.gltf> <out_prefix>
"""
import math
import sys

import bpy
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:]
path, out = args[0], args[1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=path)
meshes = [o for o in bpy.data.objects if o.type == "MESH"]
lo = Vector((1e9, 1e9, 1e9)); hi = Vector((-1e9, -1e9, -1e9))
for o in meshes:
    for c in o.bound_box:
        w = o.matrix_world @ Vector(c)
        lo = Vector(map(min, lo, w)); hi = Vector(map(max, hi, w))
print(f"[views] bounds lo={tuple(round(v,2) for v in lo)} hi={tuple(round(v,2) for v in hi)}")
centre = (lo + hi) / 2
size = max(hi - lo) * 1.15

# Metre grid lines (thin boxes) on the three planes behind the model.
mat = bpy.data.materials.new("grid"); mat.diffuse_color = (1, 0, 0, 1)
def bar(p, s):
    bpy.ops.mesh.primitive_cube_add(size=1, location=p)
    b = bpy.context.active_object; b.scale = s; b.data.materials.append(mat)
for i in range(math.floor(lo.z), math.ceil(hi.z) + 1):
    bar((centre.x, hi.y + 0.5, i), (size * 2, 0.01, 0.01))   # for front view (looking +Y)
    bar((hi.x + 0.5, centre.y, i), (0.01, size * 2, 0.01))   # for side view (looking -X)
for i in range(math.floor(lo.x), math.ceil(hi.x) + 1):
    bar((i, hi.y + 0.5, centre.z), (0.01, 0.01, size * 2))
    bar((i, centre.y, lo.z - 0.5), (0.01, size * 2, 0.01))
for i in range(math.floor(lo.y), math.ceil(hi.y) + 1):
    bar((hi.x + 0.5, i, centre.z), (0.01, 0.01, size * 2))
    bar((centre.x, i, lo.z - 0.5), (size * 2, 0.01, 0.01))

scene = bpy.context.scene
scene.render.engine = "BLENDER_WORKBENCH"
scene.display.shading.light = "STUDIO"
scene.display.shading.color_type = "TEXTURE"
scene.render.resolution_x = scene.render.resolution_y = 900
cam_data = bpy.data.cameras.new("cam"); cam_data.type = "ORTHO"; cam_data.ortho_scale = size
cam = bpy.data.objects.new("cam", cam_data); scene.collection.objects.link(cam); scene.camera = cam
views = {
    "front": (centre + Vector((0, -20, 0)), (math.radians(90), 0, 0)),          # looking +Y
    "side": (centre + Vector((-20, 0, 0)), (math.radians(90), 0, math.radians(-90))),  # looking +X
    "top": (centre + Vector((0, 0, 20)), (0, 0, 0)),
}
for name, (loc, rot) in views.items():
    cam.location = loc; cam.rotation_euler = rot
    scene.render.filepath = f"{out}_{name}.png"
    bpy.ops.render.render(write_still=True)
print("[views] done")
