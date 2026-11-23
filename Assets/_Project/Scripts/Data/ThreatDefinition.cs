using UnityEngine;

namespace AirDefense.Data
{
    public enum ThreatClass
    {
        SmallDrone,
        FixedWingDrone,
        LoiteringMunition,
        Rocket,
        Mortar,
        CruiseMissile,
        StrikeAircraft
    }

    public enum ThreatMovement
    {
        /// <summary>Weaving approach, then a steep dive onto the target.</summary>
        WeaveAndDive,
        /// <summary>Straight, level run at altitude, then a shallow dive.</summary>
        StraightRunAndDive
    }

    /// <summary>
    /// One threat type. All values are FICTIONAL GAME-BALANCE VALUES (C-09).
    /// See docs/GDD.md §4.
    /// </summary>
    [CreateAssetMenu(menuName = "AirDefense/Threat Definition", fileName = "Threat_")]
    public sealed class ThreatDefinition : ScriptableObject
    {
        public string threatId = "small_drone";
        public string displayName = "Small drone";
        public ThreatClass threatClass = ThreatClass.SmallDrone;
        public ThreatMovement movement = ThreatMovement.WeaveAndDive;

        [Header("Flight")]
        public float speed = 30f;
        public float cruiseAltitude = 60f;
        [Tooltip("Begin the terminal dive at this horizontal distance from the target.")]
        public float diveDistance = 150f;
        [Tooltip("Sideways weave amplitude, metres (WeaveAndDive only).")]
        public float weaveAmplitude = 25f;
        [Tooltip("Weave frequency, cycles per second.")]
        public float weaveFrequency = 0.15f;

        [Header("Survivability")]
        public float health = 3f;
        [Tooltip("Gameplay hit radius, metres. Deliberately larger than the model for touch readability.")]
        public float hitRadius = 2.5f;

        [Header("Effect on the installation")]
        public float impactDamage = 8f;

        [Header("Rewards and presentation")]
        public int interceptCredits = 10;
        public Color radarColor = new Color(1f, 0.75f, 0.2f);
        [Tooltip("Model size multiplier for the greybox placeholder.")]
        public float visualScale = 1f;
    }
}
