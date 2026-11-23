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
            Transform a = mount.cameraAnchor;
            operatorCamera.transform.SetPositionAndRotation(a.position, a.rotation);
        }

        private void FireOne()
        {
            var def = mount.definition;
            Transform muzzle = mount.NextMuzzle();
            Vector3 dir = Ballistics.ApplyDispersion(mount.BoreDirection, def.dispersion, _rng);
            bool tracer = def.tracerEvery > 0 && (_roundCounter++ % def.tracerEvery) == 0;
            projectiles.Fire(muzzle.position, dir, def.muzzleVelocity, def.projectileLifetime, def.damagePerRound, tracer);
            GameEvents.RaiseRoundFired(muzzle.position);
        }
    }
}
