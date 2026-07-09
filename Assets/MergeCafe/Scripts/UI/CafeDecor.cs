using UnityEngine;
using UnityEngine.UI;

namespace MergeCafe.UI
{
    /// <summary>
    /// Applies the self-made 2D cafe decoration (webGL_game.md art direction stays
    /// license-clean: all shapes are procedural — see <see cref="CafeArt"/>).
    /// Everything here is non-interactive (raycastTarget = false) and added behind
    /// the gameplay UI, so readability of the board and panels is never reduced.
    /// </summary>
    public static class CafeDecor
    {
        private static readonly Color WarmGlow = Hex("6B4A2E");

        /// <summary>Call right after the base layout is built and before the board grid.</summary>
        public static void Apply(UiLayout layout)
        {
            // The panels tile the whole screen, so the screen background itself is
            // occluded — decoration lives on the panels and the board instead.
            AddPanelSheen(layout.TopHud);
            AddPanelSheen(layout.LeftPanel);
            AddPanelSheen(layout.BottomBar);

            AddBoardBackdrop(layout.BoardPanel);
            AddHudCup(layout.TopHud);
        }

        private static void AddPanelSheen(RectTransform panel)
        {
            Image sheen = UIFactory.CreateImage(panel, "Sheen", new Color(1f, 1f, 1f, 0.05f));
            sheen.sprite = CafeArt.VerticalGradient;
            sheen.raycastTarget = false;
            var rect = (RectTransform)sheen.transform;
            UIFactory.Stretch(rect);
            rect.SetAsFirstSibling();
        }

        private static void AddBoardBackdrop(RectTransform boardPanel)
        {
            // Warm radial glow so the play area feels cozy and lifts off the flat panel.
            Image glow = UIFactory.CreateImage(boardPanel, "BoardGlow",
                new Color(WarmGlow.r, WarmGlow.g, WarmGlow.b, 0.22f));
            glow.sprite = CafeArt.RadialGlow;
            glow.raycastTarget = false;
            var glowRect = (RectTransform)glow.transform;
            glowRect.anchorMin = new Vector2(0.5f, 0.5f);
            glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.sizeDelta = new Vector2(1200f, 1200f);
            glowRect.SetAsFirstSibling();

            // Large faint coffee-cup watermark behind the grid.
            Image cup = UIFactory.CreateImage(boardPanel, "BoardCupWatermark",
                new Color(1f, 1f, 1f, 0.05f));
            cup.sprite = CafeArt.CoffeeCup;
            cup.preserveAspect = true;
            cup.raycastTarget = false;
            var cupRect = (RectTransform)cup.transform;
            cupRect.anchorMin = new Vector2(0.5f, 0.5f);
            cupRect.anchorMax = new Vector2(0.5f, 0.5f);
            cupRect.sizeDelta = new Vector2(520f, 520f);
            cupRect.SetSiblingIndex(1);
        }

        private static void AddHudCup(RectTransform topHud)
        {
            Image cup = UIFactory.CreateImage(topHud, "HudCup", UITheme.TextGold);
            cup.sprite = CafeArt.CoffeeCup;
            cup.preserveAspect = true;
            cup.raycastTarget = false;
            var rect = (RectTransform)cup.transform;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(232f, -2f);
            rect.sizeDelta = new Vector2(56f, 56f);
        }

        private static Color Hex(string rgb)
        {
            return ColorUtility.TryParseHtmlString("#" + rgb, out Color c) ? c : Color.magenta;
        }
    }
}
