using UnityEngine;

namespace AirDefense.Core
{
    public enum AimAssist { Off = 0, Light = 1 }

    /// <summary>Player settings stored in PlayerPrefs. Replaced by the save system in M2.</summary>
    public static class GameSettings
    {
        private const string KeySensitivity = "aim.sensitivity";
        private const string KeyInvertY = "aim.invertY";
        private const string KeyAssist = "aim.assist";

        public static float Sensitivity
        {
            get => PlayerPrefs.GetFloat(KeySensitivity, 1f);
            set { PlayerPrefs.SetFloat(KeySensitivity, Mathf.Clamp(value, 0.2f, 3f)); PlayerPrefs.Save(); }
        }

        public static bool InvertY
        {
            get => PlayerPrefs.GetInt(KeyInvertY, 0) == 1;
            set { PlayerPrefs.SetInt(KeyInvertY, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Light assist shows a lead indicator for the target nearest the sight.</summary>
        public static AimAssist Assist
        {
            get => (AimAssist)PlayerPrefs.GetInt(KeyAssist, (int)AimAssist.Light);
            set { PlayerPrefs.SetInt(KeyAssist, (int)value); PlayerPrefs.Save(); }
        }
    }
}
