"""Prepare CC0 Poly Haven props for mobile: pick parts, merge, decimate, ground, export.

Each prop: source .gltf, optional list of part-name prefixes to keep, a triangle
target, and a uniform scale. Output: Assets/_Project/Art/Models/Props/<name>/<name>.gltf
(.gltf + separate textures so Unity compresses them).

Usage: blender -b --factory-startup --python process_props.py -- <SourceArt/PolyHaven> <Assets/.../Props>
"""
import os
import sys

import bpy
from mathutils import Vector

SRC, DST = sys.argv[sys.argv.index("--") + 1:][:2]

PROPS = [
    # name               source folder              keep parts (prefixes) or None        tris   scale
    ("fence_segment",    "modular_chainlink_fence", ["modular_chainlink_fence_double", "modular_chainlink_fence_post"], 450, 1.0),
    ("concrete_barrier", "concrete_road_barrier",   None,                                 1500,  1.0),
    ("floodlight_pair",  "flood_lights",            ["flood_lights_floor_double_a_"],      2000,  1.6),
    ("searchlight_head", "portable_searchlight",    None,                                 3000,  6.0),
    ("security_light",   "security_light",          None,                                 1500,  1.5),
    ("military_crate",   "old_military_crate",      None,                                 1500,  1.0),
    ("wooden_crate",     "wooden_military_crate",   None,                                 1500,  1.0),
    ("ammo_box",         "ammo_box",                None,                                  600,  1.0),
    ("jerrycan",         "metal_jerrycan_green",    None,                                  800,  1.0),
    ("barrel",           "Barrel_01",               None,                                 1200,  1.0),
    ("generator",        "portable_generator",      None,                                 2000,  1.0),
    # Visual-clarity pass (2026-11-24): building detail, roads, clutter, terrain scatter.
    ("aircon",           "exterior_aircon_unit",    ["exterior_aircon_unit"],             2500,  1.0),
    ("roller_door",      "rollershutter_door",      ["rollershutter_door"],               1200,  1.0),
    ("roller_window",    "rollershutter_window_01", ["rollershutter_window_01"],          1200,  1.0),
    ("roof_duct",        "modular_airduct_rectangular_01", None,                          5000,  1.0),
    ("security_camera",  "security_camera_01",      None,                                 1200,  1.0),
    ("utility_box",      "utility_box_01",          None,                                 1200,  1.0),
    ("propane_tank",     "propane_tank",            None,                                 1000,  1.0),
    ("tyre",             "old_tyre",                None,                                  700,  1.0),
    ("plastic_crate",    "plastic_crate_01",        None,                                  800,  1.0),
    ("cardboard_box",    "cardboard_box_01",        None,                                  500,  1.0),
    ("covered_car",      "covered_car",             None,                                 4000,  1.0),
    ("boulder_a",        "namaqualand_boulder_02",  None,                                 1500,  1.0),
    ("boulder_b",        "namaqualand_boulder_04",  None,                                 1500,  1.0),
    ("rock_small",       "rock_07",                 None,                                  400,  6.0),
    ("bush_a",           "wild_rooibos_bush",       ["wild_rooibos_bush_a"],              2000,  1.0),
    ("bush_b",           "wild_rooibos_bush",       ["wild_rooibos_bush_c"],              2000,  1.0),
    ("dead_log",         "dead_tree_trunk",         None,                                 1500,  1.0),
    ("wall_lamp",        "industrial_wall_lamp",    None,                                 1200,  1.5),
    ("trash_can",        "metal_trash_can",         ["metal_trash_can", "metal_trash_can_handle_left", "metal_trash_can_handle_right", "metal_trash_can_lid"], 2500, 1.0),
    ("cement_bag",       "cement_bag",              None,                                  844,  1.0),
    ("barrier_b",        "concrete_road_barrier_02", None,                                1500,  1.0),
    ("street_lamp",      "street_lamp_01",          None,                                 3000,  2.0),
]
# Optional: only these prop names (extra arguments after the two folders).
ONLY = set(sys.argv[sys.argv.index("--") + 1:][2:])


def tri_count(o):
    return sum(len(p.vertices) - 2 for p in o.data.polygons)


def select_only(objs):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]


for name, folder, keep, target, scale in PROPS:
    if ONLY and name not in ONLY:
        continue
    src = os.path.join(SRC, folder, f"{folder}_1k.gltf")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=src)

    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    if keep:
        # A keep entry matches the exact part name (Blender may add ".001"), or a
        # prefix ending in "_" for whole families, e.g. "flood_lights_floor_double_a_".
        def kept(n):
            return any(n == k or n.startswith(k + ".") or (k.endswith("_") and n.startswith(k)) for k in keep)
        drop = [o for o in meshes if not kept(o.name)]
        meshes = [o for o in meshes if o not in drop]
        for o in drop:
            bpy.data.objects.remove(o, do_unlink=True)

    select_only(meshes)
    bpy.ops.object.make_single_user(object=True, obdata=True)
    bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    before = sum(tri_count(o) for o in meshes)

    if name == "fence_segment":
        # The kit lays pieces out apart; put the post at the panel's +X end so
        # segments tile edge to edge (segment length = panel width).
        panel = next(o for o in meshes if o.name.startswith("modular_chainlink_fence_double"))
        post = next(o for o in meshes if o.name.startswith("modular_chainlink_fence_post"))
        def bounds(o):
            ws = [o.matrix_world @ Vector(c) for c in o.bound_box]
            return Vector((min(w.x for w in ws), min(w.y for w in ws), min(w.z for w in ws))), \
                   Vector((max(w.x for w in ws), max(w.y for w in ws), max(w.z for w in ws)))
        plo, phi = bounds(panel)
        qlo, qhi = bounds(post)
        post.location += Vector((phi.x - (qlo.x + qhi.x) / 2, (plo.y + phi.y) / 2 - (qlo.y + qhi.y) / 2, plo.z - qlo.z))
        bpy.context.view_layer.update()
        bpy.ops.object.transform_apply(location=True)
    if len(meshes) > 1:
        bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    obj.name = obj.data.name = name

    # Weld, then decimate to the target.
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.remove_doubles(threshold=0.0005)
    bpy.ops.object.mode_set(mode="OBJECT")
    t = tri_count(obj)
    if t > target:
        mod = obj.modifiers.new("Decimate", "DECIMATE")
        mod.decimate_type = "COLLAPSE"
        mod.ratio = target / t
        mod.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=mod.name)

    # Scale, centre on X/Y, sit on the ground (z = 0), origin at the base centre.
    obj.scale = (scale, scale, scale)
    bpy.ops.object.transform_apply(scale=True)
    ws = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    cx = (min(w.x for w in ws) + max(w.x for w in ws)) / 2
    cy = (min(w.y for w in ws) + max(w.y for w in ws)) / 2
    minz = min(w.z for w in ws)
    obj.location -= Vector((cx, cy, minz))
    bpy.ops.object.transform_apply(location=True)
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")

    # Remove leftover empties from the import.
    for o in list(bpy.data.objects):
        if o is not obj:
            bpy.data.objects.remove(o, do_unlink=True)

    obj.data.validate(clean_customdata=False)  # drop degenerate faces left by decimation
    obj.data.update()

    out_dir = os.path.join(DST, name)
    os.makedirs(out_dir, exist_ok=True)
    bpy.ops.export_scene.gltf(filepath=os.path.join(out_dir, f"{name}.gltf"),
                              export_format="GLTF_SEPARATE", export_yup=True, export_apply=True)
    ws = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    size = Vector((max(w.x for w in ws) - min(w.x for w in ws), max(w.y for w in ws) - min(w.y for w in ws), max(w.z for w in ws)))
    print(f"[props] {name:18s} {before:7,d} -> {tri_count(obj):6,d} tris  size=({size.x:.2f},{size.y:.2f},{size.z:.2f}) m  mats={len(obj.data.materials)}")
