using System.Collections.Generic;
using MergeCafe.UI;

namespace MergeCafe.Data
{
    /// <summary>
    /// Code-based item catalog (webGL_game.md §10, §19). All 3 families x 5 levels,
    /// with the exact Korean names / short labels / sell prices from the spec tables.
    /// </summary>
    public static class ItemCatalog
    {
        public const int MaxLevel = 5;

        private static readonly Dictionary<int, ItemDefinition> Definitions = new Dictionary<int, ItemDefinition>();
        private static readonly List<ItemDefinition> All = new List<ItemDefinition>();

        static ItemCatalog()
        {
            AddFamily(ItemType.Coffee, "C",
                new[] { "원두 커피", "따뜻한 커피", "아메리카노", "라떼", "스페셜 커피" },
                new[] { 5, 15, 35, 80, 160 },
                UITheme.CoffeeColors);

            AddFamily(ItemType.Bread, "B",
                new[] { "반죽", "작은 빵", "크루아상", "샌드위치", "디저트 플레이트" },
                new[] { 5, 18, 45, 100, 210 },
                UITheme.BreadColors);

            AddFamily(ItemType.Dessert, "D",
                new[] { "크림", "푸딩", "컵케이크", "케이크", "시그니처 케이크" },
                new[] { 8, 22, 55, 130, 260 },
                UITheme.DessertColors);
        }

        private static void AddFamily(ItemType type, string labelPrefix, string[] names,
            int[] prices, UnityEngine.Color[] colors)
        {
            for (int level = 1; level <= MaxLevel; level++)
            {
                var def = new ItemDefinition(type, level, names[level - 1],
                    labelPrefix + level, prices[level - 1], colors[level - 1]);
                Definitions[Key(type, level)] = def;
                All.Add(def);
            }
        }

        private static int Key(ItemType type, int level) => (int)type * 100 + level;

        public static IReadOnlyList<ItemDefinition> AllDefinitions => All;

        public static bool IsValid(ItemType type, int level)
        {
            return Definitions.ContainsKey(Key(type, level));
        }

        public static ItemDefinition Get(ItemType type, int level)
        {
            if (!Definitions.TryGetValue(Key(type, level), out ItemDefinition def))
                throw new KeyNotFoundException($"No item definition for {type} Lv.{level}");
            return def;
        }

        public static bool TryGet(ItemType type, int level, out ItemDefinition def)
        {
            return Definitions.TryGetValue(Key(type, level), out def);
        }
    }
}
