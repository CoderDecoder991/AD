using System.Collections.Generic;
using AirDefense.Data;
using AirDefense.Threats;
using AirDefense.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace AirDefense.UI
{
    /// <summary>
    /// North-up search radar (docs/HUD_UI_SPECIFICATION.md §4, VS-2).
    /// - Rotating sweep (2.5 s per turn) with afterglow; contacts brighten as the sweep passes and fade
    ///   between passes (never fully, while tracked).
    /// - Labelled range rings, north tick, the selected weapon's facing as a wedge.
    /// - Class symbols: drone = triangle, aircraft = chevron, pointing along the contact's track;
    ///   threat colour, critical (pulsing) when diving onto the base.
    /// - Selected state: bracket on the contact closest to the gun's bore; autonomous engagements: blue ring.
    /// </summary>
    public sealed class RadarView : MonoBehaviour
    {
        public ThreatManager threats;
        public GunMount gun;
        public Transform center;
        public float rangeMetres = 3000f;
        public float sweepPeriod = 2.5f;

        private RectTransform _rect;
        private RectTransform _blipRoot;
        private RectTransform _wedge;
        private RectTransform _sweep;
        private RectTransform _selected;
        private AutonomousWeaponController[] _autos;
        private readonly List<Blip> _blips = new List<Blip>();

        private sealed class Blip
        {
            public RectTransform rt;
            public Image symbol;
            public Image engaged;
        }

        public static RadarView Build(RectTransform parent, ThreatManager threats, GunMount gun, Transform center)
        {
            var root = UiFactory.Image("Radar", parent, new Color(0.02f, 0.06f, 0.045f, 0.82f), UiFactory.Circle).rectTransform;
            var view = root.gameObject.AddComponent<RadarView>();
            view.threats = threats;
            view.gun = gun;
            view.center = center;
            view._rect = root;
            Color line = UiTheme.System.WithAlpha(0.32f);

            // Outer rim and range rings with labels (1 / 2 / 3 km).
            var rim = UiFactory.Image("Rim", root, UiTheme.System.WithAlpha(0.7f), UiFactory.ThinRing).rectTransform;
            rim.Stretch(Vector2.zero, Vector2.one);
            foreach (float k in new[] { 2f / 3f, 1f / 3f })
            {
                var ring = UiFactory.Image("Ring", root, line, UiFactory.ThinRing).rectTransform;
                ring.Stretch(new Vector2(0.5f - k / 2f, 0.5f - k / 2f), new Vector2(0.5f + k / 2f, 0.5f + k / 2f));
            }
            for (int i = 1; i <= 3; i++)
            {
                float k = i / 3f;
                var lbl = UiFactory.Text("Range" + i, root, $"{view.rangeMetres * k / 1000f:0} km", UiTheme.Caption - 3,
                                         TextAnchor.LowerLeft, UiTheme.System.WithAlpha(0.6f), UiTheme.Weight.Regular);
                // Along the 45° line, just inside each ring (south-east quadrant, away from the north tick).
                float off = 0.5f + k * 0.5f * 0.707f;
                lbl.rectTransform.Anchor(new Vector2(off, 1f - off), new Vector2(1f, 0f), new Vector2(-2f, 2f), new Vector2(60f, 20f));
            }
            // Cross lines (N-S, E-W), faint.
            foreach (bool vertical in new[] { true, false })
            {
                var l = UiFactory.Image(vertical ? "NS" : "EW", root, UiTheme.System.WithAlpha(0.14f)).rectTransform;
                l.anchorMin = l.anchorMax = new Vector2(0.5f, 0.5f);
                l.sizeDelta = vertical ? new Vector2(1.5f, 270f) : new Vector2(270f, 1.5f);
            }

            view._wedge = UiFactory.Image("WeaponWedge", root, UiTheme.System.WithAlpha(0.28f), UiFactory.Wedge).rectTransform;
            view._wedge.Stretch(Vector2.zero, Vector2.one);
            view._sweep = UiFactory.Image("Sweep", root, UiTheme.System.WithAlpha(0.85f), UiFactory.Sweep).rectTransform;
            view._sweep.Stretch(Vector2.zero, Vector2.one);

            var n = UiFactory.Text("N", root, "N", UiTheme.Caption, TextAnchor.UpperCenter, UiTheme.System, UiTheme.Weight.SemiBold);
            n.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -3f), new Vector2(40f, 24f));

            var me = UiFactory.Image("Base", root, UiTheme.System, UiFactory.Circle).rectTransform;
            me.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8f, 8f));

            view._blipRoot = UiFactory.Rect("Blips", root);
            view._blipRoot.Stretch(Vector2.zero, Vector2.one);
            view._selected = UiFactory.Image("Selected", view._blipRoot, UiTheme.Ink, UiFactory.Bracket).rectTransform;
            view._selected.anchorMin = view._selected.anchorMax = new Vector2(0.5f, 0.5f);
            view._selected.sizeDelta = new Vector2(26f, 26f);
            view._autos = Object.FindObjectsByType<AutonomousWeaponController>(FindObjectsInactive.Exclude);
            return view;
        }

        private void LateUpdate()
        {
            float radius = _rect.rect.width * 0.5f;
            float sweepAngle = Mathf.Repeat(Time.time / sweepPeriod * 360f, 360f); // clockwise from north
            _sweep.localRotation = Quaternion.Euler(0f, 0f, -sweepAngle);
            _wedge.localRotation = Quaternion.Euler(0f, 0f, -gun.Yaw);

            var list = threats.Active;
            int shown = 0;
            RectTransform selected = null;
            float bestAngle = 8f;
            Vector3 bore = gun.BoreDirection;
            for (int i = 0; i < list.Count; i++)
            {
                ThreatAgent t = list[i];
                if (!t.Alive) continue;
                Vector3 d = t.transform.position - center.position;
                Vector2 p = new Vector2(d.x, d.z) / rangeMetres;
                if (p.sqrMagnitude > 1f) p = p.normalized * 0.97f; // clamp to the rim
                Blip b = GetBlip(shown++);
                b.rt.anchoredPosition = p * radius;

                // Brightness: full as the sweep crosses the contact's bearing, fading to 35% before the next pass.
                float bearing = Mathf.Repeat(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 360f);
                float since = Mathf.Repeat(sweepAngle - bearing, 360f) / 360f * sweepPeriod;
                float glow = 0.35f + 0.65f * Mathf.Exp(-since / 0.9f);
                bool critical = t.Diving;
                Color c = critical ? UiTheme.Critical : UiTheme.Threat;
                if (critical) glow = Mathf.Max(glow, 0.6f + 0.4f * Mathf.PingPong(Time.time * 4f, 1f));
                b.symbol.color = c.WithAlpha(glow);

                bool drone = t.Definition.threatClass == ThreatClass.SmallDrone;
                b.symbol.sprite = drone ? UiFactory.Triangle : UiFactory.Chevron;
                b.symbol.rectTransform.sizeDelta = drone ? new Vector2(13f, 13f) : new Vector2(17f, 17f);
                Vector3 v = t.Velocity;
                float heading = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
                b.symbol.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -heading);

                bool engaged = false;
                foreach (var a in _autos) if (a != null && a.Engaging && a.Target == t) engaged = true;
                b.engaged.enabled = engaged;

                float ang = Vector3.Angle(bore, t.transform.position - gun.pitchPivot.position);
                if (ang < bestAngle) { bestAngle = ang; selected = b.rt; }
            }
            for (int i = shown; i < _blips.Count; i++) _blips[i].rt.gameObject.SetActive(false);
            _selected.gameObject.SetActive(selected != null);
            if (selected != null) _selected.anchoredPosition = selected.anchoredPosition;
        }

        private Blip GetBlip(int i)
        {
            while (_blips.Count <= i)
            {
                var rt = UiFactory.Rect("Contact", _blipRoot);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(18f, 18f);
                var b = new Blip { rt = rt };
                b.engaged = UiFactory.Image("Engaged", rt, UiTheme.Info, UiFactory.ThinRing);
                b.engaged.rectTransform.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
                b.symbol = UiFactory.Image("Symbol", rt, Color.white, UiFactory.Triangle);
                b.symbol.rectTransform.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14f, 14f));
                _blips.Add(b);
            }
            _blips[i].rt.gameObject.SetActive(true);
            return _blips[i];
        }
    }
}
