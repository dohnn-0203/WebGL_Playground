using System.Collections.Generic;

namespace MergeCafe.Data
{
    /// <summary>Immutable definition of one generator (webGL_game.md §11).</summary>
    public sealed class GeneratorDefinition
    {
        public ItemType Output { get; }
        public string DisplayName { get; }
        public int BaseMaxEnergy { get; }
        public int RecoverySeconds { get; }

        /// <summary>Gold needed to unlock. 0 = unlocked from the start.</summary>
        public int UnlockCost { get; }

        public GeneratorDefinition(ItemType output, string displayName, int baseMaxEnergy,
            int recoverySeconds, int unlockCost)
        {
            Output = output;
            DisplayName = displayName;
            BaseMaxEnergy = baseMaxEnergy;
            RecoverySeconds = recoverySeconds;
            UnlockCost = unlockCost;
        }
    }

    /// <summary>
    /// Code-based generator catalog + upgrade table, exactly as specified in
    /// webGL_game.md §11 (generators) and its upgrade recommendation table.
    /// </summary>
    public static class GeneratorCatalog
    {
        // Energy balance (max energy, +1 every 5s). Higher starting pools and a fast,
        // uniform 5-second recovery keep the early game flowing.
        public static readonly GeneratorDefinition CoffeeMachine =
            new GeneratorDefinition(ItemType.Coffee, "커피머신", 20, 5, 0);

        public static readonly GeneratorDefinition Oven =
            new GeneratorDefinition(ItemType.Bread, "오븐", 16, 5, 150);

        public static readonly GeneratorDefinition Fridge =
            new GeneratorDefinition(ItemType.Dessert, "냉장고", 12, 5, 300);

        public static readonly IReadOnlyList<GeneratorDefinition> All =
            new[] { CoffeeMachine, Oven, Fridge };

        public const int MaxUpgradeLevel = 4;

        // Indexed by upgrade level (1-based, index 0 unused).
        private static readonly int[] EnergyBonuses = { 0, 0, 3, 5, 8 };
        private static readonly float[] Level2Chances = { 0f, 0f, 0f, 0.10f, 0.20f };

        // Cost to REACH the given level (index = target level).
        private static readonly int[] UpgradeCosts = { 0, 0, 200, 500, 1000 };

        public static GeneratorDefinition For(ItemType type)
        {
            switch (type)
            {
                case ItemType.Coffee: return CoffeeMachine;
                case ItemType.Bread: return Oven;
                default: return Fridge;
            }
        }

        public static int EnergyBonus(int upgradeLevel)
        {
            return EnergyBonuses[Clamp(upgradeLevel)];
        }

        /// <summary>Chance that a spawn comes out as Lv.2 instead of Lv.1.</summary>
        public static float Level2Chance(int upgradeLevel)
        {
            return Level2Chances[Clamp(upgradeLevel)];
        }

        public static int UpgradeCost(int targetLevel)
        {
            return UpgradeCosts[Clamp(targetLevel)];
        }

        private static int Clamp(int level)
        {
            if (level < 1) return 1;
            if (level > MaxUpgradeLevel) return MaxUpgradeLevel;
            return level;
        }
    }
}
