using AirDefense.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace AirDefense.Tests
{
    /// <summary>Air-drag ballistics (23 mm HEI-T: 970 m/s, k = 4.6e-4 per metre).</summary>
    public sealed class DragBallisticsTests
    {
        private const float V0 = 970f;
        private const float K = 4.6e-4f;

        [Test]
        public void Speed_At1km_IsAbout610()
        {
            float v = Ballistics.SpeedAt(1000f, V0, K);
            Assert.That(v, Is.InRange(590f, 630f));
        }

        [Test]
        public void FlightTime_MatchesStepSimulation()
        {
            // Simulate drag only (no gravity) by stepping and removing the gravity term each step.
            Vector3 p = Vector3.zero, v = new Vector3(V0, 0f, 0f);
            float t = 0f;
            const float dt = 0.0005f;
            while (p.x < 1000f)
            {
                v = Ballistics.Step(v, K, dt) - Ballistics.Gravity * dt;
                p += v * dt;
                t += dt;
            }
            Assert.AreEqual(Ballistics.FlightTime(1000f, V0, K), t, 0.01f);
        }

        [Test]
        public void ZeroDrag_FallsBackToStraightLineTime()
        {
            Assert.AreEqual(1f, Ballistics.FlightTime(970f, V0, 0f), 1e-4f);
        }

        [TestCase(600f, 30f)]
        [TestCase(900f, 50f)]
        [TestCase(1200f, 28f)]
        public void LeadWithDrag_RoundMeetsCrossingTarget(float range, float targetSpeed)
        {
            Vector3 shooter = new Vector3(0f, 15f, 0f);
            Vector3 target = new Vector3(0f, 120f, range);
            Vector3 tv = new Vector3(targetSpeed, -2f, -5f);
            Assert.IsTrue(Ballistics.TryLeadPoint(shooter, target, tv, V0, K, out Vector3 aim));

            // Fly a real round (gravity + drag) and find its closest approach to the moving target.
            Vector3 p = shooter, v = (aim - shooter).normalized * V0, tp = target;
            float best = float.MaxValue;
            const float dt = 0.001f;
            for (float t = 0f; t < 4f; t += dt)
            {
                v = Ballistics.Step(v, K, dt);
                p += v * dt;
                tp += tv * dt;
                best = Mathf.Min(best, Vector3.Distance(p, tp));
            }
            Assert.Less(best, 2.5f, $"miss distance at {range} m");
        }

        [Test]
        public void DropAtSameRange_IsMoreWithDrag()
        {
            // At a given range the slowed round arrives later, so it has fallen further than in vacuum.
            const float range = 1200f;
            float tVac = range / V0;
            float vacuumDrop = 0.5f * 9.81f * tVac * tVac;
            float dragDrop = Ballistics.GravityDrop(Ballistics.FlightTime(range, V0, K), V0, K);
            Assert.Greater(dragDrop, vacuumDrop * 1.5f);
        }
    }
}
