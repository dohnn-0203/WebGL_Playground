using MergeCafe.Core;
using MergeCafe.Generators;
using UnityEngine;
using UnityEngine.UI;

namespace MergeCafe.UI
{
    /// <summary>
    /// Shared-energy gauge at the top of the left panel: a filled bar with
    /// "current / max" and the countdown to the next +1 (webGL total gauge).
    /// </summary>
    public sealed class TotalGaugeView : MonoBehaviour
    {
        private EnergyPool _pool;
        private RectTransform _fill;
        private Text _valueText;
        private Text _timerText;

        public const float Height = 96f;

        public static TotalGaugeView Build(RectTransform leftPanel, GameManager game)
        {
            RectTransform root = UIFactory.CreateUiObject(leftPanel, "TotalGauge");
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.offsetMin = new Vector2(14f, -Height - 12f);
            root.offsetMax = new Vector2(-14f, -12f);

            var view = root.gameObject.AddComponent<TotalGaugeView>();
            view._pool = game.Generators.Energy;

            Text title = UIFactory.CreateText(root, "Title", "에너지", 24, UITheme.TextMain,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            var titleRect = (RectTransform)title.transform;
            titleRect.anchorMin = new Vector2(0f, 0.62f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(6f, 0f);
            titleRect.offsetMax = new Vector2(-6f, 0f);

            view._timerText = UIFactory.CreateText(root, "Timer", "", 20, UITheme.TextDim,
                TextAnchor.MiddleRight);
            var timerRect = (RectTransform)view._timerText.transform;
            timerRect.anchorMin = new Vector2(0f, 0.62f);
            timerRect.anchorMax = new Vector2(1f, 1f);
            timerRect.offsetMin = new Vector2(6f, 0f);
            timerRect.offsetMax = new Vector2(-6f, 0f);

            // Bar track.
            Image track = UIFactory.CreateImage(root, "Track", UITheme.EnergyTrack);
            track.sprite = SpriteFactory.RoundedRect;
            track.type = Image.Type.Sliced;
            track.raycastTarget = false;
            var trackRect = (RectTransform)track.transform;
            trackRect.anchorMin = new Vector2(0f, 0.12f);
            trackRect.anchorMax = new Vector2(1f, 0.54f);
            trackRect.offsetMin = new Vector2(6f, 0f);
            trackRect.offsetMax = new Vector2(-6f, 0f);

            Image fill = UIFactory.CreateImage(trackRect, "Fill", UITheme.EnergyFill);
            fill.sprite = SpriteFactory.RoundedRect;
            fill.type = Image.Type.Sliced;
            fill.raycastTarget = false;
            view._fill = (RectTransform)fill.transform;
            view._fill.anchorMin = Vector2.zero;
            view._fill.anchorMax = new Vector2(1f, 1f);
            view._fill.offsetMin = Vector2.zero;
            view._fill.offsetMax = Vector2.zero;

            view._valueText = UIFactory.CreateText(trackRect, "Value", "", 22, UITheme.TextMain,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch((RectTransform)view._valueText.transform);
            view._valueText.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(1f, -1f);

            game.Generators.StatesChanged += view.Refresh;
            view.Refresh();
            return view;
        }

        private void Update()
        {
            // Live countdown to the next recovery tick.
            double now = TimeUtil.NowUnixSeconds();
            if (_pool.Current >= _pool.Max)
                _timerText.text = "가득 참";
            else
                _timerText.text = $"+1 : {Mathf.CeilToInt((float)_pool.SecondsToNextRecovery(now))}초";
        }

        private void Refresh()
        {
            float ratio = _pool.Max > 0 ? Mathf.Clamp01(_pool.Current / (float)_pool.Max) : 0f;
            _fill.anchorMax = new Vector2(ratio, 1f);
            _valueText.text = $"{_pool.Current} / {_pool.Max}";
        }
    }
}
