using AirDefense.Missions;
using AirDefense.Threats;
using UnityEngine;

namespace AirDefense.Weapons
{
    /// <summary>
    /// Automatic fire control for a weapon the player is not operating (C-05, C-07): it keeps defending
    /// the base on its own, under the same ammunition, heat and ballistics rules as the player.
    ///
    /// Modelled on how C-RAM is described in public sources (docs/WEAPON_SYSTEMS.md section 7):
    /// sense (the radar tracks a threat), decide (engage only threats whose path takes them into the
    /// protected area, most urgent first), then intercept (lead the target, fire bursts while the bore
    /// is on the computed intercept point). A gun crew (ZU-23-2) works the same way, but reacts more
    /// slowly and aims less precisely than a radar-directed mount.
    /// </summary>
    [DefaultExecutionOrder(90)] // before WeaponStation (110) reads the trigger
    public sealed class AutonomousWeaponController : MonoBehaviour
    {
        public WeaponStation station;
        public ThreatManager threats;
        [Tooltip("Optional: only engages while the mission is running.")]
        public MissionDirector director;
        [Tooltip("Centre of the protected area (threats heading here are engaged).")]
        public Transform defendedCenter;
        [Tooltip("Radius of the protected area, metres.")]
        public float defendedRadius = 140f;

        [Header("Fire control")]
        [Tooltip("Time from first track to opening fire (radar track + engagement decision).")]
        public float reactionTime = 0.8f;
        [Tooltip("Opens fire when the bore is within this angle of the intercept point, degrees.")]
        public float fireTolerance = 0.6f;
        [Tooltip("Slowly wandering aim error, degrees (0 for radar fire control; a gun crew is less exact).")]
        public float aimError;
        [Tooltip("Longest burst, seconds, before a short pause to re-assess the target.")]
        public float burstSeconds = 2.5f;
        public float burstPause = 0.35f;
        [Tooltip("Starts tracking threats this much beyond maximum range, so the mount is already on target.")]
        public float trackBeyondRange = 1.25f;
        [Tooltip("Never fires below this elevation (rounds into the base).")]
        public float minFireElevation = 2f;

        [Header("Rest position")]
        public float restPitch = 5f;

        public ThreatAgent Target { get; private set; }
        public bool Engaging { get; private set; }

        private float _retargetIn;
        private float _tracked;
        private float _burst;
        private float _pause;
        private float _idle;
        private float _restYaw;
        private float _noiseSeed;

        private void Start()
        {
            _restYaw = station.mount.Yaw;
            _noiseSeed = Random.value * 100f;
        }

        private void Update()
        {
            Engaging = false;
            if (station.Manual || !station.Operational) { Target = null; _tracked = 0f; return; }
            bool running = director == null || director.Phase == MissionPhase.Running;
            if (!running) { Target = null; return; }

            float dt = Time.deltaTime;
            var def = station.Definition;
            GunMount mount = station.mount;
            Vector3 gun = mount.pitchPivot.position;

            _retargetIn -= dt;
            if (Target == null || !Target.Alive || _retargetIn <= 0f)
            {
                _retargetIn = 0.25f;
                ThreatAgent best = SelectTarget(gun, def.maxRange * trackBeyondRange);
                if (best != Target) { Target = best; _tracked = 0f; _burst = 0f; }
            }

            if (Target == null)
            {
                // Nothing to engage: after a while, return to the rest position and top up the magazine.
                _idle += dt;
                if (_idle > 3f)
                {
                    mount.SetTarget(_restYaw, restPitch);
                    if (station.State.Ammo < def.magazineSize / 3) station.State.RequestReload();
                }
                return;
            }
            _idle = 0f;
            _tracked += dt;

            Vector3 tPos = Target.transform.position;
            if (!Ballistics.TryLeadPoint(gun, tPos, Target.Velocity, def.muzzleVelocity, def.dragK, out Vector3 aim))
                aim = tPos;
            Vector3 dir = (aim - gun).normalized;
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float pitch = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
            if (aimError > 0f)
            {
                float t = Time.time * 0.35f;
                yaw += (Mathf.PerlinNoise(_noiseSeed, t) - 0.5f) * 2f * aimError;
                pitch += (Mathf.PerlinNoise(t, _noiseSeed) - 0.5f) * 2f * aimError;
            }
            mount.SetTarget(yaw, pitch);

            float range = Vector3.Distance(gun, tPos);
            float error = Vector3.Angle(mount.BoreDirection, Quaternion.Euler(-pitch, yaw, 0f) * Vector3.forward);
            bool solution = range <= def.maxRange && range >= def.minRange && error <= fireTolerance
                            && _tracked >= reactionTime && mount.Pitch >= minFireElevation;

            if (_pause > 0f) { _pause -= dt; return; }
            if (!solution) { _burst = 0f; return; }
            station.TriggerHeld = true;
            Engaging = true;
            _burst += dt;
            if (_burst >= burstSeconds) { _burst = 0f; _pause = burstPause; }
        }

        /// <summary>Most urgent threat (soonest to reach the protected area) inside tracking range.</summary>
        private ThreatAgent SelectTarget(Vector3 gun, float trackRange)
        {
            ThreatAgent best = null;
            float bestTime = float.MaxValue;
            Vector3 c = defendedCenter != null ? defendedCenter.position : gun;
            var live = threats.Active;
            for (int i = 0; i < live.Count; i++)
            {
                ThreatAgent t = live[i];
                if (!t.Alive) continue;
                Vector3 p = t.transform.position;
                if ((p - gun).sqrMagnitude > trackRange * trackRange) continue;
                if (!ThreatensArea(p, t.Velocity, c, defendedRadius)) continue;
                float time = TimeToArea(p, t.Velocity, c);
                // Stick with the current target unless another is clearly more urgent.
                if (t == Target) time -= 1.5f;
                if (time < bestTime) { bestTime = time; best = t; }
            }
            return best;
        }

        /// <summary>
        /// True when a threat's ground track passes within <paramref name="radius"/> of the protected
        /// centre ahead of it (or it is already inside). C-RAM engages only rounds that threaten the
        /// defended area; ones that will land outside are ignored.
        /// </summary>
        public static bool ThreatensArea(Vector3 pos, Vector3 vel, Vector3 center, float radius)
        {
            Vector3 d = Flat(center - pos);
            if (d.sqrMagnitude <= radius * radius) return true;
            Vector3 v = Flat(vel);
            float speed2 = v.sqrMagnitude;
            if (speed2 < 1e-4f) return false;
            float along = Vector3.Dot(d, v) / speed2;   // time of closest approach
            if (along <= 0f) return false;               // moving away
            Vector3 closest = Flat(pos + vel * along - center);
            return closest.sqrMagnitude <= radius * radius;
        }

        /// <summary>Seconds until the threat reaches the protected centre at its current ground speed.</summary>
        public static float TimeToArea(Vector3 pos, Vector3 vel, Vector3 center)
        {
            float speed = Flat(vel).magnitude;
            return Flat(center - pos).magnitude / Mathf.Max(1f, speed);
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
