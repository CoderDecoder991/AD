using UnityEngine;

namespace AirDefense.Data
{
    /// <summary>
    /// One weapon system. All values are FICTIONAL GAME-BALANCE VALUES (C-09),
    /// not real-world performance figures. See docs/WEAPON_SYSTEMS.md.
    /// </summary>
    [CreateAssetMenu(menuName = "AirDefense/Weapon Definition", fileName = "Weapon_")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string weaponId = "zu23";
        public string displayName = "ZU-23-2";
        [Tooltip("Credits to unlock. 0 = available from the start.")]
        public int unlockCost;

        [Header("Mount")]
        [Tooltip("Max traverse (yaw) speed, degrees/second.")]
        public float traverseSpeed = 60f;
        [Tooltip("Max elevation (pitch) speed, degrees/second.")]
        public float elevationSpeed = 45f;
        public float minElevation = -5f;
        public float maxElevation = 85f;

        [Header("Fire")]
        [Tooltip("Rounds per second across all barrels.")]
        public float fireRate = 30f;
        public int barrels = 2;
        public int magazineSize = 100;
        public float reloadTime = 4f;
        [Tooltip("Muzzle velocity, m/s (game units).")]
        public float muzzleVelocity = 900f;
        [Tooltip("Projectile lifetime, seconds (used when self-destruct is off).")]
        public float projectileLifetime = 2.2f;
        [Tooltip("Air drag constant k (1/m) in dv/dt = -k|v|v. 23 mm HEI-T ≈ 4.6e-4. 0 = no drag.")]
        public float dragK = 4.6e-4f;
        [Tooltip("Tracer burn time, seconds (23x152 HEI-T spec: at least 3.5 s). After this the round flies on unseen.")]
        public float tracerBurnTime = 3.5f;
        [Tooltip("Self-destruct window, seconds (23x152 HEI-T spec: about 5-12 s). 0 = no self-destruct.")]
        public Vector2 selfDestructTime = new Vector2(5.4f, 8f);
        public float damagePerRound = 1f;
        [Tooltip("Random spread, degrees (standard deviation of the cone).")]
        public float dispersion = 0.35f;
        [Tooltip("Every Nth round is drawn as a tracer.")]
        public int tracerEvery = 2;

        [Header("Heat")]
        [Tooltip("Heat added per second of continuous fire (0..1 scale).")]
        public float heatPerSecond = 0.2f;
        [Tooltip("Heat removed per second when not firing.")]
        public float coolPerSecond = 0.15f;
        [Tooltip("When overheated, firing is blocked until heat falls below this.")]
        public float overheatRecoverBelow = 0.3f;

        [Header("Recoil (feel)")]
        [Tooltip("How far each barrel slides back per shot, metres.")]
        public float barrelRecoilDistance = 0.12f;
        [Tooltip("Time for a barrel to return to battery, seconds.")]
        public float barrelReturnTime = 0.09f;
        [Tooltip("Camera kick upward per shot, degrees (decays quickly).")]
        public float cameraKick = 0.22f;
        [Tooltip("Random camera shake per shot, degrees.")]
        public float cameraShake = 0.12f;
        [Tooltip("Muzzle climb added to the aim per round, degrees (player must correct).")]
        public float muzzleClimbPerRound = 0.02f;
        [Tooltip("Random sideways drift per round, degrees.")]
        public float muzzleDriftPerRound = 0.012f;

        [Header("Ranges (for HUD/AI)")]
        public float minRange = 50f;
        public float maxRange = 1500f;

        public float SecondsPerRound => 1f / Mathf.Max(0.01f, fireRate);
    }
}
