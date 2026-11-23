using UnityEngine;

namespace AirDefense.Core
{
    /// <summary>
    /// Development-only on-screen readout: FPS (average and worst frame over the
    /// last second), frame time, resolution and render scale. Uses IMGUI so it
    /// needs no UI assets. Not shipped in release builds (see RuntimeBootstrap).
    /// </summary>
    public sealed class DiagnosticsOverlay : MonoBehaviour
    {
        private const float SampleWindow = 1f;

        private float _accum;
        private int _frames;
        private float _worstDt;
        private float _avgFps;
        private float _worstMs;
        private GUIStyle _style;

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _accum += dt;
            _frames++;
            if (dt > _worstDt) _worstDt = dt;

            if (_accum >= SampleWindow)
            {
                _avgFps = _frames / _accum;
                _worstMs = _worstDt * 1000f;
                _accum = 0f;
                _frames = 0;
                _worstDt = 0f;
            }
        }

        private void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.Max(18, Screen.height / 40),
                    fontStyle = FontStyle.Bold
                };
            }

            _style.normal.textColor = _avgFps >= 55f ? Color.green : (_avgFps >= 30f ? Color.yellow : Color.red);

            float renderScale = 1f;
            if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline is
                UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset urp)
            {
                renderScale = urp.renderScale;
            }

            string text =
                $"{_avgFps:0.0} fps  (worst {_worstMs:0.0} ms)\n" +
                $"{Screen.width}x{Screen.height}  scale {renderScale:0.00}\n" +
                $"{SystemInfo.graphicsDeviceType}  {SystemInfo.graphicsDeviceName}";

            // Top-right, below the pause button, clear of the radar (top-left).
            var safe = Screen.safeArea;
            float w = _style.fontSize * 16f;
            float top = Screen.height - safe.yMax + Screen.height * 0.13f;
            _style.alignment = TextAnchor.UpperRight;
            GUI.Label(new Rect(safe.xMax - w - 12, top, w, Screen.height), text, _style);
        }
    }
}
