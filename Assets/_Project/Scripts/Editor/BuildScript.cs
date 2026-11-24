using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AirDefense.EditorTools
{
    /// <summary>
    /// Repeatable Android builds.
    /// Menu: AirDefense ▸ Build ▸ ...
    /// Command line:
    ///   Unity.exe -batchmode -quit -projectPath . -buildTarget Android
    ///             -executeMethod AirDefense.EditorTools.BuildScript.BuildAndroidDev
    /// Release signing is not configured here: keystores never live in the repo.
    /// </summary>
    public static class BuildScript
    {
        public const string OutputDir = "Builds/Android";

        [MenuItem("AirDefense/Build/Android Development APK")]
        public static void BuildAndroidDev() => Build(autoRun: false);

        [MenuItem("AirDefense/Build/Android Development APK + Run on Device")]
        public static void BuildAndRunAndroidDev() => Build(autoRun: true);

        /// <summary>Same, but the player connects to the Editor profiler at startup (only with the USB cable in).</summary>
        [MenuItem("AirDefense/Build/Android Development APK (Autoconnect Profiler)")]
        public static void BuildAndroidDevProfiler() => Build(autoRun: false, profiler: true);

        /// <summary>
        /// Review build (VS-1): a non-development player, as players will see it - no "Development Build"
        /// watermark, no diagnostics readout, no DEV buttons. Use for screenshots and user review.
        /// </summary>
        [MenuItem("AirDefense/Build/Android Review APK (no dev tools)")]
        public static void BuildAndroidReview() => Build(autoRun: false, review: true);

        private static void Build(bool autoRun, bool profiler = false, bool review = false)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            EditorUserBuildSettings.buildAppBundle = false;

            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
                throw new InvalidOperationException("No enabled scenes in Build Settings. Run AirDefense ▸ Setup first.");

            Directory.CreateDirectory(OutputDir);
            string path = Path.Combine(OutputDir, review ? "AirDefense-review.apk" : "AirDefense-dev.apk");

            // No profiler autoconnect by default: it makes the game wait for the PC at startup (about 10 s
            // with the USB cable in, and it never starts properly with the cable out). Profile on request.
            var options = review ? BuildOptions.None : BuildOptions.Development;
            if (profiler) options |= BuildOptions.ConnectWithProfiler;
            if (autoRun) options |= BuildOptions.AutoRunPlayer;

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = path,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = options
            });

            var s = report.summary;
            Debug.Log($"[BuildScript] {s.result}: {path}  {s.totalSize / (1024f * 1024f):0.0} MB  " +
                      $"{s.totalTime.TotalSeconds:0}s  errors={s.totalErrors} warnings={s.totalWarnings}");

            if (s.result != BuildResult.Succeeded && Application.isBatchMode)
                EditorApplication.Exit(1);
        }
    }
}
