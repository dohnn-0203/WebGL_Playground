using MergeCafe.Board;
using MergeCafe.Data;
using MergeCafe.UI;
using UnityEngine;
using UnityEngine.UI;

namespace MergeCafe.Items
{
    /// <summary>
    /// Visual token for one board item: a soft family-tinted disc, the procedurally
    /// drawn food icon (<see cref="FoodIcons"/>), and a small level badge. Created and
    /// refreshed by BoardGridView so the view always mirrors BoardManager state.
    /// </summary>
    public sealed class ItemTokenView : MonoBehaviour
    {
        public const string TokenName = "Token";

        public ItemInstance Item { get; private set; }

        private Image _disc;
        private Image _icon;
        private Text _levelText;

        public static ItemTokenView CreateOrUpdate(BoardCell cell, ItemInstance item)
        {
            Transform existing = cell.transform.Find(TokenName);
            ItemTokenView view = existing != null ? existing.GetComponent<ItemTokenView>() : null;
            if (view == null)
                view = Create(cell);

            if (!view.gameObject.activeSelf)
                view.gameObject.SetActive(true);

            view.Bind(item);
            return view;
        }

        public static void RemoveFrom(BoardCell cell)
        {
            Transform existing = cell.transform.Find(TokenName);
            if (existing != null)
            {
                existing.name = TokenName + "_Removed";
                existing.SetParent(null, false);
                Destroy(existing.gameObject);
            }
        }

        private static ItemTokenView Create(BoardCell cell)
        {
            RectTransform root = UIFactory.CreateUiObject(cell.transform, TokenName);
            UIFactory.Stretch(root);
            root.offsetMin = new Vector2(6f, 6f);
            root.offsetMax = new Vector2(-6f, -6f);

            var view = root.gameObject.AddComponent<ItemTokenView>();

            view._disc = UIFactory.CreateImage(root, "Disc", Color.white);
            view._disc.sprite = SpriteFactory.Circle;
            view._disc.raycastTarget = false;
            UIFactory.Stretch((RectTransform)view._disc.transform);

            view._icon = UIFactory.CreateImage(root, "Icon", Color.white);
            view._icon.raycastTarget = false;
            view._icon.preserveAspect = true;
            UIFactory.Stretch((RectTransform)view._icon.transform);
            var iconRect = (RectTransform)view._icon.transform;
            iconRect.offsetMin = new Vector2(6f, 6f);
            iconRect.offsetMax = new Vector2(-6f, -6f);

            // Level badge in the bottom-right corner.
            Image badge = UIFactory.CreateImage(root, "Badge", new Color(0.12f, 0.09f, 0.07f, 0.92f));
            badge.sprite = SpriteFactory.Circle;
            badge.raycastTarget = false;
            var badgeRect = (RectTransform)badge.transform;
            badgeRect.anchorMin = new Vector2(1f, 0f);
            badgeRect.anchorMax = new Vector2(1f, 0f);
            badgeRect.pivot = new Vector2(1f, 0f);
            badgeRect.anchoredPosition = new Vector2(2f, -2f);
            badgeRect.sizeDelta = new Vector2(34f, 34f);

            view._levelText = UIFactory.CreateText(badge.transform, "Lv", "", 22, UITheme.TextMain,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch((RectTransform)view._levelText.transform);

            return view;
        }

        public void Bind(ItemInstance item)
        {
            Item = item;
            ItemDefinition def = item.Definition;
            _disc.color = new Color(def.Color.r, def.Color.g, def.Color.b, 0.28f);
            _icon.sprite = FoodIcons.Item(item.Type, item.Level);
            _levelText.text = item.Level.ToString();
        }
    }
}
