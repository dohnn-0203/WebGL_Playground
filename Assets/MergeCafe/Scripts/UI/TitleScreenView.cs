using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MergeCafe.UI
{
    /// <summary>
    /// Cafe-themed title screen shown before the game. Built entirely from the
    /// procedural <see cref="CafeArt"/> motifs (license-clean). Clicking or tapping
    /// anywhere fires the start callback and removes the screen.
    /// </summary>
    public sealed class TitleScreenView : MonoBehaviour, IPointerClickHandler
    {
        private Action _onStart;
        private bool _started;

        private static readonly Color Backdrop = Hex("241A14");
        private static readonly Color GlowColor = Hex("6B4A2E");
        private static readonly Color BeanColor = Hex("3A2A1E");

        public static TitleScreenView Build(Action onStart)
        {
            // Its own overlay canvas so it always sits above the game and disposes cleanly.
            var canvasGo = new GameObject("TitleCanvas");
            canvasGo.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            var root = (RectTransform)canvasGo.transform;

            // Full-screen backdrop that also catches the click.
            Image backdrop = UIFactory.CreateImage(root, "Backdrop", Backdrop);
            backdrop.raycastTarget = true;
            UIFactory.Stretch((RectTransform)backdrop.transform);

            var view = backdrop.gameObject.AddComponent<TitleScreenView>();
            view._onStart = onStart;
            var content = (RectTransform)backdrop.transform;

            // Warm radial glow behind the emblem.
            Image glow = UIFactory.CreateImage(content, "Glow",
                new Color(GlowColor.r, GlowColor.g, GlowColor.b, 0.4f));
            glow.sprite = CafeArt.RadialGlow;
            glow.raycastTarget = false;
            var glowRect = (RectTransform)glow.transform;
            glowRect.anchorMin = glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.anchoredPosition = new Vector2(0f, 120f);
            glowRect.sizeDelta = new Vector2(1500f, 1500f);

            ScatterBeans(content);

            // Coffee-cup emblem.
            Image cup = UIFactory.CreateImage(content, "Cup", UITheme.TextGold);
            cup.sprite = CafeArt.CoffeeCup;
            cup.preserveAspect = true;
            cup.raycastTarget = false;
            var cupRect = (RectTransform)cup.transform;
            cupRect.anchorMin = cupRect.anchorMax = new Vector2(0.5f, 0.5f);
            cupRect.anchoredPosition = new Vector2(0f, 210f);
            cupRect.sizeDelta = new Vector2(320f, 320f);

            // Title + subtitle.
            Text title = UIFactory.CreateText(content, "Title", "머지 카페", 120,
                UITheme.TextGold, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(3f, -3f);
            CenterText((RectTransform)title.transform, -40f, 150f, 1400f);

            Text subtitle = UIFactory.CreateText(content, "Subtitle", "Merge Cafe Puzzle", 46,
                UITheme.TextDim, TextAnchor.MiddleCenter);
            CenterText((RectTransform)subtitle.transform, -150f, 60f, 1000f);

            // Pulsing "click to start" prompt.
            Text prompt = UIFactory.CreateText(content, "StartPrompt", "화면을 클릭하여 시작", 44,
                UITheme.TextMain, TextAnchor.MiddleCenter);
            CenterText((RectTransform)prompt.transform, -370f, 70f, 1000f);
            view.StartCoroutine(view.PulsePrompt(prompt));

            return view;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_started)
                return;
            _started = true;

            Action start = _onStart;
            _onStart = null;
            start?.Invoke();

            Destroy(transform.root.gameObject);
        }

        private IEnumerator PulsePrompt(Text prompt)
        {
            while (prompt != null)
            {
                float a = 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.6f));
                Color c = prompt.color;
                prompt.color = new Color(c.r, c.g, c.b, a);
                yield return null;
            }
        }

        private static void ScatterBeans(RectTransform content)
        {
            // Deterministic scatter (no Random.* so the look is identical every run).
            var rng = new System.Random(2024);
            for (int i = 0; i < 14; i++)
            {
                Image bean = UIFactory.CreateImage(content, $"Bean_{i}",
                    new Color(BeanColor.r, BeanColor.g, BeanColor.b, 0.55f));
                bean.sprite = CafeArt.CoffeeBean;
                bean.preserveAspect = true;
                bean.raycastTarget = false;

                var rect = (RectTransform)bean.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                float x = ((float)rng.NextDouble() - 0.5f) * 1820f;
                float y = ((float)rng.NextDouble() - 0.5f) * 980f;
                rect.anchoredPosition = new Vector2(x, y);
                float size = 34f + (float)rng.NextDouble() * 40f;
                rect.sizeDelta = new Vector2(size, size);
                rect.localRotation = Quaternion.Euler(0f, 0f, (float)rng.NextDouble() * 360f);
            }
        }

        private static void CenterText(RectTransform rect, float y, float height, float width)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static Color Hex(string rgb)
        {
            return ColorUtility.TryParseHtmlString("#" + rgb, out Color c) ? c : Color.magenta;
        }
    }
}
