using System.Collections.Generic;
using MergeCafe.Bubble;
using NUnit.Framework;

namespace MergeCafe.Tests
{
    public sealed class BubbleGridTests
    {
        private BubbleGrid _grid;
        private readonly List<(int, int)> _buf = new List<(int, int)>();

        [SetUp]
        public void SetUp() => _grid = new BubbleGrid(5, 5);

        [Test]
        public void OddRows_HaveOneFewerColumn()
        {
            Assert.AreEqual(5, _grid.ColsInRow(0));
            Assert.AreEqual(4, _grid.ColsInRow(1));
            Assert.IsTrue(_grid.IsValid(1, 3));
            Assert.IsFalse(_grid.IsValid(1, 4));
        }

        [Test]
        public void Neighbors_EvenRowInterior_HasSix()
        {
            _grid.Neighbors(2, 2, _buf);
            CollectionAssert.AreEquivalent(
                new[] { (2, 1), (2, 3), (1, 1), (1, 2), (3, 1), (3, 2) }, _buf);
        }

        [Test]
        public void Neighbors_OddRowInterior_HasSix()
        {
            _grid.Neighbors(1, 2, _buf);
            CollectionAssert.AreEquivalent(
                new[] { (1, 1), (1, 3), (0, 2), (0, 3), (2, 2), (2, 3) }, _buf);
        }

        [Test]
        public void Neighbors_Corner_ClampsToValid()
        {
            _grid.Neighbors(0, 0, _buf);
            CollectionAssert.AreEquivalent(new[] { (0, 1), (1, 0) }, _buf);
        }

        [Test]
        public void SameColorGroup_FindsConnectedRun()
        {
            _grid.Set(0, 0, 3);
            _grid.Set(0, 1, 3);
            _grid.Set(1, 0, 3);
            _grid.Set(2, 2, 3); // separate island, same colour but not adjacent

            List<(int, int)> group = _grid.SameColorGroup(0, 0);
            CollectionAssert.AreEquivalent(new[] { (0, 0), (0, 1), (1, 0) }, group);
        }

        [Test]
        public void SameColorGroup_StopsAtDifferentColor()
        {
            _grid.Set(0, 0, 1);
            _grid.Set(0, 1, 2);
            Assert.AreEqual(1, _grid.SameColorGroup(0, 0).Count);
        }

        [Test]
        public void FindFloating_ReturnsBubblesNotHangingFromCeiling()
        {
            _grid.Set(0, 0, 0); // on the ceiling
            _grid.Set(1, 0, 0); // hangs from (0,0)
            _grid.Set(3, 3, 1); // isolated island → floating

            List<(int, int)> floating = _grid.FindFloating();
            CollectionAssert.AreEquivalent(new[] { (3, 3) }, floating);
        }

        [Test]
        public void FindFloating_Empty_WhenAllConnectedToCeiling()
        {
            _grid.Set(0, 1, 0);
            _grid.Set(1, 0, 0);
            _grid.Set(1, 1, 0);
            Assert.AreEqual(0, _grid.FindFloating().Count);
        }

        [Test]
        public void CountAndPresentColors_Track()
        {
            Assert.IsTrue(_grid.IsEmpty);
            _grid.Set(0, 0, 2);
            _grid.Set(0, 1, 5);
            Assert.AreEqual(2, _grid.Count);
            CollectionAssert.AreEquivalent(new[] { 2, 5 }, _grid.PresentColors());
        }
    }
}
