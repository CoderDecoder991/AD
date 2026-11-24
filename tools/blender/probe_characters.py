"""Inspect character .glb files: height, triangles, bones, and front/back/close-up renders
(to check proportions, materials and any insignia or flags, which the project does not use - R-14).
Usage: blender -b --factory-startup --python probe_characters.py -- <folder> <preview_dir>
"""
import glob
import math
import os
import sys

import bpy
from mathutils import Vector

folder, prev = sys.argv[sys.argv.index("--") + 1:][:2]
os.makedirs(prev, exist_ok=True)
for path in sorted(glob.glob(os.path.join(folder, "*.glb"))):
    name = os.path.splitext(os.path.basename(path))[0]
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=path)
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    arms = [o for o in bpy.data.objects if o.type == "ARMATURE"]
    bpy.context.view_layer.update()
    deps = bpy.context.evaluated_depsgraph_get()
    ws = []
    tris = 0
    for o in meshes:
        ev = o.evaluated_get(deps)
        m = ev.to_mesh()
        tris += sum(len(p.vertices) - 2 for p in m.polygons)
        ws += [o.matrix_world @ v.co for v in m.vertices]
        ev.to_mesh_clear()
    lo = Vector((min(w.x for w in ws), min(w.y for w in ws), min(w.z for w in ws)))
    hi = Vector((max(w.x for w in ws), max(w.y for w in ws), max(w.z for w in ws)))
    bones = sum(len(a.data.bones) for a in arms)
    print(f"[char] {name}: size=({hi.x-lo.x:.2f},{hi.y-lo.y:.2f},{hi.z-lo.z:.2f}) m tris={tris:,} meshes={len(meshes)} bones={bones} "
          f"actions={[a.name for a in bpy.data.actions]}")
    print(f"[char] {name} bones: {[b.name for b in arms[0].data.bones][:24] if arms else []}")

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 700, 1000
    world = bpy.data.worlds.new("W"); scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[1].default_value = 1.2
    sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN"))
    scene.collection.objects.link(sun); sun.data.energy = 3; sun.rotation_euler = (math.radians(50), 0, math.radians(30))
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); scene.collection.objects.link(cam); scene.camera = cam
    c = (lo + hi) / 2
    h = hi.z - lo.z
    for label, vec, target, lens in (("front", Vector((0, -1, 0)), c, 50), ("back", Vector((0, 1, 0)), c, 50),
                                     ("side", Vector((1, 0, 0)), c, 50),
                                     ("arm_front", Vector((0.6, -1, 0)), Vector((c.x, c.y, lo.z + h * 0.72)), 85),
                                     ("arm_back", Vector((-0.6, 1, 0)), Vector((c.x, c.y, lo.z + h * 0.72)), 85)):
        cam.data.lens = lens
        dist = h * (2.2 if lens == 50 else 0.9)
        cam.location = target + vec.normalized() * dist
        cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = os.path.join(prev, f"{name}_{label}.png")
        bpy.ops.render.render(write_still=True)
