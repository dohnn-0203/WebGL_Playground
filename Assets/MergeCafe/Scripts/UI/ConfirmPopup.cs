using System;
using UnityEngine;
using UnityEngine.UI;

namespace MergeCafe.UI
{
    /// <summary>
    /// Modal yes/no dialog used by the reset button (webGL_game.md §14).
    /// Fully runtime-built; destroys itself on any choice.
    /// </summary>
    public sealed class ConfirmPopup : MonoBehaviour
    {
        public static ConfirmPopup Show(RectTransform popupLayer, string title, string message,
            Action onConfirm)
        {
            // Dim background that also swallows clicks behind the dialog.
            Image dim = UIFactory.CreateImage(popupLayer, "ConfirmPopup", new Color(0f, 0f, 0f, 0.6f));
            dim.raycastTarget = true;
            UIFactory.Stretch((RectTransform)dim.transform);

            var popup = dim.gameObject.AddComponent<ConfirmPopup>();

            Image panel = UIFactory.CreateImage(dim.transform, "Panel", UITheme.HudBg);
            panel.sprite = SpriteFactory.RoundedRect;
            panel.type = Image.Type.Sliced;
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(560f, 300f);

            Text titleText = UIFactory.CreateText(panelRect, "Title", title, 30, UITheme.TextMain,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            var titleRect = (RectTransform)titleText.transform;
            titleRect.anchorMin = new Vector2(0f, 0.72f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;

            Text messageText = UIFactory.CreateText(panelRect, "Message", message, 24, UITheme.TextDim);
            var messageRect = (RectTransform)messageText.transform;
            messageRect.anchorMin = new Vector2(0f, 0.34f);
            messageRect.anchorMax = new Vector2(1f, 0.72f);
            messageRect.offsetMin = new Vector2(24f, 0f);
            messageRect.offsetMax = new Vector2(-24f, 0f);

            Button cancel = UIFactory.CreateButton(panelRect, "CancelButton", "취소", 24,
                UITheme.ButtonSecondary, out _);
            var cancelRect = (RectTransform)cancel.transform;
            cancelRect.anchorMin = new Vector2(0f, 0f);
            cancelRect.anchorMax = new Vector2(0.5f, 0.34f);
            cancelRect.offsetMin = new Vector2(24f, 24f);
            cancelRect.offsetMax = new Vector2(-12f, -12f);
            cancel.onClick.AddListener(popup.Close);

            Button confirm = UIFactory.CreateButton(panelRect, "ConfirmButton", "초기화", 24,
                UITheme.ButtonDanger, out _);
            var confirmRect = (RectTransform)confirm.transform;
            confirmRect.anchorMin = new Vector2(0.5f, 0f);
            confirmRect.anchorMax = new Vector2(1f, 0.34f);
            confirmRect.offsetMin = new Vector2(12f, 24f);
            confirmRect.offsetMax = new Vector2(-24f, -12f);
            confirm.onClick.AddListener(() =>
            {
                popup.Close();
                onConfirm?.Invoke();
            });

            return popup;
        }

        private void Close()
        {
            Destroy(gameObject);
        }
    }
}
