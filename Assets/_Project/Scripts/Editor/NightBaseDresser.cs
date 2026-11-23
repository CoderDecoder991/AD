using System.Collections.Generic;
using System.IO;
using AirDefense.Core;
using AirDefense.Environment;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AirDefense.EditorTools
{
    /// <summary>
    /// M2 night base: CC0 Poly Haven textures, sky and props (docs/ASSET_REGISTER.md),
    /// moonlight, practical lights, searchlights, fog and post-processing.
    /// Called by M1SceneBuilder. Every step skips itself if its assets are missing,
    /// so the scene still builds as greybox.
    /// </summary>
    public static class NightBaseDresser
    {
        private const string Tex = "Assets/_Project/Art/Textures/";
        private const string Props = "Assets/_Project/Art/Models/Props/";
        private const string Gen = "Assets/_Project/Art/Generated/";
        private const string SkyPath = "Assets/_Project/Art/Sky/rogland_clear_night_2k.hdr";

        // ----------------------------------------------------------- materials

        /// <summary>Puts Poly Haven texture sets onto the placeholder materials (kept as the same assets).</summary>
        public static void UpgradeMaterials(Dictionary<string, Material> m)
        {
            Apply(m["Ground"], "brown_mud_dry", 1f / 6f);
            Apply(m["Sandbag"], "brown_mud_dry", 1f / 3f);
            Apply(m["Concrete"], "concrete_floor_worn_001", 1f / 4f);
            Apply(m["Building"], "concrete_wall_003", 1f / 4f);
            Apply(m["Roof"], "corrugated_iron", 1f / 2.5f);
            // Ground plane/apron/berm use Unity primitives (UV 0..1), so tiling is set per material;
            // buildings use world-UV boxes (tiling handled in the mesh, so scale 1).
            SetTiling(m["Ground"], 5000f / 6f);
            SetTiling(m["Concrete"], 1f);
            SetTiling(m["Building"], 1f);
            SetTiling(m["Roof"], 1f);
            SetTiling(m["Sandbag"], 1f);
        }

        private static void Apply(Material mat, string set, float _)
        {
            var diff = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Tex}{set}/{set}_diff_1k.jpg");
            if (diff == null) return;
            mat.SetTexture("_BaseMap", diff);
            mat.SetColor("_BaseColor", Color.white);
            var nor = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Tex}{set}/{set}_nor_gl_1k.jpg");
            if (nor != null)
            {
                mat.SetTexture("_BumpMap", nor);
                mat.EnableKeyword("_NORMALMAP");
            }
            var ao = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Tex}{set}/{set}_ao_1k.jpg");
            if (ao != null)
            {
                mat.SetTexture("_OcclusionMap", ao);
                mat.EnableKeyword("_OCCLUSIONMAP");
            }
            mat.SetFloat("_Smoothness", 0.12f);
            EditorUtility.SetDirty(mat);
        }

        private static void SetTiling(Material mat, float tiles)
        {
            mat.SetTextureScale("_BaseMap", new Vector2(tiles, tiles));
            EditorUtility.SetDirty(mat);
        }

        /// <summary>
        /// A box mesh whose UVs are in world metres / tileSize, so a texture keeps
        /// its real size on boxes of any proportion. Saved as an asset (needed for builds).
        /// </summary>
        public static Mesh WorldUvBox(Vector3 size, float tileSize)
        {
            Directory.CreateDirectory(Gen);
            string path = $"{Gen}Box_{size.x:0.#}x{size.y:0.#}x{size.z:0.#}_t{tileSize:0.#}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;

            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var norms = new List<Vector3>();
            var tris = new List<int>();
            Vector3 h = size * 0.5f;
            void Face(Vector3 n, Vector3 u, Vector3 v, float uLen, float vLen)
            {
                int b = verts.Count;
                Vector3 c = Vector3.Scale(n, h);
                verts.Add(c - u * uLen / 2 - v * vLen / 2); uvs.Add(new Vector2(0, 0));
                verts.Add(c + u * uLen / 2 - v * vLen / 2); uvs.Add(new Vector2(uLen / tileSize, 0));
                verts.Add(c + u * uLen / 2 + v * vLen / 2); uvs.Add(new Vector2(uLen / tileSize, vLen / tileSize));
                verts.Add(c - u * uLen / 2 + v * vLen / 2); uvs.Add(new Vector2(0, vLen / tileSize));
                for (int i = 0; i < 4; i++) norms.Add(n);
                tris.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
            }
            Face(Vector3.forward, Vector3.left, Vector3.up, size.x, size.y);
            Face(Vector3.back, Vector3.right, Vector3.up, size.x, size.y);
            Face(Vector3.right, Vector3.forward, Vector3.up, size.z, size.y);
            Face(Vector3.left, Vector3.back, Vector3.up, size.z, size.y);
            Face(Vector3.up, Vector3.right, Vector3.forward, size.x, size.z);
            Face(Vector3.down, Vector3.right, Vector3.back, size.x, size.z);

            var mesh = new Mesh { name = Path.GetFileNameWithoutExtension(path) };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(norms);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        public static GameObject Box(Transform parent, string name, Vector3 centre, Vector3 size, Material mat, float tileSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = centre;
            go.AddComponent<MeshFilter>().sharedMesh = WorldUvBox(size, tileSize);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            return go;
        }

        // ------------------------------------------------------------ lighting

        public static void NightLighting(Light moon)
        {
            moon.name = "Moon";
            moon.type = LightType.Directional;
            moon.intensity = 0.45f;
            moon.color = new Color(0.62f, 0.72f, 1f);
            moon.shadows = LightShadows.Soft;
            moon.shadowStrength = 0.7f;
            moon.transform.rotation = Quaternion.Euler(38f, -35f, 0f);

            var sky = AssetDatabase.LoadAssetAtPath<Texture>(SkyPath);
            if (sky != null)
            {
                var skyMat = AssetDatabase.LoadAssetAtPath<Material>(Gen + "NightSky.mat");
                if (skyMat == null)
                {
                    Directory.CreateDirectory(Gen);
                    skyMat = new Material(Shader.Find("Skybox/Panoramic"));
                    AssetDatabase.CreateAsset(skyMat, Gen + "NightSky.mat");
                }
                skyMat.SetTexture("_MainTex", sky);
                skyMat.SetFloat("_Exposure", 0.42f);
                skyMat.SetFloat("_Rotation", 120f);
                EditorUtility.SetDirty(skyMat);
                RenderSettings.skybox = skyMat;
            }

            // Trilight ambient: predictable without baking lighting.
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.10f, 0.13f, 0.22f);
            RenderSettings.ambientEquatorColor = new Color(0.07f, 0.08f, 0.11f);
            RenderSettings.ambientGroundColor = new Color(0.03f, 0.03f, 0.035f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.00055f;
            RenderSettings.fogColor = new Color(0.035f, 0.045f, 0.07f);
        }

        public static void PostProcessing(Camera cam)
        {
            Directory.CreateDirectory(Gen);
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Gen + "NightPost.asset");
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, Gen + "NightPost.asset");
            }
            void Ensure<T>(System.Action<T> set) where T : VolumeComponent
            {
                if (!profile.TryGet(out T c)) { c = profile.Add<T>(true); AssetDatabase.AddObjectToAsset(c, profile); }
                c.active = true;
                set(c);
                EditorUtility.SetDirty(c);
            }
            Ensure<Bloom>(b => { b.intensity.Override(0.9f); b.threshold.Override(0.85f); b.scatter.Override(0.6f); });
            Ensure<Tonemapping>(t => t.mode.Override(TonemappingMode.ACES));
            Ensure<Vignette>(v => { v.intensity.Override(0.28f); v.smoothness.Override(0.5f); });
            Ensure<ColorAdjustments>(c => { c.postExposure.Override(0f); c.saturation.Override(-10f); });
            EditorUtility.SetDirty(profile);

            var vol = new GameObject("Post Processing").AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = profile;

            var data = cam.GetComponent<UniversalAdditionalCameraData>() ?? cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            cam.gameObject.AddComponent<CameraQuality>();
        }

        // ---------------------------------------------------------------- props

        public static GameObject Prop(string name, Transform parent, Vector3 pos, float yaw, float scale = 1f, bool isStatic = true)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{Props}{name}/{name}.gltf");
            if (asset == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            if (isStatic)
                foreach (var t in go.GetComponentsInChildren<Transform>(true))
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
                r.shadowCastingMode = scale < 0.5f ? ShadowCastingMode.Off : ShadowCastingMode.On;
            return go;
        }

        /// <summary>Chain-link fence around a square, segment by segment (static-batched).</summary>
        public static void PerimeterFence(Transform parent, float halfSide)
        {
            var root = new GameObject("PerimeterFence (CC0 Poly Haven)").transform;
            root.SetParent(parent, false);
            const float seg = 1.99f;
            int perSide = Mathf.FloorToInt(halfSide * 2f / seg);
            for (int side = 0; side < 4; side++)
            {
                Quaternion r = Quaternion.Euler(0, side * 90f, 0);
                for (int i = 0; i < perSide; i++)
                {
                    float x = -halfSide + seg * (i + 0.5f);
                    Vector3 p = r * new Vector3(x, 0f, halfSide);
                    var f = Prop("fence_segment", root, p, side * 90f);
                    if (f == null) return;
                    foreach (var mr in f.GetComponentsInChildren<MeshRenderer>()) mr.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
        }

        /// <summary>Supplies, barriers and lights around the gun pit and buildings.</summary>
        public static void DressBase(Transform env, Transform gunRoot, float bermHeight, Material beamMat)
        {
            var root = new GameObject("Props (CC0 Poly Haven)").transform;
            root.SetParent(env, false);
            Vector3 g = gunRoot.position; // top of the berm

            // Behind the gun only (south half), so the forward view stays clear.
            Prop("ammo_box", root, g + new Vector3(-2.6f, 0f, -3.2f), 10f, 2.2f);
            Prop("ammo_box", root, g + new Vector3(-2.2f, 0f, -3.4f), 80f, 2.2f);
            Prop("ammo_box", root, g + new Vector3(-2.4f, 0.4f, -3.3f), 30f, 2.2f);
            Prop("military_crate", root, g + new Vector3(2.8f, 0f, -3.6f), 15f);
            Prop("military_crate", root, g + new Vector3(2.9f, 0.3f, -3.5f), 25f);
            Prop("wooden_crate", root, g + new Vector3(-4.6f, 0f, -5.2f), -20f);
            Prop("jerrycan", root, g + new Vector3(4.3f, 0f, -5.4f), 40f);
            Prop("jerrycan", root, g + new Vector3(4.7f, 0f, -5.1f), 70f);
            Prop("barrel", root, g + new Vector3(-5.6f, 0f, -6.4f), 0f);
            Prop("generator", root, g + new Vector3(5.6f, 0f, -6.6f), -110f);

            // Floodlights at the rear corners of the berm, lighting the gun pit.
            foreach (float sx in new[] { -1f, 1f })
            {
                var fl = Prop("floodlight_pair", root, g + new Vector3(6.6f * sx, 0f, -7.2f), sx * 35f + 180f, 1f);
                var spot = new GameObject("FloodSpot").AddComponent<Light>();
                spot.transform.SetParent(root, false);
                spot.transform.position = g + new Vector3(6.6f * sx, 1.0f, -7.2f);
                spot.transform.LookAt(g + Vector3.up * 1.2f);
                spot.type = LightType.Spot;
                spot.spotAngle = 70f;
                spot.range = 22f;
                spot.intensity = 6f;
                spot.color = new Color(1f, 0.92f, 0.78f);
                spot.shadows = LightShadows.None;
                _ = fl;
            }

            // Concrete barriers ringing the berm foot and the approach road.
            for (int i = 0; i < 10; i++)
            {
                float a = (i / 10f) * 300f + 30f;
                Vector3 p = new Vector3(g.x, 0f, g.z) + Quaternion.Euler(0, a, 0) * Vector3.forward * 12.5f;
                Prop("concrete_barrier", root, p, a + 90f);
            }
            for (int i = 0; i < 6; i++)
                Prop("concrete_barrier", root, new Vector3(-6f + (i % 2) * 12f, 0f, -75f - (i / 2) * 6f), 90f);

            // Supplies by the buildings.
            Prop("generator", root, new Vector3(13.5f, 0f, 14f), 0f);
            for (int i = 0; i < 4; i++) Prop("barrel", root, new Vector3(49f + (i % 2) * 0.7f, 0f, -20f - (i / 2) * 0.7f), i * 40f);
            for (int i = 0; i < 3; i++) Prop("wooden_crate", root, new Vector3(-20f, i == 2 ? 0.46f : 0f, -26f - (i % 2) * 0.6f), 90f);
            for (int i = 0; i < 4; i++) Prop("military_crate", root, new Vector3(-50f + i * 2.1f, 0f, 14f), 0f);

            // Security lights on the building fronts (south faces), with warm point lights.
            var walls = new[] { new Vector3(0f, 6.5f, 16.8f), new Vector3(-40f, 4.8f, 23.8f), new Vector3(38f, 5.5f, -18.2f) };
            foreach (var w in walls)
            {
                Prop("security_light", root, w, 180f, 1f, isStatic: false);
                var pl = new GameObject("SecurityLight").AddComponent<Light>();
                pl.transform.SetParent(root, false);
                pl.transform.position = w + new Vector3(0f, -0.4f, -1.2f);
                pl.type = LightType.Point;
                pl.range = 16f;
                pl.intensity = 2.5f;
                pl.color = new Color(1f, 0.78f, 0.5f);
                pl.shadows = LightShadows.None;
            }

            // Searchlight masts at the north corners, sweeping the approaches.
            Searchlight(env, new Vector3(-70f, 0f, 70f), 20f, 0f, beamMat);
            Searchlight(env, new Vector3(70f, 0f, 70f), -20f, 7f, beamMat);
        }

        /// <summary>
        /// The C-RAM and 2K22 Tunguska shown in full at their firing positions (user request), facing
        /// north. Display-only until they become playable (C-RAM in M3, Tunguska in M4).
        /// Models: CC-BY 4.0 (Sketchfab), scaled by tools/blender/process_display_weapons.py.
        /// </summary>
        public static void PlaceDisplayWeapons(Transform env, Material concrete, Material sandbag)
        {
            // On raised earth positions (like the ZU's), so they stand clear of the ZU mound's front edge
            // in the third-person view. pad.y = mound height.
            Place("CRAM", "C-RAM (display until M3)", new Vector3(-14f, 0f, -8f), 0f, new Vector3(10f, 9f, 10f));
            Place("Tunguska", "2K22 Tunguska (display until M4)", new Vector3(14f, 0f, -10f), -6f, new Vector3(9f, 7f, 13f));

            void Place(string model, string label, Vector3 pos, float yaw, Vector3 pad)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Art/Models/{model}/{model}.gltf");
                if (asset == null) return;
                var root = new GameObject(label).transform;
                root.SetParent(env, false);
                root.position = pos;
                Box(root, "Mound", pos + Vector3.up * (pad.y / 2f), pad, sandbag, 3f)
                    .GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
                Box(root, "Pad", pos + Vector3.up * (pad.y + 0.1f), new Vector3(pad.x - 1.5f, 0.2f, pad.z - 1.5f), concrete, 4f);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, root);
                go.transform.position = pos + Vector3.up * (pad.y + 0.2f);
                go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
                    r.shadowCastingMode = ShadowCastingMode.On;
                // A low sandbag wall on the threat side and barriers at the corners.
                // Low sandbag parapet on the threat (north) edge of the mound top, and barriers at the foot.
                Box(root, "Sandbags", pos + new Vector3(0f, pad.y + 0.5f, pad.z / 2f - 0.5f), new Vector3(pad.x - 1f, 0.8f, 0.8f), sandbag, 2f);
                Prop("concrete_barrier", root, pos + new Vector3(-pad.x / 2f - 1f, 0f, -pad.z / 2f), 90f);
                Prop("concrete_barrier", root, pos + new Vector3(pad.x / 2f + 1f, 0f, -pad.z / 2f), 90f);
            }
        }

        private static void Searchlight(Transform env, Vector3 basePos, float centerYaw, float phase, Material beamMat)
        {
            var concrete = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Placeholder/PH_Fence.mat");
            var mast = new GameObject("SearchlightMast").transform;
            mast.SetParent(env, false);
            mast.position = basePos;
            Box(mast, "Mast", basePos + Vector3.up * 6f, new Vector3(0.6f, 12f, 0.6f), concrete, 2f);
            Box(mast, "Platform", basePos + Vector3.up * 12.1f, new Vector3(2.4f, 0.2f, 2.4f), concrete, 2f);

            var head = new GameObject("Head").transform;
            head.SetParent(mast, false);
            head.position = basePos + Vector3.up * 13f;
            var model = Prop("searchlight_head", head, head.position, 0f, 1f, isStatic: false);
            if (model != null)
            {
                // The source lamp faces +X after import; turn it so its lens points along the head's +Z.
                model.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                model.transform.localPosition = Vector3.down * 0.5f;
            }
            var spot = new GameObject("Spot").AddComponent<Light>();
            spot.transform.SetParent(head, false);
            spot.type = LightType.Spot;
            spot.spotAngle = 9f;
            spot.range = 400f;
            spot.intensity = 30f;
            spot.color = new Color(0.9f, 0.95f, 1f);
            spot.shadows = LightShadows.None;

            var sweep = mast.gameObject.AddComponent<SearchlightSweep>();
            sweep.head = head;
            sweep.spot = spot;
            sweep.beamMaterial = beamMat;
            sweep.centerYaw = centerYaw;
            sweep.phase = phase;
        }

        // ------------------------------------------------------------ materials (FX)

        public static Material AdditiveParticleMaterial(string name, Texture tex)
        {
            Directory.CreateDirectory(Gen);
            string path = $"{Gen}{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetTexture("_BaseMap", tex);
            // Transparent + additive blending, set by hand (no editor GUI here).
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 2f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.One);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>
        /// Additive glow that ignores fog (AirDefense/AdditiveGlowNoFog): for light-emitting effects that
        /// must stay visible at long range at night (tracers, flashes, sparks). Falls back to the URP particle material.
        /// </summary>
        public static Material GlowMaterial(string name, Texture tex, Color hdrColour)
        {
            var shader = Shader.Find("AirDefense/AdditiveGlowNoFog");
            if (shader == null)
            {
                var fallback = AdditiveParticleMaterial(name, tex);
                fallback.SetColor("_BaseColor", hdrColour);
                return fallback;
            }
            Directory.CreateDirectory(Gen);
            string path = $"{Gen}{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", hdrColour);
            mat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>Alpha-blended smoke that ignores fog (AirDefense/AlphaSmokeNoFog), for puffs high in the sky.</summary>
        public static Material SkySmokeMaterial(string name, Texture tex)
        {
            var shader = Shader.Find("AirDefense/AlphaSmokeNoFog");
            if (shader == null) return AlphaParticleMaterial(name, tex);
            Directory.CreateDirectory(Gen);
            string path = $"{Gen}{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
            mat.shader = shader;
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static Material AlphaParticleMaterial(string name, Texture tex)
        {
            var mat = AdditiveParticleMaterial(name, tex);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static Texture2D SoftDotAsset()
        {
            Directory.CreateDirectory(Gen);
            string path = Gen + "SoftDot.png";
            if (!File.Exists(path))
            {
                File.WriteAllBytes(path, DamageVisuals.SoftDot(128).EncodeToPNG());
                AssetDatabase.ImportAsset(path);
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                ti.alphaIsTransparency = true;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
