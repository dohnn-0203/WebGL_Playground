using MergeCafe.Board;
using MergeCafe.Core;
using MergeCafe.Data;
using MergeCafe.Save;
using NUnit.Framework;

namespace MergeCafe.Tests
{
    public sealed class SaveManagerTests
    {
        private const double T0 = 1_000_000.0;

        private static GameManager NewGame() => new GameManager(T0, () => 0.5f);

        [Test]
        public void RoundTrip_PreservesEverything()
        {
            GameManager source = NewGame();

            source.Economy.AddGold(1000);
            source.RequestExpandBoard();      // -100, unlocks 1 cell → 36
            source.RequestUpgradeEnergy();    // -200, max 25, current +5

            int itemCell = BoardManager.IndexOf(5, 5);
            source.Board.TryPlaceItem(itemCell, new ItemInstance(ItemType.Coffee, 3));

            int genFrom = GeneratorCatalog.CoffeeMachine.InitialCell;
            int genTo = BoardManager.IndexOf(5, 6);
            Assert.IsTrue(source.Board.TryMoveGenerator(genFrom, genTo));

            source.Generators.Energy.Current = 7;
            source.Generators.Energy.LastRecoveryUnix = T0;

            string json = SaveManager.ToJson(source);
            Assert.IsTrue(SaveManager.TryParse(json, out SaveData data));

            GameManager restored = NewGame();
            SaveManager.Apply(restored, data, T0);

            Assert.AreEqual(700, restored.Economy.Gold);
            Assert.AreEqual(36, restored.Board.UnlockedCount);
            Assert.AreEqual(1, restored.Upgrades.ExpandedCellCount);
            Assert.AreEqual(150, restored.Upgrades.NextCellCost);
            Assert.AreEqual(1, restored.Upgrades.EnergyUpgradeCount);
            CollectionAssert.AreEquivalent(source.Board.GetUnlockedCells(),
                restored.Board.GetUnlockedCells());

            ItemInstance item = restored.Board.GetItem(itemCell);
            Assert.AreEqual(ItemType.Coffee, item.Type);
            Assert.AreEqual(3, item.Level);

            Assert.IsFalse(restored.Board.HasGenerator(genFrom));
            Assert.IsTrue(restored.Board.HasGenerator(genTo));
            Assert.AreEqual(ItemType.Coffee, restored.Board.GetGenerator(genTo));
            Assert.IsTrue(restored.Board.HasGenerator(GeneratorCatalog.Oven.InitialCell));

            Assert.AreEqual(25, restored.Generators.Energy.Max);
            Assert.AreEqual(7, restored.Generators.Energy.Current);

            Assert.AreEqual(5, restored.Orders.Orders.Count);
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(source.Orders.Orders[i].orderId, restored.Orders.Orders[i].orderId);
                Assert.AreEqual(source.Orders.Orders[i].requiredItemType, restored.Orders.Orders[i].requiredItemType);
                Assert.AreEqual(source.Orders.Orders[i].requiredItemLevel, restored.Orders.Orders[i].requiredItemLevel);
            }
            Assert.AreEqual(source.Orders.OrderCounter, restored.Orders.OrderCounter);
        }

        [Test]
        public void Apply_GrantsOfflineEnergyRecovery()
        {
            GameManager source = NewGame();
            source.Generators.Energy.Current = 5;
            source.Generators.Energy.LastRecoveryUnix = T0;

            string json = SaveManager.ToJson(source);
            SaveManager.TryParse(json, out SaveData data);

            GameManager restored = NewGame();
            SaveManager.Apply(restored, data, T0 + 15); // 3 intervals of 5s

            Assert.AreEqual(8, restored.Generators.Energy.Current);
        }

        [Test]
        public void TryParse_RejectsCorruptOrEmptyData()
        {
            Assert.IsFalse(SaveManager.TryParse(null, out _));
            Assert.IsFalse(SaveManager.TryParse("", out _));
            Assert.IsFalse(SaveManager.TryParse("not json {", out _));
            Assert.IsFalse(SaveManager.TryParse("{}", out _));
            Assert.IsFalse(SaveManager.TryParse("{\"version\":1}", out _));
        }

        [Test]
        public void Apply_SkipsInvalidSavedItems()
        {
            GameManager source = NewGame();
            string json = SaveManager.ToJson(source);
            SaveManager.TryParse(json, out SaveData data);

            int freeBefore = new GameManager(T0, () => 0.5f).Board.FreeCellCount;
            data.items.Add(new SavedItem { cellIndex = BoardManager.IndexOf(4, 4), itemType = 0, level = 99 });
            data.items.Add(new SavedItem { cellIndex = -3, itemType = 0, level = 1 });

            GameManager restored = NewGame();
            SaveManager.Apply(restored, data, T0);

            Assert.AreEqual(freeBefore, restored.Board.FreeCellCount); // nothing placed
        }
    }
}
