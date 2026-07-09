using System.Collections.Generic;

namespace MergeCafe.Bubble
{
    /// <summary>
    /// Pure-C# hex bubble grid (offset coordinates, odd rows shifted right by half a
    /// cell). Holds colour indices, computes hex neighbours, same-colour groups, and
    /// which bubbles are floating (not connected to the ceiling). Fully unit-testable.
    /// </summary>
    public sealed class BubbleGrid
    {
        public int Rows { get; }
        public int Cols { get; }

        private readonly int[] _cells; // -1 = empty, else colour index

        public BubbleGrid(int rows, int cols)
        {
            Rows = rows;
            Cols = cols;
            _cells = new int[rows * cols];
            for (int i = 0; i < _cells.Length; i++)
                _cells[i] = -1;
        }

        /// <summary>Even rows have Cols bubbles; odd rows (shifted right) have one fewer.</summary>
        public int ColsInRow(int row) => (row % 2 == 0) ? Cols : Cols - 1;

        public bool IsValid(int row, int col) =>
            row >= 0 && row < Rows && col >= 0 && col < ColsInRow(row);

        public int Index(int row, int col) => row * Cols + col;

        public int Get(int row, int col) => IsValid(row, col) ? _cells[Index(row, col)] : -1;

        public bool IsOccupied(int row, int col) => Get(row, col) >= 0;

        public bool IsEmptyCell(int row, int col) => IsValid(row, col) && _cells[Index(row, col)] < 0;

        public void Set(int row, int col, int color)
        {
            if (IsValid(row, col))
                _cells[Index(row, col)] = color;
        }

        public void Clear(int row, int col)
        {
            if (IsValid(row, col))
                _cells[Index(row, col)] = -1;
        }

        public int Count
        {
            get
            {
                int n = 0;
                foreach (int v in _cells)
                    if (v >= 0) n++;
                return n;
            }
        }

        public bool IsEmpty => Count == 0;

        /// <summary>Up to six hex neighbours of a cell (odd-r offset layout).</summary>
        public void Neighbors(int row, int col, List<(int, int)> result)
        {
            result.Clear();
            (int dr, int dc)[] deltas = (row % 2 == 0)
                ? new[] { (0, -1), (0, 1), (-1, -1), (-1, 0), (1, -1), (1, 0) }
                : new[] { (0, -1), (0, 1), (-1, 0), (-1, 1), (1, 0), (1, 1) };
            foreach (var (dr, dc) in deltas)
            {
                int nr = row + dr, nc = col + dc;
                if (IsValid(nr, nc))
                    result.Add((nr, nc));
            }
        }

        /// <summary>Connected run of same-colour bubbles starting at (row,col).</summary>
        public List<(int, int)> SameColorGroup(int row, int col)
        {
            var found = new List<(int, int)>();
            int color = Get(row, col);
            if (color < 0)
                return found;

            var seen = new HashSet<int>();
            var stack = new Stack<(int, int)>();
            var buffer = new List<(int, int)>();
            stack.Push((row, col));
            seen.Add(Index(row, col));

            while (stack.Count > 0)
            {
                var (r, c) = stack.Pop();
                found.Add((r, c));
                Neighbors(r, c, buffer);
                foreach (var (nr, nc) in buffer)
                {
                    if (Get(nr, nc) == color && seen.Add(Index(nr, nc)))
                        stack.Push((nr, nc));
                }
            }
            return found;
        }

        /// <summary>Occupied cells NOT connected to the ceiling (row 0) via any-colour adjacency.</summary>
        public List<(int, int)> FindFloating()
        {
            var connected = new bool[Rows * Cols];
            var queue = new Queue<(int, int)>();
            var buffer = new List<(int, int)>();

            for (int c = 0; c < ColsInRow(0); c++)
            {
                if (IsOccupied(0, c))
                {
                    connected[Index(0, c)] = true;
                    queue.Enqueue((0, c));
                }
            }

            while (queue.Count > 0)
            {
                var (r, c) = queue.Dequeue();
                Neighbors(r, c, buffer);
                foreach (var (nr, nc) in buffer)
                {
                    int idx = Index(nr, nc);
                    if (IsOccupied(nr, nc) && !connected[idx])
                    {
                        connected[idx] = true;
                        queue.Enqueue((nr, nc));
                    }
                }
            }

            var floating = new List<(int, int)>();
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < ColsInRow(r); c++)
                    if (IsOccupied(r, c) && !connected[Index(r, c)])
                        floating.Add((r, c));
            return floating;
        }

        /// <summary>Distinct colours currently present on the board.</summary>
        public List<int> PresentColors()
        {
            var set = new HashSet<int>();
            foreach (int v in _cells)
                if (v >= 0) set.Add(v);
            return new List<int>(set);
        }

        public int LowestOccupiedRow()
        {
            int lowest = -1;
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < ColsInRow(r); c++)
                    if (IsOccupied(r, c)) { lowest = r; break; }
            return lowest;
        }
    }
}
