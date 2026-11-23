using UnityEngine;

namespace AirDefense.Progression
{
    /// <summary>
    /// Credit rewards. STARTING BALANCE VALUES from docs/ECONOMY_AND_MONETISATION.md §2,
    /// tuned in M6. Interception credits per threat live on each ThreatDefinition.
    /// </summary>
    [CreateAssetMenu(menuName = "AirDefense/Economy Config", fileName = "EconomyConfig")]
    public sealed class EconomyConfig : ScriptableObject
    {
        [Tooltip("Credits for completing a mission (before the tier multiplier).")]
        public int missionCompletion = 300;
        [Tooltip("Mission tier multiplier (x1.0 to x2.5 as missions get harder).")]
        public float tierMultiplier = 1f;
        [Tooltip("Bonus at 100% installation health, as a fraction of completion + interception credits.")]
        [Range(0f, 1f)] public float maxHealthBonus = 0.30f;
        [Tooltip("Bonus at full accuracy, as a fraction of completion + interception credits.")]
        [Range(0f, 1f)] public float maxAccuracyBonus = 0.15f;
        [Tooltip("Accuracy that earns the full accuracy bonus (gun hit rates are naturally low).")]
        [Range(0.01f, 1f)] public float accuracyForFullBonus = 0.25f;
        [Tooltip("One-off bonus the first time a mission is completed.")]
        public int firstCompletionBonus = 200;
    }
}
