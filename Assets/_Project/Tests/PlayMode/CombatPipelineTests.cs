using System.Collections;
using AirDefense.Core;
using AirDefense.Data;
using AirDefense.Threats;
using AirDefense.Weapons;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AirDefense.Tests
{
    /// <summary>
    /// End-to-end check of the real runtime chain: ThreatManager spawns a threat,
    /// ProjectileSystem flies rounds with gravity, hits are detected, damage is
    /// applied and the ThreatDestroyed / ThreatImpacted events fire.
    /// </summary>
    public sealed class CombatPipelineTests
    {
        private GameObject _root;
        private ThreatManager _threats;
        private ProjectileSystem _projectiles;
        private ThreatDefinition _def;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("TestRoot");
            var visuals = ScriptableObject.CreateInstance<PrototypeVisuals>();
            var center = new GameObject("Center").transform;
            center.SetParent(_root.transform);

            _threats = new GameObject("Threats").AddComponent<ThreatManager>();
            _threats.transform.SetParent(_root.transform);
            _threats.visuals = visuals;
            _threats.installationCenter = center;

            _projectiles = new GameObject("Projectiles").AddComponent<ProjectileSystem>();
            _projectiles.transform.SetParent(_root.transform);
            _projectiles.visuals = visuals;
            _projectiles.threats = _threats;

            _def = ScriptableObject.CreateInstance<ThreatDefinition>();
            _def.threatClass = ThreatClass.SmallDrone;
            _def.speed = 20f;
            _def.cruiseAltitude = 60f;
            _def.diveDistance = 50f;
            _def.weaveAmplitude = 0f;
            _def.health = 3f;
            _def.hitRadius = 3f;
            _def.impactDamage = 8f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_root);
            Object.Destroy(_def);
        }

        private ThreatAgent SpawnNorth(float distance) =>
            _threats.Spawn(new SpawnGroup
            {
                threat = _def, count = 1,
                bearingRange = new Vector2(0f, 0f),
                distanceRange = new Vector2(distance, distance)
            });

        [UnityTest]
        public IEnumerator LeadAimedBurst_DestroysMovingThreat()
        {
            int destroyed = 0;
            System.Action<ThreatAgent> onDestroyed = _ => destroyed++;
            GameEvents.ThreatDestroyed += onDestroyed;
            try
            {
                ThreatAgent t = SpawnNorth(500f);
                yield return null; // let it start moving
                Vector3 gun = new Vector3(0f, 14f, -60f);

                for (int i = 0; i < 30 && destroyed == 0; i++)
                {
                    Assert.IsTrue(Ballistics.TryLeadPoint(gun, t.transform.position, t.Velocity, 900f, out Vector3 aim));
                    _projectiles.Fire(gun, aim - gun, 900f, 2.2f, 1f, tracer: false);
                    yield return new WaitForSeconds(1f / 30f);
                }
                float timeout = Time.time + 2f;
                while (destroyed == 0 && Time.time < timeout) yield return null;

                Assert.AreEqual(1, destroyed, "threat should be destroyed by lead-aimed fire");
                Assert.GreaterOrEqual(_projectiles.RoundsHit, 3, "health 3 needs at least 3 hits");
                Assert.AreEqual(0, _threats.Active.Count);
            }
            finally { GameEvents.ThreatDestroyed -= onDestroyed; }
        }

        [UnityTest]
        public IEnumerator UnengagedThreat_ImpactsInstallation()
        {
            int impacted = 0;
            System.Action<ThreatAgent> onImpacted = _ => impacted++;
            GameEvents.ThreatImpacted += onImpacted;
            try
            {
                SpawnNorth(120f);
                float timeout = Time.time + 15f;
                while (impacted == 0 && Time.time < timeout) yield return null;
                Assert.AreEqual(1, impacted, "threat should reach and hit the installation");
                Assert.AreEqual(0, _threats.Active.Count);
            }
            finally { GameEvents.ThreatImpacted -= onImpacted; }
        }

        [UnityTest]
        public IEnumerator WideMiss_DoesNotDamage()
        {
            ThreatAgent t = SpawnNorth(400f);
            yield return null;
            Vector3 gun = new Vector3(0f, 14f, -60f);
            for (int i = 0; i < 20; i++)
            {
                _projectiles.Fire(gun, Vector3.right, 900f, 1f, 1f, tracer: false); // shooting east, target is north
                yield return null;
            }
            yield return new WaitForSeconds(1.1f);
            Assert.IsTrue(t.Alive);
            Assert.AreEqual(_def.health, t.Health);
            Assert.AreEqual(0, _projectiles.RoundsHit);
        }
    }
}
