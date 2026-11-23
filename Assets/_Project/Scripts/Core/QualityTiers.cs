using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AirDefense.Core
{
    public enum QualityTier { Low = 0, Medium = 1, High = 2 }

    /// <summary>
    /// Graphics tiers (R-11, TECHNICAL_ARCHITECTURE.md §7). Auto-detects from device
    /// memory and CPU cores; the player can override in settings. High = Xiaomi Pad 8 class.
    /// Changes the active URP asset at runtime (not saved into the asset in builds).
    /// </summary>
    public static class QualityTiers
    {
        public struct TierValues
        {
            public float renderScale;
            public float shadowDistance;   // 0 = no shadows
            public int targetFps;
        }

        public static TierValues For(QualityTier t) => t switch
        {
            QualityTier.High => new TierValues { renderScale = 0.70f, shadowDistance = 150f, targetFps = 60 },
            QualityTier.Medium => new TierValues { renderScale = 0.60f, shadowDistance = 80f, targetFps = 60 },
            _ => new TierValues { renderScale = 0.50f, shadowDistance = 0f, targetFps = 30 },
        };

        public static QualityTier Detected { get; private set; } = QualityTier.High;
        public static QualityTier Active { get; private set; } = QualityTier.High;

        /// <summary>Simple first-pass heuristic; to be refined with a short benchmark in M8.</summary>
        public static QualityTier Detect(int systemMemoryMb, int cpuCores)
        {
            if (systemMemoryMb >= 7000 && cpuCores >= 8) return QualityTier.High;
            if (systemMemoryMb >= 3500 && cpuCores >= 6) return QualityTier.Medium;
            return QualityTier.Low;
        }

        public static void Apply()
        {
            Detected = Application.isMobilePlatform
                ? Detect(SystemInfo.systemMemorySize, SystemInfo.processorCount)
                : QualityTier.High;
            int ov = GameSettings.QualityOverride;
            Active = ov >= 0 && ov <= 2 ? (QualityTier)ov : Detected;

            TierValues v = For(Active);
            // In the editor these changes would be written back into the URP asset file, so only apply on devices.
            if (!Application.isEditor && GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                urp.renderScale = v.renderScale;
                urp.shadowDistance = Mathf.Max(0.01f, v.shadowDistance);
            }
            QualitySettings.shadows = v.shadowDistance > 0f ? UnityEngine.ShadowQuality.All : UnityEngine.ShadowQuality.Disable;
            Application.targetFrameRate = v.targetFps;
        }

        public static string Label(int overrideValue) => overrideValue switch
        {
            0 => "LOW",
            1 => "MEDIUM",
            2 => "HIGH",
            _ => $"AUTO ({Detected.ToString().ToUpperInvariant()})"
        };
    }
}
