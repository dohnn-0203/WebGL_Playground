using MergeCafe.Core;
using MergeCafe.Data;
using MergeCafe.Economy;
using MergeCafe.Generators;
using UnityEngine;
using UnityEngine.UI;

namespace MergeCafe.UI
{
    /// <summary>
    /// Bottom bar: board-expand button + one upgrade button per generator.
    /// Buttons disable themselves when gold is short and always show the cost
    /// (webGL_game.md §13, §16).
    /// </summary>
    public sealed class UpgradePanelView : MonoBehaviour
    {
        private GameManager _game;

        private Button _expandButton;
        private Text _expandLabel;
        private readonly Button[] _upgradeButtons = new Button[3];
        private readonly Text[] _upgradeLabels = new Text[3];

        public static UpgradePanelView Build(RectTransform upgradePanel, GameManager game)
        {
            var view = upgradePanel.gameObject.AddComponent<UpgradePanelView>();
            view._game = game;

            view._expandButton = UIFactory.CreateButton(upgradePanel, "ExpandButton", "", 22,
                UITheme.ButtonSecondary, out view._expandLabel);
            PlaceButton((RectTransform)view._expandButton.transform, 24f, 400f);
            view._expandButton.onClick.AddListener(() => game.RequestExpandBoard());

            for (int i = 0; i < 3; i++)
            {
                var type = (ItemType)i;
                view._upgradeButtons[i] = UIFactory.CreateButton(upgradePanel,
                    $"UpgradeButton_{type}", "", 22, UITheme.ButtonSecondary, out view._upgradeLabels[i]);
                PlaceButton((RectTransform)view._upgradeButtons[i].transform, 440f + i * 396f, 380f);
                view._upgradeButtons[i].onClick.AddListener(() => game.RequestUpgradeGenerator(type));
            }

            game.Economy.GoldChanged += _ => view.Refresh();
            game.Generators.StatesChanged += view.Refresh;
            game.Upgrades.Changed += view.Refresh;
            game.Board.BoardChanged += view.Refresh;
            view.Refresh();
            return view;
        }

        private static void PlaceButton(RectTransform rect, float x, float width)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.offsetMin = new Vector2(x, 16f);
            rect.offsetMax = new Vector2(x + width, -16f);
        }

        private void Refresh()
        {
            RefreshExpand();
            for (int i = 0; i < 3; i++)
                RefreshUpgrade(i);
        }

        private void RefreshExpand()
        {
            if (UpgradeManager.IsBoardFullyUnlocked(_game.Board))
            {
                _expandLabel.text = "보드 확장 완료";
                _expandButton.interactable = false;
                return;
            }

            int cost = _game.Upgrades.NextCellCost;
            _expandLabel.text = $"보드 확장 ({cost} 골드)";
            _expandButton.interactable = _game.Economy.CanAfford(cost);
        }

        private void RefreshUpgrade(int slot)
        {
            var type = (ItemType)slot;
            GeneratorState state = _game.Generators.Get(type);
            Button button = _upgradeButtons[slot];
            Text label = _upgradeLabels[slot];
            string name = state.Definition.DisplayName;

            if (!state.Unlocked)
            {
                label.text = $"{name} 해금 필요";
                button.interactable = false;
                return;
            }

            if (state.UpgradeLevel >= GeneratorCatalog.MaxUpgradeLevel)
            {
                label.text = $"{name} 최대 강화";
                button.interactable = false;
                return;
            }

            int cost = GeneratorCatalog.UpgradeCost(state.UpgradeLevel + 1);
            label.text = $"{name} 강화 Lv.{state.UpgradeLevel + 1} ({cost} 골드)";
            button.interactable = _game.Economy.CanAfford(cost);
        }
    }
}
