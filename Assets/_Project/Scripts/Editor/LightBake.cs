using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace AirDefense.EditorTools
{
    /// <summary>
    /// Baked lighting for the static base (VS-4, VISUAL_PRODUCTION_STANDARD.md §4, decision O-18):
    /// indirect moonlight and sky bounce, ambient occlusion in corners and under props, and the fixed
    /// practical lamps (doors, streets, walls) baked into lightmaps; moving things (weapons, threats, radar,
    /// searchlights) light themselves from light probes. The moon stays a realtime light for direct light and
    /// shadows (Mixed, Baked Indirect), so the weapons keep crisp shadows.
    ///
    /// Run after the scene builder:  -executeMethod AirDefense.EditorTools.LightBake.BakeM1
    /// </summary>
    public static class LightBake
    {
        private const string SettingsPath = "Assets/_Project/Settings/NightLighting.lighting";

        [MenuItem("AirDefense/Setup/Bake M1 Lighting")]
        public static void BakeM1()
        {
            var scene = EditorSceneManager.OpenScene(M1SceneBuilder.ScenePath, OpenSceneMode.Single);
            Prepare();
            var t0 = System.DateTime.Now;
            bool ok = Lightmapping.Bake();
            Debug.Log($"[LightBake] bake {(ok ? "finished" : "FAILED")} in {(System.DateTime.Now - t0).TotalMinutes:0.0} min, " +
                      $"{LightmapSettings.lightmaps.Length} lightmap(s)");
            EditorSceneManager.SaveScene(scene);
            if (!ok && Application.isBatchMode) EditorApplication.Exit(1);
        }

        public static void Prepare()
        {
            // ---- lighting settings
            var ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(SettingsPath);
            if (ls == null)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(SettingsPath));
                ls = new LightingSettings { name = "NightLighting" };
                AssetDatabase.CreateAsset(ls, SettingsPath);
            }
            ls.bakedGI = true;
            ls.realtimeGI = false;
            ls.mixedBakeMode = MixedLightingMode.IndirectOnly;
            ls.lightmapper = LightingSettings.Lightmapper.ProgressiveCPU;
            ls.lightmapResolution = 2f;          // texels per metre (low: night, soft light)
            ls.lightmapPadding = 2;
            ls.lightmapMaxSize = 2048;
            ls.lightmapCompression = LightmapCompression.NormalQuality;
            ls.directionalityMode = LightmapsMode.NonDirectional; // cheapest on mobile
            ls.directSampleCount = 32;
            ls.indirectSampleCount = 128;
            ls.environmentSampleCount = 128;
            ls.maxBounces = 2;
            ls.ao = true;
            ls.aoMaxDistance = 1.5f;
            ls.aoExponentIndirect = 1.2f;
            ls.aoExponentDirect = 0.6f;
            ls.filteringMode = LightingSettings.FilterMode.Auto;
            EditorUtility.SetDirty(ls);
            Lightmapping.lightingSettings = ls;

            // ---- what is static (never moves) and how it receives GI
            var moving = new System.Collections.Generic.HashSet<Transform>();
            foreach (var m in Object.FindObjectsByType<Weapons.GunMount>(FindObjectsInactive.Include))
                foreach (var t in m.yawPivot.GetComponentsInChildren<Transform>(true)) moving.Add(t);
            foreach (var s in Object.FindObjectsByType<Environment.SearchlightSweep>(FindObjectsInactive.Include))
                foreach (var t in s.head.GetComponentsInChildren<Transform>(true)) moving.Add(t);
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
                if (mb is Core.Spin || mb is Environment.AlarmBeacon)
                    foreach (var t in mb.GetComponentsInChildren<Transform>(true)) moving.Add(t);

            int lightmapped = 0, probes = 0;
            var env = GameObject.Find("Environment");
            foreach (var r in env.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (moving.Contains(r.transform)) continue;
                var go = r.gameObject;
                var mat = r.sharedMaterial;
                // Light-emitting and transparent effects do not take part.
                if (mat != null && mat.shader != null && (mat.shader.name.StartsWith("AirDefense/") || mat.renderQueue >= 2500)) continue;
                var flags = GameObjectUtility.GetStaticEditorFlags(go) | StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic;
                GameObjectUtility.SetStaticEditorFlags(go, flags);
                Vector3 size = r.bounds.size;
                bool small = Mathf.Max(size.x, size.y, size.z) < 1.5f;
                bool far = Mathf.Max(Mathf.Abs(r.bounds.center.x), Mathf.Abs(r.bounds.center.z)) > 100f;
                bool huge = size.x > 600f; // the 5 km ground plane
                if (small || far || huge)
                {
                    r.receiveGI = ReceiveGI.LightProbes; // still casts occlusion into the lightmaps
                    probes++;
                }
                else
                {
                    r.receiveGI = ReceiveGI.Lightmaps;
                    var so = new SerializedObject(r);
                    // Large flat surfaces need fewer texels; the hero area (ZU position) more.
                    float scale = size.x * size.z > 5000f ? 0.35f : r.bounds.center.z < -45f && r.bounds.center.y > 10f ? 2f : 1f;
                    so.FindProperty("m_ScaleInLightmap").floatValue = scale;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    lightmapped++;
                }
            }

            // ---- lights: moon mixed; fixed practical lamps baked; everything that moves or flashes stays realtime
            int baked = 0;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
            {
                if (l.type == LightType.Directional) { l.lightmapBakeType = LightmapBakeType.Mixed; continue; }
                bool fixedLamp = l.name == "PracticalLight" || l.name == "StreetLight" || l.name == "SecurityLight";
                l.lightmapBakeType = fixedLamp ? LightmapBakeType.Baked : LightmapBakeType.Realtime;
                if (fixedLamp) baked++;
            }

            // ---- light probes for moving objects: a grid over the base, plus the weapon positions
            var old = GameObject.Find("Light Probes");
            if (old != null) Object.DestroyImmediate(old);
            var probesGo = new GameObject("Light Probes");
            probesGo.transform.SetParent(env.transform, false);
            var group = probesGo.AddComponent<LightProbeGroup>();
            var pts = new System.Collections.Generic.List<Vector3>();
            for (float x = -90f; x <= 90f; x += 15f)
            for (float z = -90f; z <= 90f; z += 15f)
                foreach (float y in new[] { 1f, 7f, 16f })
                    pts.Add(new Vector3(x, y, z));
            foreach (var c in new[] { new Vector3(0f, 14f, -60f), new Vector3(-14f, 9.2f, -8f), new Vector3(14f, 7.2f, -10f) })
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4f;
                    foreach (float h in new[] { 0.5f, 2f, 4f })
                        pts.Add(c + new Vector3(Mathf.Sin(a) * 3.5f, h, Mathf.Cos(a) * 3.5f));
                }
            group.probePositions = pts.ToArray();

            Debug.Log($"[LightBake] prepared: {lightmapped} lightmapped renderers, {probes} probe-lit, {baked} baked lamps, {pts.Count} probes");
        }
    }
}
