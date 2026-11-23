using UnityEngine;

namespace AirDefense.Weapons
{
    /// <summary>
    /// Per-barrel recoil: on each shot the firing barrel snaps back along the bore
    /// and eases forward again ("returns to battery"). Barrels alternate, as on
    /// the twin ZU-23-2. Works on any mount: barrels are optional.
    /// </summary>
    public sealed class GunRecoil : MonoBehaviour
    {
        public GunMount mount;
        [Tooltip("Barrel transforms, same order as GunMount.muzzles.")]
        public Transform[] barrels;

        private Vector3[] _rest;
        private Vector3[] _back;   // local recoil direction per barrel
        private float[] _t;        // time since that barrel fired

        private void Start()
        {
            int n = barrels?.Length ?? 0;
            _rest = new Vector3[n];
            _back = new Vector3[n];
            _t = new float[n];
            for (int i = 0; i < n; i++)
            {
                _rest[i] = barrels[i].localPosition;
                // Rearward along the bore, expressed in the barrel's parent space (constant as the mount moves).
                _back[i] = barrels[i].parent.InverseTransformDirection(-mount.BoreDirection).normalized;
                _t[i] = 999f;
            }
        }

        public void Kick(int barrelIndex)
        {
            if (_t == null || _t.Length == 0) return;
            _t[barrelIndex % _t.Length] = 0f;
        }

        private void LateUpdate()
        {
            if (_t == null) return;
            var def = mount.definition;
            for (int i = 0; i < _t.Length; i++)
            {
                _t[i] += Time.deltaTime;
                // Instant rearward snap, then an ease-out return.
                float k = Mathf.Clamp01(_t[i] / Mathf.Max(0.01f, def.barrelReturnTime));
                float offset = def.barrelRecoilDistance * (1f - k) * (1f - k);
                barrels[i].localPosition = _rest[i] + _back[i] * offset;
            }
        }
    }
}
