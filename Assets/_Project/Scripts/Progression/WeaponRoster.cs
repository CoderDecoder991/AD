using System.Collections.Generic;
using AirDefense.Core;
using AirDefense.Data;
using UnityEngine;

namespace AirDefense.Progression
{
    /// <summary>
    /// The three weapon tiles in order (C-04) and the unlock sequence (R-04:
    /// C-RAM first, then Tunguska). Costs come from each WeaponDefinition.
    /// </summary>
    [CreateAssetMenu(menuName = "AirDefense/Weapon Roster", fileName = "WeaponRoster")]
    public sealed class WeaponRoster : ScriptableObject
    {
        public List<WeaponDefinition> weapons = new List<WeaponDefinition>();

        /// <summary>The next weapon to unlock in sequence, or null when all are unlocked.</summary>
        public WeaponDefinition NextUnlock(PlayerProfile p)
        {
            foreach (var w in weapons)
                if (!p.IsUnlocked(w.weaponId)) return w;
            return null;
        }

        /// <summary>Sequential rule: only the next weapon in order can be unlocked.</summary>
        public bool CanUnlock(PlayerProfile p, WeaponDefinition w) =>
            w != null && NextUnlock(p) == w && p.credits >= w.unlockCost;

        /// <summary>Spends the credits and unlocks <paramref name="w"/> if allowed. The caller saves the profile.</summary>
        public bool TryUnlock(PlayerProfile p, WeaponDefinition w)
        {
            if (!CanUnlock(p, w)) return false;
            p.credits -= w.unlockCost;
            p.unlockedWeapons.Add(w.weaponId);
            return true;
        }
    }
}
