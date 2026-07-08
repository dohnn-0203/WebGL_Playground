using System;
using MergeCafe.Board;
using MergeCafe.Data;
using MergeCafe.Economy;
using MergeCafe.Generators;
using MergeCafe.Items;
using MergeCafe.Orders;
using UnityEngine;

namespace MergeCafe.Core
{
    /// <summary>
    /// Central coordinator (pure C#): owns the systems and routes player intents
    /// to them, emitting UI-facing events (toasts, spawn feedback).
    /// </summary>
    public sealed class GameManager
    {
        public BoardManager Board { get; }
        public GeneratorManager Generators { get; }
        public EconomyManager Economy { get; }
        public OrderManager Orders { get; }
        public UpgradeManager Upgrades { get; }

        /// <summary>Short user-facing message (space/energy warnings...).</summary>
        public event Action<string> ToastRequested;

        /// <summary>Cell index that just received a freshly generated item.</summary>
        public event Action<int> ItemSpawned;

        /// <summary>Cell index holding the item that was just upgraded by a merge.</summary>
        public event Action<int> ItemMerged;

        /// <summary>(from, to) after a simple move to an empty cell.</summary>
        public event Action<int, int> ItemMoved;

        /// <summary>The order that was just delivered (reward already paid).</summary>
        public event Action<CafeOrder> OrderCompleted;

        public GameManager(double nowUnix, Func<float> rng01 = null)
        {
            Board = new BoardManager();
            Generators = new GeneratorManager(nowUnix, rng01);
            Economy = new EconomyManager();
            Orders = new OrderManager(rng01);
            Orders.SetupInitialOrders();
            Upgrades = new UpgradeManager();
        }

        public bool IsTypeUnlocked(ItemType type) => Generators.Get(type).Unlocked;

        public void Tick(double nowUnix)
        {
            Generators.Tick(nowUnix);
        }

        /// <summary>Player clicked a generator button (webGL_game.md §11 생성 규칙).</summary>
        public bool RequestSpawn(ItemType type, double nowUnix)
        {
            SpawnResult result = Generators.TrySpawn(type, Board, nowUnix);
            switch (result.Code)
            {
                case SpawnResultCode.Ok:
                    ItemSpawned?.Invoke(result.CellIndex);
                    return true;

                case SpawnResultCode.BoardFull:
                    Toast("보드 공간이 부족합니다");
                    return false;

                case SpawnResultCode.NoEnergy:
                    int seconds = Mathf.CeilToInt(
                        (float)Generators.Get(type).SecondsToNextRecovery(nowUnix));
                    Toast($"에너지가 부족합니다 ({seconds}초 후 +1)");
                    return false;

                default:
                    // Clicking a locked generator tries to unlock it with gold (§13).
                    return RequestUnlockGenerator(type);
            }
        }

        /// <summary>Spends gold to unlock the oven / fridge (webGL_game.md §11, §13).</summary>
        public bool RequestUnlockGenerator(ItemType type)
        {
            GeneratorState state = Generators.Get(type);
            if (state.Unlocked)
                return false;

            int cost = state.Definition.UnlockCost;
            if (!Economy.TrySpend(cost))
            {
                Toast($"골드가 부족합니다 (해금 {cost} 골드)");
                return false;
            }

            state.Unlocked = true;
            Generators.RaiseStatesChanged();
            Toast($"{state.Definition.DisplayName} 해금!");
            return true;
        }

        /// <summary>Unlocks the next locked board cell for gold (§13 보드 확장).</summary>
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
            return true;
        }

        /// <summary>Raises a generator's upgrade level for gold (§11 업그레이드 규칙).</summary>
        public bool RequestUpgradeGenerator(ItemType type)
        {
            GeneratorState state = Generators.Get(type);
            if (!state.Unlocked)
            {
                Toast("먼저 생성기를 해금하세요");
                return false;
            }

            if (state.UpgradeLevel >= GeneratorCatalog.MaxUpgradeLevel)
            {
                Toast("이미 최대 강화 상태입니다");
                return false;
            }

            int cost = GeneratorCatalog.UpgradeCost(state.UpgradeLevel + 1);
            if (!Economy.CanAfford(cost))
            {
                Toast($"골드가 부족합니다 (강화 {cost} 골드)");
                return false;
            }

            if (!Upgrades.TryUpgradeGenerator(state, Economy))
                return false;

            Generators.RaiseStatesChanged();
            Toast($"{state.Definition.DisplayName} 강화 완료 (Lv.{state.UpgradeLevel})");
            return true;
        }

        /// <summary>Player pressed the complete button of an order card (§12).</summary>
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
            return true;
        }

        /// <summary>Player dropped the item at fromIndex onto toIndex (webGL_game.md §9).</summary>
        public MoveOutcome RequestMove(int fromIndex, int toIndex)
        {
            MoveOutcome outcome = MergeResolver.Resolve(Board, fromIndex, toIndex);
            switch (outcome)
            {
                case MoveOutcome.Merged:
                    ItemMerged?.Invoke(toIndex);
                    break;
                case MoveOutcome.MovedToEmpty:
                    ItemMoved?.Invoke(fromIndex, toIndex);
                    break;
                case MoveOutcome.RejectedMaxLevel:
                    Toast("이미 최고 레벨입니다");
                    break;
            }
            return outcome;
        }

        internal void Toast(string message)
        {
            ToastRequested?.Invoke(message);
        }
    }
}
