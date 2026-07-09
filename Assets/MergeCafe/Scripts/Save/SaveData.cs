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
        public int cellIndex;
        public int itemType;
    }

    [Serializable]
    public sealed class SavedEnergy
    {
        public int current;
        public int max;
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
    /// JsonUtility-serializable snapshot: gold, board locks, board items, on-board
    /// generator positions, the shared energy pool, upgrades, and the open orders.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public int version = 2;
        public long gold;
        public int[] unlockedCells;
        public int expandedCellCount;
        public int energyUpgradeCount;
        public int orderCounter;
        public SavedEnergy energy = new SavedEnergy();
        public List<SavedItem> items = new List<SavedItem>();
        public List<SavedGenerator> generators = new List<SavedGenerator>();
        public List<SavedOrder> orders = new List<SavedOrder>();
    }
}
