using System;
using System.Collections.Generic;
using MergeCafe.Data;

namespace MergeCafe.Board
{
    /// <summary>
    /// Pure-C# board state: 6x6 cells, lock state and item occupancy (webGL_game.md §9).
    /// No UnityEngine scene dependencies so it is fully unit-testable.
    /// </summary>
    public sealed class BoardManager
    {
        public const int Size = 6;
        public const int CellCount = Size * Size;

        /// <summary>Raised with the cell index whenever that cell's item or lock state changes.</summary>
        public event Action<int> CellChanged;

        /// <summary>Raised after any board mutation (for HUD counters etc).</summary>
        public event Action BoardChanged;

        private readonly bool[] _unlocked = new bool[CellCount];
        private readonly ItemInstance[] _items = new ItemInstance[CellCount];

        public BoardManager()
        {
            for (int i = 0; i < CellCount; i++)
                _unlocked[i] = IsInInitialRegion(i);
        }

        public static int RowOf(int index) => index / Size;
        public static int ColOf(int index) => index % Size;
        public static int IndexOf(int row, int col) => row * Size + col;

        /// <summary>Initial unlocked area: the centered 4x4 region (16 cells).</summary>
        public static bool IsInInitialRegion(int index)
        {
            int row = RowOf(index);
            int col = ColOf(index);
            return row >= 1 && row <= 4 && col >= 1 && col <= 4;
        }

        public bool IsValidIndex(int index) => index >= 0 && index < CellCount;

        public bool IsUnlocked(int index) => IsValidIndex(index) && _unlocked[index];

        public ItemInstance GetItem(int index) => IsValidIndex(index) ? _items[index] : null;

        /// <summary>Unlocked and holding no item.</summary>
        public bool IsEmptyCell(int index) => IsUnlocked(index) && _items[index] == null;

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

        public int EmptyUnlockedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < CellCount; i++)
                    if (_unlocked[i] && _items[i] == null) count++;
                return count;
            }
        }

        /// <summary>Finds the first empty unlocked cell in row-major order.</summary>
        public bool TryFindEmptyCell(out int index)
        {
            for (int i = 0; i < CellCount; i++)
            {
                if (_unlocked[i] && _items[i] == null)
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
            if (item == null || !IsEmptyCell(index))
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

        /// <summary>All currently unlocked cell indices (for saving).</summary>
        public List<int> GetUnlockedCells()
        {
            var result = new List<int>();
            for (int i = 0; i < CellCount; i++)
            {
                if (_unlocked[i])
                    result.Add(i);
            }
            return result;
        }

        /// <summary>
        /// Bulk restore for save loading: clears every item, applies the given lock
        /// set, then notifies once per cell so views resync.
        /// </summary>
        public void ResetForLoad(IEnumerable<int> unlockedIndices)
        {
            for (int i = 0; i < CellCount; i++)
            {
                _items[i] = null;
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
