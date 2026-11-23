using UnityEngine;

namespace AirDefense.Core
{
    public enum AimAssist { Off = 0, Light = 1 }

    /// <summary>Player settings, stored in the saved profile (SaveService).</summary>
    public static class GameSettings
    {
        private static SettingsData S => ProfileStore.Profile.settings;

        public static float Sensitivity
        {
            get => S.aimSensitivity;
            set { S.aimSensitivity = Mathf.Clamp(value, 0.2f, 3f); ProfileStore.Save(); }
        }

        public static bool InvertY
        {
            get => S.invertY;
            set { S.invertY = value; ProfileStore.Save(); }
        }

        /// <summary>Light assist shows a lead indicator for the target nearest the sight.</summary>
        public static AimAssist Assist
        {
            get => (AimAssist)S.aimAssist;
            set { S.aimAssist = (int)value; ProfileStore.Save(); }
        }

        /// <summary>-1 = automatic, otherwise a forced <see cref="QualityTier"/>.</summary>
        public static int QualityOverride
        {
            get => S.qualityOverride;
            set { S.qualityOverride = value; ProfileStore.Save(); QualityTiers.Apply(); }
        }
    }
}
