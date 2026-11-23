using System.Collections.Generic;
using AirDefense.Data;
using AirDefense.Threats;
using AirDefense.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace AirDefense.UI
{
    /// <summary>
    /// North-up radar (UI_UX_FLOW.md §4): base at centre, range rings, blips
    /// shape-coded by threat class, and the gun's current heading.
    /// </summary>
    public sealed class RadarView : MonoBehaviour
    {
        public ThreatManager threats;
        public GunMount gun;
        public Transform center;
        public float rangeMetres = 2500f;

        private RectTransform _rect;
        private RectTransform _blipRoot;
        private RectTransform _heading;
        private readonly List<Image> _blips = new List<Image>();

        public static RadarView Build(RectTransform parent, ThreatManager threats, GunMount gun, Transform center)
        {
            var root = UiFactory.Image("Radar", parent, new Color(0.02f, 0.08f, 0.04f, 0.78f), UiFactory.Circle).rectTransform;
            var view = root.gameObject.AddComponent<RadarView>();
            view.threats = threats;
            view.gun = gun;
            view.center = center;
            view._rect = root;

            foreach (float k in new[] { 1f, 0.66f, 0.33f })
            {
                var ring = UiFactory.Image("Ring", root, new Color(0.3f, 0.9f, 0.4f, 0.35f), UiFactory.Ring).rectTransform;
                ring.Stretch(new Vector2(0.5f - k / 2f, 0.5f - k / 2f), new Vector2(0.5f + k / 2f, 0.5f + k / 2f));
            }

            view._heading = UiFactory.Image("Heading", root, new Color(0.6f, 1f, 0.6f, 0.8f)).rectTransform;
            view._heading.anchorMin = view._heading.anchorMax = new Vector2(0.5f, 0.5f);
            view._heading.pivot = new Vector2(0.5f, 0f);
            view._heading.sizeDelta = new Vector2(3f, 10f);

            var n = UiFactory.Text("N", root, "N", 22, TextAnchor.UpperCenter, new Color(0.6f, 1f, 0.6f, 0.8f));
            n.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(40f, 30f));

            var me = UiFactory.Image("Base", root, new Color(0.6f, 1f, 0.6f, 1f), UiFactory.Circle).rectTransform;
            me.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 10f));

            view._blipRoot = UiFactory.Rect("Blips", root);
            view._blipRoot.Stretch(Vector2.zero, Vector2.one);
            return view;
        }

        private void LateUpdate()
        {
            float radius = _rect.rect.width * 0.5f;
            _heading.sizeDelta = new Vector2(3f, radius * 0.9f);
            _heading.localRotation = Quaternion.Euler(0f, 0f, -gun.Yaw);

            var list = threats.Active;
            int shown = 0;
            for (int i = 0; i < list.Count; i++)
            {
                ThreatAgent t = list[i];
                Vector3 d = t.transform.position - center.position;
                Vector2 p = new Vector2(d.x, d.z) / rangeMetres;
                if (p.sqrMagnitude > 1f) p = p.normalized; // clamp to the rim
                Image blip = GetBlip(shown++);
                blip.color = t.Definition.radarColor;
                var rt = blip.rectTransform;
                rt.anchoredPosition = p * radius;
                bool small = t.Definition.threatClass == ThreatClass.SmallDrone;
                // Shape coding (colour is secondary): small drone = square, fixed-wing = diamond.
                rt.sizeDelta = small ? new Vector2(10f, 10f) : new Vector2(14f, 14f);
                rt.localRotation = small ? Quaternion.identity : Quaternion.Euler(0f, 0f, 45f);
            }
            for (int i = shown; i < _blips.Count; i++) _blips[i].enabled = false;
        }

        private Image GetBlip(int i)
        {
            while (_blips.Count <= i)
            {
                var img = UiFactory.Image("Blip", _blipRoot, Color.white);
                img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                _blips.Add(img);
            }
            _blips[i].enabled = true;
            return _blips[i];
        }
    }
}
