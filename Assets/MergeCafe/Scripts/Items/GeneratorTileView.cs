using MergeCafe.Board;
using MergeCafe.Data;
using MergeCafe.UI;
using UnityEngine;
using UnityEngine.UI;

namespace MergeCafe.Items
{
    /// <summary>
    /// Visual for a generator sitting on a board cell. Rendered as a rounded machine
    /// tile (distinct from the round item tokens) tinted by its output family, with a
    /// small "tap to make" lightning badge. Created/updated by BoardGridView.
    /// </summary>
    public sealed class GeneratorTileView : MonoBehaviour
    {
        public const string TileName = "Generator";

        public ItemType Output { get; private set; }

        private Image _tile;
        private Text _label;

        public static GeneratorTileView CreateOrUpdate(BoardCell cell, ItemType output)
        {
            Transform existing = cell.transform.Find(TileName);
            GeneratorTileView view = existing != null ? existing.GetComponent<GeneratorTileView>() : null;
            if (view == null)
                view = Create(cell);

            if (!view.gameObject.activeSelf)
                view.gameObject.SetActive(true);

            view.Bind(output);
            return view;
        }

        public static void RemoveFrom(BoardCell cell)
        {
            Transform existing = cell.transform.Find(TileName);
            if (existing != null)
            {
                existing.name = TileName + "_Removed";
                existing.SetParent(null, false);
                Destroy(existing.gameObject);
            }
        }

        private static GeneratorTileView Create(BoardCell cell)
        {
            RectTransform root = UIFactory.CreateUiObject(cell.transform, TileName);
            UIFactory.Stretch(root);
            root.offsetMin = new Vector2(3f, 3f);
            root.offsetMax = new Vector2(-3f, -3f);

            var view = root.gameObject.AddComponent<GeneratorTileView>();

            view._tile = UIFactory.CreateImage(root, "Machine", Color.white);
            view._tile.sprite = SpriteFactory.RoundedRect;
            view._tile.type = Image.Type.Sliced;
            view._tile.raycastTarget = false;
            UIFactory.Stretch((RectTransform)view._tile.transform);

            // Dark inner panel so the machine reads differently from item tokens.
            Image inner = UIFactory.CreateImage(view._tile.transform, "Inner", new Color(0f, 0f, 0f, 0.28f));
            inner.sprite = SpriteFactory.RoundedRect;
            inner.type = Image.Type.Sliced;
            inner.raycastTarget = false;
            var innerRect = (RectTransform)inner.transform;
            UIFactory.Stretch(innerRect);
            innerRect.offsetMin = new Vector2(6f, 6f);
            innerRect.offsetMax = new Vector2(-6f, -6f);

            view._label = UIFactory.CreateText(view._tile.transform, "Label", "", 22,
                UITheme.TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch((RectTransform)view._label.transform);
            view._label.resizeTextForBestFit = true;
            view._label.resizeTextMinSize = 10;
            view._label.resizeTextMaxSize = 26;
            view._label.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(1.5f, -1.5f);

            // Gold accent corner marking this as a tappable producer.
            Image badge = UIFactory.CreateImage(view._tile.transform, "Badge", UITheme.TextGold);
            badge.sprite = SpriteFactory.Circle;
            badge.raycastTarget = false;
            var badgeRect = (RectTransform)badge.transform;
            badgeRect.anchorMin = new Vector2(1f, 1f);
            badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.pivot = new Vector2(1f, 1f);
            badgeRect.anchoredPosition = new Vector2(-4f, -4f);
            badgeRect.sizeDelta = new Vector2(14f, 14f);

            return view;
        }

        public void Bind(ItemType output)
        {
            Output = output;
            Color color = ItemCatalog.Get(output, 1).Color;
            _tile.color = new Color(color.r * 0.7f + 0.15f, color.g * 0.7f + 0.15f, color.b * 0.7f + 0.15f, 1f);
            _label.text = GeneratorCatalog.For(output).DisplayName;
            _label.color = UITheme.LabelOn(_tile.color);
        }
    }
}
