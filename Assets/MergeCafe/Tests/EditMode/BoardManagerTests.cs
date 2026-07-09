using MergeCafe.Board;
using MergeCafe.Data;
using NUnit.Framework;

namespace MergeCafe.Tests
{
    public sealed class BoardManagerTests
    {
        private BoardManager _board;

        [SetUp]
        public void SetUp()
        {
            _board = new BoardManager();
        }

        [Test]
        public void Dimensions_Are9x7()
        {
            Assert.AreEqual(9, BoardManager.Cols);
            Assert.AreEqual(7, BoardManager.Rows);
            Assert.AreEqual(63, BoardManager.CellCount);
        }

        [Test]
        public void InitialState_UnlocksCenteredInnerBlock()
        {
            // rows 1..5, cols 1..7 → 5 x 7 = 35 unlocked, border locked.
            Assert.AreEqual(35, _board.UnlockedCount);
            Assert.AreEqual(35, _board.FreeCellCount);

            for (int row = 0; row < BoardManager.Rows; row++)
            {
                for (int col = 0; col < BoardManager.Cols; col++)
                {
                    bool expected = row >= 1 && row <= 5 && col >= 1 && col <= 7;
                    Assert.AreEqual(expected, _board.IsUnlocked(BoardManager.IndexOf(row, col)),
                        $"cell ({row},{col})");
                }
            }
        }

        [Test]
        public void TryPlaceItem_OnFreeCell_Succeeds()
        {
            int index = BoardManager.IndexOf(1, 1);
            var item = new ItemInstance(ItemType.Coffee, 1);

            Assert.IsTrue(_board.TryPlaceItem(index, item));
            Assert.AreSame(item, _board.GetItem(index));
            Assert.AreEqual(34, _board.FreeCellCount);
        }

        [Test]
        public void TryPlaceItem_OnLockedOccupiedOrGeneratorCell_Fails()
        {
            int locked = BoardManager.IndexOf(0, 0);
            Assert.IsFalse(_board.TryPlaceItem(locked, new ItemInstance(ItemType.Coffee, 1)));

            int open = BoardManager.IndexOf(2, 2);
            Assert.IsTrue(_board.TryPlaceItem(open, new ItemInstance(ItemType.Coffee, 1)));
            Assert.IsFalse(_board.TryPlaceItem(open, new ItemInstance(ItemType.Bread, 1)));

            int genCell = BoardManager.IndexOf(3, 3);
            Assert.IsTrue(_board.TryPlaceGenerator(genCell, ItemType.Coffee));
            Assert.IsFalse(_board.TryPlaceItem(genCell, new ItemInstance(ItemType.Coffee, 1)));
        }

        [Test]
        public void Generators_PlaceMoveAndBlockCells()
        {
            int a = BoardManager.IndexOf(1, 1);
            int b = BoardManager.IndexOf(1, 2);

            Assert.IsTrue(_board.TryPlaceGenerator(a, ItemType.Bread));
            Assert.IsTrue(_board.HasGenerator(a));
            Assert.AreEqual(ItemType.Bread, _board.GetGenerator(a));
            Assert.IsFalse(_board.IsFreeCell(a));

            // Move to an empty cell.
            Assert.IsTrue(_board.TryMoveGenerator(a, b));
            Assert.IsFalse(_board.HasGenerator(a));
            Assert.IsTrue(_board.HasGenerator(b));

            // Cannot move onto an occupied cell.
            _board.TryPlaceItem(a, new ItemInstance(ItemType.Coffee, 1));
            Assert.IsFalse(_board.TryMoveGenerator(b, a));
        }

        [Test]
        public void TryFindEmptyCell_SkipsGenerators_AndFailsWhenFull()
        {
            _board.TryPlaceGenerator(BoardManager.IndexOf(1, 1), ItemType.Coffee);

            Assert.IsTrue(_board.TryFindEmptyCell(out int first));
            Assert.AreEqual(BoardManager.IndexOf(1, 2), first); // (1,1) has the generator

            for (int i = 0; i < BoardManager.CellCount; i++)
                if (_board.IsFreeCell(i))
                    _board.TryPlaceItem(i, new ItemInstance(ItemType.Coffee, 1));

            Assert.AreEqual(0, _board.FreeCellCount);
            Assert.IsFalse(_board.TryFindEmptyCell(out _));
        }

        [Test]
        public void TryUnlockCell_UnlocksLockedCellsOnly()
        {
            int locked = BoardManager.IndexOf(0, 3);
            Assert.IsTrue(_board.TryUnlockCell(locked));
            Assert.IsTrue(_board.IsUnlocked(locked));
            Assert.AreEqual(36, _board.UnlockedCount);

            Assert.IsFalse(_board.TryUnlockCell(locked));
            Assert.IsFalse(_board.TryUnlockCell(-1));
            Assert.IsFalse(_board.TryUnlockCell(999));
        }

        [Test]
        public void FindItemCell_FindsExactTypeAndLevel()
        {
            _board.TryPlaceItem(BoardManager.IndexOf(2, 2), new ItemInstance(ItemType.Bread, 2));

            Assert.AreEqual(BoardManager.IndexOf(2, 2), _board.FindItemCell(ItemType.Bread, 2));
            Assert.AreEqual(-1, _board.FindItemCell(ItemType.Bread, 3));
            Assert.AreEqual(-1, _board.FindItemCell(ItemType.Coffee, 2));
        }

        [Test]
        public void CellChanged_FiresOnMutations()
        {
            int fired = 0;
            _board.CellChanged += _ => fired++;

            int index = BoardManager.IndexOf(4, 4);
            _board.TryPlaceItem(index, new ItemInstance(ItemType.Coffee, 1));
            Assert.AreEqual(1, fired);

            _board.RemoveItem(index);
            Assert.AreEqual(2, fired);

            _board.TryPlaceGenerator(index, ItemType.Coffee);
            Assert.AreEqual(3, fired);

            _board.TryUnlockCell(BoardManager.IndexOf(0, 0));
            Assert.AreEqual(4, fired);
        }
    }
}
