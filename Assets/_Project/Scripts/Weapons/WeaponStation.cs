using System;
using AirDefense.Core;
using AirDefense.Data;
using UnityEngine;

namespace AirDefense.Weapons
{
    /// <summary>
    /// One weapon emplacement: mount, ammunition/heat state and the firing of rounds. The same rules
    /// apply whoever is in control (C-07): the player (<see cref="ManualWeaponController"/>) or the
    /// automatic fire control (<see cref="AutonomousWeaponController"/>). Controllers only set the aim
    /// and <see cref="TriggerHeld"/>.
    /// </summary>
    [DefaultExecutionOrder(110)] // after both controllers (90 auto, 100 manual) set the trigger this frame
    public sealed class WeaponStation : MonoBehaviour
    {
        public GunMount mount;
        public ProjectileSystem projectiles;
        [Tooltip("Optional: per-barrel recoil animation.")]
        public GunRecoil recoil;
        [Tooltip("Optional: muzzle flash, light and smoke.")]
        public MuzzleFlashFX muzzleFlash;
        [Tooltip("Optional: rotary barrel cluster (Gatling guns).")]
        public BarrelSpin spin;

        [Header("Third-person camera when this weapon is manual")]
        public float cameraDistance = 7.5f;
        public float cameraHeight = 2.8f;
        [Tooltip("Sideways camera offset (over-the-shoulder), metres; + = right. Keeps a tall mount from hiding the bore line.")]
        public float cameraSide;

        public WeaponDefinition Definition => mount.definition;
        public WeaponState State { get; private set; }
        /// <summary>True while the player controls this weapon.</summary>
        public bool Manual { get; set; }
        /// <summary>False while the weapon is still locked (shown at its position, not crewed).</summary>
        public bool Operational { get; set; } = true;
        public bool TriggerHeld { get; set; }
        /// <summary>Rounds fired while under manual control (the player's accuracy).</summary>
        public int ManualRoundsFired { get; private set; }
        /// <summary>Barrel speed 0..1 (rotary guns; 1 for others while firing).</summary>
        public float Spin01 { get; private set; }

        /// <summary>Raised for each round with the barrel index (manual controller adds camera kick).</summary>
        public event Action<int> Fired;

        private System.Random _rng;
        private int _roundCounter;

        private void Awake()
        {
            State = new WeaponState(Definition);
            _rng = new System.Random(4242 + Definition.weaponId.GetHashCode());
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            var def = Definition;
            bool trigger = TriggerHeld && Operational;

            // Rotary gun: the drive spins the barrels up while the trigger is held and they coast down after.
            float rate = 1f;
            if (def.spinUpTime > 0f)
            {
                bool drive = trigger && !State.Reloading && !State.Overheated && State.Ammo > 0;
                Spin01 = Mathf.MoveTowards(Spin01, drive ? 1f : 0f, dt / (drive ? def.spinUpTime : def.spinUpTime * 2.5f));
                rate = Spin01;
            }
            if (spin != null) spin.Speed01 = def.spinUpTime > 0f ? Spin01 : 0f;

            bool wasReloading = State.Reloading;
            int rounds = State.Tick(dt, trigger, rate);
            for (int i = 0; i < rounds; i++) FireOne();
            if (Manual && !wasReloading && State.Reloading) GameEvents.RaiseReloadStarted(State.Ammo == 0);
            TriggerHeld = false; // controllers set it again every frame
        }

        private void FireOne()
        {
            var def = Definition;
            Transform muzzle = mount.NextMuzzle(out int barrel);
            Vector3 dir = Ballistics.ApplyDispersion(mount.BoreDirection, def.dispersion, _rng);
            int n = _roundCounter++;
            bool tracer = def.tracerEvery > 0 && n % def.tracerEvery == 0;
            // Realistic round: drag, tracer burn, and self-destruct at a random time in the fuse window.
            bool sd = def.selfDestructTime.y > 0f;
            float life = sd ? Mathf.Lerp(def.selfDestructTime.x, def.selfDestructTime.y, (float)_rng.NextDouble()) : def.projectileLifetime;
            // Tracer compositions vary a little round to round (about +/-6%), so the stream fades out raggedly.
            float burn = def.tracerBurnTime * (0.94f + 0.12f * (float)_rng.NextDouble());
            bool smoke = _rng.NextDouble() < def.airburstSmokeChance;
            projectiles.Fire(muzzle.position, dir, def.muzzleVelocity, def.dragK, life, tracer ? burn : 0f, sd, def.damagePerRound,
                             def.airburstScale, smoke, Manual, def.tracerWidthScale, def.tracerLengthScale);
            if (Manual) ManualRoundsFired++;

            if (recoil != null) recoil.Kick(barrel);
            if (muzzleFlash != null && (def.flashEvery <= 1 || n % def.flashEvery == 0)) muzzleFlash.Flash(barrel);
            Fired?.Invoke(barrel);
            GameEvents.RaiseWeaponRoundFired(def, muzzle.position);
        }
    }
}
