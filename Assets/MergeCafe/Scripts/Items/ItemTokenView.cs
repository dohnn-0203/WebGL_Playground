using MergeCafe.Board;
using MergeCafe.Data;
using MergeCafe.UI;
using UnityEngine;
using UnityEngine.UI;

namespace MergeCafe.Items
{
    /// <summary>
    /// Visual token for one board item: colored circle + short label (C1/B2/D3)
    /// + Korean name (webGL_game.md §10). Created/refreshed by BoardGridView so the
    /// view always mirrors BoardManager state. Dragging is added in v0.0.4.
    /// </summary>
    public sealed class ItemTokenView : MonoBehaviour
    {
        public const string TokenName = "Token";

        public ItemInstance Item { get; private set; }

        private Image _circle;
        private Text _label;
        private Text _nameText;

        /// <summary>Ensures the cell shows exactly the given item.</summary>
        public static ItemTokenView CreateOrUpdate(BoardCell cell, ItemInstance item)
        {
            Transform existing = cell.transform.Find(TokenName);
            ItemTokenView view = existing != null ? existing.GetComponent<ItemTokenView>() : null;
            if (view == null)
                view = Create(cell);

            view.Bind(item);
            return view;
        }

        public static void RemoveFrom(BoardCell cell)
        {
            Transform existing = cell.transform.Find(TokenName);
            if (existing != null)
                Destroy(existing.gameObject);
        }

        private static ItemTokenView Create(BoardCell cell)
        {
            RectTransform root = UIFactory.CreateUiObject(cell.transform, TokenName);
            UIFactory.Stretch(root);
            root.offsetMin = new Vector2(8f, 8f);
            root.offsetMax = new Vector2(-8f, -8f);

            var view = root.gameObject.AddComponent<ItemTokenView>();

            view._circle = UIFactory.CreateImage(root, "Circle", Color.white);
            view._circle.sprite = SpriteFactory.Circle;
            view._circle.raycastTarget = false;
            UIFactory.Stretch((RectTransform)view._circle.transform);

            view._label = UIFactory.CreateText(view._circle.transform, "ShortLabel", "", 44,
                UITheme.TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            var labelRect = (RectTransform)view._label.transform;
            UIFactory.Stretch(labelRect);
            labelRect.offsetMin = new Vector2(0f, 14f);
            view._label.resizeTextForBestFit = true;
            view._label.resizeTextMinSize = 16;
            view._label.resizeTextMaxSize = 46;
            view._label.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(1.5f, -1.5f);

            view._nameText = UIFactory.CreateText(view._circle.transform, "NameLabel", "", 16,
                UITheme.TextMain, TextAnchor.MiddleCenter);
            var nameRect = (RectTransform)view._nameText.transform;
            nameRect.anchorMin = new Vector2(0f, 0.12f);
            nameRect.anchorMax = new Vector2(1f, 0.36f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;
            view._nameText.resizeTextForBestFit = true;
            view._nameText.resizeTextMinSize = 10;
            view._nameText.resizeTextMaxSize = 18;
            view._nameText.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(1f, -1f);

            return view;
        }

        public void Bind(ItemInstance item)
        {
            Item = item;
            ItemDefinition def = item.Definition;
            _circle.color = def.Color;
            _label.text = def.ShortLabel;
            _label.color = UITheme.LabelOn(def.Color);
            _nameText.text = def.DisplayName;
            _nameText.color = UITheme.LabelOn(def.Color);
        }
    }
}
