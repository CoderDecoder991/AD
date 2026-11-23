using System.Linq;
using AirDefense.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AirDefense.EditorTools
{
    /// <summary>
    /// Milestone 0 setup. Applies the Android player settings from
    /// docs/TECHNICAL_ARCHITECTURE.md and builds the device-test scene.
    /// Safe to run repeatedly. Menu: AirDefense ▸ Setup ▸ Apply Android Settings.
    /// Command line: -executeMethod AirDefense.EditorTools.ProjectSetup.RunAll
    /// </summary>
    public static class ProjectSetup
    {
        public const string DeviceTestScenePath = "Assets/_Project/Scenes/M0_DeviceTest.unity";

        // PLACEHOLDER: the package name becomes permanent once uploaded to Google Play.
        // Replace before the first Play upload (see DECISIONS_AND_ASSUMPTIONS.md, O-11).
        public const string PlaceholderPackageName = "com.placeholder.airdefense";

        // R-12: render below native resolution on the Pad 8 (3200x2136).
        public const float MobileRenderScale = 0.7f;

        public static void RunAll()
        {
            ApplyAndroidSettings();
            CreateDeviceTestScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectSetup] M0 setup complete.");
        }

        [MenuItem("AirDefense/Setup/Apply Android Settings")]
        public static void ApplyAndroidSettings()
        {
            var android = NamedBuildTarget.Android;

            PlayerSettings.companyName = "AirDefense";
            PlayerSettings.productName = "AirDefense";
            PlayerSettings.SetApplicationIdentifier(android, PlaceholderPackageName);
            PlayerSettings.bundleVersion = "0.0.1";
            PlayerSettings.Android.bundleVersionCode = 1;

            // R-10: IL2CPP, ARM64 only, minSdk 26, target = latest installed.
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            // R-02: Vulkan first, OpenGL ES 3 fallback.
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,
                new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });

            // Landscape only (PRD §6).
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            PlayerSettings.Android.optimizedFramePacing = true;

            SetMobileRenderScale();

            Debug.Log($"[ProjectSetup] Android settings applied (package {PlaceholderPackageName}, placeholder).");
        }

        /// <summary>Sets render scale on every URP asset used by a quality level whose name suggests mobile.</summary>
        private static void SetMobileRenderScale()
        {
            int levels = QualitySettings.names.Length;
            for (int i = 0; i < levels; i++)
            {
                var rp = QualitySettings.GetRenderPipelineAssetAt(i) as UniversalRenderPipelineAsset;
                if (rp == null) continue;
                if (!QualitySettings.names[i].ToLowerInvariant().Contains("mobile") &&
                    !rp.name.ToLowerInvariant().Contains("mobile")) continue;

                rp.renderScale = MobileRenderScale;
                EditorUtility.SetDirty(rp);
                Debug.Log($"[ProjectSetup] Render scale {MobileRenderScale} on '{rp.name}' (quality '{QualitySettings.names[i]}').");
            }
        }

        [MenuItem("AirDefense/Setup/Create Device Test Scene")]
        public static void CreateDeviceTestScene()
        {
            System.IO.Directory.CreateDirectory("Assets/_Project/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.SetPositionAndRotation(new Vector3(0f, 12f, -28f), Quaternion.Euler(20f, 0f, 0f));
                cam.backgroundColor = new Color(0.05f, 0.07f, 0.1f);
            }

            // A modest, steady load: a 20x20 grid of spinning lit cubes on a ground plane.
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(6f, 1f, 6f);

            var root = new GameObject("CubeGrid");
            for (int x = 0; x < 20; x++)
            for (int z = 0; z < 20; z++)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(root.transform);
                cube.transform.position = new Vector3((x - 9.5f) * 2f, 1f, (z - 9.5f) * 2f);
                var spin = cube.AddComponent<Spin>();
                spin.degreesPerSecond = new Vector3(0f, 30f + (x * 7 + z * 3) % 60, 0f);
                Object.DestroyImmediate(cube.GetComponent<BoxCollider>());
            }

            EditorSceneManager.SaveScene(scene, DeviceTestScenePath);

            var scenes = EditorBuildSettings.scenes.Where(s => s.path != DeviceTestScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(DeviceTestScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            Debug.Log($"[ProjectSetup] Saved {DeviceTestScenePath} and set it as build scene 0.");
        }
    }
}
