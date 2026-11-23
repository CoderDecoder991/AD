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
        private AudioSource _ui;
        private AudioSource _siren;
        private AudioSource _ambience;
        private AudioSource[] _pool;
        private int _poolNext;
        private int _roundCount;

        private void Awake()
        {
            _gun = NewSource("Gun");
            _ui = NewSource("Alerts");
            _siren = NewSource("Siren");
            _ambience = NewSource("Ambience");
            _pool = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++) _pool[i] = NewSource("Fx" + i);
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
        }

        private void OnRoundFired(Vector3 _)
        {
            // Both barrels fire together: one sound per pair keeps voices (and mush) down.
            if ((_roundCount++ & 1) != 0 || bank.zuShots == null || bank.zuShots.Length == 0) return;
            _gun.pitch = Random.Range(0.95f, 1.04f);
            _gun.PlayOneShot(bank.zuShots[Random.Range(0, bank.zuShots.Length)], bank.gunVolume);
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

        private void OnMissionEnded(MissionResult _)
        {
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
