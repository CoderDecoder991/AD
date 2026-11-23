using AirDefense.Threats;
using UnityEngine;

namespace AirDefense.Core
{
    /// <summary>
    /// Plays mission sounds from game events (UI_UX_FLOW.md §8). Explosions are
    /// delayed by distance at the speed of sound and quietened with range, so far
    /// kills are heard as a late, soft thump. Uses a small pool of 2D sources
    /// (cheap on mobile; no 3D spatialisation needed for a fixed operator position).
    /// </summary>
    public sealed class SfxPlayer : MonoBehaviour
    {
        public AudioBank bank;
        [Tooltip("Listener position for distance delays (the operator camera).")]
        public Transform listener;

        private const float SpeedOfSound = 343f;
        private const int PoolSize = 10;

        private AudioSource _gun;
        private AudioSource _gunB;
        private AudioSource _loop;
        private bool _loopFading;
        private int _lastShot = -1;
        private int _burstRounds;
        private float _lastRoundTime;
        private AudioSource _ui;

        // Distant self-destruct pops: scheduled by sound travel time, played from their own voice pool
        // so a long burst's 30-ish pops a second don't steal the gun or alert voices.
        private struct Pending { public float at; public float volume; public float pitch; }
        private readonly System.Collections.Generic.List<Pending> _pops = new System.Collections.Generic.List<Pending>(256);
        private AudioSource[] _popVoices;
        private int _popNext;
        private const int PopVoices = 12;
        private AudioSource _siren;
        private AudioSource _ambience;
        private AudioSource[] _pool;
        private int _poolNext;
        private int _roundCount;

        private void Awake()
        {
            _gun = NewSource("Gun L");
            _gun.panStereo = -0.12f;
            _gunB = NewSource("Gun R");
            _gunB.panStereo = 0.12f;
            _loop = NewSource("Gun Loop");
            _loop.loop = true;
            _loop.priority = 0; // never stolen by other sounds
            _ui = NewSource("Alerts");
            _siren = NewSource("Siren");
            _ambience = NewSource("Ambience");
            _pool = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++) _pool[i] = NewSource("Fx" + i);
            _popVoices = new AudioSource[PopVoices];
            for (int i = 0; i < PopVoices; i++) _popVoices[i] = NewSource("Pop" + i);
            AudioListener.volume = ProfileStore.Profile.settings.masterVolume;
        }

        private void Start()
        {
            if (bank.radioStaticLoop != null)
            {
                _ambience.clip = bank.radioStaticLoop;
                _ambience.loop = true;
                _ambience.volume = bank.ambienceVolume;
                _ambience.Play();
            }
        }

        private AudioSource NewSource(string n)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            return s;
        }

        private void OnEnable()
        {
            GameEvents.RoundFired += OnRoundFired;
            GameEvents.ThreatDestroyed += OnDestroyed;
            GameEvents.ThreatImpacted += OnImpacted;
            GameEvents.ThreatSpawned += OnSpawned;
            GameEvents.WaveStarted += OnWave;
            GameEvents.ReloadStarted += OnReload;
            GameEvents.MissionEnded += OnMissionEnded;
            GameEvents.RoundSelfDestructed += OnSelfDestruct;
        }

        private void OnDisable()
        {
            GameEvents.RoundFired -= OnRoundFired;
            GameEvents.ThreatDestroyed -= OnDestroyed;
            GameEvents.ThreatImpacted -= OnImpacted;
            GameEvents.ThreatSpawned -= OnSpawned;
            GameEvents.WaveStarted -= OnWave;
            GameEvents.ReloadStarted -= OnReload;
            GameEvents.MissionEnded -= OnMissionEnded;
            GameEvents.RoundSelfDestructed -= OnSelfDestruct;
        }

        private void OnRoundFired(Vector3 _)
        {
            _lastRoundTime = Time.time;
            if (bank.zuBurstLoop != null)
            {
                // Real recording: opening shot on the first round, then the sustained-fire loop.
                _burstRounds++;
                if (_burstRounds == 1)
                {
                    _gun.pitch = Random.Range(0.98f, 1.02f);
                    _gun.PlayOneShot(bank.zuFirstShot, bank.gunVolume);
                }
                else if (_burstRounds == 2)
                {
                    _loop.clip = bank.zuBurstLoop;
                    _loop.volume = bank.gunVolume;
                    _loop.pitch = 1f;
                    _loop.time = 0f;
                    _loop.Play();
                    _loopFading = false;
                }
                return;
            }

            if (bank.zuShots == null || bank.zuShots.Length == 0) return;
            // Barrels fire alternately: each round plays, panned slightly to its barrel's side.
            AudioSource s = (_roundCount++ & 1) == 0 ? _gun : _gunB;
            s.pitch = Random.Range(0.96f, 1.03f);
            int pick = Random.Range(0, bank.zuShots.Length);
            if (pick == _lastShot) pick = (pick + 1) % bank.zuShots.Length; // never the same take twice in a row
            _lastShot = pick;
            s.PlayOneShot(bank.zuShots[pick], bank.gunVolume);
            _burstRounds++;
            _lastRoundTime = Time.time;
        }

        private void Update()
        {
            PlayDuePops();
            // Burst ended: stop the loop with a quick fade and play the rolling echo (stronger after longer bursts).
            if (_burstRounds > 0 && Time.time - _lastRoundTime > 0.07f)
            {
                if (_loop.isPlaying) _loopFading = true;
                if (bank.zuBurstTail != null)
                    _ui.PlayOneShot(bank.zuBurstTail, bank.gunVolume * Mathf.Clamp(_burstRounds / 12f, 0.35f, 1f));
                _burstRounds = 0;
            }
            if (_loopFading)
            {
                _loop.volume -= bank.gunVolume * Time.deltaTime / 0.04f; // 40 ms release
                if (_loop.volume <= 0.001f) { _loop.Stop(); _loopFading = false; }
            }
        }

        private void OnDestroyed(ThreatAgent t)
        {
            if (bank.airbursts == null || bank.airbursts.Length == 0) return;
            PlayAtDistance(bank.airbursts[Random.Range(0, bank.airbursts.Length)], t.transform.position, bank.explosionVolume);
        }

        private void OnImpacted(ThreatAgent t)
        {
            PlayAtDistance(bank.impactBase, t.transform.position, 1f);
            PlayAtDistance(bank.boom, t.transform.position, 0.8f);
            _ui.PlayOneShot(bank.warningTone, bank.alertVolume);
        }

        private void OnSpawned(ThreatAgent _) => _ui.PlayOneShot(bank.radarPing, bank.alertVolume * 0.5f);

        private void OnWave(int index, string label)
        {
            if (bank.siren != null)
            {
                _siren.clip = bank.siren;
                _siren.volume = bank.alertVolume;
                _siren.Play();
            }
            _ui.PlayOneShot(bank.warningTone, bank.alertVolume);
            if (bank.distantRumble != null && index > 0)
                PlayDelayed(bank.distantRumble, 1.5f, 0.6f);
        }

        private void OnReload(bool emptied)
        {
            if (emptied) _ui.PlayOneShot(bank.emptyClick, 0.8f);
            _ui.PlayOneShot(bank.reload, 0.7f);
        }

        /// <summary>
        /// Sound reaches the listener distance / 343 m/s after the flash, quieter with distance
        /// (spherical spreading, gently floored so far pops stay audible) and slightly lower.
        /// </summary>
        private void OnSelfDestruct(Vector3 pos)
        {
            if (bank.selfDestructPops == null || bank.selfDestructPops.Length == 0) return;
            float d = listener ? Vector3.Distance(listener.position, pos) : 2500f;
            if (_pops.Count > 200) return; // a very long burst: enough pops already queued
            _pops.Add(new Pending
            {
                at = Time.time + d / SpeedOfSound,
                volume = bank.selfDestructVolume * Mathf.Clamp(900f / Mathf.Max(1f, d), 0.12f, 1f) * Random.Range(0.7f, 1.1f),
                pitch = Mathf.Lerp(1.05f, 0.85f, Mathf.InverseLerp(1500f, 4000f, d)) * Random.Range(0.94f, 1.06f),
            });
        }

        private void PlayDuePops()
        {
            for (int i = _pops.Count - 1; i >= 0; i--)
            {
                if (_pops[i].at > Time.time) continue;
                AudioSource v = _popVoices[_popNext];
                _popNext = (_popNext + 1) % _popVoices.Length;
                v.pitch = _pops[i].pitch;
                v.PlayOneShot(bank.selfDestructPops[Random.Range(0, bank.selfDestructPops.Length)], _pops[i].volume);
                _pops.RemoveAt(i);
            }
        }

        private void OnMissionEnded(MissionResult _)
        {
            _loop.Stop();
            _siren.Stop();
            _ambience.volume = bank.ambienceVolume * 0.5f;
        }

        private void PlayAtDistance(AudioClip clip, Vector3 pos, float volume)
        {
            if (clip == null) return;
            float d = listener ? Vector3.Distance(listener.position, pos) : 0f;
            float attenuation = Mathf.Clamp(250f / Mathf.Max(250f, d), 0.18f, 1f); // gentle: keep far kills audible
            PlayDelayed(clip, d / SpeedOfSound, volume * attenuation, pitch: Mathf.Lerp(1f, 0.85f, Mathf.InverseLerp(200f, 2000f, d)));
        }

        private void PlayDelayed(AudioClip clip, float delay, float volume, float pitch = 1f)
        {
            AudioSource s = _pool[_poolNext];
            _poolNext = (_poolNext + 1) % _pool.Length;
            s.Stop();
            s.clip = clip;
            s.volume = volume;
            s.pitch = pitch * Random.Range(0.96f, 1.04f);
            s.PlayDelayed(delay);
        }
    }
}
