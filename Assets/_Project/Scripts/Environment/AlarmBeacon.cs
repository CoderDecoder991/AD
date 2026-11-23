using UnityEngine;

namespace AirDefense.Environment
{
    /// <summary>
    /// Red rotating-style alarm beacon on a building: flashes a red light and lamp once the base is on
    /// alert (raised during the opening cinematic's radar shot, or immediately if it doesn't play).
    /// </summary>
    public sealed class AlarmBeacon : MonoBehaviour
    {
        public Light lamp;
        public Renderer bulb;
        public float period = 0.9f;
        public float intensity = 4f;
        private float _offset;

        private void Awake() => _offset = Random.value * period;

        private void Update()
        {
            bool alert = Cinematic.CinematicDirector.AlertRaised;
            // A rotating beacon seen from one side: a sharp bright pulse each turn.
            float phase = Mathf.Repeat(Time.time + _offset, period) / period;
            float pulse = alert ? Mathf.Pow(Mathf.Max(0f, Mathf.Sin(phase * Mathf.PI * 2f)), 6f) : 0f;
            if (lamp != null) lamp.intensity = intensity * pulse;
            if (bulb != null) bulb.enabled = alert && pulse > 0.15f;
        }
    }
}
