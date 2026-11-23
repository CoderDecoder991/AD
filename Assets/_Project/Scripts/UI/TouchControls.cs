using AirDefense.Weapons;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AirDefense.UI
{
    /// <summary>
    /// Invisible panel over the aiming area (right ~60% of the screen, R-06).
    /// Any finger dragging here aims; other fingers can hold FIRE at the same time
    /// because uGUI tracks each pointer separately.
    /// </summary>
    public sealed class AimPanel : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public PlayerInputState input;
        public System.Action FirstDrag;
        private int _activePointer = int.MinValue;

        public void OnPointerDown(PointerEventData e)
        {
            if (_activePointer == int.MinValue) _activePointer = e.pointerId;
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != _activePointer) return;
            input.AddAimPixels(e.delta);
            FirstDrag?.Invoke();
            FirstDrag = null;
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId == _activePointer) _activePointer = int.MinValue;
        }
    }

    /// <summary>
    /// A button that reports whether it is currently held down (for FIRE).
    /// uGUI sends PointerUp to the pressed object even if the thumb slid off it,
    /// so a slightly moving thumb keeps firing until it lifts.
    /// </summary>
    public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public bool Held { get; private set; }
        public System.Action<bool> Changed;

        public void OnPointerDown(PointerEventData e) => Set(true);
        public void OnPointerUp(PointerEventData e) => Set(false);

        private void OnDisable() => Set(false);

        private void Set(bool v)
        {
            if (Held == v) return;
            Held = v;
            Changed?.Invoke(v);
        }
    }
}
