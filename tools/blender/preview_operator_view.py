"""Render candidate operator-camera views from a processed ZU-23-2 .glb.
Camera offsets are relative to the pitch pivot, in the gun's frame (x forward, z up).
Usage: blender -b --factory-startup --python preview_operator_view.py -- <zu23_game.glb> <out_dir>
"""
import math
import os
import sys

import bpy
from mathutils import Vector

src, out_dir = sys.argv[sys.argv.index("--") + 1:][:2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
pitch = next(o for o in bpy.data.objects if o.name.startswith("ZU_Pitch"))
bpy.context.view_layer.update()
pivot = pitch.matrix_world.translation.copy()

scene = bpy.context.scene
scene.render.engine = "BLENDER_WORKBENCH"
scene.display.shading.light = "STUDIO"
scene.display.shading.color_type = "TEXTURE"
scene.render.resolution_x, scene.render.resolution_y = 1500, 1000  # Pad 8 is 3:2
cam_data = bpy.data.cameras.new("Cam")
cam_data.sensor_fit = "VERTICAL"
cam_data.angle = math.radians(60)  # matches ManualWeaponController.normalFov
cam = bpy.data.objects.new("Cam", cam_data)
scene.collection.objects.link(cam)
scene.camera = cam

candidates = {
    "A_behind_low":   (-1.30, 0.0, 0.45),
    "B_over_sight":   (-1.10, 0.0, 0.95),
    "C_high_back":    (-1.60, 0.0, 1.10),
    "D_left_gunner":  (-1.00, 0.45, 0.70),
}
os.makedirs(out_dir, exist_ok=True)
for name, off in candidates.items():
    loc = pivot + Vector(off)
    cam.location = loc
    target = Vector((loc.x + 30.0, 0.0, loc.z + 1.5))  # looking forward, very slightly up (gun at rest ~10 deg)
    cam.rotation_euler = (target - loc).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = os.path.join(out_dir, f"view_{name}.png")
    bpy.ops.render.render(write_still=True)
    print("[preview]", name, tuple(round(v, 2) for v in off))
