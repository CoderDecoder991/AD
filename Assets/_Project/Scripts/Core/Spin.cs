using UnityEngine;

namespace AirDefense.Core
{
    /// <summary>M0 device-test helper: rotates an object at a constant rate.</summary>
    public sealed class Spin : MonoBehaviour
    {
        public Vector3 degreesPerSecond = new Vector3(0f, 45f, 0f);

        private void Update() => transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
