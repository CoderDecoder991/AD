using UnityEngine;

namespace AirDefense.Core
{
    /// <summary>
    /// Sound clips for the mission (ASSET_REGISTER.md §5). Recordings are CC0
    /// (OpenGameArt); siren/static/ping/tone are synthesised by tools/audio/make_sfx.py.
    /// </summary>
    [CreateAssetMenu(menuName = "AirDefense/Audio Bank", fileName = "AudioBank")]
    public sealed class AudioBank : ScriptableObject
    {
        public AudioClip[] zuShots;
        [Tooltip("Rolling echo played when a burst ends.")]
        public AudioClip zuBurstTail;
        [Tooltip("Real recording: single opening shot (user's own recording, see ASSET_REGISTER S-01).")]
        public AudioClip zuFirstShot;
        [Tooltip("Real recording: seamless sustained-fire loop. When set, it replaces per-round shots.")]
        public AudioClip zuBurstLoop;
        public AudioClip[] airbursts;
        [Tooltip("Distant pops of rounds self-destructing (played distance / 343 m/s after the flash).")]
        public AudioClip[] selfDestructPops;
        public float selfDestructVolume = 0.7f;
        public AudioClip impactBase;
        public AudioClip boom;
        public AudioClip distantRumble;
        public AudioClip reload;
        public AudioClip emptyClick;
        public AudioClip siren;
        public AudioClip warningTone;
        public AudioClip radarPing;
        public AudioClip radioStaticLoop;

        [Header("Mix (0..1)")]
        public float gunVolume = 0.55f;
        public float explosionVolume = 0.9f;
        public float alertVolume = 0.45f;
        public float ambienceVolume = 0.12f;
    }
}
