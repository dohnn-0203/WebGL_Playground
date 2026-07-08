using MergeCafe.Board;
using MergeCafe.UI;
using UnityEngine;

namespace MergeCafe.Core
{
    /// <summary>
    /// Single scene entry point. The scene only contains a camera and this component;
    /// everything else (systems + UI hierarchy of webGL_game.md §7) is built here at runtime,
    /// following the code-first policy of §19.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        private UiLayout _ui;
        private HudView _hud;
        private BoardManager _board;
        private BoardGridView _gridView;

        private void Awake()
        {
            Application.targetFrameRate = 60;

            UIFactory.EnsureEventSystem();
            _ui = UIFactory.BuildBaseLayout();

            _hud = HudView.Build(_ui.TopHud);
            _hud.SetGold(0);

            UIFactory.CreatePanelTitle(_ui.GeneratorPanel, "생성기");
            UIFactory.CreatePanelTitle(_ui.OrderPanel, "주문");

            _board = new BoardManager();
            _gridView = BoardGridView.Build(_ui.BoardPanel, _board);

            _board.BoardChanged += RefreshHudSpace;
            RefreshHudSpace();

            var upgradePlaceholder = UIFactory.CreateText(_ui.UpgradePanel, "UpgradePlaceholder",
                "보드 확장 / 생성기 업그레이드", 26, UITheme.TextDim);
            UIFactory.Stretch((RectTransform)upgradePlaceholder.transform);
        }

        private void RefreshHudSpace()
        {
            _hud.SetBoardSpace(_board.EmptyUnlockedCount, _board.UnlockedCount);
        }
    }
}
