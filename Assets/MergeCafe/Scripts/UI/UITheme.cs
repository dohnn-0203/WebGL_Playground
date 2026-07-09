using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace MergeCafe.UI
{
    /// <summary>
    /// Central place for every color / size / font used by the runtime-built UI.
    /// All art is plain colored UI Images + text, so this file effectively IS the art direction.
    /// </summary>
    public static class UITheme
    {
        // ---- Layout constants (1920x1080 reference resolution) ----
        public const float TopHudHeight = 84f;
        public const float BottomBarHeight = 110f;
        public const float LeftPanelWidth = 380f;

        // ---- Energy gauge ----
        public static readonly Color EnergyFill = Hex("54C6EC");
        public static readonly Color EnergyTrack = Hex("1E2A30");

        // ---- Screen / panels ----
        public static readonly Color ScreenBg = Hex("1B1410");
        public static readonly Color HudBg = Hex("2E2420");
        public static readonly Color SidePanelBg = Hex("271E19");
        public static readonly Color BoardPanelBg = Hex("221A16");
        public static readonly Color UpgradeBg = Hex("2E2420");

        // ---- Text ----
        public static readonly Color TextMain = Hex("F5EFE6");
        public static readonly Color TextDim = Hex("BCAE9C");
        public static readonly Color TextGold = Hex("F2C14E");
        public static readonly Color TextDanger = Hex("E07A5F");

        // ---- Board cells ----
        public static readonly Color CellOpen = Hex("4A3B32");
        public static readonly Color CellLocked = Hex("2A211C");
        public static readonly Color CellHighlight = Hex("E0A458");
        public static readonly Color CellReject = Hex("B5543B");

        // ---- Cards ----
        public static readonly Color CardBg = Hex("372B22");

        // ---- Buttons ----
        public static readonly Color ButtonPrimary = Hex("8C5E3C");
        public static readonly Color ButtonSecondary = Hex("5B4636");
        public static readonly Color ButtonDanger = Hex("A3492F");
        public static readonly Color ButtonDisabled = Hex("453A32");

        // ---- Item family palettes, index = level - 1 (Lv.1 ~ Lv.5) ----
        public static readonly Color[] CoffeeColors =
        {
            Hex("A1887F"), Hex("8D6E63"), Hex("795548"), Hex("5D4037"), Hex("3E2723")
        };

        public static readonly Color[] BreadColors =
        {
            Hex("FFE082"), Hex("FFCA28"), Hex("FFB300"), Hex("FB8C00"), Hex("EF6C00")
        };

        public static readonly Color[] DessertColors =
        {
            Hex("F8BBD0"), Hex("F48FB1"), Hex("EC407A"), Hex("AB47BC"), Hex("7B1FA2")
        };

        private static Font _font;

        /// <summary>
        /// Korean-capable UI font. Noto Sans KR (SIL OFL 1.1) lives in Resources so the
        /// runtime-built UI can always reach it; falls back to the built-in font in the
        /// unlikely case the asset is missing.
        /// </summary>
        public static Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.Load<Font>("Fonts/NotoSansKR-Regular");
                    if (_font == null)
                        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return _font;
            }
        }

        private static TMP_FontAsset _tmpFont;

        /// <summary>
        /// Crisp SDF font for all UI text (TextMeshPro). Built at runtime from the same
        /// Noto Sans KR face as a DYNAMIC atlas, so Korean glyphs are rasterized on demand
        /// and the repository stays free of a huge pre-baked atlas.
        /// </summary>
        public static TMP_FontAsset TmpFont
        {
            get
            {
                if (_tmpFont == null)
                {
                    Font source = Font; // Noto Sans KR (or built-in fallback)
                    _tmpFont = TMP_FontAsset.CreateFontAsset(
                        source, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                        AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: true);
                    _tmpFont.name = "NotoSansKR SDF";
                }
                return _tmpFont;
            }
        }

        /// <summary>Pick a readable label color for the given background.</summary>
        public static Color LabelOn(Color bg)
        {
            float luminance = 0.299f * bg.r + 0.587f * bg.g + 0.114f * bg.b;
            return luminance > 0.6f ? Hex("3B2A1E") : TextMain;
        }

        private static Color Hex(string rgb)
        {
            return ColorUtility.TryParseHtmlString("#" + rgb, out Color c) ? c : Color.magenta;
        }
    }
}
