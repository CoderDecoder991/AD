"""Print world-space bounds of each top-level group in a .glb (Blender Z-up).
Usage: blender -b --factory-startup --python probe_glb.py -- <file.glb>
"""
import sys
import bpy
from mathutils import Vector

path = sys.argv[sys.argv.index("--") + 1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=path)


def mesh_descendants(obj):
    out = []
    for c in obj.children_recursive:
        if c.type == "MESH":
            out.append(c)
    if obj.type == "MESH":
        out.append(obj)
    return out


def bounds(objs):
    lo = Vector((1e9, 1e9, 1e9))
    hi = Vector((-1e9, -1e9, -1e9))
    for o in objs:
        for corner in o.bound_box:
            w = o.matrix_world @ Vector(corner)
            lo = Vector(map(min, lo, w))
            hi = Vector(map(max, hi, w))
    return lo, hi


root = next(o for o in bpy.data.objects if o.name.startswith("GLTF_SceneRootNode"))
total = bounds(mesh_descendants(root))
print(f"ALL   min={tuple(round(v, 3) for v in total[0])} max={tuple(round(v, 3) for v in total[1])}")
for g in root.children:
    objs = mesh_descendants(g)
    lo, hi = bounds(objs)
    tris = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in objs)
    print(f"{g.name:28s} tris={tris:7d} min={tuple(round(v, 3) for v in lo)} max={tuple(round(v, 3) for v in hi)}")
