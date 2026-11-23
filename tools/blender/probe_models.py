"""Print size, triangle count and material count for each .gltf/.glb under a folder.
Usage: blender -b --factory-startup --python probe_models.py -- <folder>
"""
import glob
import os
import sys

import bpy
from mathutils import Vector

folder = sys.argv[sys.argv.index("--") + 1]
for path in sorted(glob.glob(os.path.join(folder, "**", "*.gltf"), recursive=True)):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=path)
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    if not meshes:
        continue
    lo = Vector((1e9, 1e9, 1e9))
    hi = Vector((-1e9, -1e9, -1e9))
    tris = 0
    for o in meshes:
        tris += sum(len(p.vertices) - 2 for p in o.data.polygons)
        for c in o.bound_box:
            w = o.matrix_world @ Vector(c)
            lo = Vector(map(min, lo, w))
            hi = Vector(map(max, hi, w))
    size = hi - lo
    mats = {m.name for o in meshes for m in o.data.materials if m}
    print(f"[probe] {os.path.basename(path):40s} size(x,y,z)=({size.x:.2f},{size.y:.2f},{size.z:.2f}) m  "
          f"minZ={lo.z:.2f} tris={tris:,} meshes={len(meshes)} mats={len(mats)} names={[o.name for o in meshes][:6]}")
