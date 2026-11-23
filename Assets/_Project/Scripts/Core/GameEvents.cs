using System;
using AirDefense.Threats;
using UnityEngine;

namespace AirDefense.Core
{
    /// <summary>
    /// Decoupled gameplay events (TECHNICAL_ARCHITECTURE.md §3). UI, audio and
    /// (later) analytics subscribe; gameplay code does not know they exist.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<ThreatAgent> ThreatSpawned;
        public static event Action<ThreatAgent> ThreatDestroyed;
        public static event Action<ThreatAgent> ThreatImpacted;
        public static event Action<Vector3> RoundFired;
        public static event Action<int, string> WaveStarted;
        public static event Action<MissionResult> MissionEnded;
        /// <summary>bool = the magazine ran empty (vs. a manual early reload).</summary>
        public static event Action<bool> ReloadStarted;

        public static void RaiseReloadStarted(bool emptied) => ReloadStarted?.Invoke(emptied);
        /// <summary>A round self-destructed at this position (flash, sparks, distant pop).</summary>
        public static event Action<Vector3> RoundSelfDestructed;
        public static void RaiseRoundSelfDestructed(Vector3 pos) => RoundSelfDestructed?.Invoke(pos);

        public static void RaiseThreatSpawned(ThreatAgent t) => ThreatSpawned?.Invoke(t);
        public static void RaiseThreatDestroyed(ThreatAgent t) => ThreatDestroyed?.Invoke(t);
        public static void RaiseThreatImpacted(ThreatAgent t) => ThreatImpacted?.Invoke(t);
        public static void RaiseRoundFired(Vector3 muzzle) => RoundFired?.Invoke(muzzle);
        public static void RaiseWaveStarted(int index, string label) => WaveStarted?.Invoke(index, label);
        public static void RaiseMissionEnded(MissionResult r) => MissionEnded?.Invoke(r);

        /// <summary>Clears all subscribers. Called when a mission scene (re)loads.</summary>
        public static void Reset()
        {
            ThreatSpawned = null;
            ThreatDestroyed = null;
            ThreatImpacted = null;
            RoundFired = null;
            WaveStarted = null;
            MissionEnded = null;
            ReloadStarted = null;
            RoundSelfDestructed = null;
        }
    }

    public sealed class MissionResult
    {
        public bool success;
        public float duration;
        public float healthRemaining01;
        public int roundsFired;
        public int roundsHit;
        public int threatsDestroyed;
        public int threatsLeaked;
        public int threatsTotal;
        public int interceptCredits;
        public string missionId;

        public float Accuracy => roundsFired > 0 ? (float)roundsHit / roundsFired : 0f;
    }
}
