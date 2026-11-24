"""Render flat white side-view silhouettes of the weapon models for the HUD tiles (VS-2,
docs/HUD_UI_SPECIFICATION.md §5). Transparent PNG, 512x192, weapon facing right.

Usage: blender -b --factory-startup --python render_weapon_icons.py -- <out_dir> id=model.gltf[:flip][:sidex] ...
  flip  = view from the other side (weapon facing right); sidex = force a side view along X
"""
import math
import os
import sys

import bpy
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:]
out = args[0]
os.makedirs(out, exist_ok=True)
for spec in args[1:]:
    wid, rest = spec.split("=", 1)
    parts = rest.split(":")
    # Windows paths contain a drive colon: re-join it.
    if len(parts) > 1 and len(parts[0]) == 1:
        parts = [parts[0] + ":" + parts[1]] + parts[2:]
    path, opts = parts[0], set(parts[1:])
    flip = "flip" in opts
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=path)
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    bpy.context.view_layer.update()
    ws = [o.matrix_world @ Vector(c) for o in meshes for c in o.bound_box]
    lo = Vector((min(w.x for w in ws), min(w.y for w in ws), min(w.z for w in ws)))
    hi = Vector((max(w.x for w in ws), max(w.y for w in ws), max(w.z for w in ws)))
    c = (lo + hi) / 2
    size = hi - lo
    alongX = size.x >= size.y
    if "sidex" in opts:      # gun along Y: look along X to see it from the side
        alongX = False
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "FLAT"
    scene.display.shading.color_type = "SINGLE"
    scene.display.shading.single_color = (1, 1, 1)
    scene.render.film_transparent = True
    scene.render.resolution_x, scene.render.resolution_y = 512, 192
    scene.view_settings.view_transform = "Standard"
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    scene.collection.objects.link(cam)
    scene.camera = cam
    cam.data.type = "ORTHO"
    length = size.x if alongX else size.y
    cam.data.ortho_scale = max(length, size.z * 512 / 192) * 1.08
    side = -1 if not flip else 1
    if alongX:
        cam.location = c + Vector((0, side * 50, 0))
    else:
        cam.location = c + Vector((side * -50, 0, 0))
    cam.rotation_euler = (c - cam.location).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = os.path.join(out, f"icon_{wid}.png")
    bpy.ops.render.render(write_still=True)
    print(f"[icon] {wid}: {'X' if alongX else 'Y'}-long, size {size.x:.1f}x{size.y:.1f}x{size.z:.1f}")
