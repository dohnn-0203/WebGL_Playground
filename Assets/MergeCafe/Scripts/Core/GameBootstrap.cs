using MergeCafe.Board;
using MergeCafe.Data;
using MergeCafe.Generators;
using MergeCafe.Items;
using MergeCafe.Orders;
using MergeCafe.Save;
using MergeCafe.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        /// <summary>Exposed for play-mode tests.</summary>
        public GameManager Game => _game;

        private void Awake()
        {
#if !UNITY_WEBGL
            // On WebGL the default (-1) uses requestAnimationFrame — forcing 60
            // would switch Unity to a janky setTimeout loop.
            Application.targetFrameRate = 60;
#endif

            UIFactory.EnsureEventSystem();

            // Show the title screen first; the game is built when the player clicks (§ start flow).
            TitleScreenView.Build(StartGame);
        }

        /// <summary>Builds the full game. Idempotent; called by the title screen click (or tests).</summary>
        public void StartGame()
        {
            if (_game != null)
                return;

            _ui = UIFactory.BuildBaseLayout();
            CafeDecor.Apply(_ui);

            _hud = HudView.Build(_ui.TopHud);

            _game = new GameManager(TimeUtil.NowUnixSeconds());

            TotalGaugeView.Build(_ui.LeftPanel, _game);
            _gridView = BoardGridView.Build(_ui.BoardPanel, _game.Board);

            var dragController = DragController.Build(_ui.DragLayer, _game, _gridView);
            _gridView.SetDragHandler(dragController);

            _toast = ToastView.Build(_ui.ToastLayer);
            _game.ToastRequested += message => _toast.Show(message);
            _game.ItemSpawned += index => _gridView.PlayPop(index);
            _game.ItemMerged += index =>
            {
                _gridView.PlayPop(index);
                _gridView.PlayMergeFlash(index);
            };
            _game.OrderCompleted += order => _toast.FloatGold($"+{order.rewardGold} 골드");

            _orderCards = new OrderCardView[OrderManager.OrderCount];
            for (int i = 0; i < OrderManager.OrderCount; i++)
                _orderCards[i] = OrderCardView.Build(_ui.LeftPanel, _game, i);

            _game.Orders.OrdersChanged += RebindOrderCards;
            _game.Board.BoardChanged += RefreshOrderButtons;

            _game.Economy.GoldChanged += gold => _hud.SetGold(gold);
            _hud.SetGold(_game.Economy.Gold);

            UpgradePanelView.Build(_ui.BottomBar, _game);

            // Restore progress (if any), then autosave after every successful action (§14).
            SaveManager.TryLoadInto(_game, TimeUtil.NowUnixSeconds());
            _game.StateChanged += () => SaveManager.Save(_game);

            _hud.SettingsButton.onClick.AddListener(ShowResetPopup);
        }

        private void ShowResetPopup()
        {
            ConfirmPopup.Show(_ui.PopupLayer, "게임 초기화",
                "저장된 진행 상황이 모두 삭제됩니다.\n계속할까요?", ResetGame);
        }

        private void ResetGame()
        {
            SaveManager.Delete();
            SceneManager.LoadScene(gameObject.scene.buildIndex);
        }

        private void OnApplicationPause(bool paused)
        {
            // Best effort save when the browser tab loses focus / page is left (§14).
            if (paused && _game != null)
                SaveManager.Save(_game);
        }

        private void OnApplicationQuit()
        {
            if (_game != null)
                SaveManager.Save(_game);
        }

        private void Update()
        {
            // Null until the player starts the game from the title screen.
            if (_game == null)
                return;
            _game.Tick(TimeUtil.NowUnixSeconds());
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
