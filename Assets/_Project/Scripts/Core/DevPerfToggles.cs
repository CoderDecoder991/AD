using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AirDefense.Core
{
    /// <summary>
    /// Development-build switches for finding what costs GPU time on a device (VS-4 performance investigation,
    /// docs/ANDROID_PERFORMANCE_BUDGET.md §6). Each switch changes one feature live, so its cost can be measured
    /// with tools/measure_fps.sh without rebuilding. Shown in the pause menu of development builds only.
    /// </summary>
    public static class DevPerfToggles
    {
        private static UniversalRenderPipelineAsset Urp => GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        private static bool _shadowsOff, _postOff, _beamsOff;
        private static float _savedShadowDistance = 100f;

        public static string Msaa() { var u = Urp; if (u == null) return "-"; u.msaaSampleCount = u.msaaSampleCount >= 4 ? 2 : u.msaaSampleCount == 2 ? 1 : 4; return MsaaLabel; }
        public static string MsaaLabel => $"MSAA {Urp?.msaaSampleCount ?? 0}x";

        public static string Scale() { var u = Urp; if (u == null) return "-"; u.renderScale = u.renderScale > 0.8f ? 0.7f : u.renderScale > 0.6f ? 0.55f : 0.85f; return ScaleLabel; }
        public static string ScaleLabel => $"SCALE {Urp?.renderScale ?? 0f:0.00}";

        public static string Shadows()
        {
            var u = Urp;
            _shadowsOff = !_shadowsOff;
            if (u != null)
            {
                if (_shadowsOff) { _savedShadowDistance = u.shadowDistance; u.shadowDistance = 0.01f; }
                else u.shadowDistance = _savedShadowDistance;
            }
            QualitySettings.shadows = _shadowsOff ? UnityEngine.ShadowQuality.Disable : UnityEngine.ShadowQuality.All;
            return ShadowsLabel;
        }
        public static string ShadowsLabel => _shadowsOff ? "SHADOWS OFF" : "SHADOWS ON";

        public static string Post()
        {
            _postOff = !_postOff;
            var cam = Camera.main;
            if (cam != null && cam.TryGetComponent(out UniversalAdditionalCameraData data)) data.renderPostProcessing = !_postOff;
            return PostLabel;
        }
        public static string PostLabel => _postOff ? "POST OFF" : "POST ON";

        public static string Beams()
        {
            _beamsOff = !_beamsOff;
            foreach (var s in Object.FindObjectsByType<Environment.SearchlightSweep>(FindObjectsInactive.Include))
                foreach (var r in s.GetComponentsInChildren<MeshRenderer>(true))
                    if (r.name == "Beam" || r.name == "LampGlow") r.enabled = !_beamsOff;
            return BeamsLabel;
        }
        public static string BeamsLabel => _beamsOff ? "BEAMS OFF" : "BEAMS ON";
    }
}
