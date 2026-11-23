using System.Collections.Generic;
using AirDefense.Core;
using UnityEngine;

namespace AirDefense.Progression
{
    public struct RewardLine
    {
        public string label;
        public int credits;
        public RewardLine(string label, int credits) { this.label = label; this.credits = credits; }
    }

    public sealed class RewardBreakdown
    {
        public readonly List<RewardLine> lines = new List<RewardLine>();
        public int Total { get; private set; }

        public void Add(string label, int credits)
        {
            if (credits <= 0) return;
            lines.Add(new RewardLine(label, credits));
            Total += credits;
        }
    }

    /// <summary>
    /// Pure reward maths (unit-tested). Every credit is earned in play (C-06).
    /// A failed mission still pays for interceptions, so effort is never wasted,
    /// but no completion or performance bonuses.
    /// </summary>
    public static class RewardCalculator
    {
        public static RewardBreakdown Calculate(MissionResult r, EconomyConfig cfg, bool firstCompletion)
        {
            var b = new RewardBreakdown();
            b.Add($"Interceptions ({r.threatsDestroyed})", r.interceptCredits);
            if (!r.success) return b;

            int completion = Mathf.RoundToInt(cfg.missionCompletion * cfg.tierMultiplier);
            b.Add("Mission complete", completion);

            int basis = completion + r.interceptCredits;
            b.Add($"Installation {r.healthRemaining01 * 100f:0}%", Mathf.RoundToInt(basis * cfg.maxHealthBonus * Mathf.Clamp01(r.healthRemaining01)));
            float acc01 = Mathf.Clamp01(r.Accuracy / cfg.accuracyForFullBonus);
            b.Add($"Accuracy {r.Accuracy * 100f:0.0}%", Mathf.RoundToInt(basis * cfg.maxAccuracyBonus * acc01));
            if (firstCompletion) b.Add("First completion", cfg.firstCompletionBonus);
            return b;
        }
    }
}
