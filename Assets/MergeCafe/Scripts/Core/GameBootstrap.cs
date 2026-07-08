using MergeCafe.Board;
using MergeCafe.Data;
using MergeCafe.Generators;
using MergeCafe.Items;
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
        private GameManager _game;
        private BoardGridView _gridView;
        private ToastView _toast;

        private void Awake()
        {
            Application.targetFrameRate = 60;

            UIFactory.EnsureEventSystem();
            _ui = UIFactory.BuildBaseLayout();

            _hud = HudView.Build(_ui.TopHud);
            _hud.SetGold(0);

            UIFactory.CreatePanelTitle(_ui.GeneratorPanel, "생성기");
            UIFactory.CreatePanelTitle(_ui.OrderPanel, "주문");

            _game = new GameManager(TimeUtil.NowUnixSeconds());
            _gridView = BoardGridView.Build(_ui.BoardPanel, _game.Board);

            var dragController = DragController.Build(_ui.DragLayer, _game, _gridView);
            _gridView.SetDragHandler(dragController);

            _toast = ToastView.Build(_ui.ToastLayer);
            _game.ToastRequested += message => _toast.Show(message);
            _game.ItemSpawned += index => _gridView.PlayPop(index);

            GeneratorButtonView.Build(_ui.GeneratorPanel, _game, ItemType.Coffee, 0);
            GeneratorButtonView.Build(_ui.GeneratorPanel, _game, ItemType.Bread, 1);
            GeneratorButtonView.Build(_ui.GeneratorPanel, _game, ItemType.Dessert, 2);

            _game.Board.BoardChanged += RefreshHudSpace;
            RefreshHudSpace();

            var upgradePlaceholder = UIFactory.CreateText(_ui.UpgradePanel, "UpgradePlaceholder",
                "보드 확장 / 생성기 업그레이드", 26, UITheme.TextDim);
            UIFactory.Stretch((RectTransform)upgradePlaceholder.transform);
        }

        private void Update()
        {
            _game.Tick(TimeUtil.NowUnixSeconds());
        }

        private void RefreshHudSpace()
        {
            _hud.SetBoardSpace(_game.Board.EmptyUnlockedCount, _game.Board.UnlockedCount);
        }
    }
}
