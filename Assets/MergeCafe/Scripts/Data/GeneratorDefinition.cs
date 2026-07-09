using System.Collections.Generic;
using MergeCafe.Board;

namespace MergeCafe.Data
{
    /// <summary>Definition of one on-board generator tile.</summary>
    public sealed class GeneratorDefinition
    {
        public ItemType Output { get; }
        public string DisplayName { get; }

        /// <summary>Cell the generator starts on (must be inside the initial unlocked region).</summary>
        public int InitialCell { get; }

        public GeneratorDefinition(ItemType output, string displayName, int initialCell)
        {
            Output = output;
            DisplayName = displayName;
            InitialCell = initialCell;
        }
    }

    /// <summary>
    /// The generators that live on the board. All three are present from the start;
    /// every generator draws from one shared energy pool (webGL total gauge).
    /// </summary>
    public static class GeneratorCatalog
    {
        public static readonly GeneratorDefinition CoffeeMachine =
            new GeneratorDefinition(ItemType.Coffee, "커피머신", BoardManager.IndexOf(1, 2));

        public static readonly GeneratorDefinition Oven =
            new GeneratorDefinition(ItemType.Bread, "오븐", BoardManager.IndexOf(1, 4));

        public static readonly GeneratorDefinition Fridge =
            new GeneratorDefinition(ItemType.Dessert, "냉장고", BoardManager.IndexOf(1, 6));

        public static readonly IReadOnlyList<GeneratorDefinition> All =
            new[] { CoffeeMachine, Oven, Fridge };

        public static GeneratorDefinition For(ItemType type)
        {
            switch (type)
            {
                case ItemType.Coffee: return CoffeeMachine;
                case ItemType.Bread: return Oven;
                default: return Fridge;
            }
        }
    }
}
