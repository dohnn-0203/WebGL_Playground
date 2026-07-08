using MergeCafe.Core;
using MergeCafe.Data;
using MergeCafe.UI;
using UnityEngine;
using UnityEngine.UI;

namespace MergeCafe.Orders
{
    /// <summary>
    /// One of the three order cards in the right panel: required item, reward and
    /// the complete ("납품") button that enables only when the board has the item.
    /// </summary>
    public sealed class OrderCardView : MonoBehaviour
    {
        private GameManager _game;
        private int _slot;

        private Image _tokenCircle;
        private Text _tokenLabel;
        private Text _requirementText;
        private Text _rewardText;
        private Button _completeButton;

        public static OrderCardView Build(RectTransform orderPanel, GameManager game, int slot)
        {
            const float top = 60f;
            const float height = 240f;
            const float spacing = 16f;

            float y = -(top + slot * (height + spacing));

            Image card = UIFactory.CreateImage(orderPanel, $"OrderCard_{slot}", UITheme.CardBg);
            card.sprite = SpriteFactory.RoundedRect;
            card.type = Image.Type.Sliced;
            card.raycastTarget = false;

            var rect = (RectTransform)card.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(16f, y - height);
            rect.offsetMax = new Vector2(-16f, y);

            var view = card.gameObject.AddComponent<OrderCardView>();
            view._game = game;
            view._slot = slot;

            view._tokenCircle = UIFactory.CreateImage(rect, "Token", Color.white);
            view._tokenCircle.sprite = SpriteFactory.Circle;
            view._tokenCircle.raycastTarget = false;
            var tokenRect = (RectTransform)view._tokenCircle.transform;
            tokenRect.anchorMin = new Vector2(0f, 1f);
            tokenRect.anchorMax = new Vector2(0f, 1f);
            tokenRect.pivot = new Vector2(0f, 1f);
            tokenRect.anchoredPosition = new Vector2(20f, -20f);
            tokenRect.sizeDelta = new Vector2(88f, 88f);

            view._tokenLabel = UIFactory.CreateText(tokenRect, "Label", "", 32, UITheme.TextMain,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch((RectTransform)view._tokenLabel.transform);

            view._requirementText = UIFactory.CreateText(rect, "Requirement", "", 24, UITheme.TextMain,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            var reqRect = (RectTransform)view._requirementText.transform;
            reqRect.anchorMin = new Vector2(0f, 0.62f);
            reqRect.anchorMax = new Vector2(1f, 0.95f);
            reqRect.offsetMin = new Vector2(124f, 0f);
            reqRect.offsetMax = new Vector2(-16f, 0f);

            view._rewardText = UIFactory.CreateText(rect, "Reward", "", 22, UITheme.TextGold,
                TextAnchor.MiddleLeft);
            var rewardRect = (RectTransform)view._rewardText.transform;
            rewardRect.anchorMin = new Vector2(0f, 0.38f);
            rewardRect.anchorMax = new Vector2(1f, 0.62f);
            rewardRect.offsetMin = new Vector2(124f, 0f);
            rewardRect.offsetMax = new Vector2(-16f, 0f);

            view._completeButton = UIFactory.CreateButton(rect, "CompleteButton", "납품", 24,
                UITheme.ButtonPrimary, out _);
            var buttonRect = (RectTransform)view._completeButton.transform;
            buttonRect.anchorMin = new Vector2(0f, 0f);
            buttonRect.anchorMax = new Vector2(1f, 0f);
            buttonRect.offsetMin = new Vector2(16f, 14f);
            buttonRect.offsetMax = new Vector2(-16f, 72f);

            view._completeButton.onClick.AddListener(view.OnCompleteClicked);

            view.Rebind();
            return view;
        }

        private CafeOrder Order =>
            _slot < _game.Orders.Orders.Count ? _game.Orders.Orders[_slot] : null;

        private void OnCompleteClicked()
        {
            CafeOrder order = Order;
            if (order != null)
                _game.RequestCompleteOrder(order.orderId);
        }

        /// <summary>Full refresh after the order in this slot changed.</summary>
        public void Rebind()
        {
            CafeOrder order = Order;
            if (order == null)
                return;

            ItemDefinition def = ItemCatalog.Get(order.requiredItemType, order.requiredItemLevel);
            _tokenCircle.color = def.Color;
            _tokenLabel.text = def.ShortLabel;
            _tokenLabel.color = UITheme.LabelOn(def.Color);
            _requirementText.text = $"{def.DisplayName}";
            _rewardText.text = $"보상 {order.rewardGold} 골드";
            RefreshButton();
        }

        /// <summary>Cheap refresh after any board change.</summary>
        public void RefreshButton()
        {
            CafeOrder order = Order;
            _completeButton.interactable = order != null && _game.Orders.CanComplete(order, _game.Board);
        }
    }
}
