using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AirDefense.EditorTools
{
    /// <summary>
    /// Production render settings for the Android URP asset (visual-clarity pass, 2026-11-24). Values are set
    /// through serialized properties so enum values always match the installed URP version.
    /// The runtime tiers (Core/QualityTiers.cs) then scale these down for Medium/Low devices.
    ///
    /// - 4x MSAA: removes the stair-stepping and shimmer on thin edges (fence, masts, barrels, roof lines).
    /// - FSR 1 upscaling with sharpening instead of a plain bilinear stretch from the reduced render size.
    /// - 2048 shadow map, 2 cascades, soft shadows: crisp contact shadows near the gun, stable far ones.
    /// - SSAO (screen-space ambient occlusion), downsampled: grounds objects with contact darkening in corners,
    ///   under props and where walls meet the ground. Disabled at runtime below High tier.
    /// </summary>
    public static class RenderQualitySetup
    {
        private const string RpPath = "Assets/Settings/Mobile_RPAsset.asset";
        private const string RendererPath = "Assets/Settings/Mobile_Renderer.asset";

        public static void Apply()
        {
            var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(RpPath);
            if (rp == null) { Debug.LogError("[RenderQualitySetup] URP asset missing: " + RpPath); return; }
            var so = new SerializedObject(rp);
            Int(so, "m_MSAA", 4);
            Int(so, "m_UpscalingFilter", (int)UpscalingFilterSelection.FSR);
            Bool(so, "m_FsrOverrideSharpness", true);
            Float(so, "m_FsrSharpness", 0.8f);
            Int(so, "m_MainLightShadowmapResolution", 2048);
            Int(so, "m_ShadowCascadeCount", 2);
            Bool(so, "m_SoftShadowsSupported", true);
            Int(so, "m_SoftShadowQuality", (int)SoftShadowQuality.Medium);
            Float(so, "m_ShadowDistance", 150f);
            Int(so, "m_AdditionalLightsPerObjectLimit", 6);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rp);

            var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (rd == null) { Debug.LogError("[RenderQualitySetup] renderer missing: " + RendererPath); return; }
            var ssao = rd.rendererFeatures.OfType<ScreenSpaceAmbientOcclusion>().FirstOrDefault();
            if (ssao == null)
            {
                ssao = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
                ssao.name = "SSAO";
                AssetDatabase.AddObjectToAsset(ssao, rd);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ssao, out _, out long localId);
                var rso = new SerializedObject(rd);
                var list = rso.FindProperty("m_RendererFeatures");
                var map = rso.FindProperty("m_RendererFeatureMap");
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = ssao;
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                rso.ApplyModifiedPropertiesWithoutUndo();
            }
            var sso = new SerializedObject(ssao);
            Int(sso, "m_Settings.AOMethod", 0);             // blue noise
            Bool(sso, "m_Settings.Downsample", true);       // half resolution: mobile cost
            Bool(sso, "m_Settings.AfterOpaque", false);
            Int(sso, "m_Settings.Source", 0);               // depth only (no extra normals pass)
            Float(sso, "m_Settings.Intensity", 1.6f);
            Float(sso, "m_Settings.DirectLightingStrength", 0.3f);
            Float(sso, "m_Settings.Radius", 0.35f);
            Int(sso, "m_Settings.Samples", 2);              // low: 4 samples
            Int(sso, "m_Settings.BlurQuality", 2);          // low: Kawase
            Float(sso, "m_Settings.Falloff", 60f);
            sso.ApplyModifiedPropertiesWithoutUndo();
            ssao.SetActive(true);
            EditorUtility.SetDirty(ssao);
            EditorUtility.SetDirty(rd);
            AssetDatabase.SaveAssets();
            Debug.Log("[RenderQualitySetup] MSAA 4x, FSR sharpen 0.8, shadows 2048x2 soft, SSAO on");
        }

        private static void Int(SerializedObject so, string path, int v)
        {
            var p = so.FindProperty(path);
            if (p == null) { Debug.LogWarning("[RenderQualitySetup] no property " + path); return; }
            p.intValue = v; // raw value, also for enums (MSAA is 1/2/4/8, not an index)
        }

        private static void Float(SerializedObject so, string path, float v)
        {
            var p = so.FindProperty(path);
            if (p == null) { Debug.LogWarning("[RenderQualitySetup] no property " + path); return; }
            p.floatValue = v;
        }

        private static void Bool(SerializedObject so, string path, bool v)
        {
            var p = so.FindProperty(path);
            if (p == null) { Debug.LogWarning("[RenderQualitySetup] no property " + path); return; }
            p.boolValue = v;
        }
    }
}
