using System;
using System.Collections.Generic;
using MergeCafe.Data;

namespace MergeCafe.Board
{
    /// <summary>
    /// Pure-C# board state for the large play grid. A cell can hold at most one of:
    /// an item, or a generator tile. Generators live on the board and are draggable
    /// but never merge; items merge by the usual rules. Fully unit-testable.
    /// </summary>
    public sealed class BoardManager
    {
        public const int Cols = 9;
        public const int Rows = 7;
        public const int CellCount = Cols * Rows;

        /// <summary>Raised with the cell index whenever that cell's contents change.</summary>
        public event Action<int> CellChanged;

        /// <summary>Raised after any board mutation (for HUD counters etc).</summary>
        public event Action BoardChanged;

        private readonly bool[] _unlocked = new bool[CellCount];
        private readonly ItemInstance[] _items = new ItemInstance[CellCount];
        private readonly int[] _generator = new int[CellCount]; // ItemType as int, or -1

        public BoardManager()
        {
            for (int i = 0; i < CellCount; i++)
            {
                _unlocked[i] = IsInInitialRegion(i);
                _generator[i] = -1;
            }
        }

        public static int RowOf(int index) => index / Cols;
        public static int ColOf(int index) => index % Cols;
        public static int IndexOf(int row, int col) => row * Cols + col;

        /// <summary>Initial unlocked area: a centered block, leaving a locked border to expand into.</summary>
        public static bool IsInInitialRegion(int index)
        {
            int row = RowOf(index);
            int col = ColOf(index);
            return row >= 1 && row <= Rows - 2 && col >= 1 && col <= Cols - 2;
        }

        public bool IsValidIndex(int index) => index >= 0 && index < CellCount;

        public bool IsUnlocked(int index) => IsValidIndex(index) && _unlocked[index];

        public ItemInstance GetItem(int index) => IsValidIndex(index) ? _items[index] : null;

        public bool HasGenerator(int index) => IsValidIndex(index) && _generator[index] >= 0;

        public ItemType GetGenerator(int index) => (ItemType)_generator[index];

        /// <summary>Unlocked and free of both items and generators — an item may be placed here.</summary>
        public bool IsFreeCell(int index) =>
            IsUnlocked(index) && _items[index] == null && _generator[index] < 0;

        public int UnlockedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < CellCount; i++)
                    if (_unlocked[i]) count++;
                return count;
            }
        }

        public int FreeCellCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < CellCount; i++)
                    if (IsFreeCell(i)) count++;
                return count;
            }
        }

        /// <summary>First free cell in row-major order.</summary>
        public bool TryFindEmptyCell(out int index)
        {
            for (int i = 0; i < CellCount; i++)
            {
                if (IsFreeCell(i))
                {
                    index = i;
                    return true;
                }
            }
            index = -1;
            return false;
        }

        public bool TryPlaceItem(int index, ItemInstance item)
        {
            if (item == null || !IsFreeCell(index))
                return false;

            _items[index] = item;
            RaiseCellChanged(index);
            return true;
        }

        public ItemInstance RemoveItem(int index)
        {
            if (!IsValidIndex(index) || _items[index] == null)
                return null;

            ItemInstance removed = _items[index];
            _items[index] = null;
            RaiseCellChanged(index);
            return removed;
        }

        // ---- Generators on the board ----

        public bool TryPlaceGenerator(int index, ItemType type)
        {
            if (!IsFreeCell(index))
                return false;

            _generator[index] = (int)type;
            RaiseCellChanged(index);
            return true;
        }

        /// <summary>Moves a generator to a free cell. Returns false if it can't.</summary>
        public bool TryMoveGenerator(int fromIndex, int toIndex)
        {
            if (!HasGenerator(fromIndex) || fromIndex == toIndex || !IsFreeCell(toIndex))
                return false;

            _generator[toIndex] = _generator[fromIndex];
            _generator[fromIndex] = -1;
            RaiseCellChanged(fromIndex);
            RaiseCellChanged(toIndex);
            return true;
        }

        public bool TryUnlockCell(int index)
        {
            if (!IsValidIndex(index) || _unlocked[index])
                return false;

            _unlocked[index] = true;
            RaiseCellChanged(index);
            return true;
        }

        /// <summary>First cell (row-major) containing an item of the given type+level, or -1.</summary>
        public int FindItemCell(ItemType type, int level)
        {
            for (int i = 0; i < CellCount; i++)
            {
                ItemInstance item = _items[i];
                if (item != null && item.Type == type && item.Level == level)
                    return i;
            }
            return -1;
        }

        public List<int> GetUnlockedCells()
        {
            var result = new List<int>();
            for (int i = 0; i < CellCount; i++)
                if (_unlocked[i]) result.Add(i);
            return result;
        }

        /// <summary>Bulk restore for save loading: clears everything, applies locks, notifies per cell.</summary>
        public void ResetForLoad(IEnumerable<int> unlockedIndices)
        {
            for (int i = 0; i < CellCount; i++)
            {
                _items[i] = null;
                _generator[i] = -1;
                _unlocked[i] = false;
            }

            foreach (int index in unlockedIndices)
            {
                if (index >= 0 && index < CellCount)
                    _unlocked[index] = true;
            }

            for (int i = 0; i < CellCount; i++)
                CellChanged?.Invoke(i);
            BoardChanged?.Invoke();
        }

        private void RaiseCellChanged(int index)
        {
            CellChanged?.Invoke(index);
            BoardChanged?.Invoke();
        }
    }
}
