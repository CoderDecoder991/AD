using UnityEngine;

namespace AirDefense.Weapons
{
    /// <summary>Pure maths for projectiles and lead. No Unity objects, so it is unit-tested.</summary>
    public static class Ballistics
    {
        public static readonly Vector3 Gravity = new Vector3(0f, -9.81f, 0f);

        /// <summary>
        /// Time for a projectile fired from <paramref name="shooter"/> at <paramref name="speed"/>
        /// to meet a target moving at constant velocity (gravity ignored). Returns false if no intercept.
        /// </summary>
        public static bool TryInterceptTime(Vector3 shooter, Vector3 targetPos, Vector3 targetVel, float speed, out float t)
        {
            Vector3 d = targetPos - shooter;
            float a = Vector3.Dot(targetVel, targetVel) - speed * speed;
            float b = 2f * Vector3.Dot(d, targetVel);
            float c = Vector3.Dot(d, d);
            t = 0f;

            if (Mathf.Abs(a) < 1e-6f)
            {
                if (Mathf.Abs(b) < 1e-6f) return false;
                t = -c / b;
                return t > 0f;
            }

            float disc = b * b - 4f * a * c;
            if (disc < 0f) return false;
            float sq = Mathf.Sqrt(disc);
            float t1 = (-b - sq) / (2f * a);
            float t2 = (-b + sq) / (2f * a);
            t = Mathf.Min(t1, t2);
            if (t < 0f) t = Mathf.Max(t1, t2);
            return t > 0f;
        }

        /// <summary>
        /// Where to aim so a projectile hits the moving target, including gravity drop compensation.
        /// </summary>
        public static bool TryLeadPoint(Vector3 shooter, Vector3 targetPos, Vector3 targetVel, float speed, out Vector3 aimPoint)
        {
            aimPoint = targetPos;
            if (!TryInterceptTime(shooter, targetPos, targetVel, speed, out float t)) return false;
            Vector3 intercept = targetPos + targetVel * t;
            // Aim above the intercept by the distance gravity will pull the round down.
            aimPoint = intercept - 0.5f * Gravity * t * t;
            return true;
        }

        /// <summary>
        /// Does the segment p0→p1 pass within <paramref name="radius"/> of <paramref name="center"/>?
        /// </summary>
        public static bool SegmentHitsSphere(Vector3 p0, Vector3 p1, Vector3 center, float radius)
        {
            Vector3 seg = p1 - p0;
            float len2 = seg.sqrMagnitude;
            float t = len2 > 1e-8f ? Mathf.Clamp01(Vector3.Dot(center - p0, seg) / len2) : 0f;
            Vector3 closest = p0 + seg * t;
            return (closest - center).sqrMagnitude <= radius * radius;
        }

        /// <summary>Applies a random cone spread of <paramref name="sigmaDegrees"/> to a direction.</summary>
        public static Vector3 ApplyDispersion(Vector3 dir, float sigmaDegrees, System.Random rng)
        {
            if (sigmaDegrees <= 0f) return dir.normalized;
            // Box–Muller for two gaussian samples.
            double u1 = 1.0 - rng.NextDouble();
            double u2 = rng.NextDouble();
            double mag = System.Math.Sqrt(-2.0 * System.Math.Log(u1));
            float gx = (float)(mag * System.Math.Cos(2.0 * System.Math.PI * u2)) * sigmaDegrees;
            float gy = (float)(mag * System.Math.Sin(2.0 * System.Math.PI * u2)) * sigmaDegrees;
            Quaternion basis = Quaternion.LookRotation(dir);
            return (basis * Quaternion.Euler(gy, gx, 0f) * Vector3.forward).normalized;
        }
    }
}
