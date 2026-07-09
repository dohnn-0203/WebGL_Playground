using MergeCafe.Board;
using MergeCafe.Data;
using MergeCafe.UI;
using UnityEngine;
using UnityEngine.UI;

namespace MergeCafe.Items
{
    /// <summary>
    /// Visual for a generator sitting on a board cell: a rounded machine plate, the
    /// procedurally drawn appliance icon (<see cref="FoodIcons"/>), a name caption and
    /// a gold "tap to make" badge. Created/updated by BoardGridView.
    /// </summary>
    public sealed class GeneratorTileView : MonoBehaviour
    {
        public const string TileName = "Generator";

        public ItemType Output { get; private set; }

        private Image _plate;
        private Image _icon;
        private Text _name;

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
            root.offsetMin = new Vector2(2f, 2f);
            root.offsetMax = new Vector2(-2f, -2f);

            var view = root.gameObject.AddComponent<GeneratorTileView>();

            view._plate = UIFactory.CreateImage(root, "Plate", new Color(0.16f, 0.13f, 0.10f, 0.55f));
            view._plate.sprite = SpriteFactory.RoundedRect;
            view._plate.type = Image.Type.Sliced;
            view._plate.raycastTarget = false;
            UIFactory.Stretch((RectTransform)view._plate.transform);

            view._icon = UIFactory.CreateImage(view._plate.transform, "Icon", Color.white);
            view._icon.raycastTarget = false;
            view._icon.preserveAspect = true;
            var iconRect = (RectTransform)view._icon.transform;
            iconRect.anchorMin = new Vector2(0f, 0.22f);
            iconRect.anchorMax = new Vector2(1f, 1f);
            iconRect.offsetMin = new Vector2(6f, 0f);
            iconRect.offsetMax = new Vector2(-6f, -4f);

            view._name = UIFactory.CreateText(view._plate.transform, "Name", "", 18, UITheme.TextMain,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            var nameRect = (RectTransform)view._name.transform;
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(1f, 0.24f);
            nameRect.offsetMin = new Vector2(2f, 2f);
            nameRect.offsetMax = new Vector2(-2f, 0f);
            view._name.resizeTextForBestFit = true;
            view._name.resizeTextMinSize = 8;
            view._name.resizeTextMaxSize = 20;
            view._name.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(1f, -1f);

            // Gold "tap to produce" badge.
            Image badge = UIFactory.CreateImage(view._plate.transform, "Badge", UITheme.TextGold);
            badge.sprite = SpriteFactory.Circle;
            badge.raycastTarget = false;
            var badgeRect = (RectTransform)badge.transform;
            badgeRect.anchorMin = new Vector2(1f, 1f);
            badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.pivot = new Vector2(1f, 1f);
            badgeRect.anchoredPosition = new Vector2(-3f, -3f);
            badgeRect.sizeDelta = new Vector2(16f, 16f);

            return view;
        }

        public void Bind(ItemType output)
        {
            Output = output;
            _icon.sprite = FoodIcons.Generator(output);
            _name.text = GeneratorCatalog.For(output).DisplayName;
        }
    }
}
