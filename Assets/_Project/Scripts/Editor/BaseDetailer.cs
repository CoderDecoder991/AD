using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AirDefense.EditorTools
{
    /// <summary>
    /// Visual-clarity pass (user, 2026-11-24: "should look like a real high-end production game"): turns the
    /// plain base into a lived-in one. Building facades (windows - some lit -, doors with lamps, AC units,
    /// rooftop ducts, parapets, downpipes, cameras), asphalt roads with painted lines and street lamps, parked
    /// vehicles and clutter, and desert scatter (boulders, bushes, logs) outside the fence.
    /// Models and textures: CC0 Poly Haven (docs/ASSET_REGISTER.md), processed by tools/blender/process_props.py.
    /// Everything is placed from code with fixed seeds, so rebuilding the scene gives the same layout.
    /// </summary>
    public static class BaseDetailer
    {
        private const string MatDir = "Assets/_Project/Art/Materials/Detail/";
        private const string Tex = "Assets/_Project/Art/Textures/";

        // ------------------------------------------------------------ materials

        /// <summary>URP Lit material from a Poly Haven texture set (2K if present, else 1K). Tiling 1: world-UV boxes.</summary>
        public static Material Surface(string name, string set, float smoothness = 0.15f, Color? tint = null)
        {
            var m = Load(name);
            Texture2D T(string kind) =>
                AssetDatabase.LoadAssetAtPath<Texture2D>($"{Tex}{set}/{set}_{kind}_2k.jpg") ??
                AssetDatabase.LoadAssetAtPath<Texture2D>($"{Tex}{set}/{set}_{kind}_1k.jpg");
            var diff = T("diff");
            if (diff != null) m.SetTexture("_BaseMap", diff);
            m.SetColor("_BaseColor", tint ?? Color.white);
            var nor = T("nor_gl");
            if (nor != null) { m.SetTexture("_BumpMap", nor); m.EnableKeyword("_NORMALMAP"); }
            var ao = T("ao");
            if (ao != null) { m.SetTexture("_OcclusionMap", ao); m.EnableKeyword("_OCCLUSIONMAP"); }
            m.SetFloat("_Smoothness", smoothness);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Material Plain(string name, Color c, float smoothness, Color? emission = null)
        {
            var m = Load(name);
            m.SetTexture("_BaseMap", null);
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            else m.DisableKeyword("_EMISSION");
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material Load(string name)
        {
            Directory.CreateDirectory(MatDir);
            string path = $"{MatDir}{name}.mat";
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(lit); AssetDatabase.CreateAsset(m, path); }
            m.shader = lit;
            return m;
        }

        public sealed class Palette
        {
            public Material trim, darkMetal, litWindow, warmWindow, darkWindow, asphalt, paint, plaster, metalWall, container;
        }

        public static Palette MakePalette(Material concreteTrim, Material darkMetal)
        {
            return new Palette
            {
                trim = concreteTrim,
                darkMetal = darkMetal,
                // Lit windows: warm interior light (HDR so bloom makes them glow), and a cooler fluorescent variant.
                litWindow = Plain("WindowLitCool", new Color(0.1f, 0.1f, 0.1f), 0.8f, new Color(1.6f, 1.75f, 1.7f)),
                warmWindow = Plain("WindowLitWarm", new Color(0.1f, 0.1f, 0.1f), 0.8f, new Color(2.2f, 1.45f, 0.75f)),
                darkWindow = Plain("WindowDark", new Color(0.03f, 0.04f, 0.05f), 0.92f),
                asphalt = Surface("Asphalt", "worn_asphalt", 0.25f),
                paint = Plain("RoadPaint", new Color(0.78f, 0.77f, 0.72f), 0.35f),
                plaster = Surface("PlasterWall", "painted_plaster_wall", 0.12f, new Color(0.86f, 0.84f, 0.78f)),
                metalWall = Surface("CorrugatedWall", "worn_corrugated_iron", 0.3f),
                container = Surface("ContainerWall", "container_side", 0.3f, new Color(0.62f, 0.66f, 0.58f)),
            };
        }

        // ------------------------------------------------------------- helpers

        private static GameObject Box(Transform p, string n, Vector3 c, Vector3 s, Material m, float tile = 2f, bool shadows = true)
        {
            var go = NightBaseDresser.Box(p, n, c, s, m, tile);
            go.GetComponent<MeshRenderer>().shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }

        private static GameObject Prop(string name, Transform p, Vector3 pos, float yaw, float scale = 1f, bool shadows = true)
        {
            var go = NightBaseDresser.Prop(name, p, pos, yaw, scale);
            if (go != null && !shadows)
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        private static Light PointLight(Transform p, Vector3 pos, float range, float intensity, Color c)
        {
            var l = new GameObject("PracticalLight").AddComponent<Light>();
            l.transform.SetParent(p, false);
            l.transform.position = pos;
            l.type = LightType.Point;
            l.range = range;
            l.intensity = intensity;
            l.color = c;
            l.shadows = LightShadows.None;
            return l;
        }

        private static readonly Color Warm = new Color(1f, 0.78f, 0.52f);

        // ------------------------------------------------------------ buildings

        public enum Style { Office, Hangar, Depot }

        /// <summary>
        /// Facade detail for one building box (ground centre <paramref name="pos"/>, size <paramref name="size"/>).
        /// The south face (toward the gun positions) gets the entrances.
        /// </summary>
        public static void DetailBuilding(Transform env, string n, Vector3 pos, Vector3 size, Style style, Palette pal, int seed)
        {
            var root = new GameObject(n + "_Detail").transform;
            root.SetParent(env, false);
            var rng = new System.Random(seed);
            float R() => (float)rng.NextDouble();
            float sx = size.x / 2f, sz = size.z / 2f, top = size.y + 0.3f;
            float south = pos.z - sz, north = pos.z + sz;

            // Roof parapet (office/depot): a low wall around the roof edge, with a dark cap.
            if (style != Style.Hangar)
            {
                const float h = 0.7f, t = 0.3f;
                float y = top + h / 2f;
                Box(root, "Parapet_S", new Vector3(pos.x, y, south + t / 2f), new Vector3(size.x, h, t), pal.trim);
                Box(root, "Parapet_N", new Vector3(pos.x, y, north - t / 2f), new Vector3(size.x, h, t), pal.trim);
                Box(root, "Parapet_W", new Vector3(pos.x - sx + t / 2f, y, pos.z), new Vector3(t, h, size.z), pal.trim);
                Box(root, "Parapet_E", new Vector3(pos.x + sx - t / 2f, y, pos.z), new Vector3(t, h, size.z), pal.trim);
                // Plinth along the base: darker band where walls meet the ground.
                Box(root, "Plinth", new Vector3(pos.x, 0.3f, pos.z), new Vector3(size.x + 0.16f, 0.6f, size.z + 0.16f), pal.trim, 2f, false);
            }

            // Windows: rows per floor on both long faces. ~40% lit (some warm, some fluorescent).
            if (style == Style.Office)
            {
                const float floorH = 3.2f;
                int floors = Mathf.Max(1, Mathf.FloorToInt((size.y - 0.8f) / floorH));
                int perRow = Mathf.FloorToInt((size.x - 3f) / 3.4f);
                float x0 = pos.x - (perRow - 1) * 3.4f / 2f;
                for (int f = 0; f < floors; f++)
                for (int i = 0; i < perRow; i++)
                {
                    float x = x0 + i * 3.4f, y = 1.9f + f * floorH;
                    foreach (int side in new[] { -1, 1 })
                    {
                        if (side < 0 && f == 0 && Mathf.Abs(x - pos.x) < 2.2f) continue; // main entrance
                        float z = side < 0 ? south - 0.04f : north + 0.04f;
                        float r = R();
                        Material glass = r < 0.22f ? pal.warmWindow : r < 0.4f ? pal.litWindow : pal.darkWindow;
                        Box(root, "Window", new Vector3(x, y, z), new Vector3(1.4f, 1.15f, 0.08f), glass, 2f, false);
                        // Frame sits behind the glass (toward the wall), so the glass and its light stay visible.
                        Box(root, "Frame", new Vector3(x, y, z - side * 0.02f), new Vector3(1.6f, 1.35f, 0.04f), pal.darkMetal, 2f, false);
                        Box(root, "Sill", new Vector3(x, y - 0.66f, z - side * 0.06f), new Vector3(1.7f, 0.1f, 0.22f), pal.trim, 2f, false);
                    }
                }
            }

            // Entrances on the south face, each with a canopy and a lit wall lamp.
            var doors = new List<float>();
            if (style == Style.Office) { doors.Add(pos.x); if (size.x > 26f) { doors.Add(pos.x - sx + 4f); doors.Add(pos.x + sx - 4f); } }
            foreach (float dx in doors)
            {
                Box(root, "Door", new Vector3(dx, 1.15f, south - 0.05f), new Vector3(1.3f, 2.3f, 0.1f), pal.darkMetal);
                Box(root, "DoorFrame", new Vector3(dx, 1.25f, south - 0.03f), new Vector3(1.7f, 2.6f, 0.06f), pal.trim, 2f, false);
                Box(root, "Canopy", new Vector3(dx, 2.75f, south - 0.6f), new Vector3(2.2f, 0.12f, 1.2f), pal.trim);
                Prop("wall_lamp", root, new Vector3(dx, 3.0f, south - 0.15f), 180f);
                PointLight(root, new Vector3(dx, 2.5f, south - 1.4f), 10f, 3f, Warm);
            }
            if (style == Style.Hangar)
            {
                // Large roller door (the slats are horizontal, so a wide stretch keeps them looking right).
                Prop("roller_door", root, new Vector3(pos.x, 0f, south - 0.12f), 0f, 1f)?.transform.SetLocalScale(new Vector3(10.5f / 1.08f, 3.4f, 1f));
                Box(root, "DoorFrame", new Vector3(pos.x, 4.25f, south - 0.05f), new Vector3(12f, 8.5f, 0.1f), pal.darkMetal, 2f, false);
                Box(root, "SideDoor", new Vector3(pos.x + 9f, 1.15f, south - 0.05f), new Vector3(1.2f, 2.3f, 0.1f), pal.darkMetal);
                foreach (float lx in new[] { pos.x - 6.5f, pos.x + 6.5f, pos.x + 9f })
                {
                    Prop("wall_lamp", root, new Vector3(lx, lx == pos.x + 9f ? 3f : 9f, south - 0.15f), 180f);
                    PointLight(root, new Vector3(lx, lx == pos.x + 9f ? 2.5f : 8f, south - 1.5f), lx == pos.x + 9f ? 9f : 16f, 3.5f, Warm);
                }
                // High window band, dimly lit from inside.
                for (int i = 0; i < 6; i++)
                    Box(root, "HighWindow", new Vector3(pos.x - sx + 3f + i * (size.x - 6f) / 5f, size.y - 1.6f, north + 0.04f), new Vector3(2.4f, 0.8f, 0.08f), i % 2 == 0 ? pal.litWindow : pal.darkWindow, 2f, false);
            }
            if (style == Style.Depot)
            {
                foreach (float dx in new[] { pos.x - 4.5f, pos.x + 4.5f })
                {
                    Prop("roller_door", root, new Vector3(dx, 0f, south - 0.12f), 0f)?.transform.SetLocalScale(new Vector3(4f / 1.08f, 1.9f, 1f));
                    Box(root, "DoorFrame", new Vector3(dx, 2.35f, south - 0.05f), new Vector3(4.6f, 4.7f, 0.1f), pal.darkMetal, 2f, false);
                    Prop("wall_lamp", root, new Vector3(dx, 5.1f, south - 0.15f), 180f);
                    PointLight(root, new Vector3(dx, 4.6f, south - 1.5f), 11f, 3f, Warm);
                }
                Prop("roller_window", root, new Vector3(pos.x + sx + 0.12f, 1.2f, pos.z + 3f), 90f);
            }

            // Downpipes at the front corners and a gutter line.
            foreach (float cx in new[] { pos.x - sx + 0.2f, pos.x + sx - 0.2f })
                Box(root, "Downpipe", new Vector3(cx, size.y / 2f, south - 0.12f), new Vector3(0.14f, size.y, 0.14f), pal.darkMetal, 2f, false);
            Box(root, "Gutter", new Vector3(pos.x, size.y + 0.05f, south - 0.15f), new Vector3(size.x, 0.18f, 0.2f), pal.darkMetal, 2f, false);

            // Security cameras on the front corners, looking outward.
            Prop("security_camera", root, new Vector3(pos.x - sx + 0.3f, size.y - 0.6f, south - 0.2f), 200f);
            Prop("security_camera", root, new Vector3(pos.x + sx - 0.3f, size.y - 0.6f, south - 0.2f), 160f);

            // AC units on the side walls, utility box, rooftop plant.
            for (int i = 0; i < 2; i++)
            {
                Prop("aircon", root, new Vector3(pos.x - sx - 0.25f, 0f, pos.z - sz * 0.5f + i * sz), 90f);
                Prop("aircon", root, new Vector3(pos.x + sx + 0.25f, 0f, pos.z + sz * 0.5f - i * sz), -90f);
            }
            Prop("utility_box", root, new Vector3(pos.x + sx * 0.6f, 0f, south - 0.35f), 180f);
            if (style != Style.Hangar)
            {
                Prop("roof_duct", root, new Vector3(pos.x - sx * 0.25f, top, pos.z + 1f), R() * 20f - 10f);
                Prop("aircon", root, new Vector3(pos.x + sx * 0.5f, top, pos.z - 1.5f), 0f);
                Prop("aircon", root, new Vector3(pos.x + sx * 0.5f + 1.1f, top, pos.z - 1.5f), 0f);
            }
        }

        // ----------------------------------------------------------------- roads

        /// <summary>Asphalt road (axis-aligned) with solid edge lines and a dashed centre line.</summary>
        public static void Road(Transform env, string n, Vector3 a, Vector3 b, float width, Palette pal, float y)
        {
            var root = new GameObject(n).transform;
            root.SetParent(env, false);
            bool alongX = Mathf.Abs(b.x - a.x) > Mathf.Abs(b.z - a.z);
            float len = alongX ? Mathf.Abs(b.x - a.x) : Mathf.Abs(b.z - a.z);
            Vector3 c = (a + b) / 2f;
            Vector3 dir = alongX ? Vector3.right : Vector3.forward, side = alongX ? Vector3.forward : Vector3.right;
            Vector3 S(float along, float across, float h) => alongX ? new Vector3(along, h, across) : new Vector3(across, h, along);
            Box(root, "Asphalt", new Vector3(c.x, y, c.z), S(len, width, 0.04f), pal.asphalt, 6f, false);
            foreach (int s in new[] { -1, 1 })
                Box(root, "EdgeLine", new Vector3(c.x, y + 0.021f, c.z) + side * s * (width / 2f - 0.35f), S(len, 0.14f, 0.004f), pal.paint, 2f, false);
            for (float d = -len / 2f + 2f; d < len / 2f - 2f; d += 7f)
                Box(root, "CentreDash", new Vector3(c.x, y + 0.021f, c.z) + dir * (d + 1.5f), S(3f, 0.13f, 0.004f), pal.paint, 2f, false);
        }

        /// <summary>Street lamp with a warm downlight.</summary>
        public static void StreetLamp(Transform env, Vector3 pos, float yaw)
        {
            var lamp = Prop("street_lamp", env, pos, yaw);
            var l = new GameObject("StreetLight").AddComponent<Light>();
            l.transform.SetParent(lamp != null ? lamp.transform : env, true);
            l.transform.position = pos + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 7.3f, 0.6f);
            l.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            l.type = LightType.Spot;
            l.spotAngle = 115f;
            l.innerSpotAngle = 60f;
            l.range = 17f;
            l.intensity = 6f;
            l.color = new Color(1f, 0.84f, 0.62f);
            l.shadows = LightShadows.None;
        }

        // ---------------------------------------------------------------- clutter

        public static void Clutter(Transform env)
        {
            var root = new GameObject("Clutter (CC0 Poly Haven)").transform;
            root.SetParent(env, false);
            // Covered vehicles parked in front of the barracks.
            for (int i = 0; i < 3; i++) Prop("covered_car", root, new Vector3(-50f + i * 3.2f, 0f, 19.5f), 3f * i - 2f);
            for (int i = 0; i < 2; i++) Prop("covered_car", root, new Vector3(30f + i * 3.3f, 0f, 19.5f), -4f + i * 5f);
            // Depot yard: tyre stacks, crates, boxes, bagged cement, gas bottles, bins.
            Vector3 d = new Vector3(26f, 0f, -14f);
            for (int s = 0; s < 2; s++)
                for (int k = 0; k < 4; k++)
                {
                    var tyre = Prop("tyre", root, d + new Vector3(s * 0.75f, 0.3f + k * 0.17f, 0f), 0f);
                    if (tyre != null) tyre.transform.rotation = Quaternion.Euler(90f, k * 25f, 0f); // lying flat, stacked
                }
            for (int k = 0; k < 6; k++) Prop("plastic_crate", root, d + new Vector3(0.4f * (k % 3), (k / 3) * 0.26f, 1.6f), 90f + k * 3f);
            for (int k = 0; k < 5; k++) Prop("cardboard_box", root, d + new Vector3(2.2f + (k % 3) * 0.42f, (k / 3) * 0.34f, 1.4f), k * 9f);
            for (int k = 0; k < 8; k++) Prop("cement_bag", root, d + new Vector3(2.4f + (k % 2) * 0.5f, (k / 2) * 0.18f, -0.4f), 90f, 1f, false);
            for (int k = 0; k < 4; k++) Prop("propane_tank", root, new Vector3(49.5f + k * 0.4f, 0f, -18.6f), k * 40f);
            Prop("trash_can", root, new Vector3(6f, 0f, 15.9f), 180f);
            Prop("trash_can", root, new Vector3(-28f, 0f, 22.9f), 180f);
            Prop("trash_can", root, new Vector3(52f, 0f, 22.9f), 180f);
            // Concrete barriers (second type) staggered at the base entrance road.
            for (int i = 0; i < 4; i++) Prop("barrier_b", root, new Vector3(i % 2 == 0 ? -3f : 3f, 0f, -48f + i * 3f), 90f);
        }

        // ------------------------------------------------------------------ crew

        /// <summary>
        /// Posed soldier figures (user-supplied CC-BY models, tools/blender/process_characters.py): crew beside the
        /// ZU-23-2 and the C-RAM, guards at the command entrance, the gate checkpoint, the hangar and the depot.
        /// Static until animations exist. Placed clear of each weapon's gameplay camera line.
        /// </summary>
        public static void Crew(Transform env, Vector3 zuPos, Vector3 cramPadTop)
        {
            var root = new GameObject("Crew (CC-BY, user-supplied)").transform;
            root.SetParent(env, false);
            void Soldier(string model, Vector3 pos, float yaw)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Art/Models/Characters/{model}/{model}.gltf");
                if (asset == null) return;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, root);
                go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true)) r.shadowCastingMode = ShadowCastingMode.On;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            }
            // ZU-23-2 crew on the platform, beside the gun (loader by the ammo boxes, commander on the right).
            Soldier("soldier_gear", zuPos + new Vector3(-2.3f, 0.2f, -2.2f), 25f);
            Soldier("soldier_gear", zuPos + new Vector3(2.5f, 0.2f, -2.6f), -20f);
            // C-RAM operator at the electronics cabinet side (left rear, away from the over-the-shoulder camera).
            Soldier("soldier_gear", cramPadTop + new Vector3(-3.6f, 0f, -2.8f), 35f);
            // Guards: command entrance, gate checkpoint, hangar, depot yard.
            Soldier("soldier_a", new Vector3(-2.3f, 0.04f, 15.4f), 180f);
            Soldier("soldier_b", new Vector3(2.4f, 0.04f, 15.3f), 172f);
            Soldier("soldier_a", new Vector3(6.2f, 0.04f, -44f), -95f);
            Soldier("soldier_b", new Vector3(-29f, 0.04f, -24.6f), 160f);
            Soldier("soldier_a", new Vector3(27.6f, 0.04f, -11.5f), 125f);
        }

        // ---------------------------------------------------------- terrain scatter

        /// <summary>Desert scatter outside the fence (and a few bushes inside): boulders, rocks, bushes, dead logs.</summary>
        public static void TerrainScatter(Transform env, float fenceHalf)
        {
            var root = new GameObject("Terrain Scatter (CC0 Poly Haven)").transform;
            root.SetParent(env, false);
            var rng = new System.Random(2026);
            float R() => (float)rng.NextDouble();
            void Scatter(string prop, int count, float minR, float maxR, Vector2 scale, float sink)
            {
                for (int i = 0; i < count; i++)
                {
                    float a = R() * Mathf.PI * 2f, d = Mathf.Lerp(minR, maxR, Mathf.Sqrt(R()));
                    var p = new Vector3(Mathf.Sin(a) * d, 0f, Mathf.Cos(a) * d);
                    if (Mathf.Abs(p.x) < fenceHalf + 4f && Mathf.Abs(p.z) < fenceHalf + 4f) continue; // keep the base clear
                    float s = Mathf.Lerp(scale.x, scale.y, R());
                    var go = Prop(prop, root, p - Vector3.up * sink * s, R() * 360f, s, shadows: d < 170f);
                    if (go != null) go.transform.rotation *= Quaternion.Euler((R() - 0.5f) * 8f, 0f, (R() - 0.5f) * 8f);
                }
            }
            Scatter("bush_a", 140, 95f, 330f, new Vector2(1.6f, 3.2f), 0.05f);
            Scatter("bush_b", 120, 95f, 330f, new Vector2(1.8f, 3.6f), 0.05f);
            Scatter("boulder_a", 26, 100f, 340f, new Vector2(0.8f, 2.4f), 0.2f);
            Scatter("boulder_b", 22, 110f, 340f, new Vector2(0.7f, 2.2f), 0.25f);
            Scatter("rock_small", 70, 95f, 300f, new Vector2(0.4f, 1.1f), 0.15f);
            Scatter("dead_log", 16, 100f, 280f, new Vector2(0.9f, 1.4f), 0.05f);
            // A few bushes along the inside of the fence.
            for (int i = 0; i < 30; i++)
            {
                float t = R() * 4f;
                int side = (int)t;
                float u = (t - side) * 2f - 1f;
                Vector3 p = side switch
                {
                    0 => new Vector3(u * (fenceHalf - 3f), 0f, fenceHalf - 2f),
                    1 => new Vector3(fenceHalf - 2f, 0f, u * (fenceHalf - 3f)),
                    2 => new Vector3(u * (fenceHalf - 3f), 0f, -fenceHalf + 2f),
                    _ => new Vector3(-fenceHalf + 2f, 0f, u * (fenceHalf - 3f)),
                };
                Prop(i % 2 == 0 ? "bush_a" : "bush_b", root, p, R() * 360f, 1.4f + R(), shadows: true);
            }
        }
    }

    internal static class TransformExt
    {
        public static void SetLocalScale(this Transform t, Vector3 s) => t.localScale = s;
    }
}
