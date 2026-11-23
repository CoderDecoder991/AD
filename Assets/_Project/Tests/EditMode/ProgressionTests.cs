using System.IO;
using AirDefense.Core;
using AirDefense.Data;
using AirDefense.Progression;
using NUnit.Framework;
using UnityEngine;

namespace AirDefense.Tests
{
    public sealed class SaveServiceTests
    {
        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "airdefense-save-tests-" + System.Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        [Test]
        public void NoFile_GivesFreshProfileWithStartingWeapon()
        {
            var p = new SaveService(_dir).Load();
            Assert.AreEqual(0, p.credits);
            Assert.IsTrue(p.IsUnlocked("zu23"));
        }

        [Test]
        public void SaveThenLoad_RoundTrips()
        {
            var s = new SaveService(_dir);
            var p = new PlayerProfile { credits = 1234, cinematicSeen = true };
            p.completedMissions.Add("m1_greybox");
            p.settings.aimSensitivity = 1.7f;
            s.Save(p);

            var q = new SaveService(_dir).Load();
            Assert.AreEqual(1234, q.credits);
            Assert.IsTrue(q.cinematicSeen);
            Assert.IsTrue(q.HasCompleted("m1_greybox"));
            Assert.AreEqual(1.7f, q.settings.aimSensitivity, 1e-5f);
        }

        [Test]
        public void TamperedMainFile_FallsBackToBackup()
        {
            var s = new SaveService(_dir);
            s.Save(new PlayerProfile { credits = 100 });
            s.Save(new PlayerProfile { credits = 200 }); // 100 is now the .bak

            string text = File.ReadAllText(s.FilePath).Replace("200", "999999");
            File.WriteAllText(s.FilePath, text);

            var loaded = new SaveService(_dir);
            var p = loaded.Load();
            Assert.AreEqual(100, p.credits);
            StringAssert.Contains("backup", loaded.LastLoadIssue);
        }

        [Test]
        public void GarbageFiles_GiveFreshProfile()
        {
            var s = new SaveService(_dir);
            File.WriteAllText(s.FilePath, "{not json");
            File.WriteAllText(s.FilePath + ".bak", "");
            var p = s.Load();
            Assert.AreEqual(0, p.credits);
            Assert.IsNotNull(s.LastLoadIssue);
        }

        [Test]
        public void Migration_RestoresStartingWeapon()
        {
            var s = new SaveService(_dir);
            var p = new PlayerProfile { credits = 5 };
            p.unlockedWeapons.Clear();
            s.Save(p);
            Assert.IsTrue(new SaveService(_dir).Load().IsUnlocked("zu23"));
        }
    }

    public sealed class RewardCalculatorTests
    {
        private EconomyConfig _cfg;

        [SetUp] public void SetUp() => _cfg = ScriptableObject.CreateInstance<EconomyConfig>();
        [TearDown] public void TearDown() => Object.DestroyImmediate(_cfg);

        private static MissionResult Result(bool success, float health, int hits, int fired, int intercepts) => new MissionResult
        {
            success = success, healthRemaining01 = health, roundsHit = hits, roundsFired = fired,
            interceptCredits = intercepts, threatsDestroyed = intercepts / 10
        };

        [Test]
        public void Failure_PaysInterceptionsOnly()
        {
            var b = RewardCalculator.Calculate(Result(false, 0f, 10, 100, 120), _cfg, firstCompletion: true);
            Assert.AreEqual(120, b.Total);
            Assert.AreEqual(1, b.lines.Count);
        }

        [Test]
        public void PerfectFirstWin_PaysEverything()
        {
            // basis = 300 + 200 = 500; health 100% -> +150; accuracy 25%+ -> +75; first -> +200
            var b = RewardCalculator.Calculate(Result(true, 1f, 30, 100, 200), _cfg, firstCompletion: true);
            Assert.AreEqual(200 + 300 + 150 + 75 + 200, b.Total);
        }

        [Test]
        public void RepeatWin_NoFirstBonus_PartialBonuses()
        {
            // basis 500; health 50% -> 75; accuracy 12.5% (half of 25%) -> 37.5 -> 38
            var b = RewardCalculator.Calculate(Result(true, 0.5f, 125, 1000, 200), _cfg, firstCompletion: false);
            Assert.AreEqual(200 + 300 + 75 + 38, b.Total);
        }

        [Test]
        public void EarlyMissions_PayRoughlyTheDocumentedPace()
        {
            // ECONOMY §3: C-RAM (4,000) in about 8 early missions -> ~500 per mission.
            var b = RewardCalculator.Calculate(Result(true, 0.75f, 12, 150, 250), _cfg, firstCompletion: false);
            Assert.That(b.Total, Is.InRange(400, 750));
        }
    }

    public sealed class RosterAndQualityTests
    {
        [Test]
        public void Roster_UnlocksStrictlyInSequence()
        {
            var zu = ScriptableObject.CreateInstance<WeaponDefinition>(); zu.weaponId = "zu23";
            var cram = ScriptableObject.CreateInstance<WeaponDefinition>(); cram.weaponId = "cram"; cram.unlockCost = 4000;
            var tung = ScriptableObject.CreateInstance<WeaponDefinition>(); tung.weaponId = "tunguska"; tung.unlockCost = 10000;
            var roster = ScriptableObject.CreateInstance<WeaponRoster>();
            roster.weapons.AddRange(new[] { zu, cram, tung });

            var p = new PlayerProfile { credits = 50000 };
            Assert.AreEqual(cram, roster.NextUnlock(p));
            Assert.IsTrue(roster.CanUnlock(p, cram));
            Assert.IsFalse(roster.CanUnlock(p, tung), "Tunguska only after C-RAM (R-04)");

            p.unlockedWeapons.Add("cram");
            Assert.AreEqual(tung, roster.NextUnlock(p));
            p.credits = 9999;
            Assert.IsFalse(roster.CanUnlock(p, tung));

            foreach (var o in new Object[] { zu, cram, tung, roster }) Object.DestroyImmediate(o);
        }

        [Test]
        public void QualityDetect_Pad8IsHigh_SmallDevicesLower()
        {
            Assert.AreEqual(QualityTier.High, QualityTiers.Detect(7600, 8));   // Xiaomi Pad 8 (8 GB)
            Assert.AreEqual(QualityTier.Medium, QualityTiers.Detect(5800, 8));
            Assert.AreEqual(QualityTier.Low, QualityTiers.Detect(3000, 8));
            Assert.AreEqual(QualityTier.Low, QualityTiers.Detect(8000, 4));
        }
    }
}
