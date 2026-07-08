using UnityEngine;
using UnityEngine.UI;

namespace MergeCafe.UI
{
    /// <summary>
    /// Top HUD: gold, board space and the settings (reset) button.
    /// Built from code by <see cref="Build"/>; later systems push values into it.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        private Text _goldText;
        private Text _spaceText;
        private Button _settingsButton;

        public Button SettingsButton => _settingsButton;

        public static HudView Build(RectTransform topHud)
        {
            var view = topHud.gameObject.AddComponent<HudView>();

            Text title = UIFactory.CreateText(topHud, "GameTitle", "머지 카페", 34, UITheme.TextMain,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            var titleRect = (RectTransform)title.transform;
            titleRect.anchorMin = new Vector2(0f, 0f);
            titleRect.anchorMax = new Vector2(0f, 1f);
            titleRect.offsetMin = new Vector2(24f, 0f);
            titleRect.offsetMax = new Vector2(300f, 0f);

            view._goldText = UIFactory.CreateText(topHud, "GoldText", "골드 0", 32, UITheme.TextGold,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            var goldRect = (RectTransform)view._goldText.transform;
            goldRect.anchorMin = new Vector2(0.5f, 0f);
            goldRect.anchorMax = new Vector2(0.5f, 1f);
            goldRect.offsetMin = new Vector2(-220f, 0f);
            goldRect.offsetMax = new Vector2(60f, 0f);

            view._spaceText = UIFactory.CreateText(topHud, "SpaceText", "빈 칸 -/-", 26, UITheme.TextDim,
                TextAnchor.MiddleCenter);
            var spaceRect = (RectTransform)view._spaceText.transform;
            spaceRect.anchorMin = new Vector2(0.5f, 0f);
            spaceRect.anchorMax = new Vector2(0.5f, 1f);
            spaceRect.offsetMin = new Vector2(120f, 0f);
            spaceRect.offsetMax = new Vector2(420f, 0f);

            view._settingsButton = UIFactory.CreateButton(topHud, "SettingsButton", "초기화", 24,
                UITheme.ButtonSecondary, out _);
            var settingsRect = (RectTransform)view._settingsButton.transform;
            settingsRect.anchorMin = new Vector2(1f, 0.5f);
            settingsRect.anchorMax = new Vector2(1f, 0.5f);
            settingsRect.pivot = new Vector2(1f, 0.5f);
            settingsRect.anchoredPosition = new Vector2(-24f, 0f);
            settingsRect.sizeDelta = new Vector2(140f, 56f);

            return view;
        }

        public void SetGold(long gold)
        {
            _goldText.text = $"골드 {gold:N0}";
        }

        public void SetBoardSpace(int emptyUnlocked, int unlockedTotal)
        {
            _spaceText.text = $"빈 칸 {emptyUnlocked}/{unlockedTotal}";
        }
    }
}
