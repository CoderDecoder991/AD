using UnityEngine;

namespace AirDefense.Core
{
    /// <summary>
    /// PLACEHOLDER sound effects synthesised at runtime (no audio assets yet).
    /// Replaced by licensed SFX in M2 (ASSET_REGISTER.md S-01, S-04, S-06).
    /// </summary>
    public sealed class ProceduralAudio : MonoBehaviour
    {
        private const int SampleRate = 44100;

        private AudioSource _gunSource;
        private AudioSource _fxSource;
        private AudioClip _gunshot;
        private AudioClip _explosion;
        private AudioClip _alarm;
        private AudioClip _ping;

        private void Awake()
        {
            _gunSource = gameObject.AddComponent<AudioSource>();
            _gunSource.playOnAwake = false;
            _gunSource.volume = 0.35f;
            _fxSource = gameObject.AddComponent<AudioSource>();
            _fxSource.playOnAwake = false;

            var rng = new System.Random(7);
            _gunshot = MakeNoiseBurst("gunshot", 0.09f, 35f, 0.9f, rng);
            _explosion = MakeNoiseBurst("explosion", 0.9f, 4.5f, 0.6f, rng);
            _alarm = MakeTone("alarm", 0.35f, 880f, 660f);
            _ping = MakeTone("ping", 0.08f, 1400f, 1400f);
        }

        private void OnEnable()
        {
            GameEvents.RoundFired += OnRoundFired;
            GameEvents.ThreatDestroyed += OnDestroyed;
            GameEvents.ThreatImpacted += OnImpacted;
            GameEvents.ThreatSpawned += OnSpawned;
        }

        private void OnDisable()
        {
            GameEvents.RoundFired -= OnRoundFired;
            GameEvents.ThreatDestroyed -= OnDestroyed;
            GameEvents.ThreatImpacted -= OnImpacted;
            GameEvents.ThreatSpawned -= OnSpawned;
        }

        private void OnRoundFired(Vector3 _)
        {
            _gunSource.pitch = Random.Range(0.92f, 1.05f);
            _gunSource.PlayOneShot(_gunshot);
        }

        private void OnDestroyed(Threats.ThreatAgent _) => _fxSource.PlayOneShot(_explosion, 0.8f);
        private void OnImpacted(Threats.ThreatAgent _) { _fxSource.PlayOneShot(_explosion, 1f); _fxSource.PlayOneShot(_alarm, 0.6f); }
        private void OnSpawned(Threats.ThreatAgent _) => _fxSource.PlayOneShot(_ping, 0.25f);

        private static AudioClip MakeNoiseBurst(string name, float seconds, float decay, float lowpass, System.Random rng)
        {
            int n = Mathf.CeilToInt(seconds * SampleRate);
            var data = new float[n];
            float prev = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                prev = Mathf.Lerp(noise, prev, lowpass); // simple one-pole low-pass
                data[i] = prev * Mathf.Exp(-decay * t) * 1.6f;
            }
            var clip = AudioClip.Create(name, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip MakeTone(string name, float seconds, float startHz, float endHz)
        {
            int n = Mathf.CeilToInt(seconds * SampleRate);
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float k = (float)i / n;
                float hz = Mathf.Lerp(startHz, endHz, k);
                phase += 2f * Mathf.PI * hz / SampleRate;
                float env = Mathf.Min(1f, k * 20f) * (1f - k);
                data[i] = Mathf.Sin(phase) * env * 0.5f;
            }
            var clip = AudioClip.Create(name, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
