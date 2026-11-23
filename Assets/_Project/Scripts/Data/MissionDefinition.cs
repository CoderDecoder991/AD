using System;
using System.Collections.Generic;
using UnityEngine;

namespace AirDefense.Data
{
    [Serializable]
    public sealed class SpawnGroup
    {
        public ThreatDefinition threat;
        public int count = 3;
        [Tooltip("Seconds after the wave starts before the first spawn.")]
        public float startDelay;
        [Tooltip("Seconds between spawns in this group.")]
        public float interval = 2f;
        [Tooltip("Compass bearing range (degrees, 0 = north, 90 = east) the group spawns from.")]
        public Vector2 bearingRange = new Vector2(20f, 70f);
        [Tooltip("Horizontal spawn distance range from the installation centre, metres.")]
        public Vector2 distanceRange = new Vector2(1800f, 2200f);
    }

    [Serializable]
    public sealed class WaveDefinition
    {
        public string label = "Wave 1";
        [Tooltip("Seconds of calm after the previous wave is cleared, before this one starts.")]
        public float preDelay = 4f;
        public List<SpawnGroup> groups = new List<SpawnGroup>();
    }

    /// <summary>A scripted mission: briefing text, waves and end conditions.</summary>
    [CreateAssetMenu(menuName = "AirDefense/Mission Definition", fileName = "Mission_")]
    public sealed class MissionDefinition : ScriptableObject
    {
        public string missionId = "m1_greybox";
        public string title = "Night Watch (greybox)";
        [TextArea] public string briefing = "Defend the installation.";
        public float installationHealth = 100f;
        [Tooltip("Seed for spawn randomness, so a mission plays the same way each time.")]
        public int seed = 1234;
        public List<WaveDefinition> waves = new List<WaveDefinition>();
    }
}
