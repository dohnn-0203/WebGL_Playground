using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MergeCafe.UI
{
    /// <summary>Top HUD: title, gold, and the settings (reset) button.</summary>
    public sealed class HudView : MonoBehaviour
    {
        private TextMeshProUGUI _goldText;
        private Button _settingsButton;

        public Button SettingsButton => _settingsButton;

        public static HudView Build(RectTransform topHud)
        {
            var view = topHud.gameObject.AddComponent<HudView>();

            var title = UIFactory.CreateText(topHud, "GameTitle", "머지 카페", 32, UITheme.TextMain,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            var titleRect = (RectTransform)title.transform;
            titleRect.anchorMin = new Vector2(0f, 0f);
            titleRect.anchorMax = new Vector2(0f, 1f);
            titleRect.offsetMin = new Vector2(24f, 0f);
            titleRect.offsetMax = new Vector2(300f, 0f);

            view._goldText = UIFactory.CreateText(topHud, "GoldText", "골드 0", 30, UITheme.TextGold,
                TextAnchor.MiddleRight, FontStyle.Bold);
            var goldRect = (RectTransform)view._goldText.transform;
            goldRect.anchorMin = new Vector2(1f, 0f);
            goldRect.anchorMax = new Vector2(1f, 1f);
            goldRect.offsetMin = new Vector2(-380f, 0f);
            goldRect.offsetMax = new Vector2(-170f, 0f);

            view._settingsButton = UIFactory.CreateButton(topHud, "SettingsButton", "초기화", 22,
                UITheme.ButtonSecondary, out _);
            var settingsRect = (RectTransform)view._settingsButton.transform;
            settingsRect.anchorMin = new Vector2(1f, 0.5f);
            settingsRect.anchorMax = new Vector2(1f, 0.5f);
            settingsRect.pivot = new Vector2(1f, 0.5f);
            settingsRect.anchoredPosition = new Vector2(-20f, 0f);
            settingsRect.sizeDelta = new Vector2(130f, 52f);

            return view;
        }

        public void SetGold(long gold)
        {
            _goldText.text = $"골드 {gold:N0}";
        }
    }
}
