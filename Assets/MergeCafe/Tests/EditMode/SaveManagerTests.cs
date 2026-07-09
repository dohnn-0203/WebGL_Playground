using MergeCafe.Board;
using MergeCafe.Core;
using MergeCafe.Data;
using MergeCafe.Generators;
using MergeCafe.Save;
using NUnit.Framework;

namespace MergeCafe.Tests
{
    public sealed class SaveManagerTests
    {
        private const double T0 = 1_000_000.0;

        private static GameManager NewGame() => new GameManager(T0, () => 0.99f);

        [Test]
        public void RoundTrip_PreservesEverything()
        {
            GameManager source = NewGame();

            // Mutate a bit of everything.
            source.Economy.AddGold(700);
            source.RequestUnlockGenerator(ItemType.Bread);          // -150 gold
            source.RequestUpgradeGenerator(ItemType.Coffee);        // -200 gold → Lv.2
            source.RequestExpandBoard();                            // -100 gold → 17 cells
            source.Board.TryPlaceItem(BoardManager.IndexOf(2, 2), new ItemInstance(ItemType.Coffee, 2));
            source.Board.TryPlaceItem(BoardManager.IndexOf(3, 4), new ItemInstance(ItemType.Dessert, 5));
            source.Generators.Get(ItemType.Coffee).Energy = 4;
            // Anchor at T0 so Apply(...,T0) triggers no recovery — this test checks the
            // round-trip, not offline recovery (covered separately).
            source.Generators.Get(ItemType.Coffee).LastRecoveryUnix = T0;

            string json = SaveManager.ToJson(source);
            Assert.IsTrue(SaveManager.TryParse(json, out SaveData data));

            GameManager restored = NewGame();
            SaveManager.Apply(restored, data, T0);

            Assert.AreEqual(250, restored.Economy.Gold);
            Assert.AreEqual(17, restored.Board.UnlockedCount);
            Assert.AreEqual(1, restored.Upgrades.ExpandedCellCount);
            Assert.AreEqual(150, restored.Upgrades.NextCellCost);
            CollectionAssert.AreEquivalent(source.Board.GetUnlockedCells(),
                restored.Board.GetUnlockedCells());

            ItemInstance coffee = restored.Board.GetItem(BoardManager.IndexOf(2, 2));
            Assert.AreEqual(ItemType.Coffee, coffee.Type);
            Assert.AreEqual(2, coffee.Level);
            ItemInstance dessert = restored.Board.GetItem(BoardManager.IndexOf(3, 4));
            Assert.AreEqual(ItemType.Dessert, dessert.Type);
            Assert.AreEqual(5, dessert.Level);

            GeneratorState coffeeGen = restored.Generators.Get(ItemType.Coffee);
            Assert.AreEqual(2, coffeeGen.UpgradeLevel);
            Assert.AreEqual(4, coffeeGen.Energy);
            Assert.AreEqual(T0, coffeeGen.LastRecoveryUnix, 0.001);
            Assert.IsTrue(restored.Generators.Get(ItemType.Bread).Unlocked);
            Assert.IsFalse(restored.Generators.Get(ItemType.Dessert).Unlocked);

            Assert.AreEqual(3, restored.Orders.Orders.Count);
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(source.Orders.Orders[i].orderId, restored.Orders.Orders[i].orderId);
                Assert.AreEqual(source.Orders.Orders[i].requiredItemType, restored.Orders.Orders[i].requiredItemType);
                Assert.AreEqual(source.Orders.Orders[i].requiredItemLevel, restored.Orders.Orders[i].requiredItemLevel);
                Assert.AreEqual(source.Orders.Orders[i].rewardGold, restored.Orders.Orders[i].rewardGold);
            }
            Assert.AreEqual(source.Orders.OrderCounter, restored.Orders.OrderCounter);
        }

        [Test]
        public void Apply_GrantsOfflineEnergyRecovery()
        {
            GameManager source = NewGame();
            GeneratorState coffee = source.Generators.Get(ItemType.Coffee);
            coffee.Energy = 5;
            coffee.LastRecoveryUnix = T0;

            string json = SaveManager.ToJson(source);
            SaveManager.TryParse(json, out SaveData data);

            GameManager restored = NewGame();
            SaveManager.Apply(restored, data, T0 + 15); // 3 intervals of 5s while "offline"

            Assert.AreEqual(8, restored.Generators.Get(ItemType.Coffee).Energy);
        }

        [Test]
        public void TryParse_RejectsCorruptOrEmptyData()
        {
            Assert.IsFalse(SaveManager.TryParse(null, out _));
            Assert.IsFalse(SaveManager.TryParse("", out _));
            Assert.IsFalse(SaveManager.TryParse("not json at all {", out _));
            Assert.IsFalse(SaveManager.TryParse("{}", out _));
            Assert.IsFalse(SaveManager.TryParse("{\"version\":0}", out _));
        }

        [Test]
        public void Apply_SkipsInvalidSavedItems()
        {
            GameManager source = NewGame();
            string json = SaveManager.ToJson(source);
            SaveManager.TryParse(json, out SaveData data);

            data.items.Add(new SavedItem { cellIndex = 10, itemType = 0, level = 99 }); // bad level
            data.items.Add(new SavedItem { cellIndex = -3, itemType = 0, level = 1 });  // bad cell

            GameManager restored = NewGame();
            SaveManager.Apply(restored, data, T0);

            Assert.AreEqual(16, restored.Board.EmptyUnlockedCount); // nothing was placed
        }
    }
}
