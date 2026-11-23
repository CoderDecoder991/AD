"""Rig the C-RAM display model (CC-BY 4.0, Muhamad Mirza Arrafi; see docs/ASSET_REGISTER.md)
into moving parts, like a Phalanx-type mount:
    CRAM_Base     pedestal, electronics cabinet                       - fixed
    CRAM_Yaw      turntable and the two yoke arms                     - traverses about vertical
    CRAM_Pitch    radome, sensor, gun cradle, drum, feed chute        - elevates about the yoke trunnions
    CRAM_Barrels  six-barrel cluster                                  - spins about the bore axis
plus a Muzzle empty at the barrel tips. Loose parts are classified by their centre position
(layout found with probe_parts.py / probe_views.py; the gun points -Y in Blender).

Usage: blender -b --factory-startup --python process_cram.py -- <in CRAM.gltf> <out .gltf> <preview_dir>
"""
import math
import os
import sys

import bpy
from mathutils import Vector

SRC, DST, PREV = sys.argv[sys.argv.index("--") + 1:][:3]

TURRET_XY = Vector((0.46, 0.46))   # vertical traverse axis
TURNTABLE_Z = 1.25                 # below this: fixed pedestal
TRUNNION_Z = 2.44                  # elevation axis height (yoke arm bosses, gun cradle centre)
ARM_GAP = (-0.05, 0.95)            # x range between the yoke arms' inner faces
CABINET_X = -0.9                   # separate electronics cabinet left of the pedestal
PITCH_MIN_Z = 1.45                 # turntable cover disc and ring stay on the yaw part


def select_only(objs, active=None):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = active or objs[0]


def bounds(o):
    ws = [o.matrix_world @ v.co for v in o.data.vertices]
    lo = Vector((min(w.x for w in ws), min(w.y for w in ws), min(w.z for w in ws)))
    hi = Vector((max(w.x for w in ws), max(w.y for w in ws), max(w.z for w in ws)))
    return lo, hi


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=SRC)
meshes = [o for o in bpy.data.objects if o.type == "MESH"]
select_only(meshes)
bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
if len(meshes) > 1:
    bpy.ops.object.join()
src = bpy.context.view_layer.objects.active
for o in list(bpy.data.objects):
    if o is not src:
        bpy.data.objects.remove(o, do_unlink=True)
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.mesh.select_all(action="SELECT")
bpy.ops.mesh.separate(type="LOOSE")
bpy.ops.object.mode_set(mode="OBJECT")
parts = [o for o in bpy.data.objects if o.type == "MESH"]

# Barrel cluster: long thin parts ahead of the cradle on the bore axis.
groups = {"CRAM_Base": [], "CRAM_Yaw": [], "CRAM_Pitch": [], "CRAM_Barrels": []}
for o in parts:
    lo, hi = bounds(o)
    c = (lo + hi) / 2
    if c.z < TURNTABLE_Z or c.x < CABINET_X:
        g = "CRAM_Base"
    elif c.z > 2.85 or (c.z > PITCH_MIN_Z and ARM_GAP[0] < c.x < ARM_GAP[1]):
        g = "CRAM_Pitch"
        if c.y < -0.55 and abs(c.x - TURRET_XY.x) < 0.2 and abs(c.z - TRUNNION_Z) < 0.2:
            g = "CRAM_Barrels"
    else:
        g = "CRAM_Yaw"
    groups[g].append(o)
for g, objs in groups.items():
    print(f"[cram] {g}: {len(objs)} loose parts")

rig = {}
for g, objs in groups.items():
    select_only(objs)
    bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active
    o.name = o.data.name = g
    o.rotation_mode = "XYZ"
    rig[g] = o

# Bore axis from the barrel cluster bounds (x/z centre); muzzle at its forward (-Y) tip.
blo, bhi = bounds(rig["CRAM_Barrels"])
bore_x, bore_z = (blo.x + bhi.x) / 2, (blo.z + bhi.z) / 2
print(f"[cram] barrels x {blo.x:.2f}..{bhi.x:.2f} y {blo.y:.2f}..{bhi.y:.2f} z {blo.z:.2f}..{bhi.z:.2f}")
yaw_pivot = Vector((TURRET_XY.x, TURRET_XY.y, TURNTABLE_Z))
pitch_pivot = Vector((TURRET_XY.x, TURRET_XY.y, TRUNNION_Z))
bore_pivot = Vector((bore_x, blo.y, bore_z))


def set_origin(obj, point):
    bpy.context.scene.cursor.location = point
    select_only([obj])
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")


set_origin(rig["CRAM_Base"], Vector((TURRET_XY.x, TURRET_XY.y, 0)))
set_origin(rig["CRAM_Yaw"], yaw_pivot)
set_origin(rig["CRAM_Pitch"], pitch_pivot)
set_origin(rig["CRAM_Barrels"], bore_pivot)
muzzle = bpy.data.objects.new("Muzzle", None)
bpy.context.collection.objects.link(muzzle)
muzzle.location = Vector((bore_x, blo.y - 0.02, bore_z))


def parent_keep(child, parent):
    child.parent = parent
    child.matrix_parent_inverse = parent.matrix_world.inverted()


parent_keep(rig["CRAM_Yaw"], rig["CRAM_Base"])
parent_keep(rig["CRAM_Pitch"], rig["CRAM_Yaw"])
parent_keep(rig["CRAM_Barrels"], rig["CRAM_Pitch"])
parent_keep(muzzle, rig["CRAM_Pitch"])
bpy.context.view_layer.update()
for k, o in rig.items():
    print(f"[cram] {k}: {sum(len(p.vertices) - 2 for p in o.data.polygons):,} tris, origin "
          f"{tuple(round(v, 3) for v in o.matrix_world.translation)}")
print(f"[cram] Muzzle {tuple(round(v, 3) for v in muzzle.matrix_world.translation)}")

# ---- previews: rest, aimed, and classification colours ----------------------
scene = bpy.context.scene
scene.render.engine = "BLENDER_WORKBENCH"
scene.display.shading.light = "STUDIO"
scene.render.resolution_x, scene.render.resolution_y = 1000, 800
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
scene.collection.objects.link(cam)
scene.camera = cam
os.makedirs(PREV, exist_ok=True)


def shot(name, loc, target=Vector((0.46, 0.46, 2.2))):
    cam.location = loc
    cam.rotation_euler = (target - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = os.path.join(PREV, name)
    bpy.ops.render.render(write_still=True)


scene.display.shading.color_type = "TEXTURE"
shot("cram_rest.png", (5.5, -7.5, 4.0))
# Aimed: traverse 35 deg, elevate 50 deg. Gun points -Y, so raising the muzzle is -X rotation.
rig["CRAM_Yaw"].rotation_euler = (0, 0, math.radians(35))
rig["CRAM_Pitch"].rotation_euler = (math.radians(-50), 0, 0)
bpy.context.view_layer.update()
print(f"[cram] aimed Muzzle {tuple(round(v, 3) for v in muzzle.matrix_world.translation)}")
shot("cram_aimed.png", (5.5, -7.5, 4.0))
shot("cram_aimed_back.png", (-5.5, 7.5, 4.0))
scene.display.shading.color_type = "OBJECT"
colours = {"CRAM_Base": (0.4, 0.4, 0.4, 1), "CRAM_Yaw": (0.2, 0.5, 1, 1),
           "CRAM_Pitch": (1, 0.4, 0.2, 1), "CRAM_Barrels": (0.2, 1, 0.3, 1)}
for k, o in rig.items():
    o.color = colours[k]
shot("cram_parts.png", (5.5, -7.5, 4.0))
rig["CRAM_Pitch"].rotation_euler = (math.radians(-85), 0, 0)
shot("cram_parts_max_elev.png", (7.5, 3.0, 3.0))
rig["CRAM_Yaw"].rotation_euler = (0, 0, 0)
rig["CRAM_Pitch"].rotation_euler = (0, 0, 0)
bpy.data.objects.remove(cam, do_unlink=True)

bpy.ops.export_scene.gltf(filepath=DST, export_format="GLTF_SEPARATE", export_yup=True, export_apply=True)
print(f"[cram] exported {DST}")
