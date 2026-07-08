using System;
using System.Collections.Generic;
using MergeCafe.Board;
using MergeCafe.Data;
using MergeCafe.Generators;

namespace MergeCafe.Economy
{
    /// <summary>
    /// Growth systems of webGL_game.md §13: board cell unlocking with increasing
    /// cost (100, +50 each) and generator upgrades using the §11 upgrade table.
    /// </summary>
    public sealed class UpgradeManager
    {
        public const int FirstCellCost = 100;
        public const int CellCostIncrement = 50;

        /// <summary>Locked cells in the order they get unlocked: closest to the board center first.</summary>
        public static readonly int[] CellUnlockOrder = BuildUnlockOrder();

        private int _expandedCellCount;

        /// <summary>Raised after a successful expansion or upgrade purchase.</summary>
        public event Action Changed;

        /// <summary>Number of cells unlocked beyond the initial 16 (persisted in saves).</summary>
        public int ExpandedCellCount
        {
            get => _expandedCellCount;
            set => _expandedCellCount = Math.Max(0, value);
        }

        public int NextCellCost => FirstCellCost + CellCostIncrement * _expandedCellCount;

        public static bool IsBoardFullyUnlocked(BoardManager board)
        {
            return board.UnlockedCount >= BoardManager.CellCount;
        }

        public int NextLockedCell(BoardManager board)
        {
            foreach (int index in CellUnlockOrder)
            {
                if (!board.IsUnlocked(index))
                    return index;
            }
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

        public bool TryUpgradeGenerator(GeneratorState state, EconomyManager economy)
        {
            if (!state.Unlocked || state.UpgradeLevel >= GeneratorCatalog.MaxUpgradeLevel)
                return false;

            int cost = GeneratorCatalog.UpgradeCost(state.UpgradeLevel + 1);
            if (!economy.TrySpend(cost))
                return false;

            state.UpgradeLevel++;
            Changed?.Invoke();
            return true;
        }

        private static int[] BuildUnlockOrder()
        {
            var locked = new List<int>();
            for (int i = 0; i < BoardManager.CellCount; i++)
            {
                if (!BoardManager.IsInInitialRegion(i))
                    locked.Add(i);
            }

            // Center of the 6x6 grid sits between cells at (2.5, 2.5).
            locked.Sort((a, b) =>
            {
                float da = DistanceFromCenter(a);
                float db = DistanceFromCenter(b);
                int byDistance = da.CompareTo(db);
                return byDistance != 0 ? byDistance : a.CompareTo(b);
            });
            return locked.ToArray();
        }

        private static float DistanceFromCenter(int index)
        {
            float dr = BoardManager.RowOf(index) - 2.5f;
            float dc = BoardManager.ColOf(index) - 2.5f;
            return dr * dr + dc * dc;
        }
    }
}
