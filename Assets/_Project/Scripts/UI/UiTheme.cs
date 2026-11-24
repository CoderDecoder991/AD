using UnityEngine;

namespace AirDefense.UI
{
    /// <summary>
    /// HUD design tokens (docs/HUD_UI_SPECIFICATION.md §1–3, VS-2). Every UI element takes its colours,
    /// fonts and sizes from here, so the HUD reads as one system. Colour is never the only signal:
    /// states also change text, icon or shape.
    /// </summary>
    public static class UiTheme
    {
        // ---- colour semantics (§2)
        public static readonly Color Ink = Hex(0xE8ECE6);
        public static readonly Color InkDim = Hex(0xE8ECE6, 0.55f);
        public static readonly Color Panel = Hex(0x0E1412, 0.72f);
        public static readonly Color PanelSolid = Hex(0x0E1412, 0.94f);
        public static readonly Color PanelLine = Hex(0x9FB8A6, 0.28f);
        public static readonly Color System = Hex(0x7FD49A);      // friendly / selected / radar
        public static readonly Color Info = Hex(0x7FB8D4);        // autonomous support
        public static readonly Color Caution = Hex(0xE6B64C);     // reloading, heat, unlock available
        public static readonly Color Threat = Hex(0xF0904A);      // hostile contacts
        public static readonly Color Critical = Hex(0xE5534B);    // impacts, alarms, low health
        public static readonly Color Disabled = Hex(0x6B706C);    // locked / unavailable

        // ---- typography (§1): Barlow Semi Condensed, SIL Open Font License (Resources/Fonts/OFL.txt)
        public enum Weight { Regular, Medium, SemiBold, Bold }
        private static Font _regular, _medium, _semiBold, _bold;

        public static Font FontFor(Weight w) => w switch
        {
            Weight.Regular => _regular ? _regular : (_regular = Load("BarlowSemiCondensed-Regular")),
            Weight.SemiBold => _semiBold ? _semiBold : (_semiBold = Load("BarlowSemiCondensed-SemiBold")),
            Weight.Bold => _bold ? _bold : (_bold = Load("BarlowSemiCondensed-Bold")),
            _ => _medium ? _medium : (_medium = Load("BarlowSemiCondensed-Medium")),
        };

        // ---- sizes (reference px at 1920x1080)
        public const int Caption = 18, Label = 22, Body = 26, Value = 30, Banner = 38, Title = 48;
        public const float Grid = 8f, Margin = 24f, CornerRadius = 6f;

        private static Font Load(string name)
        {
            var f = Resources.Load<Font>("Fonts/" + name);
            return f != null ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static Color Hex(int rgb, float a = 1f) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, a);

        public static Color WithAlpha(this Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
