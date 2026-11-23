using AirDefense.Core;
using AirDefense.Data;
using AirDefense.Progression;
using AirDefense.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace AirDefense.Tests
{
    /// <summary>M3 C-RAM rules: rotary spin-up, engage-only-threats-to-the-base, unlock purchase.</summary>
    public sealed class CramTests
    {
        private WeaponDefinition _gatling;

        [SetUp]
        public void SetUp()
        {
            _gatling = ScriptableObject.CreateInstance<WeaponDefinition>();
            _gatling.weaponId = "cram";
            _gatling.fireRate = 75f;
            _gatling.magazineSize = 1550;
            _gatling.spinUpTime = 0.3f;
            _gatling.heatPerSecond = 0f;
            _gatling.unlockCost = 4000;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_gatling);

        [Test]
        public void SpinUp_BelowMinimumSpeed_DoesNotFire()
        {
            var s = new WeaponState(_gatling);
            Assert.AreEqual(0, s.Tick(0.1f, true, WeaponState.MinSpinToFire * 0.5f));
            Assert.AreEqual(1550, s.Ammo);
        }

        [Test]
        public void SpinUp_HalfSpeed_FiresAtHalfRate()
        {
            var s = new WeaponState(_gatling);
            int rounds = 0;
            for (int i = 0; i < 60; i++) rounds += s.Tick(1f / 60f, true, 0.5f);
            // 1 s at half of 75 rounds/s (+1 for the immediate first round).
            Assert.That(rounds, Is.InRange(37, 39));
        }

        [Test]
        public void FullSpeed_FiresSeventyFivePerSecond()
        {
            var s = new WeaponState(_gatling);
            int rounds = 0;
            for (int i = 0; i < 120; i++) rounds += s.Tick(1f / 60f, true, 1f);
            Assert.That(rounds, Is.InRange(150, 152));
        }

        [Test]
        public void ThreatHeadingIntoBase_IsEngaged()
        {
            Vector3 c = Vector3.zero;
            Assert.IsTrue(AutonomousWeaponController.ThreatensArea(new Vector3(0f, 80f, 1500f), new Vector3(0f, 0f, -40f), c, 140f));
            // Passing 100 m to the side still threatens a 140 m area.
            Assert.IsTrue(AutonomousWeaponController.ThreatensArea(new Vector3(100f, 80f, 1500f), new Vector3(0f, 0f, -40f), c, 140f));
        }

        [Test]
        public void ThreatMissingOrLeavingBase_IsIgnored()
        {
            Vector3 c = Vector3.zero;
            // Track passes 600 m to the side.
            Assert.IsFalse(AutonomousWeaponController.ThreatensArea(new Vector3(600f, 80f, 1500f), new Vector3(0f, 0f, -40f), c, 140f));
            // Flying away.
            Assert.IsFalse(AutonomousWeaponController.ThreatensArea(new Vector3(0f, 80f, 1500f), new Vector3(0f, 0f, 40f), c, 140f));
            // Already overhead always counts.
            Assert.IsTrue(AutonomousWeaponController.ThreatensArea(new Vector3(50f, 80f, 0f), new Vector3(0f, 0f, 40f), c, 140f));
        }

        [Test]
        public void TryUnlock_SpendsCreditsOnce()
        {
            var zu = ScriptableObject.CreateInstance<WeaponDefinition>();
            zu.weaponId = "zu23";
            var roster = ScriptableObject.CreateInstance<WeaponRoster>();
            roster.weapons.Add(zu);
            roster.weapons.Add(_gatling);
            var p = new PlayerProfile { credits = 4500 };

            Assert.IsTrue(roster.TryUnlock(p, _gatling));
            Assert.AreEqual(500, p.credits);
            Assert.IsTrue(p.IsUnlocked("cram"));
            Assert.IsFalse(roster.TryUnlock(p, _gatling), "already unlocked");
            Assert.AreEqual(500, p.credits);

            Object.DestroyImmediate(roster);
            Object.DestroyImmediate(zu);
        }

        [Test]
        public void TryUnlock_NotEnoughCredits_Refused()
        {
            var zu = ScriptableObject.CreateInstance<WeaponDefinition>();
            zu.weaponId = "zu23";
            var roster = ScriptableObject.CreateInstance<WeaponRoster>();
            roster.weapons.Add(zu);
            roster.weapons.Add(_gatling);
            var p = new PlayerProfile { credits = 3999 };
            Assert.IsFalse(roster.TryUnlock(p, _gatling));
            Assert.AreEqual(3999, p.credits);
            Assert.IsFalse(p.IsUnlocked("cram"));
            Object.DestroyImmediate(roster);
            Object.DestroyImmediate(zu);
        }
    }
}
