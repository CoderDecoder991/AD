using AirDefense.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace AirDefense.Tests
{
    public sealed class BallisticsTests
    {
        [Test]
        public void Intercept_StationaryTarget_TimeIsDistanceOverSpeed()
        {
            bool ok = Ballistics.TryInterceptTime(Vector3.zero, new Vector3(0, 0, 900), Vector3.zero, 900f, out float t);
            Assert.IsTrue(ok);
            Assert.AreEqual(1f, t, 1e-4f);
        }

        [Test]
        public void Intercept_CrossingTarget_ProjectileAndTargetMeet()
        {
            Vector3 shooter = Vector3.zero;
            Vector3 target = new Vector3(0, 100, 1000);
            Vector3 vel = new Vector3(40, 0, 0);
            const float speed = 900f;

            Assert.IsTrue(Ballistics.TryInterceptTime(shooter, target, vel, speed, out float t));
            Vector3 meet = target + vel * t;
            Assert.AreEqual(speed * t, Vector3.Distance(shooter, meet), 0.01f);
        }

        [Test]
        public void Intercept_TargetFasterAndReceding_NoSolution()
        {
            bool ok = Ballistics.TryInterceptTime(Vector3.zero, new Vector3(0, 0, 100), new Vector3(0, 0, 2000), 900f, out _);
            Assert.IsFalse(ok);
        }

        [Test]
        public void LeadPoint_CompensatesForGravityDrop()
        {
            Assert.IsTrue(Ballistics.TryLeadPoint(Vector3.zero, new Vector3(0, 0, 900), Vector3.zero, 900f, out Vector3 aim));
            // 1 s flight: gravity drops the round 4.905 m, so aim that much higher.
            Assert.AreEqual(4.905f, aim.y, 0.01f);
            Assert.AreEqual(900f, aim.z, 0.01f);
        }

        [Test]
        public void SegmentSphere_HitsWhenPassingThrough_MissesWhenWide()
        {
            Vector3 c = new Vector3(0, 0, 50);
            Assert.IsTrue(Ballistics.SegmentHitsSphere(Vector3.zero, new Vector3(0, 0, 100), c, 1f));
            Assert.IsTrue(Ballistics.SegmentHitsSphere(new Vector3(0.9f, 0, 0), new Vector3(0.9f, 0, 100), c, 1f));
            Assert.IsFalse(Ballistics.SegmentHitsSphere(new Vector3(1.1f, 0, 0), new Vector3(1.1f, 0, 100), c, 1f));
            Assert.IsFalse(Ballistics.SegmentHitsSphere(Vector3.zero, new Vector3(0, 0, 40), c, 1f), "segment ends before the sphere");
        }

        [Test]
        public void Dispersion_StaysNearBoreAndIsDeterministicPerSeed()
        {
            var a = new System.Random(5);
            var b = new System.Random(5);
            for (int i = 0; i < 500; i++)
            {
                Vector3 d1 = Ballistics.ApplyDispersion(Vector3.forward, 0.35f, a);
                Vector3 d2 = Ballistics.ApplyDispersion(Vector3.forward, 0.35f, b);
                Assert.AreEqual(d1, d2);
                Assert.Less(Vector3.Angle(Vector3.forward, d1), 0.35f * 5f, "beyond 5 sigma");
            }
        }
    }
}
