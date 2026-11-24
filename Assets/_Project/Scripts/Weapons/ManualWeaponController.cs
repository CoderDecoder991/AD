using AirDefense.Core;
using UnityEngine;

namespace AirDefense.Weapons
{
    /// <summary>
    /// Player ("manual") control of one weapon station at a time (C-05, C-07, R-06): drag to aim,
    /// hold to fire, tap to reload, zoom toggle. Drives the operator camera, which blends across
    /// when the player switches weapon. Unselected, unlocked stations defend on their own
    /// (<see cref="AutonomousWeaponController"/>).
    /// </summary>
    [DefaultExecutionOrder(100)] // LateUpdate after GunMount so the camera follows this frame's pose
    public sealed class ManualWeaponController : MonoBehaviour
    {
        [Tooltip("The weapon under manual control at start.")]
        public WeaponStation station;
        [Tooltip("All weapon stations at the base, in roster order.")]
        public WeaponStation[] stations;
        public PlayerInputState input;
        public Camera operatorCamera;
        [Tooltip("Seconds for the camera to move to a newly selected weapon.")]
        public float switchBlendTime = 0.4f; // M3 acceptance: switching <= 0.4 s

        public GunMount mount => station.mount;

        [Header("Camera")]
        [Tooltip("Third person: camera behind and above the gun so the whole weapon is visible (user request).")]
        public bool thirdPerson = true;
        [Tooltip("How much the camera orbits down as the gun elevates (0 = stays level, 1 = follows the barrel).")]
        [Range(0f, 1f)] public float pitchFollow = 0.6f;
        [Tooltip("Tilts the view down so the aim point sits in the upper screen and the whole gun is visible below it. The HUD crosshair follows the real aim point.")]
        public float viewTiltDown = 13f; // VS-3: horizon higher in frame, more of the base visible
        [Tooltip("Crosshair and bore line meet at this distance (metres), so aim is exact at typical ranges.")]
        public float aimConvergence = 800f;

        [Header("Feel")]
        [Tooltip("Degrees turned when dragging the full screen height, at sensitivity 1 and no zoom.")]
        public float degreesPerScreenHeight = 70f;
        public float normalFov = 60f;
        public float zoomFov = 22f;
        public float fovLerpSpeed = 10f;

        /// <summary>Where the bore line is at the convergence range: the HUD crosshair is drawn here.</summary>
        public Vector3 AimPoint => mount.pitchPivot.position + mount.BoreDirection * aimConvergence;

        public WeaponState State => station.State;
        /// <summary>Rounds the player fired, across all weapons (accuracy).</summary>
        public int ManualRoundsFired
        {
            get
            {
                int n = 0;
                foreach (var s in stations) if (s != null) n += s.ManualRoundsFired;
                return n;
            }
        }
        public bool Zoomed { get; private set; }
        public bool InputEnabled { get; set; } = true;

        private System.Random _rng;

        // Camera recoil state (degrees), decaying back to zero.
        private float _kickPitch;
        private float _kickYaw;
        private float _shake;
        private const float KickRecovery = 18f;   // 1/s
        private const float ShakeRecovery = 22f;  // 1/s

        // Camera blend when switching weapon.
        private float _blend = 1f;
        private Vector3 _blendFromPos;
        private Quaternion _blendFromRot;

        private void Awake()
        {
            _rng = new System.Random(4242);
            operatorCamera.fieldOfView = normalFov;
            if (stations == null || stations.Length == 0) stations = new[] { station };
        }

        private void Start()
        {
            RefreshUnlocks();
            foreach (var s in stations) if (s != null) s.Manual = s == station;
            station.Fired += OnFired;
        }

        /// <summary>Unlocked weapons are crewed; locked ones stand idle at their positions.</summary>
        public void RefreshUnlocks()
        {
            var p = ProfileStore.Profile;
            foreach (var s in stations)
                if (s != null) s.Operational = p.IsUnlocked(s.Definition.weaponId);
        }

        public WeaponStation StationFor(Data.WeaponDefinition def)
        {
            foreach (var s in stations) if (s != null && s.Definition == def) return s;
            return null;
        }

        /// <summary>Takes manual control of another (unlocked) weapon; the previous one goes autonomous.</summary>
        public bool Select(WeaponStation next)
        {
            if (next == null || next == station || !next.Operational) return false;
            station.Fired -= OnFired;
            station.Manual = false;
            station.TriggerHeld = false;
            _blendFromPos = operatorCamera.transform.position;
            _blendFromRot = operatorCamera.transform.rotation;
            _blend = 0f;
            station = next;
            station.Manual = true;
            station.Fired += OnFired;
            _kickPitch = _kickYaw = _shake = 0f;
            Zoomed = false;
            GameEvents.RaiseManualWeaponChanged(next.Definition);
            return true;
        }

        public bool Switching => _blend < 1f;

        private void Update()
        {
            float dt = Time.deltaTime;

            if (InputEnabled)
            {
                Vector2 px = input.ConsumeAimPixels();
                float fovScale = operatorCamera.fieldOfView / normalFov;
                float k = degreesPerScreenHeight / Mathf.Max(1, Screen.height) * GameSettings.Sensitivity * fovScale;
                float pitchSign = GameSettings.InvertY ? -1f : 1f;
                mount.AddTarget(px.x * k, px.y * k * pitchSign);

                if (input.ReloadRequested) State.RequestReload();
                if (input.ZoomToggleRequested) Zoomed = !Zoomed;
            }
            else
            {
                input.ConsumeAimPixels();
            }
            input.ReloadRequested = false;
            input.ZoomToggleRequested = false;

            // The station fires (WeaponStation, later this frame) while the trigger is held.
            if (InputEnabled && input.FireHeld && _blend >= 1f) station.TriggerHeld = true;

            float targetFov = Zoomed ? zoomFov : normalFov;
            operatorCamera.fieldOfView = Mathf.Lerp(operatorCamera.fieldOfView, targetFov, 1f - Mathf.Exp(-fovLerpSpeed * dt));
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            float decay = Mathf.Exp(-KickRecovery * dt);
            _kickPitch *= decay;
            _kickYaw *= decay;
            _shake *= Mathf.Exp(-ShakeRecovery * dt);

            Transform a = mount.cameraAnchor;
            Quaternion rot = a.rotation;
            Vector3 pos = a.position;
            if (thirdPerson)
            {
                // Orbit behind the mount: follows traverse fully and elevation partly, never below the mount.
                Vector3 pivot = mount.yawPivot.position;
                Quaternion orbit = Quaternion.Euler(-mount.Pitch * pitchFollow, mount.Yaw, 0f);
                pos = pivot + orbit * new Vector3(station.cameraSide, station.cameraHeight, -station.cameraDistance);
                pos.y = Mathf.Max(pos.y, pivot.y + 1.2f);
                // Look at the point the bore hits at the convergence range, so the crosshair is the aim.
                rot = Quaternion.LookRotation(AimPoint - pos, Vector3.up) * Quaternion.Euler(viewTiltDown, 0f, 0f);
            }
            if (GameSettings.ScreenShake)
            {
                float sx = (Mathf.PerlinNoise(Time.time * 40f, 0.3f) - 0.5f) * 2f * _shake;
                float sy = (Mathf.PerlinNoise(0.7f, Time.time * 40f) - 0.5f) * 2f * _shake;
                rot *= Quaternion.Euler(-_kickPitch + sy, _kickYaw + sx, 0f);
                pos -= rot * Vector3.forward * (_kickPitch * 0.02f); // slight rearward jolt with the kick
            }
            if (_blend < 1f)
            {
                // Smooth move from the previous weapon's view to the new one.
                _blend = Mathf.Min(1f, _blend + dt / Mathf.Max(0.01f, switchBlendTime));
                float k = Mathf.SmoothStep(0f, 1f, _blend);
                pos = Vector3.Lerp(_blendFromPos, pos, k);
                rot = Quaternion.Slerp(_blendFromRot, rot, k);
            }
            operatorCamera.transform.SetPositionAndRotation(pos, rot);
        }

        /// <summary>Recoil felt by the operator: camera kick and shake, and the aim climbs a little (the player corrects).</summary>
        private void OnFired(int barrel)
        {
            var def = mount.definition;
            _kickPitch = Mathf.Min(_kickPitch + def.cameraKick, def.cameraKick * 4f);
            _kickYaw += ((float)_rng.NextDouble() - 0.5f) * def.cameraKick * 0.6f;
            _shake = Mathf.Min(_shake + def.cameraShake, def.cameraShake * 3f);
            float drift = ((float)_rng.NextDouble() - 0.5f) * 2f * def.muzzleDriftPerRound;
            mount.AddTarget(drift, def.muzzleClimbPerRound);
        }
    }
}
