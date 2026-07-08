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

        private void Awake()
        {
            Application.targetFrameRate = 60;

            UIFactory.EnsureEventSystem();
            _ui = UIFactory.BuildBaseLayout();

            _hud = HudView.Build(_ui.TopHud);
            _hud.SetGold(0);

            UIFactory.CreatePanelTitle(_ui.GeneratorPanel, "생성기");
            UIFactory.CreatePanelTitle(_ui.OrderPanel, "주문");

            // v0.0.1 placeholder — replaced by the real 6x6 board in v0.0.2.
            var boardPlaceholder = UIFactory.CreateText(_ui.BoardPanel, "BoardPlaceholder",
                "6 x 6 머지 보드 준비 중", 30, UITheme.TextDim);
            UIFactory.Stretch((RectTransform)boardPlaceholder.transform);

            var upgradePlaceholder = UIFactory.CreateText(_ui.UpgradePanel, "UpgradePlaceholder",
                "보드 확장 / 생성기 업그레이드", 26, UITheme.TextDim);
            UIFactory.Stretch((RectTransform)upgradePlaceholder.transform);
        }
    }
}
