using System.Collections.Generic;
using MergeCafe.Board;
using MergeCafe.Data;
using MergeCafe.Economy;
using MergeCafe.Generators;
using NUnit.Framework;

namespace MergeCafe.Tests
{
    public sealed class UpgradeManagerTests
    {
        private const double T0 = 1_000_000.0;

        private BoardManager _board;
        private EconomyManager _economy;
        private UpgradeManager _upgrades;

        [SetUp]
        public void SetUp()
        {
            _board = new BoardManager();
            _economy = new EconomyManager();
            _upgrades = new UpgradeManager();
        }

        [Test]
        public void CellUnlockOrder_CoversExactlyTheTwentyLockedCells()
        {
            Assert.AreEqual(20, UpgradeManager.CellUnlockOrder.Length);

            var seen = new HashSet<int>();
            foreach (int index in UpgradeManager.CellUnlockOrder)
            {
                Assert.IsFalse(BoardManager.IsInInitialRegion(index), $"cell {index} is initial");
                Assert.IsTrue(seen.Add(index), $"cell {index} duplicated");
            }
        }

        [Test]
        public void CellUnlockOrder_StartsNearTheBoardCenter()
        {
            // The nearest locked cells to the center are the edge-adjacent ones
            // (distance² = 0.25+6.25); corners (12.5) must come last.
            int first = UpgradeManager.CellUnlockOrder[0];
            int last = UpgradeManager.CellUnlockOrder[19];

            Assert.AreEqual(BoardManager.IndexOf(0, 2), first);
            Assert.AreEqual(BoardManager.IndexOf(5, 5), last);
        }

        [Test]
        public void NextCellCost_Grows100Then50Increments()
        {
            Assert.AreEqual(100, _upgrades.NextCellCost);

            _economy.AddGold(1000);
            _upgrades.TryExpandBoard(_board, _economy);
            Assert.AreEqual(150, _upgrades.NextCellCost);

            _upgrades.TryExpandBoard(_board, _economy);
            Assert.AreEqual(200, _upgrades.NextCellCost);
        }

        [Test]
        public void TryExpandBoard_SpendsGoldAndUnlocksNextCell()
        {
            _economy.AddGold(100);

            Assert.IsTrue(_upgrades.TryExpandBoard(_board, _economy));
            Assert.AreEqual(0, _economy.Gold);
            Assert.AreEqual(17, _board.UnlockedCount);
            Assert.IsTrue(_board.IsUnlocked(BoardManager.IndexOf(0, 2)));
        }

        [Test]
        public void TryExpandBoard_WithoutGold_Fails()
        {
            _economy.AddGold(99);

            Assert.IsFalse(_upgrades.TryExpandBoard(_board, _economy));
            Assert.AreEqual(99, _economy.Gold);
            Assert.AreEqual(16, _board.UnlockedCount);
        }

        [Test]
        public void ExpandingAllCells_FullyUnlocksBoard_ThenStops()
        {
            _economy.AddGold(11_500); // Σ (100 + 50k) for k = 0..19

            for (int i = 0; i < 20; i++)
                Assert.IsTrue(_upgrades.TryExpandBoard(_board, _economy), $"expansion {i}");

            Assert.AreEqual(0, _economy.Gold);
            Assert.IsTrue(UpgradeManager.IsBoardFullyUnlocked(_board));

            _economy.AddGold(10_000);
            Assert.IsFalse(_upgrades.TryExpandBoard(_board, _economy));
        }

        [Test]
        public void TryUpgradeGenerator_RaisesLevelAndMaxEnergy()
        {
            var generators = new GeneratorManager(T0, () => 0.99f);
            GeneratorState coffee = generators.Get(ItemType.Coffee);
            _economy.AddGold(200);

            Assert.IsTrue(_upgrades.TryUpgradeGenerator(coffee, _economy));
            Assert.AreEqual(2, coffee.UpgradeLevel);
            Assert.AreEqual(23, coffee.MaxEnergy); // 20 + 3
            Assert.AreEqual(0, _economy.Gold);
        }

        [Test]
        public void TryUpgradeGenerator_RejectsLockedMaxedOrUnaffordable()
        {
            var generators = new GeneratorManager(T0, () => 0.99f);

            GeneratorState oven = generators.Get(ItemType.Bread);
            _economy.AddGold(10_000);
            Assert.IsFalse(_upgrades.TryUpgradeGenerator(oven, _economy)); // locked

            GeneratorState coffee = generators.Get(ItemType.Coffee);
            Assert.IsTrue(_upgrades.TryUpgradeGenerator(coffee, _economy));  // → 2
            Assert.IsTrue(_upgrades.TryUpgradeGenerator(coffee, _economy));  // → 3
            Assert.IsTrue(_upgrades.TryUpgradeGenerator(coffee, _economy));  // → 4
            Assert.IsFalse(_upgrades.TryUpgradeGenerator(coffee, _economy)); // maxed
            Assert.AreEqual(4, coffee.UpgradeLevel);
            Assert.AreEqual(28, coffee.MaxEnergy); // 20 + 8

            var poor = new EconomyManager();
            var generators2 = new GeneratorManager(T0, () => 0.99f);
            poor.AddGold(199);
            Assert.IsFalse(_upgrades.TryUpgradeGenerator(generators2.Get(ItemType.Coffee), poor));
        }
    }
}
