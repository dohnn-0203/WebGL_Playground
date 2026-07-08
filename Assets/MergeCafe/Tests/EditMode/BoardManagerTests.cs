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
        public void InitialState_Has16UnlockedCellsOf36()
        {
            Assert.AreEqual(36, BoardManager.CellCount);
            Assert.AreEqual(16, _board.UnlockedCount);
            Assert.AreEqual(16, _board.EmptyUnlockedCount);
        }

        [Test]
        public void InitialState_UnlockedRegionIsCentered4x4()
        {
            for (int row = 0; row < 6; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    bool expected = row >= 1 && row <= 4 && col >= 1 && col <= 4;
                    Assert.AreEqual(expected, _board.IsUnlocked(BoardManager.IndexOf(row, col)),
                        $"cell ({row},{col})");
                }
            }
        }

        [Test]
        public void TryPlaceItem_OnEmptyUnlockedCell_Succeeds()
        {
            int index = BoardManager.IndexOf(1, 1);
            var item = new ItemInstance(ItemType.Coffee, 1);

            Assert.IsTrue(_board.TryPlaceItem(index, item));
            Assert.AreSame(item, _board.GetItem(index));
            Assert.AreEqual(15, _board.EmptyUnlockedCount);
        }

        [Test]
        public void TryPlaceItem_OnLockedOrOccupiedCell_Fails()
        {
            int locked = BoardManager.IndexOf(0, 0);
            Assert.IsFalse(_board.TryPlaceItem(locked, new ItemInstance(ItemType.Coffee, 1)));

            int open = BoardManager.IndexOf(2, 2);
            Assert.IsTrue(_board.TryPlaceItem(open, new ItemInstance(ItemType.Coffee, 1)));
            Assert.IsFalse(_board.TryPlaceItem(open, new ItemInstance(ItemType.Bread, 1)));
        }

        [Test]
        public void TryFindEmptyCell_ScansRowMajor_AndFailsWhenFull()
        {
            Assert.IsTrue(_board.TryFindEmptyCell(out int first));
            Assert.AreEqual(BoardManager.IndexOf(1, 1), first);

            for (int i = 0; i < BoardManager.CellCount; i++)
            {
                if (_board.IsUnlocked(i))
                    _board.TryPlaceItem(i, new ItemInstance(ItemType.Coffee, 1));
            }

            Assert.AreEqual(0, _board.EmptyUnlockedCount);
            Assert.IsFalse(_board.TryFindEmptyCell(out _));
        }

        [Test]
        public void RemoveItem_FreesTheCell()
        {
            int index = BoardManager.IndexOf(3, 3);
            var item = new ItemInstance(ItemType.Dessert, 2);
            _board.TryPlaceItem(index, item);

            Assert.AreSame(item, _board.RemoveItem(index));
            Assert.IsNull(_board.GetItem(index));
            Assert.IsTrue(_board.IsEmptyCell(index));
            Assert.IsNull(_board.RemoveItem(index));
        }

        [Test]
        public void TryUnlockCell_UnlocksLockedCellsOnly()
        {
            int locked = BoardManager.IndexOf(0, 3);
            Assert.IsTrue(_board.TryUnlockCell(locked));
            Assert.IsTrue(_board.IsUnlocked(locked));
            Assert.AreEqual(17, _board.UnlockedCount);

            Assert.IsFalse(_board.TryUnlockCell(locked));
            Assert.IsFalse(_board.TryUnlockCell(-1));
            Assert.IsFalse(_board.TryUnlockCell(99));
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
        public void CellChanged_FiresOnPlaceRemoveUnlock()
        {
            int fired = 0;
            int lastIndex = -1;
            _board.CellChanged += i => { fired++; lastIndex = i; };

            int index = BoardManager.IndexOf(4, 4);
            _board.TryPlaceItem(index, new ItemInstance(ItemType.Coffee, 1));
            Assert.AreEqual(1, fired);
            Assert.AreEqual(index, lastIndex);

            _board.RemoveItem(index);
            Assert.AreEqual(2, fired);

            _board.TryUnlockCell(BoardManager.IndexOf(0, 0));
            Assert.AreEqual(3, fired);
        }
    }
}
