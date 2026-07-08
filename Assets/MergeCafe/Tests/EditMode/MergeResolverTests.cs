using MergeCafe.Board;
using MergeCafe.Data;
using MergeCafe.Items;
using NUnit.Framework;

namespace MergeCafe.Tests
{
    public sealed class MergeResolverTests
    {
        private BoardManager _board;

        private static readonly int A = BoardManager.IndexOf(1, 1);
        private static readonly int B = BoardManager.IndexOf(1, 2);
        private static readonly int LockedCell = BoardManager.IndexOf(0, 0);

        [SetUp]
        public void SetUp()
        {
            _board = new BoardManager();
        }

        [Test]
        public void CanMerge_RequiresSameTypeSameLevelBelowMax()
        {
            Assert.IsTrue(MergeResolver.CanMerge(
                new ItemInstance(ItemType.Coffee, 1), new ItemInstance(ItemType.Coffee, 1)));

            Assert.IsFalse(MergeResolver.CanMerge(
                new ItemInstance(ItemType.Coffee, 1), new ItemInstance(ItemType.Bread, 1)));

            Assert.IsFalse(MergeResolver.CanMerge(
                new ItemInstance(ItemType.Coffee, 1), new ItemInstance(ItemType.Coffee, 2)));

            Assert.IsFalse(MergeResolver.CanMerge(
                new ItemInstance(ItemType.Coffee, 5), new ItemInstance(ItemType.Coffee, 5)));

            Assert.IsFalse(MergeResolver.CanMerge(new ItemInstance(ItemType.Coffee, 1), null));
        }

        [Test]
        public void Resolve_MoveToEmptyUnlockedCell_MovesItem()
        {
            var item = new ItemInstance(ItemType.Coffee, 1);
            _board.TryPlaceItem(A, item);

            MoveOutcome outcome = MergeResolver.Resolve(_board, A, B);

            Assert.AreEqual(MoveOutcome.MovedToEmpty, outcome);
            Assert.IsNull(_board.GetItem(A));
            Assert.AreSame(item, _board.GetItem(B));
        }

        [Test]
        public void Resolve_SameTypeSameLevel_MergesToNextLevel()
        {
            _board.TryPlaceItem(A, new ItemInstance(ItemType.Coffee, 1));
            _board.TryPlaceItem(B, new ItemInstance(ItemType.Coffee, 1));

            MoveOutcome outcome = MergeResolver.Resolve(_board, A, B);

            Assert.AreEqual(MoveOutcome.Merged, outcome);
            Assert.IsNull(_board.GetItem(A));
            ItemInstance merged = _board.GetItem(B);
            Assert.AreEqual(ItemType.Coffee, merged.Type);
            Assert.AreEqual(2, merged.Level);
        }

        [Test]
        public void Resolve_DifferentType_RejectsAndKeepsBoard()
        {
            var coffee = new ItemInstance(ItemType.Coffee, 1);
            var bread = new ItemInstance(ItemType.Bread, 1);
            _board.TryPlaceItem(A, coffee);
            _board.TryPlaceItem(B, bread);

            MoveOutcome outcome = MergeResolver.Resolve(_board, A, B);

            Assert.AreEqual(MoveOutcome.RejectedIncompatible, outcome);
            Assert.AreSame(coffee, _board.GetItem(A));
            Assert.AreSame(bread, _board.GetItem(B));
        }

        [Test]
        public void Resolve_DifferentLevel_Rejects()
        {
            _board.TryPlaceItem(A, new ItemInstance(ItemType.Coffee, 1));
            _board.TryPlaceItem(B, new ItemInstance(ItemType.Coffee, 2));

            Assert.AreEqual(MoveOutcome.RejectedIncompatible, MergeResolver.Resolve(_board, A, B));
        }

        [Test]
        public void Resolve_MaxLevelPair_RejectsAsMaxLevel()
        {
            var left = new ItemInstance(ItemType.Dessert, 5);
            var right = new ItemInstance(ItemType.Dessert, 5);
            _board.TryPlaceItem(A, left);
            _board.TryPlaceItem(B, right);

            MoveOutcome outcome = MergeResolver.Resolve(_board, A, B);

            Assert.AreEqual(MoveOutcome.RejectedMaxLevel, outcome);
            Assert.AreSame(left, _board.GetItem(A));
            Assert.AreSame(right, _board.GetItem(B));
        }

        [Test]
        public void Resolve_LockedTarget_Rejects()
        {
            _board.TryPlaceItem(A, new ItemInstance(ItemType.Coffee, 1));

            Assert.AreEqual(MoveOutcome.RejectedLocked, MergeResolver.Resolve(_board, A, LockedCell));
            Assert.IsNotNull(_board.GetItem(A));
        }

        [Test]
        public void Resolve_SameCellOrInvalid_DoesNothing()
        {
            _board.TryPlaceItem(A, new ItemInstance(ItemType.Coffee, 1));

            Assert.AreEqual(MoveOutcome.SameCell, MergeResolver.Resolve(_board, A, A));
            Assert.AreEqual(MoveOutcome.InvalidTarget, MergeResolver.Resolve(_board, A, -5));
            Assert.AreEqual(MoveOutcome.InvalidTarget, MergeResolver.Resolve(_board, A, 99));
            Assert.AreEqual(MoveOutcome.InvalidTarget, MergeResolver.Resolve(_board, B, A));
            Assert.IsNotNull(_board.GetItem(A));
        }

        [Test]
        public void CanDrop_TrueForEmptyUnlockedOrMergeableTargets()
        {
            _board.TryPlaceItem(A, new ItemInstance(ItemType.Coffee, 1));
            _board.TryPlaceItem(B, new ItemInstance(ItemType.Coffee, 1));
            int breadCell = BoardManager.IndexOf(2, 2);
            _board.TryPlaceItem(breadCell, new ItemInstance(ItemType.Bread, 1));

            Assert.IsTrue(MergeResolver.CanDrop(_board, A, BoardManager.IndexOf(3, 3))); // empty
            Assert.IsTrue(MergeResolver.CanDrop(_board, A, B));                          // mergeable
            Assert.IsFalse(MergeResolver.CanDrop(_board, A, breadCell));                 // incompatible
            Assert.IsFalse(MergeResolver.CanDrop(_board, A, LockedCell));                // locked
            Assert.IsFalse(MergeResolver.CanDrop(_board, A, A));                         // self
        }
    }
}
