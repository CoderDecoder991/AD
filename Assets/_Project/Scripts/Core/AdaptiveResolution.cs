using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AirDefense.Core
{
    /// <summary>
    /// Dynamic resolution (docs/ANDROID_PERFORMANCE_BUDGET.md §8). Watches real frame times and lowers the URP
    /// render scale in small steps when frames miss the 60 fps deadline, then raises it again after a long run of
    /// on-time frames, never above the quality tier's scale or below its floor.
    ///
    /// Why: the Pad 8 holds 60 fps on High while cool, but drops to 40 when warm or charging (the device lowers its
    /// GPU clocks; measured 2026-11-24). A fixed setting is either too soft for a cool device or too slow for a warm
    /// one; adapting keeps frame pacing smooth and the image as sharp as the device can afford at that moment.
    /// </summary>
    public sealed class AdaptiveResolution : MonoBehaviour
    {
        /// <summary>Highest render scale (the quality tier's value).</summary>
        public static float MaxScale = 0.85f;
        /// <summary>Lowest render scale this tier may drop to.</summary>
        public static float MinScale = 0.6f;
        public static bool Enabled = true;
        public static float CurrentScale => Urp != null ? Urp.renderScale : 1f;

        private const float Step = 0.05f;
        private const int Window = 60;               // frames per evaluation (~1 s)
        private const float SlowFrameMs = 20f;       // a frame that missed the 16.7 ms deadline
        private const int DownIfSlow = 6;            // >10% missed frames in a window: step down
        private const float UpAfterSeconds = 20f;    // this long with (almost) no misses: step up (long: avoids see-sawing)
        private const float Cooldown = 1.5f;

        private int _frames, _slow;
        private float _goodFor, _lastChange;

        private static UniversalRenderPipelineAsset Urp => GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (Application.isEditor) return; // the editor would write the scale into the asset file
            var go = new GameObject("[AdaptiveResolution]");
            DontDestroyOnLoad(go);
            go.AddComponent<AdaptiveResolution>();
        }

        private void Update()
        {
            var urp = Urp;
            if (!Enabled || urp == null || Time.timeScale == 0f || Application.targetFrameRate < 50) { _frames = _slow = 0; return; }
            float ms = Time.unscaledDeltaTime * 1000f;
            if (ms > 250f) return; // a hitch from loading or the app resuming, not a rendering cost
            _frames++;
            if (ms > SlowFrameMs) _slow++;
            if (_frames < Window) return;

            float now = Time.unscaledTime;
            if (_slow >= DownIfSlow)
            {
                _goodFor = 0f;
                if (now - _lastChange > Cooldown && urp.renderScale > MinScale + 0.001f)
                {
                    urp.renderScale = Mathf.Max(MinScale, urp.renderScale - Step);
                    _lastChange = now;
                }
            }
            else
            {
                _goodFor = _slow <= 1 ? _goodFor + _frames * Time.unscaledDeltaTime : 0f;
                if (_goodFor >= UpAfterSeconds && now - _lastChange > Cooldown && urp.renderScale < MaxScale - 0.001f)
                {
                    urp.renderScale = Mathf.Min(MaxScale, urp.renderScale + Step);
                    _lastChange = now;
                    _goodFor = 0f;
                }
            }
            _frames = _slow = 0;
        }
    }
}
