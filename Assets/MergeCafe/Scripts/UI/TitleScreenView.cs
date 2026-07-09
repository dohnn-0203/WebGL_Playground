using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MergeCafe.UI
{
    /// <summary>
    /// Game-select hub shown before any game. Each entry is a card; the "머지 카페"
    /// card starts the current merge game, other cards are placeholders for future
    /// games (marked 준비 중). Built entirely from procedural art (license-clean).
    /// </summary>
    public sealed class TitleScreenView : MonoBehaviour
    {
        private Action _onStartMerge;
        private bool _started;
        private Text _notice;
        private Coroutine _noticeRoutine;

        private static readonly Color Backdrop = Hex("241A14");
        private static readonly Color GlowColor = Hex("6B4A2E");
        private static readonly Color BeanColor = Hex("3A2A1E");
        private static readonly Color CardBg = Hex("2E2420");
        private static readonly Color CardLocked = Hex("241C18");

        /// <summary>A future game slot. Extend this list to add more games to the hub.</summary>
        private static readonly (string name, string subtitle)[] ComingSoon =
        {
            ("숫자 합치기", "준비 중"),
            ("카드 짝맞추기", "준비 중"),
        };

        public static TitleScreenView Build(Action onStartMerge)
        {
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

            Image bg = UIFactory.CreateImage(root, "Backdrop", Backdrop);
            bg.raycastTarget = true; // absorbs clicks outside the cards
            UIFactory.Stretch((RectTransform)bg.transform);

            var view = bg.gameObject.AddComponent<TitleScreenView>();
            view._onStartMerge = onStartMerge;
            var content = (RectTransform)bg.transform;

            Image glow = UIFactory.CreateImage(content, "Glow",
                new Color(GlowColor.r, GlowColor.g, GlowColor.b, 0.4f));
            glow.sprite = CafeArt.RadialGlow;
            glow.raycastTarget = false;
            var glowRect = (RectTransform)glow.transform;
            glowRect.anchorMin = glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.anchoredPosition = new Vector2(0f, 60f);
            glowRect.sizeDelta = new Vector2(1600f, 1600f);

            ScatterBeans(content);

            Text title = UIFactory.CreateText(content, "Title", "미니게임 천국", 92,
                UITheme.TextGold, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(3f, -3f);
            Center((RectTransform)title.transform, 340f, 130f, 1400f);

            Text subtitle = UIFactory.CreateText(content, "Subtitle", "플레이할 게임을 선택하세요", 34,
                UITheme.TextDim, TextAnchor.MiddleCenter);
            Center((RectTransform)subtitle.transform, 250f, 60f, 1200f);

            // Cards: merge game first (playable), then placeholders.
            int total = 1 + ComingSoon.Length;
            const float cardW = 300f, gap = 44f;
            float startX = -(total - 1) * 0.5f * (cardW + gap);

            view.BuildCard(content, startX, "머지 카페", "플레이 가능", true, view.SelectMerge);
            for (int i = 0; i < ComingSoon.Length; i++)
            {
                float x = startX + (i + 1) * (cardW + gap);
                view.BuildCard(content, x, ComingSoon[i].name, ComingSoon[i].subtitle, false,
                    view.ShowComingSoon);
            }

            view._notice = UIFactory.CreateText(content, "Notice", "", 32, UITheme.TextMain,
                TextAnchor.MiddleCenter);
            Center((RectTransform)view._notice.transform, -320f, 60f, 1200f);
            view._notice.canvasRenderer.SetAlpha(0f);

            return view;
        }

        private void BuildCard(RectTransform parent, float x, string name, string status,
            bool playable, Action onClick)
        {
            const float cardW = 300f, cardH = 380f;

            Image card = UIFactory.CreateImage(parent, $"Card_{name}", playable ? CardBg : CardLocked);
            card.sprite = SpriteFactory.RoundedRect;
            card.type = Image.Type.Sliced;
            card.raycastTarget = true;
            var rect = (RectTransform)card.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, -30f);
            rect.sizeDelta = new Vector2(cardW, cardH);

            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card;
            ColorBlock colors = button.colors;
            colors.highlightedColor = playable ? new Color(1.1f, 1.1f, 1.1f, 1f) : Color.white;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.onClick.AddListener(() => onClick());

            // Icon disc.
            Image disc = UIFactory.CreateImage(rect, "Disc",
                playable ? new Color(0.42f, 0.29f, 0.18f, 1f) : new Color(0.2f, 0.16f, 0.13f, 1f));
            disc.sprite = SpriteFactory.Circle;
            disc.raycastTarget = false;
            var discRect = (RectTransform)disc.transform;
            discRect.anchorMin = new Vector2(0.5f, 1f);
            discRect.anchorMax = new Vector2(0.5f, 1f);
            discRect.pivot = new Vector2(0.5f, 1f);
            discRect.anchoredPosition = new Vector2(0f, -40f);
            discRect.sizeDelta = new Vector2(180f, 180f);

            if (playable)
            {
                Image cup = UIFactory.CreateImage(discRect, "Cup", UITheme.TextGold);
                cup.sprite = CafeArt.CoffeeCup;
                cup.preserveAspect = true;
                cup.raycastTarget = false;
                UIFactory.Stretch((RectTransform)cup.transform);
                ((RectTransform)cup.transform).offsetMin = new Vector2(24f, 24f);
                ((RectTransform)cup.transform).offsetMax = new Vector2(-24f, -24f);
            }
            else
            {
                Text q = UIFactory.CreateText(discRect, "Q", "?", 96, UITheme.TextDim,
                    TextAnchor.MiddleCenter, FontStyle.Bold);
                UIFactory.Stretch((RectTransform)q.transform);
            }

            Text nameText = UIFactory.CreateText(rect, "Name", name, 38,
                playable ? UITheme.TextMain : UITheme.TextDim, TextAnchor.MiddleCenter, FontStyle.Bold);
            var nameRect = (RectTransform)nameText.transform;
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(1f, 0f);
            nameRect.offsetMin = new Vector2(8f, 108f);
            nameRect.offsetMax = new Vector2(-8f, 160f);
            nameText.raycastTarget = false;

            Text statusText = UIFactory.CreateText(rect, "Status", status, 26,
                playable ? UITheme.TextGold : UITheme.TextDanger, TextAnchor.MiddleCenter, FontStyle.Bold);
            var statusRect = (RectTransform)statusText.transform;
            statusRect.anchorMin = new Vector2(0f, 0f);
            statusRect.anchorMax = new Vector2(1f, 0f);
            statusRect.offsetMin = new Vector2(8f, 48f);
            statusRect.offsetMax = new Vector2(-8f, 96f);
            statusText.raycastTarget = false;
        }

        /// <summary>Starts the merge game (also used by play-mode tests).</summary>
        public void SelectMerge()
        {
            if (_started)
                return;
            _started = true;

            Action start = _onStartMerge;
            _onStartMerge = null;
            start?.Invoke();

            Destroy(transform.root.gameObject);
        }

        private void ShowComingSoon()
        {
            if (_notice == null)
                return;
            if (_noticeRoutine != null)
                StopCoroutine(_noticeRoutine);
            _noticeRoutine = StartCoroutine(NoticeRoutine("아직 준비 중이에요. 곧 만나요!"));
        }

        private IEnumerator NoticeRoutine(string message)
        {
            _notice.text = message;
            _notice.canvasRenderer.SetAlpha(1f);
            yield return new WaitForSecondsRealtime(1.4f);
            for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
            {
                if (_notice == null)
                    yield break;
                _notice.canvasRenderer.SetAlpha(1f - t / 0.4f);
                yield return null;
            }
            if (_notice != null)
                _notice.canvasRenderer.SetAlpha(0f);
            _noticeRoutine = null;
        }

        private static void ScatterBeans(RectTransform content)
        {
            var rng = new System.Random(2024);
            for (int i = 0; i < 14; i++)
            {
                Image bean = UIFactory.CreateImage(content, $"Bean_{i}",
                    new Color(BeanColor.r, BeanColor.g, BeanColor.b, 0.5f));
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

        private static void Center(RectTransform rect, float y, float height, float width)
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
