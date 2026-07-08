using MergeCafe.Core;
using MergeCafe.Data;
using MergeCafe.UI;
using UnityEngine;
using UnityEngine.UI;

namespace MergeCafe.Generators
{
    /// <summary>
    /// One generator button in the left panel: name, energy, recovery countdown /
    /// unlock cost. Clicking spawns an item (or explains why it can't).
    /// </summary>
    public sealed class GeneratorButtonView : MonoBehaviour
    {
        private GameManager _game;
        private ItemType _type;

        private Image _background;
        private Text _nameText;
        private Text _energyText;
        private Text _statusText;

        public static GeneratorButtonView Build(RectTransform generatorPanel, GameManager game,
            ItemType type, int slotIndex)
        {
            const float top = 60f;      // below the panel title
            const float height = 150f;
            const float spacing = 16f;

            float y = -(top + slotIndex * (height + spacing));

            Image background = UIFactory.CreateImage(generatorPanel, $"Generator_{type}", UITheme.ButtonPrimary);
            background.sprite = SpriteFactory.RoundedRect;
            background.type = Image.Type.Sliced;

            var rect = (RectTransform)background.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(16f, y - height);
            rect.offsetMax = new Vector2(-16f, y);

            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;

            var view = background.gameObject.AddComponent<GeneratorButtonView>();
            view._game = game;
            view._type = type;
            view._background = background;

            view._nameText = UIFactory.CreateText(rect, "Name", "", 28, UITheme.TextMain,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            SetRow((RectTransform)view._nameText.transform, 0.62f, 1f);

            view._energyText = UIFactory.CreateText(rect, "Energy", "", 24, UITheme.TextMain,
                TextAnchor.MiddleLeft);
            SetRow((RectTransform)view._energyText.transform, 0.32f, 0.62f);

            view._statusText = UIFactory.CreateText(rect, "Status", "", 20, UITheme.TextDim,
                TextAnchor.MiddleLeft);
            SetRow((RectTransform)view._statusText.transform, 0f, 0.32f);

            button.onClick.AddListener(view.OnClick);
            view.Refresh(TimeUtil.NowUnixSeconds());
            return view;
        }

        private static void SetRow(RectTransform rect, float yMin, float yMax)
        {
            rect.anchorMin = new Vector2(0f, yMin);
            rect.anchorMax = new Vector2(1f, yMax);
            rect.offsetMin = new Vector2(20f, 0f);
            rect.offsetMax = new Vector2(-14f, 0f);
        }

        private void OnClick()
        {
            _game.RequestSpawn(_type, TimeUtil.NowUnixSeconds());
        }

        private void Update()
        {
            Refresh(TimeUtil.NowUnixSeconds());
        }

        private void Refresh(double nowUnix)
        {
            GeneratorState state = _game.Generators.Get(_type);
            GeneratorDefinition def = state.Definition;
            ItemDefinition output = ItemCatalog.Get(_type, 1);

            if (!state.Unlocked)
            {
                _background.color = UITheme.ButtonDisabled;
                _nameText.text = $"{def.DisplayName} (잠김)";
                _energyText.text = $"생산: {output.DisplayName}";
                _statusText.text = $"{def.UnlockCost} 골드로 해금";
                return;
            }

            _background.color = UITheme.ButtonPrimary;
            _nameText.text = def.DisplayName;
            _energyText.text = $"에너지 {state.Energy}/{state.MaxEnergy}";
            _statusText.text = state.Energy >= state.MaxEnergy
                ? $"가득 참 · 생산: {output.DisplayName}"
                : $"다음 회복 {Mathf.CeilToInt((float)state.SecondsToNextRecovery(nowUnix))}초";
        }
    }
}
