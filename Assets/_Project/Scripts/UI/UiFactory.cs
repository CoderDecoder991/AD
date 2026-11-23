using UnityEngine;
using UnityEngine.UI;

namespace AirDefense.UI
{
    /// <summary>
    /// Helpers that build uGUI elements from code, plus generated sprites
    /// (circle, ring, bracket, arrow). Greybox-stage UI: replaced by designed
    /// layouts and art in M2.
    /// </summary>
    public static class UiFactory
    {
        public static readonly Color Panel = new Color(0.05f, 0.08f, 0.07f, 0.72f);
        public static readonly Color Accent = new Color(0.55f, 0.95f, 0.55f, 1f);
        public static readonly Color Warn = new Color(1f, 0.75f, 0.2f, 1f);
        public static readonly Color Danger = new Color(1f, 0.3f, 0.25f, 1f);
        public static readonly Color TextColor = new Color(0.88f, 0.95f, 0.88f, 1f);

        private static Font _font;
        private static Sprite _circle, _ring, _bracket, _arrow, _rounded, _white;

        /// <summary>Plain white sprite. Image.Type.Filled bars need a sprite to fill correctly.</summary>
        public static Sprite White => _white ? _white : (_white = MakeSprite(8, (x, y, r) => 1f));

        public static Font Font => _font ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
        public static Sprite Circle => _circle ? _circle : (_circle = MakeSprite(128, (x, y, r) => r <= 1f ? 1f : 0f));
        public static Sprite Ring => _ring ? _ring : (_ring = MakeSprite(128, (x, y, r) => r <= 1f && r >= 0.86f ? 1f : 0f));
        public static Sprite Bracket => _bracket ? _bracket : (_bracket = MakeSprite(64, Bracket01));
        public static Sprite Arrow => _arrow ? _arrow : (_arrow = MakeSprite(64, (x, y, r) => (y >= -0.8f && Mathf.Abs(x) <= (0.8f - y) * 0.6f) ? 1f : 0f));
        public static Sprite Rounded => _rounded ? _rounded : (_rounded = MakeSliced(64, 14));

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

        public static Text Text(string name, Transform parent, string text, int size, TextAnchor align = TextAnchor.MiddleCenter, Color? color = null)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
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
            var b = img.gameObject.AddComponent<Button>();
            var colors = b.colors;
            colors.pressedColor = new Color(0.7f, 1f, 0.7f, 1f);
            b.colors = colors;
            b.onClick.AddListener(() => onClick());
            var t = Text("Label", img.transform, label, fontSize);
            t.rectTransform.Stretch(Vector2.zero, Vector2.one);
            return b;
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

        private static float Bracket01(float x, float y, float r)
        {
            float ax = Mathf.Abs(x), ay = Mathf.Abs(y);
            bool edge = (ax >= 0.84f || ay >= 0.84f) && ax <= 1f && ay <= 1f;
            bool corner = ax >= 0.45f && ay >= 0.45f;
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
