using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MergeCafe.UI
{
    /// <summary>
    /// Builds every UI element from code (no prefabs, no scene-authored UI) so the project
    /// stays free of hand-edited asset files. See webGL_game.md §19 for the code-first policy.
    /// </summary>
    public static class UIFactory
    {
        public static EventSystem EnsureEventSystem()
        {
            EventSystem existing = Object.FindObjectOfType<EventSystem>();
            if (existing != null)
                return existing;

            var go = new GameObject("EventSystem");
            var eventSystem = go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
            return eventSystem;
        }

        /// <summary>Creates the root canvas and the five fixed panels + overlay layers.</summary>
        public static UiLayout BuildBaseLayout()
        {
            var canvasGo = new GameObject("Canvas");
            canvasGo.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            var root = (RectTransform)canvasGo.transform;

            var layout = new UiLayout
            {
                Canvas = canvas,
                Root = root
            };

            // Full screen background so the camera color never shows through.
            Image bg = CreateImage(root, "ScreenBackground", UITheme.ScreenBg);
            Stretch((RectTransform)bg.transform);

            float hud = UITheme.TopHudHeight;
            float bottom = UITheme.UpgradePanelHeight;
            float left = UITheme.GeneratorPanelWidth;
            float right = UITheme.OrderPanelWidth;

            // TopHud: full-width bar at the very top.
            layout.TopHud = CreatePanel(root, "TopHud", UITheme.HudBg,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -hud), new Vector2(0f, 0f));

            // GeneratorPanel: left column between the HUD and the upgrade bar.
            layout.GeneratorPanel = CreatePanel(root, "GeneratorPanel", UITheme.SidePanelBg,
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, bottom), new Vector2(left, -hud));

            // OrderPanel: right column between the HUD and the upgrade bar.
            layout.OrderPanel = CreatePanel(root, "OrderPanel", UITheme.SidePanelBg,
                new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-right, bottom), new Vector2(0f, -hud));

            // BoardPanel: whatever is left in the middle.
            layout.BoardPanel = CreatePanel(root, "BoardPanel", UITheme.BoardPanelBg,
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(left, bottom), new Vector2(-right, -hud));

            // UpgradePanel: full-width bar at the bottom.
            layout.UpgradePanel = CreatePanel(root, "UpgradePanel", UITheme.UpgradeBg,
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 0f), new Vector2(0f, bottom));

            // Overlay layers (order matters: later siblings draw on top).
            layout.DragLayer = CreateLayer(root, "DragLayer");
            layout.ToastLayer = CreateLayer(root, "ToastLayer");
            layout.PopupLayer = CreateLayer(root, "PopupLayer");

            return layout;
        }

        /// <summary>Empty full-stretch, non-raycast-blocking overlay layer.</summary>
        public static RectTransform CreateLayer(RectTransform parent, string name)
        {
            RectTransform rect = CreateUiObject(parent, name);
            Stretch(rect);
            return rect;
        }

        public static RectTransform CreatePanel(RectTransform parent, string name, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            Image image = CreateImage(parent, name, color);
            var rect = (RectTransform)image.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        public static RectTransform CreateUiObject(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.layer = LayerMask.NameToLayer("UI");
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        public static Image CreateImage(Transform parent, string name, Color color)
        {
            RectTransform rect = CreateUiObject(parent, name);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static Text CreateText(Transform parent, string name, string content, int fontSize,
            Color color, TextAnchor alignment = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            RectTransform rect = CreateUiObject(parent, name);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = UITheme.Font;
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(RectTransform parent, string name, string label,
            int fontSize, Color background, out Text labelText)
        {
            Image image = CreateImage(parent, name, background);
            image.raycastTarget = true;

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.6f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            labelText = CreateText(image.transform, "Label", label, fontSize, UITheme.LabelOn(background));
            Stretch((RectTransform)labelText.transform);
            return button;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>Small dim header text used as a panel title.</summary>
        public static Text CreatePanelTitle(RectTransform panel, string title)
        {
            Text text = CreateText(panel, "Title", title, 26, UITheme.TextDim, TextAnchor.MiddleCenter, FontStyle.Bold);
            var rect = (RectTransform)text.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(0f, -46f);
            rect.offsetMax = new Vector2(0f, 0f);
            return text;
        }
    }
}
