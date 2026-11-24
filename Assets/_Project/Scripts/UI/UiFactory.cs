using UnityEngine;
using UnityEngine.UI;

namespace AirDefense.UI
{
    /// <summary>
    /// Helpers that build uGUI elements from code, plus generated sprites. Colours and fonts come from
    /// <see cref="UiTheme"/> (HUD design system, VS-2); the old names below map onto its tokens.
    /// </summary>
    public static class UiFactory
    {
        public static readonly Color Panel = UiTheme.Panel;
        public static readonly Color Accent = UiTheme.System;
        public static readonly Color Warn = UiTheme.Caution;
        public static readonly Color Danger = UiTheme.Critical;
        public static readonly Color TextColor = UiTheme.Ink;

        private static Sprite _circle, _ring, _bracket, _arrow, _rounded, _white, _thinRing, _frame, _triangle, _chevron, _sweep, _wedge, _lens;

        /// <summary>Plain white sprite. Image.Type.Filled bars need a sprite to fill correctly.</summary>
        public static Sprite White => _white ? _white : (_white = MakeSprite(8, (x, y, r) => 1f));

        public static Font Font => UiTheme.FontFor(UiTheme.Weight.Medium);
        /// <summary>Thin ring (radar range rings, lead ring).</summary>
        public static Sprite ThinRing => _thinRing ? _thinRing : (_thinRing = MakeSprite(256, (x, y, r) => r <= 1f && r >= 0.975f ? 1f : 0f));
        /// <summary>Rounded panel outline, 1.5 px line, 9-sliced (panel border).</summary>
        public static Sprite Frame => _frame ? _frame : (_frame = MakeSlicedFrame(64, 10, 1.6f));
        /// <summary>Radar symbol: drone (triangle pointing up = toward travel).</summary>
        public static Sprite Triangle => _triangle ? _triangle : (_triangle = MakeSprite(64, (x, y, r) => y >= -0.7f && Mathf.Abs(x) <= (0.85f - y) * 0.55f && y <= 0.85f ? 1f : 0f));
        /// <summary>Radar symbol: aircraft (chevron).</summary>
        public static Sprite Chevron => _chevron ? _chevron : (_chevron = MakeSprite(64, Chevron01));
        /// <summary>Radar sweep: a bright leading edge with a fading 70° tail (rotates about the centre).</summary>
        public static Sprite Sweep => _sweep ? _sweep : (_sweep = MakeSprite(256, Sweep01));
        /// <summary>Weapon field-of-fire wedge (24°, pointing up).</summary>
        public static Sprite Wedge => _wedge ? _wedge : (_wedge = MakeSprite(256, (x, y, r) => r <= 1f && y > 0f && Mathf.Abs(Mathf.Atan2(x, y)) <= 12f * Mathf.Deg2Rad ? 0.55f + 0.45f * (1f - r) : 0f));
        /// <summary>Zoom icon (magnifier).</summary>
        public static Sprite Lens => _lens ? _lens : (_lens = MakeSprite(128, Lens01));
        public static Sprite Circle => _circle ? _circle : (_circle = MakeSprite(128, (x, y, r) => r <= 1f ? 1f : 0f));
        public static Sprite Ring => _ring ? _ring : (_ring = MakeSprite(128, (x, y, r) => r <= 1f && r >= 0.86f ? 1f : 0f));
        public static Sprite Bracket => _bracket ? _bracket : (_bracket = MakeSprite(64, Bracket01));
        public static Sprite Arrow => _arrow ? _arrow : (_arrow = MakeSprite(64, (x, y, r) => (y >= -0.8f && Mathf.Abs(x) <= (0.8f - y) * 0.6f) ? 1f : 0f));
        public static Sprite Rounded => _rounded ? _rounded : (_rounded = MakeSliced(64, 10));

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Anchor(this RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Image(string name, Transform parent, Color color, Sprite sprite = null, bool raycast = false)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            if (sprite == Rounded) img.type = UnityEngine.UI.Image.Type.Sliced;
            img.raycastTarget = raycast;
            return img;
        }

        public static Text Text(string name, Transform parent, string text, int size, TextAnchor align = TextAnchor.MiddleCenter, Color? color = null,
                                UiTheme.Weight weight = UiTheme.Weight.Medium)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = UiTheme.FontFor(weight);
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = color ?? TextColor;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static Button Button(string name, Transform parent, string label, int fontSize, System.Action onClick, Color? bg = null)
        {
            var img = Image(name, parent, bg ?? Panel, Rounded, raycast: true);
            Border(img.transform, bg.HasValue ? UiTheme.Ink.WithAlpha(0.18f) : UiTheme.PanelLine);
            var b = img.gameObject.AddComponent<Button>();
            var colors = b.colors;
            colors.pressedColor = new Color(0.75f, 0.95f, 0.8f, 1f);
            colors.highlightedColor = Color.white;
            b.colors = colors;
            b.onClick.AddListener(() => onClick());
            var t = Text("Label", img.transform, label, fontSize, TextAnchor.MiddleCenter, null, UiTheme.Weight.SemiBold);
            t.rectTransform.Stretch(Vector2.zero, Vector2.one);
            return b;
        }

        /// <summary>Panel in the design-system style: translucent fill, rounded corners, 1.5 px line.</summary>
        public static Image PanelImage(string name, Transform parent, bool raycast = false, Color? fill = null)
        {
            var img = Image(name, parent, fill ?? UiTheme.Panel, Rounded, raycast);
            Border(img.transform, UiTheme.PanelLine);
            return img;
        }

        /// <summary>Adds a rounded outline child that follows the parent's size.</summary>
        public static Image Border(Transform parent, Color c)
        {
            var b = Image("Border", parent, c, Frame);
            b.type = UnityEngine.UI.Image.Type.Sliced;
            b.rectTransform.Stretch(Vector2.zero, Vector2.one);
            return b;
        }

        /// <summary>Soft dark underlay so HUD text stays readable over bright or busy 3D scenery.</summary>
        public static Text Underlay(this Text t, float alpha = 0.85f)
        {
            var sh = t.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, alpha);
            sh.effectDistance = new Vector2(1.5f, -1.5f);
            return t;
        }

        private delegate float Shape(float x, float y, float r);

        /// <summary>Generates a white alpha-mask sprite; x,y in [-1,1], r = distance from centre.</summary>
        private static Sprite MakeSprite(int size, Shape shape)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            const int ss = 3; // supersampling for smooth edges
            for (int j = 0; j < size; j++)
            for (int i = 0; i < size; i++)
            {
                float a = 0f;
                for (int sj = 0; sj < ss; sj++)
                for (int si = 0; si < ss; si++)
                {
                    float x = ((i + (si + 0.5f) / ss) / size) * 2f - 1f;
                    float y = ((j + (sj + 0.5f) / ss) / size) * 2f - 1f;
                    a += shape(x, y, Mathf.Sqrt(x * x + y * y));
                }
                byte b = (byte)Mathf.RoundToInt(a / (ss * ss) * 255f);
                px[j * size + i] = new Color32(255, 255, 255, b);
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static float Chevron01(float x, float y, float r)
        {
            // Two arms meeting at the top (pointing up), 0.28 thick.
            float ax = Mathf.Abs(x);
            float arm = Mathf.Abs((y - 0.7f) + ax * 1.1f);
            return ax <= 0.9f && arm <= 0.28f && y >= -0.4f ? 1f : 0f;
        }

        private static float Sweep01(float x, float y, float r)
        {
            if (r > 1f) return 0f;
            // Angle clockwise from "up"; the leading edge is at 0, the tail trails anticlockwise.
            float a = Mathf.Atan2(x, y) * Mathf.Rad2Deg;      // -180..180, clockwise positive
            float behind = Mathf.Repeat(-a, 360f);              // degrees behind the leading edge
            if (behind > 70f) return 0f;
            float tail = Mathf.Pow(1f - behind / 70f, 2.2f) * 0.55f;
            float edge = behind < 1.6f ? 1f : 0f;
            return Mathf.Max(tail, edge) * Mathf.Clamp01(r * 4f);
        }

        private static float Lens01(float x, float y, float r)
        {
            float cx = x + 0.18f, cy = y - 0.18f;
            float rr = Mathf.Sqrt(cx * cx + cy * cy);
            bool ring = rr <= 0.58f && rr >= 0.42f;
            // Handle from the ring toward the bottom-right.
            float t = (x - y) / 1.414f, n = (x + y) / 1.414f;
            bool handle = t > 0.38f && t < 0.95f && Mathf.Abs(n) < 0.1f;
            return ring || handle ? 1f : 0f;
        }

        private static Sprite MakeSlicedFrame(int size, int radius, float line)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int j = 0; j < size; j++)
            for (int i = 0; i < size; i++)
            {
                float cx = Mathf.Clamp(i + 0.5f, radius, size - radius);
                float cy = Mathf.Clamp(j + 0.5f, radius, size - radius);
                float d = Vector2.Distance(new Vector2(i + 0.5f, j + 0.5f), new Vector2(cx, cy));
                float inner = radius - line;
                float a = Mathf.Clamp01(radius - d + 0.5f) * Mathf.Clamp01(d - inner + 0.5f);
                px[j * size + i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
        }

        private static float Bracket01(float x, float y, float r)
        {
            float ax = Mathf.Abs(x), ay = Mathf.Abs(y);
            bool edge = (ax >= 0.9f || ay >= 0.9f) && ax <= 1f && ay <= 1f;
            bool corner = ax >= 0.55f && ay >= 0.55f;
            return edge && corner ? 1f : 0f;
        }

        private static Sprite MakeSliced(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int j = 0; j < size; j++)
            for (int i = 0; i < size; i++)
            {
                float cx = Mathf.Clamp(i + 0.5f, radius, size - radius);
                float cy = Mathf.Clamp(j + 0.5f, radius, size - radius);
                float d = Vector2.Distance(new Vector2(i + 0.5f, j + 0.5f), new Vector2(cx, cy));
                byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(radius - d + 0.5f) * 255f);
                px[j * size + i] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
        }
    }
}
