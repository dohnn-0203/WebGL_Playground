using System;
using MergeCafe.Board;
using MergeCafe.Data;
using MergeCafe.Economy;
using MergeCafe.Generators;
using MergeCafe.Items;
using MergeCafe.Orders;

namespace MergeCafe.Core
{
    /// <summary>
    /// Central coordinator (pure C#): owns the systems and routes player intents.
    /// Generators live on the board and share one energy pool; orders sit on the left.
    /// </summary>
    public sealed class GameManager
    {
        public BoardManager Board { get; }
        public GeneratorManager Generators { get; }
        public EconomyManager Economy { get; }
        public OrderManager Orders { get; }
        public UpgradeManager Upgrades { get; }

        public event Action<string> ToastRequested;
        public event Action<int> ItemSpawned;
        public event Action<int> ItemMerged;
        public event Action<int, int> ItemMoved;
        public event Action<CafeOrder> OrderCompleted;

        /// <summary>Autosave hook — raised after every successful state-mutating action.</summary>
        public event Action StateChanged;

        public GameManager(double nowUnix, Func<float> rng01 = null)
        {
            Board = new BoardManager();
            Generators = new GeneratorManager(nowUnix);
            Economy = new EconomyManager();
            Orders = new OrderManager(rng01);
            Upgrades = new UpgradeManager();

            Generators.PlaceInitialGenerators(Board);
            Orders.SetupInitialOrders(IsTypeUnlocked);
        }

        /// <summary>Every generator type is present on the board from the start.</summary>
        public bool IsTypeUnlocked(ItemType type) => true;

        public void Tick(double nowUnix) => Generators.Tick(nowUnix);

        /// <summary>Player tapped a generator tile.</summary>
        public bool RequestSpawn(ItemType type, double nowUnix)
        {
            SpawnResult result = Generators.TrySpawn(type, Board, nowUnix);
            switch (result.Code)
            {
                case SpawnResultCode.Ok:
                    ItemSpawned?.Invoke(result.CellIndex);
                    StateChanged?.Invoke();
                    return true;

                case SpawnResultCode.BoardFull:
                    Toast("보드 공간이 부족합니다");
                    return false;

                default:
                    Toast("에너지가 부족합니다");
                    return false;
            }
        }

        /// <summary>Player dropped an item at fromIndex onto toIndex.</summary>
        public MoveOutcome RequestMoveItem(int fromIndex, int toIndex)
        {
            MoveOutcome outcome = MergeResolver.Resolve(Board, fromIndex, toIndex);
            switch (outcome)
            {
                case MoveOutcome.Merged:
                    ItemMerged?.Invoke(toIndex);
                    StateChanged?.Invoke();
                    break;
                case MoveOutcome.MovedToEmpty:
                    ItemMoved?.Invoke(fromIndex, toIndex);
                    StateChanged?.Invoke();
                    break;
                case MoveOutcome.RejectedMaxLevel:
                    Toast("이미 최고 레벨입니다");
                    break;
            }
            return outcome;
        }

        /// <summary>Player dragged a generator tile to another cell.</summary>
        public bool RequestMoveGenerator(int fromIndex, int toIndex)
        {
            if (!Board.TryMoveGenerator(fromIndex, toIndex))
                return false;
            StateChanged?.Invoke();
            return true;
        }

        public bool RequestExpandBoard()
        {
            if (UpgradeManager.IsBoardFullyUnlocked(Board))
            {
                Toast("보드가 모두 열려 있습니다");
                return false;
            }

            int cost = Upgrades.NextCellCost;
            if (!Economy.CanAfford(cost))
            {
                Toast($"골드가 부족합니다 (확장 {cost} 골드)");
                return false;
            }
            if (!Upgrades.TryExpandBoard(Board, Economy))
                return false;

            Toast("보드 칸이 열렸습니다!");
            StateChanged?.Invoke();
            return true;
        }

        public bool RequestUpgradeEnergy()
        {
            int cost = Upgrades.NextEnergyCost;
            if (!Economy.CanAfford(cost))
            {
                Toast($"골드가 부족합니다 (에너지 강화 {cost} 골드)");
                return false;
            }
            if (!Upgrades.TryUpgradeEnergy(Generators.Energy, Economy))
                return false;

            Toast($"최대 에너지 +{UpgradeManager.EnergyStep}");
            StateChanged?.Invoke();
            return true;
        }

        public bool RequestCompleteOrder(string orderId)
        {
            CafeOrder order = Orders.Find(orderId);
            if (order == null)
                return false;

            int reward = order.rewardGold;
            if (!Orders.TryComplete(orderId, Board, Economy, IsTypeUnlocked))
                return false;

            OrderCompleted?.Invoke(order);
            Toast($"주문 완료! +{reward} 골드");
            StateChanged?.Invoke();
            return true;
        }

        internal void Toast(string message) => ToastRequested?.Invoke(message);
    }
}
