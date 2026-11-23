using AirDefense.Data;
using UnityEngine;

namespace AirDefense.Weapons
{
    /// <summary>
    /// The physical rig of a gun: a yaw (traverse) pivot and a pitch (elevation)
    /// pivot that turn toward requested angles at the weapon's speed limits.
    /// Visual models are children of the pivots, so a placeholder can be swapped
    /// for a final model without code changes.
    /// </summary>
    public sealed class GunMount : MonoBehaviour
    {
        public WeaponDefinition definition;
        [Tooltip("Rotates around world Y.")]
        public Transform yawPivot;
        [Tooltip("Child of yawPivot; rotates around its local X.")]
        public Transform pitchPivot;
        [Tooltip("Muzzle points; rounds alternate between them.")]
        public Transform[] muzzles;
        [Tooltip("Operator camera anchor (child of yawPivot).")]
        public Transform cameraAnchor;

        public float TargetYaw { get; private set; }
        public float TargetPitch { get; private set; }
        public float Yaw { get; private set; }
        public float Pitch { get; private set; }

        private int _nextMuzzle;

        private void Awake()
        {
            Yaw = TargetYaw = yawPivot.eulerAngles.y;
            Pitch = TargetPitch = 10f;
            Apply();
        }

        /// <summary>Adds to the requested angles (degrees). Pitch is clamped to the mount limits.</summary>
        public void AddTarget(float deltaYaw, float deltaPitch)
        {
            TargetYaw = Mathf.Repeat(TargetYaw + deltaYaw, 360f);
            TargetPitch = Mathf.Clamp(TargetPitch + deltaPitch, definition.minElevation, definition.maxElevation);
        }

        public void SetTarget(float yaw, float pitch)
        {
            TargetYaw = Mathf.Repeat(yaw, 360f);
            TargetPitch = Mathf.Clamp(pitch, definition.minElevation, definition.maxElevation);
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            Yaw = Mathf.MoveTowardsAngle(Yaw, TargetYaw, definition.traverseSpeed * dt);
            Pitch = Mathf.MoveTowards(Pitch, TargetPitch, definition.elevationSpeed * dt);
            Apply();
        }

        private void Apply()
        {
            yawPivot.rotation = Quaternion.Euler(0f, Yaw, 0f);
            pitchPivot.localRotation = Quaternion.Euler(-Pitch, 0f, 0f);
        }

        public Vector3 BoreDirection => pitchPivot.forward;

        public Transform NextMuzzle()
        {
            var m = muzzles[_nextMuzzle % muzzles.Length];
            _nextMuzzle++;
            return m;
        }
    }
}
