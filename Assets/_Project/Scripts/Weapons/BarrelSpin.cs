using UnityEngine;

namespace AirDefense.Weapons
{
    /// <summary>
    /// Turns a rotary gun's barrel cluster about the bore (local Z). A six-barrel 20 mm Gatling at
    /// 4,500 rounds/min turns at 750 rpm (12.5 rev/s); <see cref="Speed01"/> is set by the station.
    /// </summary>
    public sealed class BarrelSpin : MonoBehaviour
    {
        public Transform barrels;
        [Tooltip("Revolutions per second at full rate (rounds per second / number of barrels).")]
        public float maxRevsPerSecond = 12.5f;
        public float Speed01 { get; set; }

        private Quaternion _rest;
        private float _angle;

        private void Awake()
        {
            if (barrels != null) _rest = barrels.localRotation;
        }

        private void LateUpdate()
        {
            if (barrels == null || Speed01 <= 0f) return;
            _angle = Mathf.Repeat(_angle + Speed01 * maxRevsPerSecond * 360f * Time.deltaTime, 360f);
            barrels.localRotation = _rest * Quaternion.Euler(0f, 0f, _angle);
        }
    }
}
