using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace AirDefense.Core
{
    /// <summary>
    /// Reads and writes the <see cref="PlayerProfile"/> as JSON.
    /// - Atomic-ish writes: write a temp file, keep the previous file as .bak, then swap.
    /// - Integrity hash: detects corruption and casual editing (offline single-player,
    ///   so this is not anti-cheat, just a guard against broken saves).
    /// - Corrupt or unreadable main file falls back to the .bak, then to a fresh profile.
    /// </summary>
    public sealed class SaveService
    {
        private const string Salt = "airdefense-profile-v1";

        [Serializable]
        private sealed class Envelope
        {
            public int version;
            public string payload;
            public string hash;
        }

        public string FilePath { get; }
        private string BackupPath => FilePath + ".bak";
        private string TempPath => FilePath + ".tmp";

        /// <summary>Why the last Load fell back, or null if it loaded cleanly.</summary>
        public string LastLoadIssue { get; private set; }

        public SaveService(string directory, string fileName = "profile.json")
        {
            Directory.CreateDirectory(directory);
            FilePath = Path.Combine(directory, fileName);
        }

        public PlayerProfile Load()
        {
            LastLoadIssue = null;
            if (TryRead(FilePath, out PlayerProfile p, out string issue)) return p;
            if (File.Exists(FilePath)) LastLoadIssue = issue;

            if (TryRead(BackupPath, out p, out string bakIssue))
            {
                LastLoadIssue = (LastLoadIssue ?? "main save missing") + "; restored from backup";
                return p;
            }
            if (LastLoadIssue != null) LastLoadIssue += $"; backup unusable ({bakIssue}); starting fresh";
            return new PlayerProfile();
        }

        public void Save(PlayerProfile profile)
        {
            profile.saveVersion = PlayerProfile.CurrentVersion;
            string payload = JsonUtility.ToJson(profile);
            var env = new Envelope { version = profile.saveVersion, payload = payload, hash = Hash(payload) };
            File.WriteAllText(TempPath, JsonUtility.ToJson(env), Encoding.UTF8);

            if (File.Exists(FilePath))
            {
                if (File.Exists(BackupPath)) File.Delete(BackupPath);
                File.Move(FilePath, BackupPath);
            }
            File.Move(TempPath, FilePath);
        }

        private static bool TryRead(string path, out PlayerProfile profile, out string issue)
        {
            profile = null;
            issue = null;
            if (!File.Exists(path)) { issue = "missing"; return false; }
            try
            {
                var env = JsonUtility.FromJson<Envelope>(File.ReadAllText(path, Encoding.UTF8));
                if (env == null || string.IsNullOrEmpty(env.payload)) { issue = "empty or malformed"; return false; }
                if (env.hash != Hash(env.payload)) { issue = "integrity check failed"; return false; }
                profile = JsonUtility.FromJson<PlayerProfile>(env.payload);
                if (profile == null) { issue = "payload malformed"; return false; }
                Migrate(profile, env.version);
                return true;
            }
            catch (Exception e)
            {
                issue = e.GetType().Name;
                return false;
            }
        }

        /// <summary>Upgrades older saves step by step. Version 1 is the first format.</summary>
        private static void Migrate(PlayerProfile p, int fromVersion)
        {
            // if (fromVersion < 2) { ...convert v1 -> v2... }
            if (p.unlockedWeapons == null || p.unlockedWeapons.Count == 0) p.unlockedWeapons = new System.Collections.Generic.List<string> { "zu23" };
            if (!p.unlockedWeapons.Contains("zu23")) p.unlockedWeapons.Add("zu23"); // starting weapon is always available (C-05)
            p.completedMissions ??= new System.Collections.Generic.List<string>();
            p.settings ??= new SettingsData();
            p.stats ??= new StatsData();
            if (p.credits < 0) p.credits = 0;
            p.saveVersion = PlayerProfile.CurrentVersion;
        }

        private static string Hash(string payload)
        {
            using var sha = SHA256.Create();
            byte[] h = sha.ComputeHash(Encoding.UTF8.GetBytes(Salt + payload));
            return Convert.ToBase64String(h);
        }
    }
}
