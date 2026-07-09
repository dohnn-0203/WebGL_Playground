using MergeCafe.Core;
using MergeCafe.Data;
using MergeCafe.UI;
using UnityEngine;
using UnityEngine.UI;

namespace MergeCafe.Orders
{
    /// <summary>
    /// One order card in the left column: a customer portrait, the required item,
    /// the reward, and a "납품" button that lights up when the board has the item.
    /// </summary>
    public sealed class OrderCardView : MonoBehaviour
    {
        private const float Top = 128f;   // below the energy gauge
        private const float Height = 132f;
        private const float Spacing = 10f;

        private static readonly Color[] PortraitColors =
        {
            Hex("E07A5F"), Hex("81B29A"), Hex("F2CC8F"), Hex("A5668B"), Hex("6D9DC5")
        };

        private GameManager _game;
        private int _slot;

        private Image _portrait;
        private Image _tokenCircle;
        private Image _tokenIcon;
        private Text _tokenLabel;
        private Text _nameText;
        private Text _rewardText;
        private Button _completeButton;

        public static OrderCardView Build(RectTransform leftPanel, GameManager game, int slot)
        {
            float y = -(Top + slot * (Height + Spacing));

            Image card = UIFactory.CreateImage(leftPanel, $"OrderCard_{slot}", UITheme.CardBg);
            card.sprite = SpriteFactory.RoundedRect;
            card.type = Image.Type.Sliced;
            card.raycastTarget = false;

            var rect = (RectTransform)card.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(14f, y - Height);
            rect.offsetMax = new Vector2(-14f, y);

            var view = card.gameObject.AddComponent<OrderCardView>();
            view._game = game;
            view._slot = slot;

            // Customer portrait (procedural placeholder: two-tone circle).
            view._portrait = UIFactory.CreateImage(rect, "Portrait", PortraitColors[slot % PortraitColors.Length]);
            view._portrait.sprite = SpriteFactory.Circle;
            view._portrait.raycastTarget = false;
            var portraitRect = (RectTransform)view._portrait.transform;
            portraitRect.anchorMin = new Vector2(0f, 0.5f);
            portraitRect.anchorMax = new Vector2(0f, 0.5f);
            portraitRect.pivot = new Vector2(0f, 0.5f);
            portraitRect.anchoredPosition = new Vector2(12f, 18f);
            portraitRect.sizeDelta = new Vector2(56f, 56f);

            Image head = UIFactory.CreateImage(portraitRect, "Head", new Color(1f, 1f, 1f, 0.85f));
            head.sprite = SpriteFactory.Circle;
            head.raycastTarget = false;
            var headRect = (RectTransform)head.transform;
            headRect.anchorMin = new Vector2(0.5f, 0.5f);
            headRect.anchorMax = new Vector2(0.5f, 0.5f);
            headRect.sizeDelta = new Vector2(26f, 26f);
            headRect.anchoredPosition = new Vector2(0f, 2f);

            // Required item token: tinted disc + food icon.
            view._tokenCircle = UIFactory.CreateImage(rect, "Token", Color.white);
            view._tokenCircle.sprite = SpriteFactory.Circle;
            view._tokenCircle.raycastTarget = false;
            var tokenRect = (RectTransform)view._tokenCircle.transform;
            tokenRect.anchorMin = new Vector2(0f, 0.5f);
            tokenRect.anchorMax = new Vector2(0f, 0.5f);
            tokenRect.pivot = new Vector2(0f, 0.5f);
            tokenRect.anchoredPosition = new Vector2(80f, 18f);
            tokenRect.sizeDelta = new Vector2(52f, 52f);

            view._tokenIcon = UIFactory.CreateImage(tokenRect, "Icon", Color.white);
            view._tokenIcon.raycastTarget = false;
            view._tokenIcon.preserveAspect = true;
            UIFactory.Stretch((RectTransform)view._tokenIcon.transform);
            ((RectTransform)view._tokenIcon.transform).offsetMin = new Vector2(4f, 4f);
            ((RectTransform)view._tokenIcon.transform).offsetMax = new Vector2(-4f, -4f);

            view._tokenLabel = UIFactory.CreateText(tokenRect, "Lv", "", 18, UITheme.TextMain,
                TextAnchor.LowerRight, FontStyle.Bold);
            UIFactory.Stretch((RectTransform)view._tokenLabel.transform);
            view._tokenLabel.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(1f, -1f);

            view._nameText = UIFactory.CreateText(rect, "Name", "", 22, UITheme.TextMain,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            var nameRect = (RectTransform)view._nameText.transform;
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(144f, 0f);
            nameRect.offsetMax = new Vector2(-12f, -10f);

            view._rewardText = UIFactory.CreateText(rect, "Reward", "", 22, UITheme.TextGold,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            var rewardRect = (RectTransform)view._rewardText.transform;
            rewardRect.anchorMin = new Vector2(0f, 0.32f);
            rewardRect.anchorMax = new Vector2(1f, 0.56f);
            rewardRect.offsetMin = new Vector2(144f, 0f);
            rewardRect.offsetMax = new Vector2(-12f, 0f);

            view._completeButton = UIFactory.CreateButton(rect, "CompleteButton", "납품", 22,
                UITheme.ButtonPrimary, out _);
            var buttonRect = (RectTransform)view._completeButton.transform;
            buttonRect.anchorMin = new Vector2(0f, 0f);
            buttonRect.anchorMax = new Vector2(1f, 0f);
            buttonRect.offsetMin = new Vector2(80f, 10f);
            buttonRect.offsetMax = new Vector2(-12f, 44f);
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

        public void Rebind()
        {
            CafeOrder order = Order;
            if (order == null)
                return;

            ItemDefinition def = ItemCatalog.Get(order.requiredItemType, order.requiredItemLevel);
            _tokenCircle.color = new Color(def.Color.r, def.Color.g, def.Color.b, 0.28f);
            _tokenIcon.sprite = FoodIcons.Item(order.requiredItemType, order.requiredItemLevel);
            _tokenLabel.text = order.requiredItemLevel.ToString();
            _nameText.text = def.DisplayName;
            _rewardText.text = $"+{order.rewardGold} 골드";
            RefreshButton();
        }

        public void RefreshButton()
        {
            CafeOrder order = Order;
            _completeButton.interactable = order != null && _game.Orders.CanComplete(order, _game.Board);
        }

        private static Color Hex(string rgb)
        {
            return ColorUtility.TryParseHtmlString("#" + rgb, out Color c) ? c : Color.magenta;
        }
    }
}
