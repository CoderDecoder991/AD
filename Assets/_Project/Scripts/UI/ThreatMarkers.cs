using System.Collections.Generic;
using AirDefense.Core;
using AirDefense.Threats;
using AirDefense.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace AirDefense.UI
{
    /// <summary>
    /// Screen-space readability aids (GDD §4): a bracket and range on every
    /// visible threat, an edge arrow for threats off-screen, and (Light aim
    /// assist) a lead ring showing where to aim at the threat nearest the sight.
    /// </summary>
    [DefaultExecutionOrder(200)] // after the camera moves, so markers don't lag a frame
    public sealed class ThreatMarkers : MonoBehaviour
    {
        public ThreatManager threats;
        public Camera cam;
        public GunMount gun;

        private RectTransform _root;
        private Canvas _canvas;
        private Image _leadRing;
        private readonly List<Marker> _markers = new List<Marker>();

        private sealed class Marker
        {
            public RectTransform root;
            public Image bracket;
            public Image arrow;
            public Text label;
        }

        public static ThreatMarkers Build(RectTransform parent, Canvas canvas, ThreatManager threats, Camera cam, GunMount gun)
        {
            var root = UiFactory.Rect("ThreatMarkers", parent);
            root.Stretch(Vector2.zero, Vector2.one);
            var m = root.gameObject.AddComponent<ThreatMarkers>();
            m._root = root;
            m._canvas = canvas;
            m.threats = threats;
            m.cam = cam;
            m.gun = gun;
            m._leadRing = UiFactory.Image("LeadRing", root, new Color(1f, 0.95f, 0.4f, 0.9f), UiFactory.Ring);
            m._leadRing.rectTransform.anchorMin = m._leadRing.rectTransform.anchorMax = Vector2.zero;
            m._leadRing.rectTransform.sizeDelta = new Vector2(34f, 34f);
            return m;
        }

        private void LateUpdate()
        {
            var list = threats.Active;
            float scale = _canvas.scaleFactor;
            Vector2 screen = new Vector2(Screen.width, Screen.height);
            Vector2 centre = screen * 0.5f;
            Vector3 camPos = cam.transform.position;

            ThreatAgent leadTarget = null;
            float bestAngle = 12f; // degrees from bore
            Vector3 bore = gun.BoreDirection;

            int i = 0;
            for (; i < list.Count; i++)
            {
                ThreatAgent t = list[i];
                Marker mk = Get(i);
                Vector3 wp = t.transform.position;
                Vector3 sp = cam.WorldToScreenPoint(wp);
                bool inFront = sp.z > 0f;
                bool onScreen = inFront && sp.x >= 0 && sp.x <= screen.x && sp.y >= 0 && sp.y <= screen.y;
                float dist = Vector3.Distance(camPos, wp);

                mk.bracket.color = mk.arrow.color = mk.label.color = t.Definition.radarColor;
                if (onScreen)
                {
                    mk.root.anchoredPosition = new Vector2(sp.x, sp.y) / scale;
                    mk.bracket.enabled = true;
                    mk.arrow.enabled = false;
                    mk.label.enabled = true;
                    mk.label.text = dist >= 1000f ? $"{dist / 1000f:0.0} km" : $"{dist:0} m";
                }
                else
                {
                    // Direction on screen toward the threat; flip when behind the camera.
                    Vector2 dir = new Vector2(sp.x, sp.y) - centre;
                    if (!inFront) dir = -dir;
                    if (dir.sqrMagnitude < 1f) dir = Vector2.up;
                    dir.Normalize();
                    Vector2 edge = EdgePoint(centre, dir, screen, scale);
                    mk.root.anchoredPosition = edge / scale;
                    mk.arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
                    mk.bracket.enabled = false;
                    mk.arrow.enabled = true;
                    mk.label.enabled = false;
                }

                float ang = Vector3.Angle(bore, wp - gun.pitchPivot.position);
                if (ang < bestAngle) { bestAngle = ang; leadTarget = t; }
            }
            for (int j = i; j < _markers.Count; j++) _markers[j].root.gameObject.SetActive(false);

            UpdateLead(leadTarget, scale);
        }

        // Edge-arrow keep-out (reference units, 1920x1080 canvas): clear of the pause button (top-right),
        // weapon tiles / ammo / FIRE (bottom) and the radar + health bar block (top-left).
        private const float InsetLeft = 50f, InsetRight = 170f, InsetTop = 150f, InsetBottom = 190f;
        private const float RadarBlockRight = 330f, RadarBlockBottom = 370f;

        /// <summary>Where along the inset screen edge an arrow toward <paramref name="dir"/> should sit.</summary>
        private static Vector2 EdgePoint(Vector2 centre, Vector2 dir, Vector2 screen, float scale)
        {
            float minX = InsetLeft * scale, maxX = screen.x - InsetRight * scale;
            float minY = InsetBottom * scale, maxY = screen.y - InsetTop * scale;
            float kx = dir.x > 0 ? (maxX - centre.x) / dir.x : dir.x < 0 ? (minX - centre.x) / dir.x : float.MaxValue;
            float ky = dir.y > 0 ? (maxY - centre.y) / dir.y : dir.y < 0 ? (minY - centre.y) / dir.y : float.MaxValue;
            Vector2 p = centre + dir * Mathf.Min(kx, ky);

            // Slide out of the radar block (top-left corner) along the edge we landed on.
            float blockRight = RadarBlockRight * scale, blockBottom = screen.y - RadarBlockBottom * scale;
            if (p.x < blockRight && p.y > blockBottom)
            {
                if (Mathf.Abs(p.y - maxY) < 1f) p.x = blockRight;  // on the top edge: move right
                else p.y = blockBottom;                            // on the left edge: move down
            }
            return p;
        }

        private void UpdateLead(ThreatAgent target, float scale)
        {
            if (target == null || GameSettings.Assist == AimAssist.Off)
            {
                _leadRing.enabled = false;
                return;
            }
            Vector3 muzzle = gun.pitchPivot.position;
            if (Ballistics.TryLeadPoint(muzzle, target.transform.position, target.Velocity, gun.definition.muzzleVelocity, gun.definition.dragK, out Vector3 aim)
                && Vector3.Distance(muzzle, aim) <= gun.definition.maxRange * 1.2f)
            {
                Vector3 sp = cam.WorldToScreenPoint(aim);
                if (sp.z > 0f)
                {
                    _leadRing.enabled = true;
                    _leadRing.rectTransform.anchoredPosition = new Vector2(sp.x, sp.y) / scale;
                    return;
                }
            }
            _leadRing.enabled = false;
        }

        private Marker Get(int i)
        {
            while (_markers.Count <= i)
            {
                var root = UiFactory.Rect("Marker", _root);
                root.anchorMin = root.anchorMax = Vector2.zero;
                root.sizeDelta = new Vector2(46f, 46f);
                var mk = new Marker { root = root };
                mk.bracket = UiFactory.Image("Bracket", root, Color.white, UiFactory.Bracket);
                mk.bracket.rectTransform.Stretch(Vector2.zero, Vector2.one);
                mk.arrow = UiFactory.Image("Arrow", root, Color.white, UiFactory.Arrow);
                mk.arrow.rectTransform.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 34f));
                mk.label = UiFactory.Text("Range", root, "", 20, TextAnchor.UpperCenter);
                mk.label.rectTransform.Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(120f, 24f));
                _markers.Add(mk);
            }
            _markers[i].root.gameObject.SetActive(true);
            return _markers[i];
        }
    }
}
