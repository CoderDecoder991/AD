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
        private int _lastShot = -1;
        private AudioSource _ui;

        /// <summary>
        /// One weapon's firing sound: an opening clip on the first round (ZU: the first shot; C-RAM: the
        /// barrels spinning up), then a seamless sustained-fire loop, then a rolling echo when the burst
        /// ends. Quieter with distance from the camera, so the gun you are not operating sounds distant.
        /// </summary>
        private sealed class GunVoice
        {
            public AudioClip first, loop, tail;
            public int loopFrom = 2;
            public float volume, gap = 0.07f, gain = 1f;
            public AudioSource oneShot, loopSrc;
            public int rounds;
            public float lastRound;
            public bool fading;
        }
        private readonly System.Collections.Generic.Dictionary<string, GunVoice> _voices =
            new System.Collections.Generic.Dictionary<string, GunVoice>();

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

        private void Awake()
        {
            _gun = NewSource("Gun L");
            _gun.panStereo = -0.12f;
            _gunB = NewSource("Gun R");
            _gunB.panStereo = 0.12f;
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
            GameEvents.WeaponRoundFired += OnRoundFired;
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
            GameEvents.WeaponRoundFired -= OnRoundFired;
            GameEvents.ThreatDestroyed -= OnDestroyed;
            GameEvents.ThreatImpacted -= OnImpacted;
            GameEvents.ThreatSpawned -= OnSpawned;
            GameEvents.WaveStarted -= OnWave;
            GameEvents.ReloadStarted -= OnReload;
            GameEvents.MissionEnded -= OnMissionEnded;
            GameEvents.RoundSelfDestructed -= OnSelfDestruct;
        }

        private GunVoice VoiceFor(Data.WeaponDefinition w)
        {
            if (_voices.TryGetValue(w.weaponId, out var v)) return v;
            v = new GunVoice();
            if (w.weaponId == "cram" && bank.cramBurstLoop != null)
            {
                v.first = bank.cramSpinUp;
                v.loop = bank.cramBurstLoop;
                v.tail = bank.cramTail != null ? bank.cramTail : bank.zuBurstTail;
                v.loopFrom = Mathf.Max(2, bank.cramLoopFromRound);
                v.volume = bank.cramVolume;
            }
            else
            {
                v.first = bank.zuFirstShot;
                v.loop = bank.zuBurstLoop;
                v.tail = bank.zuBurstTail;
                v.volume = bank.gunVolume;
            }
            // A rotary gun's first rounds come slowly while it spins up: allow for that before calling the burst over.
            v.gap = w.spinUpTime > 0f ? Mathf.Max(0.07f, 1.5f / (w.fireRate * Weapons.WeaponState.MinSpinToFire)) : 0.07f;
            v.oneShot = NewSource("Gun " + w.weaponId);
            v.loopSrc = NewSource("Gun Loop " + w.weaponId);
            v.loopSrc.loop = true;
            v.loopSrc.priority = 0; // never stolen by other sounds
            _voices[w.weaponId] = v;
            return v;
        }

        private void OnRoundFired(Data.WeaponDefinition w, Vector3 muzzle)
        {
            GunVoice v = VoiceFor(w);
            float d = listener ? Vector3.Distance(listener.position, muzzle) : 0f;
            v.gain = Mathf.Clamp(14f / Mathf.Max(14f, d), 0.12f, 1f); // the operated gun is ~8-10 m from the camera
            v.lastRound = Time.time;
            if (v.loop != null)
            {
                // Real recording: opening clip on the first round, then the sustained-fire loop.
                v.rounds++;
                if (v.rounds == 1 && v.first != null)
                {
                    v.oneShot.pitch = Random.Range(0.98f, 1.02f);
                    v.oneShot.PlayOneShot(v.first, v.volume * v.gain);
                }
                else if (v.rounds == v.loopFrom)
                {
                    v.loopSrc.clip = v.loop;
                    v.loopSrc.pitch = 1f;
                    v.loopSrc.time = 0f;
                    v.loopSrc.Play();
                    v.fading = false;
                }
                if (!v.fading) v.loopSrc.volume = v.volume * v.gain;
                return;
            }

            if (bank.zuShots == null || bank.zuShots.Length == 0) return;
            // Barrels fire alternately: each round plays, panned slightly to its barrel's side.
            AudioSource s = (v.rounds++ & 1) == 0 ? _gun : _gunB;
            s.pitch = Random.Range(0.96f, 1.03f);
            int pick = Random.Range(0, bank.zuShots.Length);
            if (pick == _lastShot) pick = (pick + 1) % bank.zuShots.Length; // never the same take twice in a row
            _lastShot = pick;
            s.PlayOneShot(bank.zuShots[pick], v.volume * v.gain);
        }

        private void Update()
        {
            PlayDuePops();
            foreach (var v in _voices.Values)
            {
                // Burst ended: stop the loop with a quick fade and play the rolling echo (stronger after longer bursts).
                if (v.rounds > 0 && Time.time - v.lastRound > v.gap)
                {
                    if (v.loopSrc.isPlaying) v.fading = true;
                    if (v.tail != null)
                        _ui.PlayOneShot(v.tail, v.volume * v.gain * Mathf.Clamp(v.rounds / 12f, 0.35f, 1f));
                    v.rounds = 0;
                }
                if (v.fading)
                {
                    v.loopSrc.volume -= v.volume * Time.deltaTime / 0.04f; // 40 ms release
                    if (v.loopSrc.volume <= 0.001f) { v.loopSrc.Stop(); v.fading = false; }
                }
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
            foreach (var v in _voices.Values) v.loopSrc.Stop();
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
