using MergeCafe.Core;
using MergeCafe.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace MergeCafe.UI
{
    /// <summary>
    /// Bottom bar: board-expand and energy-upgrade buttons. Each disables itself
    /// when gold is short and always shows its cost (webGL_game.md §13, §16).
    /// </summary>
    public sealed class UpgradePanelView : MonoBehaviour
    {
        private GameManager _game;

        private Button _expandButton;
        private Text _expandLabel;
        private Button _energyButton;
        private Text _energyLabel;

        public static UpgradePanelView Build(RectTransform bottomBar, GameManager game)
        {
            var view = bottomBar.gameObject.AddComponent<UpgradePanelView>();
            view._game = game;

            view._expandButton = UIFactory.CreateButton(bottomBar, "ExpandButton", "", 24,
                UITheme.ButtonSecondary, out view._expandLabel);
            Place((RectTransform)view._expandButton.transform, 0.06f, 0.30f);
            view._expandButton.onClick.AddListener(() => game.RequestExpandBoard());

            view._energyButton = UIFactory.CreateButton(bottomBar, "EnergyButton", "", 24,
                UITheme.ButtonSecondary, out view._energyLabel);
            Place((RectTransform)view._energyButton.transform, 0.34f, 0.58f);
            view._energyButton.onClick.AddListener(() => game.RequestUpgradeEnergy());

            game.Economy.GoldChanged += _ => view.Refresh();
            game.Generators.StatesChanged += view.Refresh;
            game.Upgrades.Changed += view.Refresh;
            game.Board.BoardChanged += view.Refresh;
            view.Refresh();
            return view;
        }

        private static void Place(RectTransform rect, float xMin, float xMax)
        {
            rect.anchorMin = new Vector2(xMin, 0f);
            rect.anchorMax = new Vector2(xMax, 1f);
            rect.offsetMin = new Vector2(0f, 20f);
            rect.offsetMax = new Vector2(0f, -20f);
        }

        private void Refresh()
        {
            if (UpgradeManager.IsBoardFullyUnlocked(_game.Board))
            {
                _expandLabel.text = "보드 확장 완료";
                _expandButton.interactable = false;
            }
            else
            {
                int cost = _game.Upgrades.NextCellCost;
                _expandLabel.text = $"보드 확장  ({cost} 골드)";
                _expandButton.interactable = _game.Economy.CanAfford(cost);
            }

            int energyCost = _game.Upgrades.NextEnergyCost;
            _energyLabel.text = $"최대 에너지 +{UpgradeManager.EnergyStep}  ({energyCost} 골드)";
            _energyButton.interactable = _game.Economy.CanAfford(energyCost);
        }
    }
}
