using System;
using System.Collections.Generic;
using MergeCafe.Board;
using MergeCafe.Data;
using MergeCafe.Economy;

namespace MergeCafe.Orders
{
    /// <summary>
    /// Keeps exactly three open orders (webGL_game.md §12): the fixed starter set,
    /// then weighted-random generation restricted to unlocked generator types.
    /// </summary>
    public sealed class OrderManager
    {
        public const int OrderCount = 3;

        // Level weights: Lv.2 45%, Lv.3 35%, Lv.4 15%, Lv.5 5%.
        private const float Lv2Threshold = 0.45f;
        private const float Lv3Threshold = 0.80f;
        private const float Lv4Threshold = 0.95f;

        private readonly List<CafeOrder> _orders = new List<CafeOrder>();
        private readonly Func<float> _rng01;
        private int _orderCounter;

        public event Action OrdersChanged;

        public IReadOnlyList<CafeOrder> Orders => _orders;

        public OrderManager(Func<float> rng01 = null)
        {
            _rng01 = rng01 ?? (() => UnityEngine.Random.value);
        }

        /// <summary>Fixed starter orders from the spec table (§12 초기 주문).</summary>
        public void SetupInitialOrders()
        {
            _orders.Clear();
            _orders.Add(new CafeOrder(NextOrderId(), ItemType.Coffee, 2, 30));
            _orders.Add(new CafeOrder(NextOrderId(), ItemType.Coffee, 3, 70));
            _orders.Add(new CafeOrder(NextOrderId(), ItemType.Bread, 2, 50));
            OrdersChanged?.Invoke();
        }

        public CafeOrder GenerateOrder(Func<ItemType, bool> isTypeUnlocked)
        {
            var types = new List<ItemType>();
            foreach (GeneratorDefinition def in GeneratorCatalog.All)
            {
                if (isTypeUnlocked(def.Output))
                    types.Add(def.Output);
            }
            if (types.Count == 0)
                types.Add(ItemType.Coffee); // coffee machine is always unlocked

            int typeIndex = Math.Min((int)(_rng01() * types.Count), types.Count - 1);
            ItemType type = types[typeIndex];

            float roll = _rng01();
            int level = roll < Lv2Threshold ? 2
                : roll < Lv3Threshold ? 3
                : roll < Lv4Threshold ? 4
                : 5;

            return new CafeOrder(NextOrderId(), type, level, RewardFor(type, level));
        }

        /// <summary>rewardGold = baseSellPrice * 2 + level * 10 (§12 보상 계산).</summary>
        public static int RewardFor(ItemType type, int level)
        {
            return ItemCatalog.Get(type, level).SellPrice * 2 + level * 10;
        }

        public CafeOrder Find(string orderId)
        {
            foreach (CafeOrder order in _orders)
            {
                if (order.orderId == orderId)
                    return order;
            }
            return null;
        }

        public bool CanComplete(CafeOrder order, BoardManager board)
        {
            return order != null &&
                   board.FindItemCell(order.requiredItemType, order.requiredItemLevel) >= 0;
        }

        /// <summary>
        /// Completes the order: removes one matching item, pays the reward and
        /// replaces the order slot with a freshly generated one (§12 완료 조건).
        /// </summary>
        public bool TryComplete(string orderId, BoardManager board, EconomyManager economy,
            Func<ItemType, bool> isTypeUnlocked)
        {
            CafeOrder order = Find(orderId);
            if (order == null)
                return false;

            int cellIndex = board.FindItemCell(order.requiredItemType, order.requiredItemLevel);
            if (cellIndex < 0)
                return false;

            board.RemoveItem(cellIndex);
            economy.AddGold(order.rewardGold);

            int slot = _orders.IndexOf(order);
            _orders[slot] = GenerateOrder(isTypeUnlocked);
            OrdersChanged?.Invoke();
            return true;
        }

        /// <summary>Replaces all orders (save restore).</summary>
        public void LoadOrders(IEnumerable<CafeOrder> orders, int orderCounter)
        {
            _orders.Clear();
            _orders.AddRange(orders);
            _orderCounter = orderCounter;
            OrdersChanged?.Invoke();
        }

        public int OrderCounter => _orderCounter;

        private string NextOrderId() => $"order_{++_orderCounter}";
    }
}
