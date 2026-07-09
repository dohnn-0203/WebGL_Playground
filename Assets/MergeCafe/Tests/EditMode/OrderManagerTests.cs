using System.Collections.Generic;
using MergeCafe.Board;
using MergeCafe.Data;
using MergeCafe.Economy;
using MergeCafe.Orders;
using NUnit.Framework;

namespace MergeCafe.Tests
{
    public sealed class OrderManagerTests
    {
        private BoardManager _board;
        private EconomyManager _economy;
        private OrderManager _orders;
        private Queue<float> _rolls;

        [SetUp]
        public void SetUp()
        {
            _board = new BoardManager();
            _economy = new EconomyManager();
            _rolls = new Queue<float>();
            _orders = new OrderManager(NextRoll);
            _orders.SetupInitialOrders(AllTypes);
        }

        private float NextRoll() => _rolls.Count > 0 ? _rolls.Dequeue() : 0f;

        private static bool OnlyCoffee(ItemType type) => type == ItemType.Coffee;
        private static bool AllTypes(ItemType type) => true;

        [Test]
        public void SetupInitialOrders_HasFiveWithFixedStarters()
        {
            Assert.AreEqual(5, _orders.Orders.Count);

            Assert.AreEqual(ItemType.Coffee, _orders.Orders[0].requiredItemType);
            Assert.AreEqual(2, _orders.Orders[0].requiredItemLevel);
            Assert.AreEqual(30, _orders.Orders[0].rewardGold);

            Assert.AreEqual(ItemType.Coffee, _orders.Orders[1].requiredItemType);
            Assert.AreEqual(3, _orders.Orders[1].requiredItemLevel);
            Assert.AreEqual(70, _orders.Orders[1].rewardGold);

            Assert.AreEqual(ItemType.Bread, _orders.Orders[2].requiredItemType);
            Assert.AreEqual(2, _orders.Orders[2].requiredItemLevel);
            Assert.AreEqual(50, _orders.Orders[2].rewardGold);
        }

        [Test]
        public void RewardFormula_IsBasePriceTimesTwoPlusLevelTimesTen()
        {
            Assert.AreEqual(35 * 2 + 30, OrderManager.RewardFor(ItemType.Coffee, 3));
            Assert.AreEqual(18 * 2 + 20, OrderManager.RewardFor(ItemType.Bread, 2));
            Assert.AreEqual(260 * 2 + 50, OrderManager.RewardFor(ItemType.Dessert, 5));
        }

        [TestCase(0.00f, 2)]
        [TestCase(0.44f, 2)]
        [TestCase(0.45f, 3)]
        [TestCase(0.79f, 3)]
        [TestCase(0.80f, 4)]
        [TestCase(0.94f, 4)]
        [TestCase(0.95f, 5)]
        [TestCase(0.99f, 5)]
        public void GenerateOrder_LevelWeightsFollowSpecThresholds(float levelRoll, int expectedLevel)
        {
            _rolls.Enqueue(0f);        // type roll → first unlocked type
            _rolls.Enqueue(levelRoll); // level roll

            CafeOrder order = _orders.GenerateOrder(AllTypes);

            Assert.AreEqual(expectedLevel, order.requiredItemLevel);
            Assert.AreEqual(OrderManager.RewardFor(order.requiredItemType, expectedLevel),
                order.rewardGold);
        }

        [Test]
        public void GenerateOrder_NeverPicksLockedTypes()
        {
            for (int i = 0; i < 10; i++)
            {
                _rolls.Enqueue(0.99f); // type roll — even the highest roll must stay in unlocked set
                _rolls.Enqueue(0.5f);
                CafeOrder order = _orders.GenerateOrder(OnlyCoffee);
                Assert.AreEqual(ItemType.Coffee, order.requiredItemType);
            }
        }

        [Test]
        public void CanComplete_OnlyWhenBoardHasExactItem()
        {
            CafeOrder first = _orders.Orders[0]; // Coffee Lv.2
            Assert.IsFalse(_orders.CanComplete(first, _board));

            _board.TryPlaceItem(BoardManager.IndexOf(1, 1), new ItemInstance(ItemType.Coffee, 1));
            Assert.IsFalse(_orders.CanComplete(first, _board));

            _board.TryPlaceItem(BoardManager.IndexOf(1, 2), new ItemInstance(ItemType.Coffee, 2));
            Assert.IsTrue(_orders.CanComplete(first, _board));
        }

        [Test]
        public void TryComplete_RemovesItemPaysGoldAndReplacesOrder()
        {
            int cell = BoardManager.IndexOf(2, 2);
            _board.TryPlaceItem(cell, new ItemInstance(ItemType.Coffee, 2));

            string completedId = _orders.Orders[0].orderId;
            _rolls.Enqueue(0f);
            _rolls.Enqueue(0f);

            bool ok = _orders.TryComplete(completedId, _board, _economy, OnlyCoffee);

            Assert.IsTrue(ok);
            Assert.IsNull(_board.GetItem(cell));
            Assert.AreEqual(30, _economy.Gold);
            Assert.AreEqual(5, _orders.Orders.Count);
            Assert.AreNotEqual(completedId, _orders.Orders[0].orderId);
            Assert.IsNull(_orders.Find(completedId));
        }

        [Test]
        public void TryComplete_WithoutMatchingItem_Fails()
        {
            string id = _orders.Orders[0].orderId;

            Assert.IsFalse(_orders.TryComplete(id, _board, _economy, OnlyCoffee));
            Assert.AreEqual(0, _economy.Gold);
            Assert.AreEqual(id, _orders.Orders[0].orderId);
        }
    }

    public sealed class EconomyManagerTests
    {
        [Test]
        public void Gold_StartsAtZero_AddAndSpendWork()
        {
            var economy = new EconomyManager();
            Assert.AreEqual(0, economy.Gold);

            economy.AddGold(100);
            Assert.AreEqual(100, economy.Gold);
            Assert.IsTrue(economy.CanAfford(100));
            Assert.IsFalse(economy.CanAfford(101));

            Assert.IsTrue(economy.TrySpend(40));
            Assert.AreEqual(60, economy.Gold);

            Assert.IsFalse(economy.TrySpend(61));
            Assert.AreEqual(60, economy.Gold);
        }

        [Test]
        public void GoldChanged_FiresOnMutation()
        {
            var economy = new EconomyManager();
            long observed = -1;
            economy.GoldChanged += gold => observed = gold;

            economy.AddGold(30);
            Assert.AreEqual(30, observed);

            economy.TrySpend(10);
            Assert.AreEqual(20, observed);

            economy.SetGold(500);
            Assert.AreEqual(500, observed);
        }

        [Test]
        public void InvalidAmounts_AreIgnored()
        {
            var economy = new EconomyManager();
            economy.AddGold(-50);
            Assert.AreEqual(0, economy.Gold);
            Assert.IsFalse(economy.TrySpend(-10));
        }
    }
}
