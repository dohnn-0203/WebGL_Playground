using MergeCafe.Data;
using UnityEngine;
using UnityEngine.UI;

namespace MergeCafe.UI
{
    /// <summary>
    /// Modal "merge recipe" guide: for each family it shows the Lv.1 → Lv.5 chain of
    /// food icons, so players see what combines into what. Opened by the "!" button.
    /// </summary>
    public sealed class MergeGuideView : MonoBehaviour
    {
        private static readonly (ItemType type, string name)[] Families =
        {
            (ItemType.Coffee, "커피"),
            (ItemType.Bread, "빵"),
            (ItemType.Dessert, "디저트")
        };

        public static MergeGuideView Show(RectTransform popupLayer)
        {
            Image dim = UIFactory.CreateImage(popupLayer, "MergeGuide", new Color(0f, 0f, 0f, 0.62f));
            dim.raycastTarget = true;
            UIFactory.Stretch((RectTransform)dim.transform);
            var view = dim.gameObject.AddComponent<MergeGuideView>();

            Image panel = UIFactory.CreateImage(dim.transform, "Panel", UITheme.HudBg);
            panel.sprite = SpriteFactory.RoundedRect;
            panel.type = Image.Type.Sliced;
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(780f, 660f);

            var title = UIFactory.CreateText(panelRect, "Title", "머지 조합표", 34, UITheme.TextMain,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            Row((RectTransform)title.transform, 1f, -16f, -70f);

            var subtitle = UIFactory.CreateText(panelRect, "Subtitle",
                "같은 아이템 2개를 합치면 다음 단계가 됩니다", 22, UITheme.TextDim);
            Row((RectTransform)subtitle.transform, 1f, -74f, -112f);

            for (int f = 0; f < Families.Length; f++)
                BuildFamilyRow(panelRect, Families[f].type, Families[f].name, -130f - f * 158f);

            Button close = UIFactory.CreateButton(panelRect, "CloseButton", "닫기", 24,
                UITheme.ButtonPrimary, out _);
            var closeRect = (RectTransform)close.transform;
            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.anchoredPosition = new Vector2(0f, 20f);
            closeRect.sizeDelta = new Vector2(220f, 52f);
            close.onClick.AddListener(view.Close);

            return view;
        }

        private static void BuildFamilyRow(RectTransform panel, ItemType type, string familyName, float top)
        {
            RectTransform row = UIFactory.CreateUiObject(panel, $"Row_{type}");
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.offsetMin = new Vector2(24f, top - 148f);
            row.offsetMax = new Vector2(-24f, top);

            var label = UIFactory.CreateText(row, "Family", familyName, 24, UITheme.TextGold,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            var labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.sizeDelta = new Vector2(96f, 0f);
            labelRect.anchoredPosition = new Vector2(48f, 0f);

            const float cell = 92f, arrow = 30f, startX = 108f;
            for (int level = 1; level <= 5; level++)
            {
                float x = startX + (level - 1) * (cell + arrow);
                BuildIcon(row, type, level, x, cell);
                if (level < 5)
                {
                    var arr = UIFactory.CreateText(row, $"Arrow_{level}", "→", 30, UITheme.TextDim,
                        TextAnchor.MiddleCenter, FontStyle.Bold);
                    var arrRect = (RectTransform)arr.transform;
                    arrRect.anchorMin = new Vector2(0f, 0.5f);
                    arrRect.anchorMax = new Vector2(0f, 0.5f);
                    arrRect.pivot = new Vector2(0.5f, 0.5f);
                    arrRect.anchoredPosition = new Vector2(x + cell * 0.5f + arrow * 0.5f, 6f);
                    arrRect.sizeDelta = new Vector2(arrow, 40f);
                }
            }
        }

        private static void BuildIcon(RectTransform row, ItemType type, int level, float x, float cell)
        {
            ItemDefinition def = ItemCatalog.Get(type, level);

            RectTransform holder = UIFactory.CreateUiObject(row, $"Icon_{level}");
            holder.anchorMin = new Vector2(0f, 0.5f);
            holder.anchorMax = new Vector2(0f, 0.5f);
            holder.pivot = new Vector2(0f, 0.5f);
            holder.anchoredPosition = new Vector2(x, 8f);
            holder.sizeDelta = new Vector2(cell, cell);

            Image disc = UIFactory.CreateImage(holder, "Disc",
                new Color(def.Color.r, def.Color.g, def.Color.b, 0.28f));
            disc.sprite = SpriteFactory.Circle;
            disc.raycastTarget = false;
            UIFactory.Stretch((RectTransform)disc.transform);

            Image icon = UIFactory.CreateImage(holder, "Icon", Color.white);
            icon.sprite = FoodIcons.Item(type, level);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            UIFactory.Stretch((RectTransform)icon.transform);
            ((RectTransform)icon.transform).offsetMin = new Vector2(8f, 8f);
            ((RectTransform)icon.transform).offsetMax = new Vector2(-8f, -8f);

            var lv = UIFactory.CreateText(holder, "Lv", $"Lv.{level}", 16, UITheme.TextDim,
                TextAnchor.UpperCenter);
            var lvRect = (RectTransform)lv.transform;
            lvRect.anchorMin = new Vector2(0f, 0f);
            lvRect.anchorMax = new Vector2(1f, 0f);
            lvRect.offsetMin = new Vector2(0f, -22f);
            lvRect.offsetMax = new Vector2(0f, 0f);
        }

        private static void Row(RectTransform rect, float _, float topOffset, float bottomOffset)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(16f, bottomOffset);
            rect.offsetMax = new Vector2(-16f, topOffset);
        }

        private void Close() => Destroy(gameObject);
    }
}
