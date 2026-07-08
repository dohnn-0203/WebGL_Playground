using UnityEngine;

namespace MergeCafe.Data
{
    /// <summary>
    /// Immutable definition of one item (type + level). Instances live in the
    /// code-based <see cref="ItemCatalog"/> (webGL_game.md §19).
    /// </summary>
    public sealed class ItemDefinition
    {
        public ItemType Type { get; }
        public int Level { get; }
        public string DisplayName { get; }
        public string ShortLabel { get; }
        public int SellPrice { get; }
        public Color Color { get; }

        public ItemDefinition(ItemType type, int level, string displayName, string shortLabel,
            int sellPrice, Color color)
        {
            Type = type;
            Level = level;
            DisplayName = displayName;
            ShortLabel = shortLabel;
            SellPrice = sellPrice;
            Color = color;
        }
    }
}
