using UnityEngine;

namespace AirDefense.Weapons
{
    /// <summary>
    /// Input gathered from the touch UI each frame. The UI writes; the weapon
    /// controller reads and consumes. Keeps gameplay code free of UI details.
    /// </summary>
    public sealed class PlayerInputState : MonoBehaviour
    {
        /// <summary>Accumulated drag since last consume, in pixels.</summary>
        public Vector2 PendingAimPixels { get; private set; }
        public bool FireHeld { get; set; }
        public bool ReloadRequested { get; set; }
        public bool ZoomToggleRequested { get; set; }

        public void AddAimPixels(Vector2 delta) => PendingAimPixels += delta;

        public Vector2 ConsumeAimPixels()
        {
            Vector2 d = PendingAimPixels;
            PendingAimPixels = Vector2.zero;
            return d;
        }
    }
}
