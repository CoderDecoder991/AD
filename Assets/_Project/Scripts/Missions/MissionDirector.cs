using System.Collections;
using System.Collections.Generic;
using AirDefense.Core;
using AirDefense.Data;
using AirDefense.Threats;
using AirDefense.Weapons;
using UnityEngine;

namespace AirDefense.Missions
{
    public enum MissionPhase { Briefing, Running, Ended }

    /// <summary>
    /// Runs one <see cref="MissionDefinition"/>: spawns its waves in order, tracks
    /// installation health and statistics, and decides success or failure.
    /// Success = every wave spawned and cleared with health above zero.
    /// </summary>
    public sealed class MissionDirector : MonoBehaviour
    {
        public MissionDefinition mission;
        public ThreatManager threats;
        public ProjectileSystem projectiles;
        public ManualWeaponController player;

        public MissionPhase Phase { get; private set; } = MissionPhase.Briefing;
        public float Health { get; private set; }
        public float Health01 => mission.installationHealth > 0 ? Health / mission.installationHealth : 0f;
        public int WaveIndex { get; private set; } = -1;
        public int Destroyed { get; private set; }
        public int Leaked { get; private set; }
        public float Elapsed { get; private set; }
        public int TotalThreats { get; private set; }
        public MissionResult Result { get; private set; }

        private bool _spawningDone;

        private void Awake()
        {
            Health = mission.installationHealth;
            foreach (var w in mission.waves)
                foreach (var g in w.groups) TotalThreats += g.count;
            threats.SetSeed(mission.seed);
        }

        private void OnEnable()
        {
            GameEvents.ThreatDestroyed += OnDestroyed;
            GameEvents.ThreatImpacted += OnImpacted;
        }

        private void OnDisable()
        {
            GameEvents.ThreatDestroyed -= OnDestroyed;
            GameEvents.ThreatImpacted -= OnImpacted;
        }

        /// <summary>Called by the briefing UI.</summary>
        public void Begin()
        {
            if (Phase != MissionPhase.Briefing) return;
            Phase = MissionPhase.Running;
            player.InputEnabled = true;
            StartCoroutine(RunWaves());
        }

        private void Update()
        {
            if (Phase != MissionPhase.Running) return;
            Elapsed += Time.deltaTime;
            if (_spawningDone && threats.Active.Count == 0) End(true);
        }

        private IEnumerator RunWaves()
        {
            for (int w = 0; w < mission.waves.Count; w++)
            {
                WaveDefinition wave = mission.waves[w];
                yield return new WaitForSeconds(wave.preDelay);
                if (Phase != MissionPhase.Running) yield break;

                WaveIndex = w;
                GameEvents.RaiseWaveStarted(w, wave.label);

                var running = new List<Coroutine>();
                foreach (SpawnGroup g in wave.groups) running.Add(StartCoroutine(SpawnGroupRoutine(g)));
                foreach (Coroutine c in running) yield return c;

                // Next wave only after this one is cleared.
                while (threats.Active.Count > 0 && Phase == MissionPhase.Running) yield return null;
            }
            _spawningDone = true;
        }

        private IEnumerator SpawnGroupRoutine(SpawnGroup g)
        {
            if (g.startDelay > 0f) yield return new WaitForSeconds(g.startDelay);
            for (int i = 0; i < g.count; i++)
            {
                if (Phase != MissionPhase.Running) yield break;
                threats.Spawn(g);
                if (i < g.count - 1) yield return new WaitForSeconds(g.interval);
            }
        }

        private void OnDestroyed(ThreatAgent t)
        {
            Destroyed++;
            if (Debug.isDebugBuild)
                Debug.Log($"[Mission] {t.Definition.displayName} #{t.Id} destroyed at {Vector3.Distance(t.transform.position, player.mount.pitchPivot.position):0} m ({Destroyed}/{TotalThreats})");
        }

        private void OnImpacted(ThreatAgent t)
        {
            Leaked++;
            if (Phase != MissionPhase.Running) return;
            Health = Mathf.Max(0f, Health - t.Definition.impactDamage);
            if (Health <= 0f) End(false);
        }

        private void End(bool success)
        {
            if (Phase == MissionPhase.Ended) return;
            Phase = MissionPhase.Ended;
            player.InputEnabled = false;
            StopAllCoroutines();
            threats.ClearAll();

            Result = new MissionResult
            {
                success = success,
                duration = Elapsed,
                healthRemaining01 = Health01,
                roundsFired = player.State.RoundsFired,
                roundsHit = projectiles.RoundsHit,
                threatsDestroyed = Destroyed,
                threatsLeaked = Leaked,
                threatsTotal = TotalThreats
            };
            GameEvents.RaiseMissionEnded(Result);
        }

        /// <summary>Development-only stress test: spawns <paramref name="count"/> threats at once (M1 acceptance: 20).</summary>
        public void DebugSpawnStress(int count)
        {
            if (Phase != MissionPhase.Running || mission.waves.Count == 0) return;
            var source = mission.waves[mission.waves.Count - 1].groups;
            for (int i = 0; i < count; i++)
            {
                SpawnGroup g = source[i % source.Count];
                threats.Spawn(g);
                TotalThreats++;
            }
        }
    }
}
