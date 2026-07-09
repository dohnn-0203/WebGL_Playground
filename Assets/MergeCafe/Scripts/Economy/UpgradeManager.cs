using System;
using System.Collections.Generic;
using MergeCafe.Board;
using MergeCafe.Generators;

namespace MergeCafe.Economy
{
    /// <summary>
    /// Growth systems: unlock locked board cells (increasing cost) and raise the
    /// shared energy pool's maximum. Both spend gold.
    /// </summary>
    public sealed class UpgradeManager
    {
        public const int FirstCellCost = 100;
        public const int CellCostIncrement = 50;

        public const int EnergyStep = 5;      // +max energy per upgrade
        public const int EnergyBaseCost = 200;
        public const int EnergyCostIncrement = 150;

        /// <summary>Locked cells ordered by proximity to the board centre.</summary>
        public static readonly int[] CellUnlockOrder = BuildUnlockOrder();

        private int _expandedCellCount;
        private int _energyUpgradeCount;

        public event Action Changed;

        public int ExpandedCellCount
        {
            get => _expandedCellCount;
            set => _expandedCellCount = Math.Max(0, value);
        }

        public int EnergyUpgradeCount
        {
            get => _energyUpgradeCount;
            set => _energyUpgradeCount = Math.Max(0, value);
        }

        public int NextCellCost => FirstCellCost + CellCostIncrement * _expandedCellCount;
        public int NextEnergyCost => EnergyBaseCost + EnergyCostIncrement * _energyUpgradeCount;

        public static bool IsBoardFullyUnlocked(BoardManager board) =>
            board.UnlockedCount >= BoardManager.CellCount;

        public int NextLockedCell(BoardManager board)
        {
            foreach (int index in CellUnlockOrder)
                if (!board.IsUnlocked(index))
                    return index;
            return -1;
        }

        public bool TryExpandBoard(BoardManager board, EconomyManager economy)
        {
            int cellIndex = NextLockedCell(board);
            if (cellIndex < 0)
                return false;
            if (!economy.TrySpend(NextCellCost))
                return false;

            board.TryUnlockCell(cellIndex);
            _expandedCellCount++;
            Changed?.Invoke();
            return true;
        }

        public bool TryUpgradeEnergy(EnergyPool pool, EconomyManager economy)
        {
            if (!economy.TrySpend(NextEnergyCost))
                return false;

            pool.Max += EnergyStep;
            pool.Current += EnergyStep; // reward the purchase with immediate energy
            _energyUpgradeCount++;
            pool.RaiseChanged();
            Changed?.Invoke();
            return true;
        }

        /// <summary>Re-applies stored energy upgrades to a freshly-created pool (save load).</summary>
        public void ApplyEnergyUpgradesTo(EnergyPool pool)
        {
            pool.Max = EnergyPool.BaseMax + EnergyStep * _energyUpgradeCount;
        }

        private static int[] BuildUnlockOrder()
        {
            var locked = new List<int>();
            for (int i = 0; i < BoardManager.CellCount; i++)
                if (!BoardManager.IsInInitialRegion(i))
                    locked.Add(i);

            float cr = (BoardManager.Rows - 1) * 0.5f;
            float cc = (BoardManager.Cols - 1) * 0.5f;
            locked.Sort((a, b) =>
            {
                float da = Dist(a, cr, cc);
                float db = Dist(b, cr, cc);
                int byDistance = da.CompareTo(db);
                return byDistance != 0 ? byDistance : a.CompareTo(b);
            });
            return locked.ToArray();
        }

        private static float Dist(int index, float cr, float cc)
        {
            float dr = BoardManager.RowOf(index) - cr;
            float dc = BoardManager.ColOf(index) - cc;
            return dr * dr + dc * dc;
        }
    }
}
