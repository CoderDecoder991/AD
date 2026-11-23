"""Split a model into loose parts and print each part's bounds, to plan a rig.
Usage: blender -b --factory-startup --python probe_parts.py -- <model.gltf> [scale]
"""
import sys

import bpy
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:]
path = args[0]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=path)
meshes = [o for o in bpy.data.objects if o.type == "MESH"]
bpy.ops.object.select_all(action="DESELECT")
for o in meshes:
    o.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
if len(meshes) > 1:
    bpy.ops.object.join()
obj = bpy.context.view_layer.objects.active
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.mesh.select_all(action="SELECT")
bpy.ops.mesh.separate(type="LOOSE")
bpy.ops.object.mode_set(mode="OBJECT")
parts = [o for o in bpy.data.objects if o.type == "MESH"]
rows = []
for o in parts:
    ws = [o.matrix_world @ v.co for v in o.data.vertices]
    lo = Vector((min(w.x for w in ws), min(w.y for w in ws), min(w.z for w in ws)))
    hi = Vector((max(w.x for w in ws), max(w.y for w in ws), max(w.z for w in ws)))
    rows.append((len(o.data.polygons), lo, hi))
print(f"[parts] {len(rows)} loose parts")
rows.sort(key=lambda r: -r[0])
for n, lo, hi in rows[:60]:
    c = (lo + hi) / 2
    s = hi - lo
    print(f"[parts] faces={n:5d} centre=({c.x:6.2f},{c.y:6.2f},{c.z:6.2f}) size=({s.x:5.2f},{s.y:5.2f},{s.z:5.2f})")
