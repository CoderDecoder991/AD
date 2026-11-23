using AirDefense.Data;
using UnityEngine;

namespace AirDefense.Weapons
{
    public enum WeaponStatus { Ready, Firing, Reloading, Overheated }

    /// <summary>
    /// Ammunition, heat and reload state for one weapon. Plain C# (no MonoBehaviour)
    /// so the same rules apply whether the player or the AI drives it (C-07), and so
    /// it can be unit-tested. Call <see cref="Tick"/> once per frame.
    /// </summary>
    public sealed class WeaponState
    {
        public WeaponDefinition Def { get; }
        public int Ammo { get; private set; }
        public float Heat01 { get; private set; }
        public bool Overheated { get; private set; }
        public bool Reloading => _reloadRemaining > 0f;
        public float ReloadProgress01 => Reloading ? 1f - _reloadRemaining / Def.reloadTime : 1f;
        public int RoundsFired { get; private set; }

        public WeaponStatus Status =>
            Reloading ? WeaponStatus.Reloading :
            Overheated ? WeaponStatus.Overheated :
            _firedThisTick ? WeaponStatus.Firing : WeaponStatus.Ready;

        private float _reloadRemaining;
        private float _fireAccumulator;
        private bool _firedThisTick;

        public WeaponState(WeaponDefinition def)
        {
            Def = def;
            Ammo = def.magazineSize;
            _fireAccumulator = def.SecondsPerRound; // first press fires immediately
        }

        /// <summary>Starts a reload if the magazine is not full and not already reloading.</summary>
        public void RequestReload()
        {
            if (Reloading || Ammo >= Def.magazineSize) return;
            _reloadRemaining = Def.reloadTime;
        }

        /// <summary>
        /// Advances time by <paramref name="dt"/>. Returns how many rounds leave the barrel this tick.
        /// </summary>
        public int Tick(float dt, bool triggerHeld)
        {
            _firedThisTick = false;

            if (Reloading)
            {
                _reloadRemaining -= dt;
                if (_reloadRemaining <= 0f)
                {
                    _reloadRemaining = 0f;
                    Ammo = Def.magazineSize;
                }
            }

            bool canFire = triggerHeld && !Reloading && !Overheated && Ammo > 0;
            int rounds = 0;

            if (canFire)
            {
                _fireAccumulator += dt;
                float spr = Def.SecondsPerRound;
                while (_fireAccumulator >= spr && Ammo > 0)
                {
                    _fireAccumulator -= spr;
                    Ammo--;
                    rounds++;
                }
                Heat01 = Mathf.Min(1f, Heat01 + Def.heatPerSecond * dt);
                if (Heat01 >= 1f) Overheated = true;
                _firedThisTick = rounds > 0;
            }
            else
            {
                // First round fires as soon as the trigger is pressed again.
                _fireAccumulator = Def.SecondsPerRound;
                Heat01 = Mathf.Max(0f, Heat01 - Def.coolPerSecond * dt);
                if (Overheated && Heat01 < Def.overheatRecoverBelow) Overheated = false;
            }

            if (Ammo == 0 && !Reloading) RequestReload();

            RoundsFired += rounds;
            return rounds;
        }
    }
}
