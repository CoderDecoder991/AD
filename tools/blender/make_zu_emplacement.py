"""Generate the ZU-23-2 firing position (VS-3, docs/ENVIRONMENT_ART_BRIEF.md §4 "hero"):

  zu_berm.gltf       round earth mound with sloped sides (top radius 10.5 m, base 20 m, 14 m high), top at z = 0,
                     dried-mud texture mapped at real scale; compacted gravel pit floor (radius 4.4 m)
  zu_revetment.gltf  sandbag ring around the gun: individual, slightly irregular bags in a staggered double ring,
                     lower on the threat side (north) so the base stays visible over it, higher on the flanks,
                     an access gap at the rear (south, under the gameplay camera)

Origin = gun position on the berm top. Blender -Y = north (Unity +Z after glTF export).
Textures: CC0 Poly Haven hessian_230 (bags), dry_mud_field_001 (berm), gravel_concrete (pit floor).

Usage: blender -b --factory-startup --python make_zu_emplacement.py -- <SourceArt/PolyHaven> <out_dir>
"""
import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

SRC, OUT = [os.path.abspath(p) for p in sys.argv[sys.argv.index("--") + 1:][:2]]
os.makedirs(OUT, exist_ok=True)
rng = random.Random(23)

TOP_R, BASE_R, HEIGHT = 10.5, 20.0, 14.0
PIT_R = 4.4
BAG = (0.58, 0.32, 0.15)          # length, width, height (m) - a filled sandbag
RING_R = (4.55, 4.89)             # inner / outer bag line radius


def material(name, tex_set, res, rough=0.85):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = rough
    def img(kind, colour):
        p = os.path.join(SRC, tex_set, f"{tex_set}_{kind}_{res}.jpg")
        if not os.path.exists(p):
            return None
        n = nt.nodes.new("ShaderNodeTexImage")
        n.image = bpy.data.images.load(p)
        if not colour:
            n.image.colorspace_settings.name = "Non-Color"
        return n
    d = img("diff", True)
    if d:
        nt.links.new(d.outputs["Color"], bsdf.inputs["Base Color"])
    nrm = img("nor_gl", False)
    if nrm:
        nm = nt.nodes.new("ShaderNodeNormalMap")
        nt.links.new(nrm.outputs["Color"], nm.inputs["Color"])
        nt.links.new(nm.outputs["Normal"], bsdf.inputs["Normal"])
    return m


def export(objs, name):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    path = os.path.join(OUT, f"{name}.gltf")
    bpy.ops.export_scene.gltf(filepath=path, export_format="GLTF_SEPARATE", use_selection=True,
                              export_yup=True, export_apply=True)
    tris = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in objs)
    print(f"[emplacement] {name}: {tris:,} tris -> {path}")


bpy.ops.wm.read_factory_settings(use_empty=True)

# ------------------------------------------------------------------ berm
mud = material("BermMud", "dry_mud_field_001", "2k", 0.9)
gravel = material("PitGravel", "gravel_concrete", "2k", 0.8)
SEG = 48
bm = bmesh.new()
uv = bm.loops.layers.uv.new("UVMap")
TILE = 6.0
top_v = [bm.verts.new((math.cos(2 * math.pi * i / SEG) * TOP_R, math.sin(2 * math.pi * i / SEG) * TOP_R, 0.0)) for i in range(SEG)]
bot_v = []
for i in range(SEG):
    a = 2 * math.pi * i / SEG
    r = BASE_R * (1.0 + rng.uniform(-0.04, 0.04))     # slightly irregular foot
    bot_v.append(bm.verts.new((math.cos(a) * r, math.sin(a) * r, -HEIGHT)))
slant = math.hypot(BASE_R - TOP_R, HEIGHT)
for i in range(SEG):
    j = (i + 1) % SEG
    f = bm.faces.new((top_v[i], bot_v[i], bot_v[j], top_v[j]))
    for loop in f.loops:
        k = i if loop.vert in (top_v[i], bot_v[i]) else i + 1
        down = 0.0 if loop.vert in (top_v[i], top_v[j]) else slant
        rad = TOP_R if down == 0.0 else BASE_R
        loop[uv].uv = (k / SEG * 2 * math.pi * (TOP_R + BASE_R) / 2 / TILE, down / TILE)
# Top: ring between pit and edge (mud), pit floor (gravel) as a separate disc.
pit_v = [bm.verts.new((math.cos(2 * math.pi * i / SEG) * PIT_R, math.sin(2 * math.pi * i / SEG) * PIT_R, 0.0)) for i in range(SEG)]
for i in range(SEG):
    j = (i + 1) % SEG
    f = bm.faces.new((pit_v[i], pit_v[j], top_v[j], top_v[i]))
    for loop in f.loops:
        loop[uv].uv = (loop.vert.co.x / TILE, loop.vert.co.y / TILE)
bm.normal_update()
me = bpy.data.meshes.new("zu_berm")
bm.to_mesh(me)
bm.free()
berm = bpy.data.objects.new("zu_berm", me)
bpy.context.collection.objects.link(berm)
me.materials.append(mud)
for p in me.polygons:
    p.use_smooth = True

# Pit floor disc (slightly raised to avoid z-fighting), gravel texture at 4 m.
bm = bmesh.new()
uv = bm.loops.layers.uv.new("UVMap")
centre = bm.verts.new((0, 0, 0.015))
ring = [bm.verts.new((math.cos(2 * math.pi * i / SEG) * (PIT_R + 0.3), math.sin(2 * math.pi * i / SEG) * (PIT_R + 0.3), 0.015)) for i in range(SEG)]
for i in range(SEG):
    f = bm.faces.new((centre, ring[i], ring[(i + 1) % SEG]))
    for loop in f.loops:
        loop[uv].uv = (loop.vert.co.x / 4.0, loop.vert.co.y / 4.0)
bm.normal_update()
me2 = bpy.data.meshes.new("zu_pit_floor")
bm.to_mesh(me2)
bm.free()
floor = bpy.data.objects.new("zu_pit_floor", me2)
bpy.context.collection.objects.link(floor)
me2.materials.append(gravel)
export([berm, floor], "zu_berm")

# -------------------------------------------------------------- sandbags
hessian = material("Sandbag", "hessian_230", "1k", 0.95)


def make_bag_mesh():
    """One filled sandbag: a bevelled box, pinched at the ends and slightly sagging. UVs by cube projection."""
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=Vector(BAG), verts=bm.verts)
    bmesh.ops.bevel(bm, geom=list(bm.edges), offset=0.055, segments=2, affect="EDGES", profile=0.6)
    for v in bm.verts:
        x, y, z = v.co
        end = abs(x) / (BAG[0] / 2)                       # 0 centre .. 1 ends
        v.co.y *= 1.0 - 0.18 * end ** 3                    # pinched seams at the ends
        v.co.z *= 1.0 - 0.25 * end ** 3
        if z > 0:
            v.co.z -= 0.012 * (1 - end)                    # filled bag sags on top
    uvl = bm.loops.layers.uv.new("UVMap")
    for f in bm.faces:
        n = f.normal
        for loop in f.loops:
            co = loop.vert.co
            if abs(n.z) > 0.6:
                loop[uvl].uv = (co.x * 1.6, co.y * 1.6)
            elif abs(n.y) > abs(n.x):
                loop[uvl].uv = (co.x * 1.6, co.z * 1.6)
            else:
                loop[uvl].uv = (co.y * 1.6, co.z * 1.6)
    me = bpy.data.meshes.new("bag")
    bm.to_mesh(me)
    bm.free()
    return me


base = make_bag_mesh()
GAP = 26.0                       # rear access gap half-angle (degrees, centred on south)


def rows_at(theta_deg):
    """Wall height (bag rows) by bearing: low on the threat side, higher on the flanks."""
    t = abs((theta_deg + 180) % 360 - 180)            # 0 = north .. 180 = south
    if t > 180 - GAP:
        return 0
    if t < 50:
        return 4                                      # ~0.55 m: the base stays visible over the front
    if t < 70:
        return 5
    return 7                                          # ~0.95 m flanks and rear


bags = []
for ring_i, R in enumerate(RING_R):
    n = int(2 * math.pi * R / (BAG[0] + 0.02))
    step = 360.0 / n
    for row in range(7):
        offset = step / 2 if (row + ring_i) % 2 else 0.0   # staggered courses
        for k in range(n):
            theta = k * step + offset
            if row >= rows_at(theta):
                continue
            a = math.radians(theta)
            r = R - row * 0.012 * (1 if ring_i else -1) * 0.5 - (row * 0.018 if ring_i else 0.0)  # outer face battered
            pos = Vector((math.sin(a) * r, -math.cos(a) * r, BAG[2] / 2 + row * (BAG[2] - 0.012)))
            me = base.copy()
            du, dv = rng.random(), rng.random()
            for l in me.uv_layers[0].data:
                l.uv = (l.uv[0] + du, l.uv[1] + dv)
            o = bpy.data.objects.new("bag", me)
            bpy.context.collection.objects.link(o)
            o.location = pos
            # Lie along the ring tangent, with small irregularities.
            o.rotation_euler = (math.radians(rng.uniform(-3, 3)), math.radians(rng.uniform(-4, 4)),
                                -a + math.radians(rng.uniform(-5, 5)))
            s = rng.uniform(0.94, 1.06)
            o.scale = (s, rng.uniform(0.95, 1.05), rng.uniform(0.9, 1.05))
            bags.append(o)

bpy.ops.object.select_all(action="DESELECT")
for o in bags:
    o.select_set(True)
bpy.context.view_layer.objects.active = bags[0]
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
bpy.ops.object.join()
rev = bpy.context.view_layer.objects.active
rev.name = rev.data.name = "zu_revetment"
rev.data.materials.clear()
rev.data.materials.append(hessian)
for p in rev.data.polygons:
    p.use_smooth = True
print(f"[emplacement] bags: {len(bags)}")
export([rev], "zu_revetment")

# Preview render (top-down-ish) to check the layout.
scene = bpy.context.scene
scene.render.engine = "BLENDER_WORKBENCH"
scene.display.shading.color_type = "TEXTURE"
scene.render.resolution_x, scene.render.resolution_y = 1000, 700
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
scene.collection.objects.link(cam)
scene.camera = cam
target = Vector((0, 0, 0.4))
cam.location = Vector((0, 9.5, 3.4))      # behind the gun (south), like the gameplay camera
cam.rotation_euler = (target - cam.location + Vector((0, -30, 2))).to_track_quat("-Z", "Y").to_euler()
scene.render.filepath = os.path.join(OUT, "_preview_gameplay.png")
bpy.ops.render.render(write_still=True)
cam.location = Vector((14, 14, 12))
cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()
scene.render.filepath = os.path.join(OUT, "_preview_overview.png")
bpy.ops.render.render(write_still=True)
