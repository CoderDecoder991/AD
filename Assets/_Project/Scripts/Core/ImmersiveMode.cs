using UnityEngine;

namespace AirDefense.Core
{
    /// <summary>
    /// Keeps the game in Android immersive full screen (VS-1, brief v1.1 §17.1 #2, §18.8).
    /// The status and navigation bars can reappear after a system gesture (screenshot gesture,
    /// edge swipe, notification shade, recent-apps screen) and the player does not always hide
    /// them again. This re-hides them when the app starts, regains focus or resumes, and a short
    /// moment later (some gestures restore the bars after focus returns). A swipe from the edge
    /// still shows them temporarily, as Android expects.
    /// </summary>
    public sealed class ImmersiveMode : MonoBehaviour
    {
        private float _reapplyAt = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (!Application.isMobilePlatform) return;
            var go = new GameObject("[ImmersiveMode]");
            DontDestroyOnLoad(go);
            go.AddComponent<ImmersiveMode>();
        }

        private void Start() => Apply();

        private void OnApplicationFocus(bool focus)
        {
            if (!focus) return;
            Apply();
            _reapplyAt = Time.unscaledTime + 0.5f;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) return;
            Apply();
            _reapplyAt = Time.unscaledTime + 0.5f;
        }

        private void Update()
        {
            if (_reapplyAt > 0f && Time.unscaledTime >= _reapplyAt)
            {
                _reapplyAt = -1f;
                Apply();
            }
        }

        public static void Apply()
        {
            Screen.fullScreen = true;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                if (activity == null) return;
                activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    using var version = new AndroidJavaClass("android.os.Build$VERSION");
                    int sdk = version.GetStatic<int>("SDK_INT");
                    using var window = activity.Call<AndroidJavaObject>("getWindow");
                    if (sdk >= 30)
                    {
                        using var types = new AndroidJavaClass("android.view.WindowInsets$Type");
                        int bars = types.CallStatic<int>("systemBars");
                        using var controller = window.Call<AndroidJavaObject>("getInsetsController");
                        if (controller == null) return;
                        controller.Call("hide", bars);
                        // BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE = 2: an edge swipe shows them briefly, then they hide.
                        controller.Call("setSystemBarsBehavior", 2);
                    }
                    else
                    {
                        // IMMERSIVE_STICKY | FULLSCREEN | HIDE_NAVIGATION | LAYOUT_* flags.
                        const int flags = 0x00001000 | 0x00000004 | 0x00000002 | 0x00000400 | 0x00000200 | 0x00000100;
                        using var decor = window.Call<AndroidJavaObject>("getDecorView");
                        decor.Call("setSystemUiVisibility", flags);
                    }
                }));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[ImmersiveMode] " + e.Message);
            }
#endif
        }
    }
}
