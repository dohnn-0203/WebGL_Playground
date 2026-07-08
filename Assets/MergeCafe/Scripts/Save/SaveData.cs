using System;
using System.Collections.Generic;

namespace MergeCafe.Save
{
    [Serializable]
    public sealed class SavedItem
    {
        public int cellIndex;
        public int itemType;
        public int level;
    }

    [Serializable]
    public sealed class SavedGenerator
    {
        public int itemType;
        public bool unlocked;
        public int upgradeLevel;
        public int energy;
        public double lastRecoveryUnix;
    }

    [Serializable]
    public sealed class SavedOrder
    {
        public string orderId;
        public int itemType;
        public int level;
        public int rewardGold;
    }

    /// <summary>
    /// JsonUtility-serializable snapshot of everything webGL_game.md §14 requires:
    /// gold, board locks, board items, generator states and the three open orders.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public int version = 1;
        public long gold;
        public int[] unlockedCells;
        public int expandedCellCount;
        public int orderCounter;
        public List<SavedItem> items = new List<SavedItem>();
        public List<SavedGenerator> generators = new List<SavedGenerator>();
        public List<SavedOrder> orders = new List<SavedOrder>();
    }
}
