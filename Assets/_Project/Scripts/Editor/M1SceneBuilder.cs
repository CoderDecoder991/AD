using System.Collections.Generic;
using System.IO;
using System.Linq;
using AirDefense.Core;
using AirDefense.Data;
using AirDefense.Missions;
using AirDefense.Threats;
using AirDefense.UI;
using AirDefense.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace AirDefense.EditorTools
{
    /// <summary>
    /// Generates the Milestone 1 greybox: placeholder materials, data assets
    /// (weapon, threats, mission) and the playable scene. Safe to rerun.
    /// Data assets are only created if missing, so tuning done in the editor is kept.
    /// Menu: AirDefense ▸ Setup ▸ Build M1 Greybox Scene
    /// CLI:  -executeMethod AirDefense.EditorTools.M1SceneBuilder.Build
    /// </summary>
    public static class M1SceneBuilder
    {
        private const string Root = "Assets/_Project";
        private const string DataDir = Root + "/Data";
        private const string MatDir = Root + "/Art/Materials/Placeholder";
        public const string ScenePath = Root + "/Scenes/M1_Greybox.unity";

        [MenuItem("AirDefense/Setup/Build M1 Greybox Scene")]
        public static void Build()
        {
            Directory.CreateDirectory(DataDir + "/Weapons");
            Directory.CreateDirectory(DataDir + "/Threats");
            Directory.CreateDirectory(DataDir + "/Missions");
            Directory.CreateDirectory(MatDir);
            Directory.CreateDirectory(Root + "/Scenes");
            AssetDatabase.Refresh();

            var mats = CreateMaterials();
            var visuals = CreateVisuals(mats);
            var zu = CreateZu23();
            var small = CreateSmallDrone();
            var wing = CreateFixedWing();
            var mission = CreateMission(small, wing);
            var economy = LoadOrCreate<Progression.EconomyConfig>($"{DataDir}/EconomyConfig.asset", out _);
            var cram = CreateLockedStub("Weapon_CRAM", "cram", "C-RAM", 4000);
            var tunguska = CreateLockedStub("Weapon_Tunguska", "tunguska", "2K22 Tunguska", 10000);
            var roster = LoadOrCreate<Progression.WeaponRoster>($"{DataDir}/WeaponRoster.asset", out _);
            roster.weapons = new List<WeaponDefinition> { zu, cram, tunguska }; // tile order + unlock sequence (R-04)
            EditorUtility.SetDirty(roster);
            AssetDatabase.SaveAssets();

            BuildScene(mats, visuals, zu, mission, economy, roster);
            Debug.Log("[M1SceneBuilder] M1 greybox scene built: " + ScenePath);
        }

        // ------------------------------------------------------------ materials

        private static Dictionary<string, Material> CreateMaterials()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            return new Dictionary<string, Material>
            {
                ["Ground"] = Mat("PH_Ground", lit, new Color(0.42f, 0.40f, 0.33f)),
                ["Concrete"] = Mat("PH_Concrete", lit, new Color(0.62f, 0.62f, 0.60f)),
                ["Building"] = Mat("PH_Building", lit, new Color(0.50f, 0.52f, 0.46f)),
                ["Roof"] = Mat("PH_Roof", lit, new Color(0.30f, 0.33f, 0.30f)),
                ["Sandbag"] = Mat("PH_Sandbag", lit, new Color(0.58f, 0.52f, 0.40f)),
                ["Gun"] = Mat("PH_GunOlive", lit, new Color(0.28f, 0.32f, 0.22f)),
                ["GunDark"] = Mat("PH_GunDark", lit, new Color(0.12f, 0.12f, 0.11f)),
                ["Fence"] = Mat("PH_Fence", lit, new Color(0.35f, 0.35f, 0.35f)),
                ["ThreatBody"] = Mat("PH_ThreatBody", lit, new Color(0.18f, 0.18f, 0.20f)),
                ["ThreatAccent"] = Mat("PH_ThreatAccent", lit, new Color(0.55f, 0.55f, 0.58f)),
                ["Tracer"] = Mat("PH_Tracer", unlit, new Color(1f, 0.78f, 0.35f)),
                ["Explosion"] = Mat("PH_Explosion", unlit, new Color(1f, 0.55f, 0.15f)),
                ["Impact"] = Mat("PH_Impact", unlit, new Color(1f, 0.25f, 0.1f)),
            };
        }

        private static Material Mat(string name, Shader shader, Color color)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = shader;
            m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static PrototypeVisuals CreateVisuals(Dictionary<string, Material> m)
        {
            var v = LoadOrCreate<PrototypeVisuals>($"{DataDir}/PrototypeVisuals.asset", out _);
            v.threatBody = m["ThreatBody"];
            v.threatAccent = m["ThreatAccent"];
            v.tracer = m["Tracer"];
            v.explosion = m["Explosion"];
            v.impactFlash = m["Impact"];
            v.cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            v.sphere = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            v.cylinder = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
            v.capsule = Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
            EditorUtility.SetDirty(v);
            return v;
        }

        // ----------------------------------------------------------------- data
        // Starting values from docs/WEAPON_SYSTEMS.md and docs/GDD.md (fictional balance values).

        private static WeaponDefinition CreateZu23()
        {
            var w = LoadOrCreate<WeaponDefinition>($"{DataDir}/Weapons/Weapon_ZU23.asset", out bool created);
            if (!created) return w;
            w.weaponId = "zu23";
            w.displayName = "ZU-23-2";
            w.unlockCost = 0;
            w.traverseSpeed = 60f;
            w.elevationSpeed = 45f;
            w.minElevation = -5f;
            w.maxElevation = 85f;
            w.fireRate = 30f;
            w.barrels = 2;
            w.magazineSize = 100;
            w.reloadTime = 4f;
            w.muzzleVelocity = 900f;
            w.projectileLifetime = 2.2f;
            w.damagePerRound = 1f;
            w.dispersion = 0.35f;
            w.tracerEvery = 2;
            w.heatPerSecond = 0.2f;
            w.coolPerSecond = 0.15f;
            w.overheatRecoverBelow = 0.3f;
            w.minRange = 50f;
            w.maxRange = 1500f;
            EditorUtility.SetDirty(w);
            return w;
        }

        private static ThreatDefinition CreateSmallDrone()
        {
            var t = LoadOrCreate<ThreatDefinition>($"{DataDir}/Threats/Threat_SmallDrone.asset", out bool created);
            if (!created) return t;
            t.threatId = "small_drone";
            t.displayName = "Small drone";
            t.threatClass = ThreatClass.SmallDrone;
            t.movement = ThreatMovement.WeaveAndDive;
            t.speed = 28f;
            t.cruiseAltitude = 50f;
            t.diveDistance = 120f;
            t.weaveAmplitude = 30f;
            t.weaveFrequency = 0.12f;
            t.health = 3f;
            t.hitRadius = 3f;
            t.impactDamage = 8f;
            t.interceptCredits = 10;
            t.radarColor = new Color(1f, 0.8f, 0.25f);
            t.visualScale = 1.6f;
            EditorUtility.SetDirty(t);
            return t;
        }

        private static ThreatDefinition CreateFixedWing()
        {
            var t = LoadOrCreate<ThreatDefinition>($"{DataDir}/Threats/Threat_FixedWingDrone.asset", out bool created);
            if (!created) return t;
            t.threatId = "fixed_wing_drone";
            t.displayName = "Fixed-wing drone";
            t.threatClass = ThreatClass.FixedWingDrone;
            t.movement = ThreatMovement.StraightRunAndDive;
            t.speed = 50f;
            t.cruiseAltitude = 140f;
            t.diveDistance = 450f;
            t.health = 8f;
            t.hitRadius = 4.5f;
            t.impactDamage = 20f;
            t.interceptCredits = 25;
            t.radarColor = new Color(1f, 0.45f, 0.3f);
            t.visualScale = 1.4f;
            EditorUtility.SetDirty(t);
            return t;
        }

        private static MissionDefinition CreateMission(ThreatDefinition small, ThreatDefinition wing)
        {
            var m = LoadOrCreate<MissionDefinition>($"{DataDir}/Missions/Mission_M1_Greybox.asset", out bool created);
            if (!created) return m;
            m.missionId = "m1_greybox";
            m.title = "Night Watch (greybox)";
            m.briefing =
                "Radar reports small drones approaching the installation.\n\n" +
                "You are on the ZU-23-2. Drag on the right side of the screen to aim, hold FIRE to shoot. " +
                "Brackets mark targets on screen; arrows at the edge point to targets you can't see.\n\n" +
                "Moving targets need leading: aim at the yellow ring.\n\n" +
                "Objective: survive three waves with the installation intact.";
            m.installationHealth = 100f;
            m.seed = 1234;
            Vector2 smallDist = new Vector2(1200f, 1500f);
            Vector2 wingDist = new Vector2(2200f, 2600f);
            m.waves = new List<WaveDefinition>
            {
                new WaveDefinition
                {
                    label = "Wave 1", preDelay = 3f,
                    groups = { new SpawnGroup { threat = small, count = 3, interval = 4f, bearingRange = new Vector2(-20f, 20f), distanceRange = smallDist } }
                },
                new WaveDefinition
                {
                    label = "Wave 2", preDelay = 5f,
                    groups =
                    {
                        new SpawnGroup { threat = small, count = 4, interval = 3f, bearingRange = new Vector2(300f, 350f), distanceRange = smallDist },
                        new SpawnGroup { threat = wing, count = 2, startDelay = 6f, interval = 6f, bearingRange = new Vector2(30f, 60f), distanceRange = wingDist }
                    }
                },
                new WaveDefinition
                {
                    label = "Wave 3", preDelay = 6f,
                    groups =
                    {
                        new SpawnGroup { threat = small, count = 5, interval = 2f, bearingRange = new Vector2(0f, 90f), distanceRange = smallDist },
                        new SpawnGroup { threat = wing, count = 3, startDelay = 4f, interval = 5f, bearingRange = new Vector2(270f, 330f), distanceRange = wingDist },
                        new SpawnGroup { threat = small, count = 3, startDelay = 10f, interval = 3f, bearingRange = new Vector2(120f, 160f), distanceRange = smallDist }
                    }
                }
            };
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>
        /// Locked weapon used for tiles and unlock progress only. Its gameplay values are
        /// filled in when the weapon is built (C-RAM in M3, Tunguska in M4).
        /// </summary>
        private static WeaponDefinition CreateLockedStub(string file, string id, string name, int cost)
        {
            var w = LoadOrCreate<WeaponDefinition>($"{DataDir}/Weapons/{file}.asset", out bool created);
            if (!created) return w;
            w.weaponId = id;
            w.displayName = name;
            w.unlockCost = cost;
            EditorUtility.SetDirty(w);
            return w;
        }

        private static T LoadOrCreate<T>(string path, out bool created) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            created = a == null;
            if (created)
            {
                a = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(a, path);
            }
            return a;
        }

        // ---------------------------------------------------------------- scene

        private static void BuildScene(Dictionary<string, Material> m, PrototypeVisuals visuals, WeaponDefinition zu, MissionDefinition mission,
                                       Progression.EconomyConfig economy, Progression.WeaponRoster roster)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Lighting: late-afternoon sun (night lighting arrives with real art in M2).
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.2f;
            sun.color = new Color(1f, 0.93f, 0.82f);
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(32f, 150f, 0f);
            RenderSettings.ambientMode = AmbientMode.Skybox;

            var env = new GameObject("Environment (PLACEHOLDER)").transform;
            var center = new GameObject("InstallationCenter").transform;
            center.SetParent(env, false);

            Prim(env, "Ground", PrimitiveType.Plane, new Vector3(0, 0, 0), new Vector3(500, 1, 500), m["Ground"], false);
            Prim(env, "Apron", PrimitiveType.Cube, new Vector3(0, 0.02f, 10), new Vector3(150, 0.04f, 130), m["Concrete"], false);

            // Buildings; their roofs are the threats' target points.
            var targets = new List<Transform>();
            void Building(string n, Vector3 pos, Vector3 size)
            {
                var b = Prim(env, n, PrimitiveType.Cube, pos + Vector3.up * size.y / 2f, size, m["Building"], true);
                Prim(b.transform, "Roof", PrimitiveType.Cube, new Vector3(0, 0.5f, 0), new Vector3(1.04f, 0.04f / size.y, 1.04f), m["Roof"], true);
                var tp = new GameObject(n + "_Target").transform;
                tp.SetParent(env, false);
                tp.position = pos + Vector3.up * (size.y + 0.5f);
                targets.Add(tp);
            }
            Building("Command", new Vector3(0, 0, 25), new Vector3(24, 8, 16));
            Building("Barracks_A", new Vector3(-40, 0, 30), new Vector3(30, 6, 12));
            Building("Barracks_B", new Vector3(40, 0, 30), new Vector3(30, 6, 12));
            Building("Hangar", new Vector3(-35, 0, -10), new Vector3(28, 12, 26));
            Building("Depot", new Vector3(38, 0, -8), new Vector3(20, 7, 20));

            for (int i = 0; i < 3; i++)
                Prim(env, "FuelTank_" + i, PrimitiveType.Cylinder, new Vector3(62 + i * 9, 4, -45), new Vector3(7, 4, 7), m["Concrete"], true);
            var mast = Prim(env, "RadarMast", PrimitiveType.Cylinder, new Vector3(-8, 10, 45), new Vector3(1, 10, 1), m["Fence"], true);
            Prim(mast.transform, "Dish", PrimitiveType.Cube, new Vector3(0, 1.05f, 0), new Vector3(8, 0.15f, 2.5f), m["GunDark"], true);

            // Perimeter fence (square, 180 m side).
            for (int side = 0; side < 4; side++)
            {
                Quaternion r = Quaternion.Euler(0, side * 90f, 0);
                var f = Prim(env, "Fence_" + side, PrimitiveType.Cube, r * new Vector3(0, 1.2f, 90), new Vector3(180, 2.4f, 0.2f), m["Fence"], false);
                f.transform.rotation = r;
            }

            // Gun position south of the buildings on a 14 m earth berm, so the
            // operator sees over the rooftops (buildings are 6-12 m tall). Facing north.
            const float bermHeight = 14f;
            Prim(env, "GunBerm", PrimitiveType.Cube, new Vector3(0, bermHeight / 2f, -60), new Vector3(16, bermHeight, 16), m["Sandbag"], true);
            var gunRoot = new GameObject("ZU-23-2").transform;
            gunRoot.position = new Vector3(0, bermHeight, -60);
            Prim(gunRoot, "Platform", PrimitiveType.Cylinder, new Vector3(0, 0.1f, 0), new Vector3(7, 0.1f, 7), m["Concrete"], false);
            for (int i = 0; i < 20; i++)
            {
                float a = i / 20f * Mathf.PI * 2f;
                if (Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, 0f)) < 25f) continue; // gap at the front for low shots
                var bag = Prim(gunRoot, "Sandbag", PrimitiveType.Cube, new Vector3(Mathf.Sin(a) * 4.4f, 0.45f, Mathf.Cos(a) * 4.4f), new Vector3(1.4f, 0.9f, 0.7f), m["Sandbag"], false);
                bag.transform.rotation = Quaternion.Euler(0, a * Mathf.Rad2Deg, 0);
            }

            Transform yaw, pitch, camAnchor;
            List<Transform> muzzles;
            if (!TryBuildZu23Model(gunRoot, out yaw, out pitch, out muzzles, out camAnchor))
                BuildGreyboxGun(gunRoot, m, out yaw, out pitch, out muzzles, out camAnchor);

            var mount = gunRoot.gameObject.AddComponent<GunMount>();
            mount.definition = zu;
            mount.yawPivot = yaw;
            mount.pitchPivot = pitch;
            mount.muzzles = muzzles.ToArray();
            mount.cameraAnchor = camAnchor;

            // Camera.
            var camGo = new GameObject("OperatorCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 6000f;
            camGo.AddComponent<AudioListener>();
            camGo.transform.SetPositionAndRotation(camAnchor.position, camAnchor.rotation);

            // Systems.
            var systems = new GameObject("Systems").transform;
            var threatMgr = new GameObject("ThreatManager").AddComponent<ThreatManager>();
            threatMgr.transform.SetParent(systems, false);
            threatMgr.visuals = visuals;
            threatMgr.installationCenter = center;
            threatMgr.targetPoints = targets.ToArray();

            var proj = new GameObject("ProjectileSystem").AddComponent<ProjectileSystem>();
            proj.transform.SetParent(systems, false);
            proj.visuals = visuals;
            proj.threats = threatMgr;

            var inputGo = new GameObject("PlayerInput");
            inputGo.transform.SetParent(systems, false);
            var input = inputGo.AddComponent<PlayerInputState>();

            var ctrl = gunRoot.gameObject.AddComponent<ManualWeaponController>();
            ctrl.mount = mount;
            ctrl.projectiles = proj;
            ctrl.input = input;
            ctrl.operatorCamera = cam;

            var director = new GameObject("MissionDirector").AddComponent<MissionDirector>();
            director.transform.SetParent(systems, false);
            director.mission = mission;
            director.economy = economy;
            director.threats = threatMgr;
            director.projectiles = proj;
            director.player = ctrl;

            var audio = new GameObject("Audio (procedural placeholder)").AddComponent<ProceduralAudio>();
            audio.transform.SetParent(systems, false);

            var hud = new GameObject("HUD").AddComponent<HudController>();
            hud.director = director;
            hud.player = ctrl;
            hud.input = input;
            hud.threats = threatMgr;
            hud.cam = cam;
            hud.roster = roster;

            EditorSceneManager.SaveScene(scene, ScenePath);

            // M1 scene first in the build; keep the M0 device test after it.
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ------------------------------------------------------------- gun rigs

        public const string Zu23ModelPath = "Assets/_Project/Art/Models/ZU23/ZU23.gltf";

        /// <summary>
        /// Rigs the processed ZU-23-2 model (tools/blender/process_zu23.py) onto
        /// yaw/pitch pivots placed at the model's own pivot nodes. Model: CC-BY-4.0,
        /// LuckySkink9661 (Sketchfab), modified. See docs/ASSET_REGISTER.md M-01.
        /// </summary>
        private static bool TryBuildZu23Model(Transform gunRoot, out Transform yaw, out Transform pitch,
                                              out List<Transform> muzzles, out Transform camAnchor)
        {
            yaw = pitch = camAnchor = null;
            muzzles = null;
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Zu23ModelPath);
            if (asset == null)
            {
                Debug.LogWarning("[M1SceneBuilder] ZU-23-2 model not found; using greybox gun.");
                return false;
            }

            var model = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.name = "ZU-23-2 model (CC-BY LuckySkink9661)";
            model.transform.SetParent(gunRoot, false);

            Transform Find(string n) => model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == n);
            // glTFast merges a single root node into the asset root (renamed after the file),
            // so the base may be the model root itself.
            Transform zBase = Find("ZU_Base") ?? model.transform, zYaw = Find("ZU_Yaw"), zPitch = Find("ZU_Pitch");
            Transform mL = Find("Muzzle_L"), mR = Find("Muzzle_R");
            if (zBase == null || zYaw == null || zPitch == null || mL == null || mR == null)
            {
                Debug.LogError("[M1SceneBuilder] ZU-23-2 model is missing rig nodes; using greybox gun.");
                Object.DestroyImmediate(model);
                return false;
            }

            // Turn the whole model so the barrels point north (+Z), whatever axis the import used.
            Vector3 bore = (mL.position + mR.position) * 0.5f - zPitch.position;
            bore.y = 0f;
            model.transform.rotation = Quaternion.FromToRotation(bore.normalized, Vector3.forward) * model.transform.rotation;

            yaw = new GameObject("YawPivot").transform;
            yaw.SetParent(gunRoot, false);
            yaw.position = zYaw.position;
            yaw.rotation = Quaternion.identity;

            pitch = new GameObject("PitchPivot").transform;
            pitch.SetParent(yaw, true);
            pitch.position = zPitch.position;
            pitch.rotation = Quaternion.identity;

            zYaw.SetParent(yaw, true);
            zPitch.SetParent(pitch, true);
            muzzles = new List<Transform> { mL, mR };

            // Operator view "B" (tools/blender/preview_operator_view.py): above the sight, behind the breech.
            camAnchor = new GameObject("OperatorCameraAnchor").transform;
            camAnchor.SetParent(pitch, false);
            camAnchor.localPosition = new Vector3(0f, 0.95f, -1.1f);

            foreach (var r in model.GetComponentsInChildren<MeshRenderer>(true))
                r.shadowCastingMode = ShadowCastingMode.On;
            return true;
        }

        private static void BuildGreyboxGun(Transform gunRoot, Dictionary<string, Material> m, out Transform yaw,
                                            out Transform pitch, out List<Transform> muzzles, out Transform camAnchor)
        {
            yaw = new GameObject("YawPivot").transform;
            yaw.SetParent(gunRoot, false);
            yaw.localPosition = new Vector3(0, 0.9f, 0);
            Prim(yaw, "Carriage", PrimitiveType.Cube, new Vector3(0, 0.15f, 0), new Vector3(1.8f, 0.3f, 2.2f), m["Gun"], true);
            Prim(yaw, "Seat_L", PrimitiveType.Cube, new Vector3(-0.75f, 0.55f, -0.7f), new Vector3(0.45f, 0.12f, 0.45f), m["GunDark"], true);
            Prim(yaw, "Seat_R", PrimitiveType.Cube, new Vector3(0.75f, 0.55f, -0.7f), new Vector3(0.45f, 0.12f, 0.45f), m["GunDark"], true);
            Prim(yaw, "Cradle_L", PrimitiveType.Cube, new Vector3(-0.45f, 0.65f, 0.1f), new Vector3(0.12f, 0.8f, 0.5f), m["Gun"], true);
            Prim(yaw, "Cradle_R", PrimitiveType.Cube, new Vector3(0.45f, 0.65f, 0.1f), new Vector3(0.12f, 0.8f, 0.5f), m["Gun"], true);

            pitch = new GameObject("PitchPivot").transform;
            pitch.SetParent(yaw, false);
            pitch.localPosition = new Vector3(0, 0.95f, 0.1f);
            muzzles = new List<Transform>();
            foreach (float x in new[] { -0.22f, 0.22f })
            {
                Prim(pitch, "Receiver", PrimitiveType.Cube, new Vector3(x, 0, -0.1f), new Vector3(0.22f, 0.26f, 1.1f), m["GunDark"], true);
                var barrel = Prim(pitch, "Barrel", PrimitiveType.Cylinder, new Vector3(x, 0, 1.45f), new Vector3(0.07f, 1.1f, 0.07f), m["GunDark"], true);
                barrel.transform.localRotation = Quaternion.Euler(90, 0, 0);
                Prim(pitch, "Magazine", PrimitiveType.Cube, new Vector3(x * 2.1f, 0.05f, -0.15f), new Vector3(0.18f, 0.3f, 0.5f), m["Gun"], true);
                var mz = new GameObject("Muzzle").transform;
                mz.SetParent(pitch, false);
                mz.localPosition = new Vector3(x, 0, 2.6f);
                muzzles.Add(mz);
            }
            camAnchor = new GameObject("OperatorCameraAnchor").transform;
            camAnchor.SetParent(pitch, false);
            camAnchor.localPosition = new Vector3(0f, 0.62f, -0.75f);
        }

        private static GameObject Prim(Transform parent, string name, PrimitiveType type, Vector3 localPos, Vector3 scale, Material mat, bool castShadows)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }
    }
}
