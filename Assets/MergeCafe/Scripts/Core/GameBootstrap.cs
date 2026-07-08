using MergeCafe.Board;
using MergeCafe.Data;
using MergeCafe.Generators;
using MergeCafe.Items;
using MergeCafe.Orders;
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
        private OrderCardView[] _orderCards;

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

            _orderCards = new OrderCardView[OrderManager.OrderCount];
            for (int i = 0; i < OrderManager.OrderCount; i++)
                _orderCards[i] = OrderCardView.Build(_ui.OrderPanel, _game, i);

            _game.Orders.OrdersChanged += RebindOrderCards;
            _game.Board.BoardChanged += RefreshOrderButtons;

            _game.Economy.GoldChanged += gold => _hud.SetGold(gold);
            _hud.SetGold(_game.Economy.Gold);

            _game.Board.BoardChanged += RefreshHudSpace;
            RefreshHudSpace();

            UpgradePanelView.Build(_ui.UpgradePanel, _game);
        }

        private void Update()
        {
            _game.Tick(TimeUtil.NowUnixSeconds());
        }

        private void RefreshHudSpace()
        {
            _hud.SetBoardSpace(_game.Board.EmptyUnlockedCount, _game.Board.UnlockedCount);
        }

        private void RebindOrderCards()
        {
            foreach (OrderCardView card in _orderCards)
                card.Rebind();
        }

        private void RefreshOrderButtons()
        {
            foreach (OrderCardView card in _orderCards)
                card.RefreshButton();
        }
    }
}
