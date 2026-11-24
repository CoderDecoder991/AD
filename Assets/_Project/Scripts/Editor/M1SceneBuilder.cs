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
            NightBaseDresser.UpgradeMaterials(mats); // CC0 Poly Haven textures when present (M2)
            var visuals = CreateVisuals(mats);
            var zu = CreateZu23();
            var small = CreateSmallDrone();
            var wing = CreateFixedWing();
            var mission = CreateMission(small, wing);
            var economy = LoadOrCreate<Progression.EconomyConfig>($"{DataDir}/EconomyConfig.asset", out _);
            var cram = CreateCram(CreateLockedStub("Weapon_CRAM", "cram", "C-RAM", 4000));
            var tunguska = CreateLockedStub("Weapon_Tunguska", "tunguska", "2K22 Tunguska", 10000);
            var roster = LoadOrCreate<Progression.WeaponRoster>($"{DataDir}/WeaponRoster.asset", out _);
            roster.weapons = new List<WeaponDefinition> { zu, cram, tunguska }; // tile order + unlock sequence (R-04)
            EditorUtility.SetDirty(roster);
            AssetDatabase.SaveAssets();

            RenderQualitySetup.Apply(); // MSAA, FSR, soft shadows, SSAO (visual-clarity pass)
            BuildScene(mats, visuals, zu, cram, mission, economy, roster);
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
                // Emissive FX colours are above 1.0 (HDR) so bloom makes them glow at night.
                ["Tracer"] = Mat("PH_Tracer", unlit, new Color(2.6f, 1.9f, 0.8f)),
                ["Explosion"] = Mat("PH_Explosion", unlit, new Color(3.0f, 1.5f, 0.4f)),
                ["Impact"] = Mat("PH_Impact", unlit, new Color(3f, 0.8f, 0.3f)),
                ["NavLight"] = Mat("PH_NavLight", unlit, new Color(4f, 0.25f, 0.2f)),
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
            v.navLight = m["NavLight"];
            // Tracer: glowing streak texture, additive, HDR red-orange (Soviet-style red tracer) so bloom makes it glow.
            var tracerMat = NightBaseDresser.GlowMaterial("TracerStreak", GeneratedTexture("TracerStreak.png", () => MuzzleFlashFX.MakeTracerTexture()), new Color(3.4f, 1.3f, 0.55f, 1f));
            v.tracer = tracerMat;
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
            w.muzzleVelocity = 970f; // 23x152 HEI-T spec
            w.projectileLifetime = 2.2f;
            w.damagePerRound = 1f;
            w.dispersion = 0.35f;
            w.tracerEvery = 1; // ZU-23 belts are mostly tracer rounds; both barrels show
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
            // Text is refreshed every build (it describes the controls); balance values are kept once tuned.
            m.title = "Night Watch";
            m.briefing =
                "Radar reports small drones approaching the installation.\n\n" +
                "You are on the ZU-23-2. Drag on the left side of the screen to aim, hold FIRE with your right thumb. " +
                "Brackets mark targets on screen; arrows at the edge point to targets you can't see.\n\n" +
                "Moving targets need leading: aim at the amber ring.\n\n" +
                "Objective: survive three waves with the installation intact.";
            EditorUtility.SetDirty(m);
            if (!created) return m;
            m.missionId = "m1_greybox";
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
        /// <summary>
        /// C-RAM (Phalanx-type 20 mm six-barrel rotary gun). FICTIONAL GAME-BALANCE VALUES informed by public
        /// sources (docs/WEAPON_SYSTEMS.md section 7): 4,500 rounds/min, about 115 deg/s slew, -25..+85 deg
        /// elevation, 1,550-round magazine, rounds that self-destruct at about 2 km (tracer burnout).
        /// Filled in once (when the asset still holds the M2 stub defaults), then tunable in the editor.
        /// </summary>
        private static WeaponDefinition CreateCram(WeaponDefinition w)
        {
            if (w.tracerWidthScale == 1f)
            {
                // M3a look pass (user: "more the tracer stream and the muzzle flash"): night video shows one
                // bright continuous stream and a flame that never goes out while the gun fires.
                w.tracerWidthScale = 1.7f;
                w.tracerLengthScale = 1.6f;   // 18 m streaks x 1.6 > 15 m round spacing: a continuous stream
                w.flashEvery = 1;
                EditorUtility.SetDirty(w);
            }
            if (w.fireRate >= 50f) return w; // already configured
            w.traverseSpeed = 115f;
            w.elevationSpeed = 115f;
            w.minElevation = -25f;
            w.maxElevation = 85f;
            w.fireRate = 75f;          // 4,500 rounds/min
            w.barrels = 6;
            w.spinUpTime = 0.3f;
            w.magazineSize = 1550;
            w.reloadTime = 15f;        // game value (a real reload takes far longer)
            w.muzzleVelocity = 1100f;  // 20 mm M940 class
            w.dragK = 5.5e-4f;         // lighter 20 mm round slows faster than the 23 mm
            w.tracerBurnTime = 3.5f;   // M940 self-destructs when its tracer burns out
            w.selfDestructTime = new Vector2(3.3f, 3.8f); // about 2.0-2.2 km down range
            w.projectileLifetime = 3.8f;
            w.damagePerRound = 0.6f;
            w.dispersion = 0.12f;
            w.tracerEvery = 1;
            w.flashEvery = 2;
            w.airburstScale = 0.7f;
            w.airburstSmokeChance = 0.15f;
            w.heatPerSecond = 0.07f;   // about 14 s of continuous fire
            w.coolPerSecond = 0.1f;
            w.overheatRecoverBelow = 0.3f;
            w.barrelRecoilDistance = 0f;
            w.cameraKick = 0.05f;
            w.cameraShake = 0.08f;
            w.muzzleClimbPerRound = 0.002f;
            w.muzzleDriftPerRound = 0.004f;
            w.minRange = 30f;
            w.maxRange = 2000f;
            EditorUtility.SetDirty(w);
            return w;
        }

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

        private static void BuildScene(Dictionary<string, Material> m, PrototypeVisuals visuals, WeaponDefinition zu, WeaponDefinition cramDef, MissionDefinition mission,
                                       Progression.EconomyConfig economy, Progression.WeaponRoster roster)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Night lighting: moon, CC0 night-sky HDRI, trilight ambient, fog (M2).
            var moon = new GameObject("Moon").AddComponent<Light>();
            NightBaseDresser.NightLighting(moon);

            var env = new GameObject("Environment").transform;
            var center = new GameObject("InstallationCenter").transform;
            center.SetParent(env, false);

            Prim(env, "Ground", PrimitiveType.Plane, new Vector3(0, 0, 0), new Vector3(500, 1, 500), m["Ground"], false);
            // Visual-clarity pass: gravel-concrete apron, roads, detailed facades, clutter, desert scatter (BaseDetailer).
            var pal = BaseDetailer.MakePalette(m["Concrete"], m["GunDark"]);
            NightBaseDresser.Box(env, "Apron", new Vector3(0, 0.02f, 10), new Vector3(150, 0.04f, 130), BaseDetailer.Surface("Apron", "gravel_concrete", 0.2f), 4f);

            // Buildings (world-UV boxes so textures keep real scale); roofs are the threats' target points.
            var targets = new List<Transform>();
            void Building(string n, Vector3 pos, Vector3 size, BaseDetailer.Style style = BaseDetailer.Style.Office, Material wall = null)
            {
                var b = NightBaseDresser.Box(env, n, pos + Vector3.up * size.y / 2f, size, wall != null ? wall : m["Building"], 4f);
                BaseDetailer.DetailBuilding(env, n, pos, size, style, pal, n.GetHashCode());
                b.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
                NightBaseDresser.Box(env, n + "_Roof", pos + Vector3.up * (size.y + 0.15f), new Vector3(size.x * 1.04f, 0.3f, size.z * 1.04f), m["Roof"], 2.5f);
                var tp = new GameObject(n + "_Target").transform;
                tp.SetParent(env, false);
                tp.position = pos + Vector3.up * (size.y + 0.5f);
                targets.Add(tp);
            }
            Building("Command", new Vector3(0, 0, 25), new Vector3(24, 8, 16), BaseDetailer.Style.Office, pal.plaster);
            Building("Barracks_A", new Vector3(-40, 0, 30), new Vector3(30, 6, 12), BaseDetailer.Style.Office, pal.plaster);
            Building("Barracks_B", new Vector3(40, 0, 30), new Vector3(30, 6, 12), BaseDetailer.Style.Office, pal.plaster);
            Building("Hangar", new Vector3(-35, 0, -10), new Vector3(28, 12, 26), BaseDetailer.Style.Hangar, pal.metalWall);
            Building("Depot", new Vector3(38, 0, -8), new Vector3(20, 7, 20), BaseDetailer.Style.Depot, pal.container);

            // Roads: north-south from the gun berm to Command, east-west between the barracks and the hangar/depot.
            BaseDetailer.Road(env, "Road_NS", new Vector3(0, 0, -38), new Vector3(0, 0, 15), 8f, pal, 0.06f); // from the berm foot
            BaseDetailer.Road(env, "Road_EW", new Vector3(-62, 0, 7), new Vector3(62, 0, 7), 6f, pal, 0.07f);
            foreach (float z in new[] { -34f, -18f, -2f }) BaseDetailer.StreetLamp(env, new Vector3(-5.2f, 0f, z), 90f);
            foreach (float x in new[] { -52f, -32f, 26f, 52f }) BaseDetailer.StreetLamp(env, new Vector3(x, 0f, 11f), 180f); // -32: clear of the C-RAM view
            BaseDetailer.Clutter(env);
            BaseDetailer.Crew(env, new Vector3(0f, 14f, -60f), new Vector3(-14f, 9.2f, -8f)); // ZU pit, C-RAM pad top
            BaseDetailer.TerrainScatter(env, 90f);

            for (int i = 0; i < 3; i++)
                Prim(env, "FuelTank_" + i, PrimitiveType.Cylinder, new Vector3(62 + i * 9, 4, -45), new Vector3(7, 4, 7), m["Concrete"], true);
            var mast = Prim(env, "RadarMast", PrimitiveType.Cylinder, new Vector3(-8, 10, 45), new Vector3(1, 10, 1), m["Fence"], true);
            // Dish as its own object (not under the stretched mast) so it can rotate without distorting.
            var radarDish = Prim(env, "RadarDish", PrimitiveType.Cube, new Vector3(-8, 20.6f, 45), new Vector3(8, 1.2f, 0.4f), m["GunDark"], true).transform;
            radarDish.gameObject.AddComponent<Spin>().degreesPerSecond = new Vector3(0f, 36f, 0f);
            GameObjectUtility.SetStaticEditorFlags(radarDish.gameObject, 0);

            // Red alarm beacons on the rooftops: flash once the base goes to alert.
            void Beacon(Vector3 p)
            {
                var b = new GameObject("AlarmBeacon").transform;
                b.SetParent(env, false);
                b.position = p;
                var bulb = new GameObject("Bulb");
                bulb.transform.SetParent(b, false);
                bulb.transform.localScale = Vector3.one * 0.45f;
                bulb.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
                var br = bulb.AddComponent<MeshRenderer>();
                br.sharedMaterial = m["NavLight"];
                br.shadowCastingMode = ShadowCastingMode.Off;
                var l = new GameObject("BeaconLight").AddComponent<Light>();
                l.transform.SetParent(b, false);
                l.type = LightType.Point;
                l.color = new Color(1f, 0.12f, 0.08f);
                l.range = 22f;
                l.intensity = 0f;
                l.shadows = LightShadows.None;
                var ab = b.gameObject.AddComponent<Environment.AlarmBeacon>();
                ab.lamp = l;
                ab.bulb = br;
            }
            Beacon(new Vector3(0f, 8.6f, 25f));
            Beacon(new Vector3(-40f, 6.6f, 30f));
            Beacon(new Vector3(-35f, 12.6f, -10f));
            Beacon(new Vector3(38f, 7.6f, -8f));

            // Perimeter fence (square, 180 m side): CC0 chain-link segments, greybox wall if missing.
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/Props/fence_segment/fence_segment.gltf") != null)
                NightBaseDresser.PerimeterFence(env, 90f);
            else
                for (int side = 0; side < 4; side++)
                {
                    Quaternion r = Quaternion.Euler(0, side * 90f, 0);
                    var f = Prim(env, "Fence_" + side, PrimitiveType.Cube, r * new Vector3(0, 1.2f, 90), new Vector3(180, 2.4f, 0.2f), m["Fence"], false);
                    f.transform.rotation = r;
                }

            // Gun position south of the buildings on a 14 m earth berm, so the
            // operator sees over the rooftops (buildings are 6-12 m tall). Facing north.
            const float bermHeight = 14f;
            var gunRoot = new GameObject("ZU-23-2").transform;
            gunRoot.position = new Vector3(0, bermHeight, -60);
            // VS-3: round earth mound with sloped sides and a sandbag revetment (tools/blender/make_zu_emplacement.py):
            // low on the threat side so the base stays visible, higher on the flanks, access gap at the rear.
            var bermAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/Emplacement/zu_berm.gltf");
            var revAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/Emplacement/zu_revetment.gltf");
            if (bermAsset != null && revAsset != null)
            {
                foreach (var asset in new[] { bermAsset, revAsset })
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, env);
                    go.transform.position = gunRoot.position;
                    // The berm casts shadows; the 486 low sandbags only receive them (their contact shading is baked).
                    foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
                        r.shadowCastingMode = asset == bermAsset ? ShadowCastingMode.On : ShadowCastingMode.Off;
                    GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
                }
            }
            else
            {
                NightBaseDresser.Box(env, "GunBerm", new Vector3(0, bermHeight / 2f, -60), new Vector3(16, bermHeight, 16), m["Sandbag"], 3f)
                    .GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
                for (int i = 0; i < 20; i++)
                {
                    float a = i / 20f * Mathf.PI * 2f;
                    if (Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, 0f)) < 25f) continue;
                    var bag = Prim(gunRoot, "Sandbag", PrimitiveType.Cube, new Vector3(Mathf.Sin(a) * 4.4f, 0.45f, Mathf.Cos(a) * 4.4f), new Vector3(1.4f, 0.9f, 0.7f), m["Sandbag"], false);
                    bag.transform.rotation = Quaternion.Euler(0, a * Mathf.Rad2Deg, 0);
                }
            }
            // Concrete gun pad on the gravel pit floor.
            var pad = Prim(gunRoot, "Platform", PrimitiveType.Cylinder, new Vector3(0, 0.06f, 0), new Vector3(5.2f, 0.06f, 5.2f), m["Concrete"], false);
            pad.GetComponent<MeshRenderer>().receiveShadows = true;

            Transform yaw, pitch, camAnchor;
            List<Transform> muzzles;
            Transform[] barrels = null;
            if (!TryBuildZu23Model(gunRoot, out yaw, out pitch, out muzzles, out camAnchor, out barrels))
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
            NightBaseDresser.PostProcessing(cam);

            // Props, practical lights and searchlights (CC0 Poly Haven).
            var beamMat = NightBaseDresser.BeamMaterial(); // VS-4: soft-edged beam shader (AirDefense/LightBeam)
            NightBaseDresser.DressBase(env, gunRoot, bermHeight, beamMat);
            GameObject cramModel = NightBaseDresser.PlaceDisplayWeapons(env, m["Concrete"], m["Sandbag"]);

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
            proj.viewCamera = cam;
            // Self-destruct bursts reuse the fireball sheet and soft smoke (realistic 23 mm HEI-T behaviour).
            // Airburst: round soft flash (white-hot, bloom) and a dark puff readable through the night fog.
            proj.burstFireMaterial = NightBaseDresser.GlowMaterial("SelfDestructFlash", NightBaseDresser.SoftDotAsset(), new Color(6f, 5f, 3.8f, 1f));
            proj.burstSmokeMaterial = NightBaseDresser.SkySmokeMaterial("SelfDestructSmoke", NightBaseDresser.SoftDotAsset());

            var inputGo = new GameObject("PlayerInput");
            inputGo.transform.SetParent(systems, false);
            var input = inputGo.AddComponent<PlayerInputState>();

            // ZU-23-2 station: recoil (per-barrel when the model has separate barrels) and muzzle flash.
            var zuStation = gunRoot.gameObject.AddComponent<WeaponStation>();
            // VS-3 framing: a little higher and further back so the weapon, the low front revetment and the base
            // below the horizon are all readable (VISUAL_PRODUCTION_STANDARD.md §7).
            zuStation.cameraDistance = 7.8f;
            zuStation.cameraHeight = 3.2f;
            zuStation.mount = mount;
            zuStation.projectiles = proj;
            var recoil = gunRoot.gameObject.AddComponent<GunRecoil>();
            recoil.mount = mount;
            recoil.barrels = barrels ?? new Transform[0];
            zuStation.recoil = recoil;
            var flash = gunRoot.gameObject.AddComponent<MuzzleFlashFX>();
            flash.mount = mount;
            flash.viewCamera = cam;
            // Turbulent fireball sheet (2x2 noise-generated shapes), additive, HDR so the core blooms.
            var fireMat = NightBaseDresser.AdditiveParticleMaterial("MuzzleFireball", GeneratedTexture("FireballSheet.png", () => MuzzleFlashFX.MakeFireballSheet()));
            fireMat.SetColor("_BaseColor", new Color(4.0f, 3.0f, 2.2f, 1f));
            flash.fireMaterial = fireMat;
            flash.smokeMaterial = NightBaseDresser.AlphaParticleMaterial("MuzzleSmoke", NightBaseDresser.SoftDotAsset());
            zuStation.muzzleFlash = flash;

            // C-RAM station on its own position, rigged from tools/blender/process_cram.py.
            WeaponStation cramStation = BuildCramStation(cramModel, cramDef, proj, cam, fireMat, flash.smokeMaterial);

            var ctrl = gunRoot.gameObject.AddComponent<ManualWeaponController>();
            ctrl.station = zuStation;
            ctrl.stations = cramStation != null ? new[] { zuStation, cramStation } : new[] { zuStation };
            ctrl.input = input;
            ctrl.operatorCamera = cam;

            var director = new GameObject("MissionDirector").AddComponent<MissionDirector>();
            director.transform.SetParent(systems, false);
            director.mission = mission;
            director.economy = economy;
            director.threats = threatMgr;
            director.projectiles = proj;
            director.player = ctrl;

            // Automatic fire control for whichever weapon the player is not operating (C-05).
            foreach (var st in ctrl.stations)
            {
                var auto = st.gameObject.AddComponent<AutonomousWeaponController>();
                auto.station = st;
                auto.threats = threatMgr;
                auto.director = director;
                auto.defendedCenter = center;
                if (st == zuStation)
                {
                    // Gun crew: slower to react, less exact, short bursts.
                    auto.reactionTime = 1.5f;
                    auto.fireTolerance = 1.2f;
                    auto.aimError = 0.5f;
                    auto.burstSeconds = 1.2f;
                    auto.burstPause = 0.8f;
                    auto.restPitch = 10f;
                }
                else
                {
                    // Radar fire control: tracks, decides, and fires long bursts on the intercept point.
                    auto.reactionTime = 0.8f;
                    auto.fireTolerance = 0.5f;
                    auto.aimError = 0f;
                    auto.burstSeconds = 2.5f;
                    auto.burstPause = 0.35f;
                    auto.restPitch = 0f;
                }
            }

            var bank = CreateAudioBank();
            if (bank != null)
            {
                var sfx = new GameObject("Audio").AddComponent<SfxPlayer>();
                sfx.transform.SetParent(systems, false);
                sfx.bank = bank;
                sfx.listener = camGo.transform;
            }
            else
            {
                var audio = new GameObject("Audio (procedural placeholder)").AddComponent<ProceduralAudio>();
                audio.transform.SetParent(systems, false);
            }

            var damage = new GameObject("DamageVisuals").AddComponent<Environment.DamageVisuals>();
            damage.transform.SetParent(systems, false);
            damage.particleMaterial = NightBaseDresser.AlphaParticleMaterial("SmokeFire", NightBaseDresser.SoftDotAsset());

            var hud = new GameObject("HUD").AddComponent<HudController>();
            hud.director = director;
            hud.player = ctrl;
            hud.input = input;
            hud.threats = threatMgr;
            hud.cam = cam;
            hud.roster = roster;
            hud.credits = CreditsText();

            // Opening cinematic (CINEMATIC_OPENING.md): plays on first launch, hands over to Mission 1.
            var cineGo = new GameObject("OpeningCinematic");
            cineGo.transform.SetParent(systems, false);
            var cine = cineGo.AddComponent<Cinematic.CinematicDirector>();
            cine.cam = cam;
            cine.player = ctrl;
            cine.director = director;
            cine.hud = hud;
            cine.projectiles = proj;
            cine.bank = bank;
            cine.radioSquelch = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxDir + "radio_squelch.wav");
            cine.navLightMaterial = m["NavLight"];
            cine.sphereMesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            cine.radarDish = radarDish;
            cine.gunRoot = gunRoot;
            Transform FindModel(string root, string child)
            {
                var r = GameObject.Find(root);
                if (r == null) return null;
                var c = r.transform.Find(child);
                return c != null ? c : r.transform;
            }
            cine.cram = cramModel != null ? cramModel.transform : null;
            cine.tunguska = FindModel("2K22 Tunguska (display until M4)", "Tunguska");

            EditorSceneManager.SaveScene(scene, ScenePath);

            // M1 scene first in the build; keep the M0 device test after it.
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>Saves a code-generated texture as a PNG asset once (needed so builds include it).</summary>
        private static Texture2D GeneratedTexture(string file, System.Func<Texture2D> make)
        {
            string path = "Assets/_Project/Art/Generated/" + file;
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, make().EncodeToPNG());
                AssetDatabase.ImportAsset(path);
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                ti.alphaIsTransparency = true;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ---------------------------------------------------------- audio + credits

        private const string SfxDir = Root + "/Audio/SFX/";

        /// <summary>Audio bank from Assets/_Project/Audio/SFX (built by tools/audio/make_sfx.py). Null if missing.</summary>
        private static AudioBank CreateAudioBank()
        {
            AudioClip C(string file) => AssetDatabase.LoadAssetAtPath<AudioClip>(SfxDir + file);
            if (C("zu23_shot_1.wav") == null) return null;
            var b = LoadOrCreate<AudioBank>($"{DataDir}/AudioBank.asset", out _);
            // Layered autocannon rounds (tools/audio/make_sfx.py zu_round); older pitched takes as fallback.
            b.zuShots = C("zu23_round_1.wav") != null
                ? new[] { C("zu23_round_1.wav"), C("zu23_round_2.wav"), C("zu23_round_3.wav"), C("zu23_round_4.wav") }
                : new[] { C("zu23_shot_1.wav"), C("zu23_shot_2.wav"), C("zu23_shot_3.wav") };
            b.zuBurstTail = C("zu23_tail.wav");
            // User's own Tunguska recording, pitched for the ZU (tools/audio/make_user_burst.py). Null = per-round fallback.
            b.zuFirstShot = C("zu23_first_shot.wav");
            b.zuBurstLoop = C("zu23_burst_loop.wav");
            // C-RAM: the same real shot laid at 75 rounds/s (tools/audio/make_cram_sound.py).
            b.cramSpinUp = C("cram_spinup.wav");
            b.cramBurstLoop = C("cram_loop.wav");
            b.cramTail = C("cram_tail.wav");
            b.cramLoopFromRound = 12;
            b.cramVolume = 0.5f;
            b.gunVolume = 0.45f; // every round now plays (not one per pair)
            b.airbursts = new[] { C("airburst_1.wav"), C("airburst_2.wav") };
            b.selfDestructPops = new[] { C("sd_pop_1.wav"), C("sd_pop_2.wav"), C("sd_pop_3.wav"), C("sd_pop_4.wav") };
            b.impactBase = C("impact_base.wav");
            b.boom = C("boom_residue.ogg");
            b.distantRumble = C("distant_rumble.wav");
            b.reload = C("reload_residue.ogg");
            b.emptyClick = C("empty_clip_residue.ogg");
            b.siren = C("siren.wav");
            b.warningTone = C("warning_tone.wav");
            b.radarPing = C("radar_ping.wav");
            b.radioStaticLoop = C("radio_static_loop.wav");
            EditorUtility.SetDirty(b);
            return b;
        }

        /// <summary>Copies CREDITS.md (single source of truth) into a TextAsset the game can show.</summary>
        private static TextAsset CreditsText()
        {
            const string src = "CREDITS.md";
            const string dst = DataDir + "/Credits.txt";
            if (!File.Exists(src)) return null;
            File.WriteAllText(dst, File.ReadAllText(src));
            AssetDatabase.ImportAsset(dst);
            return AssetDatabase.LoadAssetAtPath<TextAsset>(dst);
        }

        // ------------------------------------------------------------- gun rigs

        public const string Zu23ModelPath = "Assets/_Project/Art/Models/ZU23/ZU23.gltf";

        /// <summary>
        /// Rigs the processed ZU-23-2 model (tools/blender/process_zu23.py) onto
        /// yaw/pitch pivots placed at the model's own pivot nodes. Model: CC-BY-4.0,
        /// LuckySkink9661 (Sketchfab), modified. See docs/ASSET_REGISTER.md M-01.
        /// </summary>
        private static bool TryBuildZu23Model(Transform gunRoot, out Transform yaw, out Transform pitch,
                                              out List<Transform> muzzles, out Transform camAnchor, out Transform[] barrels)
        {
            yaw = pitch = camAnchor = null;
            muzzles = null;
            barrels = null;
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
            // Look these up now: the yaw/pitch parts are moved out of the model below.
            Transform bL = Find("ZU_Barrel_L"), bR = Find("ZU_Barrel_R");
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
            // Separately recoiling barrels (tools/blender/process_zu23.py splits them), same order as muzzles.
            if (bL != null && bR != null) barrels = new[] { bL, bR };

            // Operator view "B" (tools/blender/preview_operator_view.py): above the sight, behind the breech.
            camAnchor = new GameObject("OperatorCameraAnchor").transform;
            camAnchor.SetParent(pitch, false);
            camAnchor.localPosition = new Vector3(0f, 0.95f, -1.1f);

            foreach (var r in model.GetComponentsInChildren<MeshRenderer>(true))
                r.shadowCastingMode = ShadowCastingMode.On;
            return true;
        }

        /// <summary>
        /// Turns the placed C-RAM model into a working station: yaw/pitch pivots at the rig nodes
        /// (CRAM_Yaw turntable and yoke, CRAM_Pitch radome/gun/drum), spinning barrels, muzzle.
        /// Model: CC-BY 4.0, Muhamad Mirza Arrafi (Sketchfab), modified (rigged). ASSET_REGISTER.md.
        /// </summary>
        private static WeaponStation BuildCramStation(GameObject model, WeaponDefinition def, ProjectileSystem proj, Camera cam,
                                                      Material fireMat, Material smokeMat)
        {
            if (model == null) return null;
            if (PrefabUtility.IsPartOfPrefabInstance(model))
                PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            Transform Find(string n) => model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == n);
            Transform cYaw = Find("CRAM_Yaw"), cPitch = Find("CRAM_Pitch"), cBarrels = Find("CRAM_Barrels"), cMuzzle = Find("Muzzle");
            if (cYaw == null || cPitch == null || cMuzzle == null)
            {
                Debug.LogError("[M1SceneBuilder] C-RAM model is missing rig nodes (run tools/blender/process_cram.py); display only.");
                return null;
            }
            Transform root = model.transform.parent; // position root (mound, pad, model)

            var yaw = new GameObject("YawPivot").transform;
            yaw.SetParent(root, false);
            yaw.position = cYaw.position;
            yaw.rotation = Quaternion.Euler(0f, model.transform.eulerAngles.y, 0f);
            var pitch = new GameObject("PitchPivot").transform;
            pitch.SetParent(yaw, false);
            pitch.position = cPitch.position;
            pitch.localRotation = Quaternion.identity;
            cYaw.SetParent(yaw, true);
            cPitch.SetParent(pitch, true);

            var camAnchor = new GameObject("CameraAnchor").transform;
            camAnchor.SetParent(yaw, false);
            camAnchor.localPosition = new Vector3(0f, 3f, -8f);

            var go = root.gameObject;
            var mount = go.AddComponent<GunMount>();
            mount.definition = def;
            mount.yawPivot = yaw;
            mount.pitchPivot = pitch;
            mount.muzzles = new[] { cMuzzle };
            mount.cameraAnchor = camAnchor;

            var station = go.AddComponent<WeaponStation>();
            station.mount = mount;
            station.projectiles = proj;
            // Over the right shoulder: straight behind, the 4.7 m radar dome hides the gun and its stream.
            station.cameraDistance = 9f;
            station.cameraHeight = 3.2f;
            station.cameraSide = 4.5f;
            if (cBarrels != null)
            {
                var spin = go.AddComponent<BarrelSpin>();
                spin.barrels = cBarrels;
                spin.maxRevsPerSecond = def.fireRate / Mathf.Max(1, def.barrels); // 75 / 6 = 12.5 rev/s
                station.spin = spin;
            }
            var flash = go.AddComponent<MuzzleFlashFX>();
            flash.mount = mount;
            flash.viewCamera = cam;
            flash.fireMaterial = fireMat;
            flash.smokeMaterial = smokeMat;
            // At 75 rounds/s the flashes merge into one big flickering flame that lights up the mount at night.
            flash.coreSize *= 1.35f;
            flash.lobeSize *= 1.35f;
            flash.lobeReach *= 1.5f;
            flash.lightIntensity = 16f;
            flash.lightRange = 28f;
            flash.smokeEvery = 3;
            station.muzzleFlash = flash;
            return station;
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
