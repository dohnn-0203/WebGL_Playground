using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MergeCafe.UI
{
    /// <summary>
    /// Short fading message near the bottom of the screen. Repeated identical
    /// messages are throttled so toasts never spam (webGL_game.md §9, §16).
    /// </summary>
    public sealed class ToastView : MonoBehaviour
    {
        private CanvasGroup _group;
        private Text _text;
        private Coroutine _routine;
        private string _lastMessage;
        private float _lastShownAt = -10f;

        public static ToastView Build(RectTransform toastLayer)
        {
            Image panel = UIFactory.CreateImage(toastLayer, "Toast", new Color(0f, 0f, 0f, 0.78f));
            panel.sprite = SpriteFactory.RoundedRect;
            panel.type = Image.Type.Sliced;
            panel.raycastTarget = false;

            var rect = (RectTransform)panel.transform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 160f);
            rect.sizeDelta = new Vector2(640f, 64f);

            var view = panel.gameObject.AddComponent<ToastView>();
            view._group = panel.gameObject.AddComponent<CanvasGroup>();
            view._group.alpha = 0f;
            view._group.interactable = false;
            view._group.blocksRaycasts = false;

            view._text = UIFactory.CreateText(panel.transform, "Message", "", 26, UITheme.TextMain);
            UIFactory.Stretch((RectTransform)view._text.transform);

            return view;
        }

        public void Show(string message, float duration = 1.6f)
        {
            // Anti-spam: identical message within 1 second is ignored.
            if (message == _lastMessage && Time.unscaledTime - _lastShownAt < 1f)
                return;

            _lastMessage = message;
            _lastShownAt = Time.unscaledTime;
            _text.text = message;

            if (_routine != null)
                StopCoroutine(_routine);
            _routine = StartCoroutine(Animate(duration));
        }

        private IEnumerator Animate(float duration)
        {
            const float fadeIn = 0.12f;
            const float fadeOut = 0.35f;

            for (float t = 0f; t < fadeIn; t += Time.unscaledDeltaTime)
            {
                _group.alpha = Mathf.Lerp(_group.alpha, 1f, t / fadeIn);
                yield return null;
            }
            _group.alpha = 1f;

            yield return new WaitForSecondsRealtime(duration);

            for (float t = 0f; t < fadeOut; t += Time.unscaledDeltaTime)
            {
                _group.alpha = 1f - t / fadeOut;
                yield return null;
            }
            _group.alpha = 0f;
            _routine = null;
        }
    }
}
