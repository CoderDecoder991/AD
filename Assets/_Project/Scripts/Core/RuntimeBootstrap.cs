using UnityEngine;

namespace AirDefense.Core
{
    /// <summary>
    /// Runs before the first scene loads. Applies frame-rate policy (R-12) and
    /// spawns the diagnostics overlay in development builds.
    /// </summary>
    public static class RuntimeBootstrap
    {
        public const int TargetFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            // On Android, vSyncCount is ignored; targetFrameRate drives frame pacing.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            ProfileStore.Load();
            QualityTiers.Apply(); // may lower targetFrameRate to 30 on the Low tier

            if (Debug.isDebugBuild || Application.isEditor)
            {
                var go = new GameObject("[DiagnosticsOverlay]");
                Object.DontDestroyOnLoad(go);
                go.AddComponent<DiagnosticsOverlay>();
            }
        }
    }
}
