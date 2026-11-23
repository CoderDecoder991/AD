using AirDefense.Data;
using AirDefense.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace AirDefense.Tests
{
    public sealed class WeaponStateTests
    {
        private WeaponDefinition _def;

        [SetUp]
        public void SetUp()
        {
            _def = ScriptableObject.CreateInstance<WeaponDefinition>();
            _def.fireRate = 30f;
            _def.magazineSize = 100;
            _def.reloadTime = 4f;
            _def.heatPerSecond = 0.2f;
            _def.coolPerSecond = 0.15f;
            _def.overheatRecoverBelow = 0.3f;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_def);

        private static int Run(WeaponState s, float seconds, bool trigger, float dt = 1f / 60f)
        {
            int total = 0;
            for (float t = 0; t < seconds - 1e-5f; t += dt) total += s.Tick(dt, trigger);
            return total;
        }

        [Test]
        public void FiresAtConfiguredRate()
        {
            var s = new WeaponState(_def);
            int rounds = Run(s, 1f, true);
            Assert.That(rounds, Is.InRange(30, 31));
            Assert.AreEqual(100 - rounds, s.Ammo);
        }

        [Test]
        public void FirstRoundLeavesImmediatelyOnTriggerPress()
        {
            var s = new WeaponState(_def);
            Assert.AreEqual(1, s.Tick(1f / 60f, true));
        }

        [Test]
        public void EmptyMagazine_AutoReloads_ThenRefills()
        {
            var s = new WeaponState(_def);
            Run(s, 4f, true); // 100 rounds take ~3.3 s
            Assert.AreEqual(0, s.Ammo);
            Assert.IsTrue(s.Reloading);
            Assert.AreEqual(0, Run(s, 1f, true), "cannot fire while reloading");
            Run(s, 3.5f, false);
            Assert.IsFalse(s.Reloading);
            Assert.AreEqual(100, s.Ammo);
        }

        [Test]
        public void SustainedFire_Overheats_AndRecoversBelowThreshold()
        {
            _def.magazineSize = 10000; // isolate heat from reloads
            var s = new WeaponState(_def);
            Run(s, 5.1f, true);
            Assert.IsTrue(s.Overheated);
            Assert.AreEqual(0, Run(s, 0.5f, true), "blocked while overheated");
            Run(s, 5f, false);
            Assert.IsFalse(s.Overheated);
            Assert.Less(s.Heat01, _def.overheatRecoverBelow);
        }

        [Test]
        public void ManualReload_IgnoredWhenFull()
        {
            var s = new WeaponState(_def);
            s.RequestReload();
            Assert.IsFalse(s.Reloading);
            Run(s, 0.5f, true);
            s.RequestReload();
            Assert.IsTrue(s.Reloading);
        }
    }
}
