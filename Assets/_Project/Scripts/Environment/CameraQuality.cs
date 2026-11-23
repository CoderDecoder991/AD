using AirDefense.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AirDefense.Environment
{
    /// <summary>
    /// Applies the quality tier to the operator camera: post-processing (bloom,
    /// tone mapping) on Medium/High, off on Low. Re-checks when the setting changes.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraQuality : MonoBehaviour
    {
        private QualityTier _applied = (QualityTier)(-1);

        private void Update()
        {
            if (QualityTiers.Active == _applied) return;
            _applied = QualityTiers.Active;
            var data = GetComponent<UniversalAdditionalCameraData>();
            if (data != null) data.renderPostProcessing = _applied != QualityTier.Low;
        }
    }
}
