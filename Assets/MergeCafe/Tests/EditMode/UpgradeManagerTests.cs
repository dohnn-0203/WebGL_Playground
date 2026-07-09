using System.Collections.Generic;
using MergeCafe.Board;
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
        public void CellUnlockOrder_CoversTheLockedCellsOnce()
        {
            // 63 total − 35 initial = 28 locked cells.
            Assert.AreEqual(28, UpgradeManager.CellUnlockOrder.Length);

            var seen = new HashSet<int>();
            foreach (int index in UpgradeManager.CellUnlockOrder)
            {
                Assert.IsFalse(BoardManager.IsInInitialRegion(index), $"cell {index} is initial");
                Assert.IsTrue(seen.Add(index), $"cell {index} duplicated");
            }
        }

        [Test]
        public void FirstUnlock_IsNearestLockedCellToCentre()
        {
            // Board centre is (3,4); nearest locked cell is (0,4).
            Assert.AreEqual(BoardManager.IndexOf(0, 4), UpgradeManager.CellUnlockOrder[0]);
        }

        [Test]
        public void NextCellCost_Grows100Then50()
        {
            Assert.AreEqual(100, _upgrades.NextCellCost);
            _economy.AddGold(10_000);
            _upgrades.TryExpandBoard(_board, _economy);
            Assert.AreEqual(150, _upgrades.NextCellCost);
            _upgrades.TryExpandBoard(_board, _economy);
            Assert.AreEqual(200, _upgrades.NextCellCost);
        }

        [Test]
        public void TryExpandBoard_SpendsGoldAndUnlocks()
        {
            _economy.AddGold(100);
            Assert.IsTrue(_upgrades.TryExpandBoard(_board, _economy));
            Assert.AreEqual(0, _economy.Gold);
            Assert.AreEqual(36, _board.UnlockedCount);
            Assert.IsTrue(_board.IsUnlocked(BoardManager.IndexOf(0, 4)));

            Assert.IsFalse(_upgrades.TryExpandBoard(_board, _economy)); // no gold
        }

        [Test]
        public void ExpandingEveryCell_FullyUnlocks_ThenStops()
        {
            _economy.AddGold(21_700); // Σ (100 + 50k), k = 0..27
            for (int i = 0; i < 28; i++)
                Assert.IsTrue(_upgrades.TryExpandBoard(_board, _economy), $"expansion {i}");

            Assert.AreEqual(0, _economy.Gold);
            Assert.IsTrue(UpgradeManager.IsBoardFullyUnlocked(_board));

            _economy.AddGold(10_000);
            Assert.IsFalse(_upgrades.TryExpandBoard(_board, _economy));
        }

        [Test]
        public void TryUpgradeEnergy_RaisesMaxAndGrantsEnergy()
        {
            var pool = new EnergyPool(T0); // 20/20
            _economy.AddGold(200);

            Assert.AreEqual(200, _upgrades.NextEnergyCost);
            Assert.IsTrue(_upgrades.TryUpgradeEnergy(pool, _economy));
            Assert.AreEqual(25, pool.Max);
            Assert.AreEqual(25, pool.Current);
            Assert.AreEqual(0, _economy.Gold);
            Assert.AreEqual(350, _upgrades.NextEnergyCost); // 200 + 150
            Assert.AreEqual(1, _upgrades.EnergyUpgradeCount);
        }

        [Test]
        public void TryUpgradeEnergy_WithoutGold_Fails()
        {
            var pool = new EnergyPool(T0);
            _economy.AddGold(199);
            Assert.IsFalse(_upgrades.TryUpgradeEnergy(pool, _economy));
            Assert.AreEqual(20, pool.Max);
        }

        [Test]
        public void ApplyEnergyUpgradesTo_RestoresMaxFromCount()
        {
            _upgrades.EnergyUpgradeCount = 3;
            var pool = new EnergyPool(T0);
            _upgrades.ApplyEnergyUpgradesTo(pool);
            Assert.AreEqual(EnergyPool.BaseMax + 3 * UpgradeManager.EnergyStep, pool.Max);
        }
    }
}
