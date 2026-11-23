using AirDefense.Core;
using UnityEngine;

namespace AirDefense.Weapons
{
    /// <summary>
    /// Player ("manual") control of a gun mount (C-07, R-06): drag to aim, hold
    /// to fire, tap to reload, zoom toggle. Drives the operator camera.
    /// </summary>
    [DefaultExecutionOrder(100)] // LateUpdate after GunMount so the camera follows this frame's pose
    public sealed class ManualWeaponController : MonoBehaviour
    {
        public GunMount mount;
        public ProjectileSystem projectiles;
        public PlayerInputState input;
        public Camera operatorCamera;
        [Tooltip("Optional: per-barrel recoil animation.")]
        public GunRecoil recoil;
        [Tooltip("Optional: muzzle flash, light and smoke.")]
        public MuzzleFlashFX muzzleFlash;

        [Header("Feel")]
        [Tooltip("Degrees turned when dragging the full screen height, at sensitivity 1 and no zoom.")]
        public float degreesPerScreenHeight = 70f;
        public float normalFov = 60f;
        public float zoomFov = 22f;
        public float fovLerpSpeed = 10f;

        public WeaponState State { get; private set; }
        public bool Zoomed { get; private set; }
        public bool InputEnabled { get; set; } = true;

        private System.Random _rng;
        private int _roundCounter;

        // Camera recoil state (degrees), decaying back to zero.
        private float _kickPitch;
        private float _kickYaw;
        private float _shake;
        private const float KickRecovery = 18f;   // 1/s
        private const float ShakeRecovery = 22f;  // 1/s

        private void Awake()
        {
            State = new WeaponState(mount.definition);
            _rng = new System.Random(4242);
            operatorCamera.fieldOfView = normalFov;
        }

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

            bool trigger = InputEnabled && input.FireHeld;
            bool wasReloading = State.Reloading;
            int rounds = State.Tick(dt, trigger);
            for (int i = 0; i < rounds; i++) FireOne();
            if (!wasReloading && State.Reloading) GameEvents.RaiseReloadStarted(State.Ammo == 0);

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
            if (GameSettings.ScreenShake)
            {
                float sx = (Mathf.PerlinNoise(Time.time * 40f, 0.3f) - 0.5f) * 2f * _shake;
                float sy = (Mathf.PerlinNoise(0.7f, Time.time * 40f) - 0.5f) * 2f * _shake;
                rot *= Quaternion.Euler(-_kickPitch + sy, _kickYaw + sx, 0f);
                pos -= a.forward * (_kickPitch * 0.02f); // slight rearward jolt with the kick
            }
            operatorCamera.transform.SetPositionAndRotation(pos, rot);
        }

        private void FireOne()
        {
            var def = mount.definition;
            Transform muzzle = mount.NextMuzzle(out int barrel);
            Vector3 dir = Ballistics.ApplyDispersion(mount.BoreDirection, def.dispersion, _rng);
            bool tracer = def.tracerEvery > 0 && (_roundCounter++ % def.tracerEvery) == 0;
            projectiles.Fire(muzzle.position, dir, def.muzzleVelocity, def.projectileLifetime, def.damagePerRound, tracer);

            // Recoil: barrel slides back, camera kicks, aim climbs a little (the player corrects).
            if (recoil != null) recoil.Kick(barrel);
            if (muzzleFlash != null) muzzleFlash.Flash(barrel);
            _kickPitch = Mathf.Min(_kickPitch + def.cameraKick, def.cameraKick * 4f);
            _kickYaw += ((float)_rng.NextDouble() - 0.5f) * def.cameraKick * 0.6f;
            _shake = Mathf.Min(_shake + def.cameraShake, def.cameraShake * 3f);
            float drift = ((float)_rng.NextDouble() - 0.5f) * 2f * def.muzzleDriftPerRound;
            mount.AddTarget(drift, def.muzzleClimbPerRound);

            GameEvents.RaiseRoundFired(muzzle.position);
        }
    }
}
