using System;
using System.Collections.Generic;

namespace AirDefense.Core
{
    /// <summary>
    /// Everything saved on the device (TECHNICAL_ARCHITECTURE.md §5). Offline only (R-07).
    /// Bump <see cref="CurrentVersion"/> and add a migration step in SaveService when the shape changes.
    /// </summary>
    [Serializable]
    public sealed class PlayerProfile
    {
        public const int CurrentVersion = 1;

        public int saveVersion = CurrentVersion;
        public int credits;
        public List<string> unlockedWeapons = new List<string> { "zu23" };
        public List<string> completedMissions = new List<string>();
        public bool cinematicSeen;
        public SettingsData settings = new SettingsData();
        public StatsData stats = new StatsData();

        public bool IsUnlocked(string weaponId) => unlockedWeapons.Contains(weaponId);
        public bool HasCompleted(string missionId) => completedMissions.Contains(missionId);
    }

    [Serializable]
    public sealed class SettingsData
    {
        public float aimSensitivity = 1f;
        public bool invertY;
        public int aimAssist = 1;            // AimAssist enum
        public int qualityOverride = -1;     // -1 = auto, else QualityTier
        public float masterVolume = 1f;
        public bool screenShake = true;
    }

    [Serializable]
    public sealed class StatsData
    {
        public int missionsPlayed;
        public int missionsWon;
        public int threatsDestroyed;
        public int roundsFired;
        public int roundsHit;
        public int creditsEarnedTotal;
        public int bestGradeRank = -1;       // 0 = D ... 4 = S
    }
}
