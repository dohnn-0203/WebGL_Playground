using System;
using System.Collections.Generic;
using MergeCafe.Core;
using MergeCafe.Data;
using MergeCafe.Generators;
using MergeCafe.Orders;
using UnityEngine;

namespace MergeCafe.Save
{
    /// <summary>
    /// PlayerPrefs-based persistence (webGL_game.md §14). The capture/apply pipeline
    /// is pure (json in/out) so the round trip is unit-testable; only Save/TryLoadInto/
    /// Delete touch PlayerPrefs (IndexedDB on WebGL).
    /// </summary>
    public static class SaveManager
    {
        public const string PrefsKey = "MergeCafe.Save.v1";

        // ---- Pure pipeline ----

        public static SaveData Capture(GameManager game)
        {
            var data = new SaveData
            {
                gold = game.Economy.Gold,
                unlockedCells = game.Board.GetUnlockedCells().ToArray(),
                expandedCellCount = game.Upgrades.ExpandedCellCount,
                orderCounter = game.Orders.OrderCounter
            };

            for (int i = 0; i < Board.BoardManager.CellCount; i++)
            {
                ItemInstance item = game.Board.GetItem(i);
                if (item != null)
                {
                    data.items.Add(new SavedItem
                    {
                        cellIndex = i,
                        itemType = (int)item.Type,
                        level = item.Level
                    });
                }
            }

            foreach (GeneratorState state in game.Generators.All)
            {
                data.generators.Add(new SavedGenerator
                {
                    itemType = (int)state.Definition.Output,
                    unlocked = state.Unlocked,
                    upgradeLevel = state.UpgradeLevel,
                    energy = state.Energy,
                    lastRecoveryUnix = state.LastRecoveryUnix
                });
            }

            foreach (CafeOrder order in game.Orders.Orders)
            {
                data.orders.Add(new SavedOrder
                {
                    orderId = order.orderId,
                    itemType = (int)order.requiredItemType,
                    level = order.requiredItemLevel,
                    rewardGold = order.rewardGold
                });
            }

            return data;
        }

        public static string ToJson(GameManager game)
        {
            return JsonUtility.ToJson(Capture(game));
        }

        public static bool TryParse(string json, out SaveData data)
        {
            data = null;
            if (string.IsNullOrEmpty(json))
                return false;

            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception)
            {
                return false;
            }

            if (data == null || data.version < 1 ||
                data.unlockedCells == null || data.unlockedCells.Length == 0 ||
                data.orders == null || data.orders.Count != OrderManager.OrderCount)
            {
                data = null;
                return false;
            }
            return true;
        }

        /// <summary>Restores the snapshot, then applies offline energy recovery up to nowUnix.</summary>
        public static void Apply(GameManager game, SaveData data, double nowUnix)
        {
            game.Economy.SetGold(data.gold);
            game.Upgrades.ExpandedCellCount = data.expandedCellCount;

            game.Board.ResetForLoad(data.unlockedCells);
            foreach (SavedItem saved in data.items)
            {
                var type = (ItemType)saved.itemType;
                if (ItemCatalog.IsValid(type, saved.level))
                    game.Board.TryPlaceItem(saved.cellIndex, new ItemInstance(type, saved.level));
            }

            foreach (SavedGenerator saved in data.generators)
            {
                var type = (ItemType)saved.itemType;
                GeneratorState state = game.Generators.Get(type);
                state.Unlocked = saved.unlocked || state.Definition.UnlockCost == 0;
                state.UpgradeLevel = Mathf.Clamp(saved.upgradeLevel, 1, GeneratorCatalog.MaxUpgradeLevel);
                state.Energy = Mathf.Clamp(saved.energy, 0, state.MaxEnergy);
                state.LastRecoveryUnix = saved.lastRecoveryUnix;
            }

            var orders = new List<CafeOrder>();
            foreach (SavedOrder saved in data.orders)
                orders.Add(new CafeOrder(saved.orderId, (ItemType)saved.itemType, saved.level, saved.rewardGold));
            game.Orders.LoadOrders(orders, data.orderCounter);

            // Energy earned while the page was closed (§11 회복 주기).
            game.Generators.Tick(nowUnix);
            game.Generators.RaiseStatesChanged();
        }

        // ---- PlayerPrefs layer ----

        public static bool HasSave() => PlayerPrefs.HasKey(PrefsKey);

        public static void Save(GameManager game)
        {
            PlayerPrefs.SetString(PrefsKey, ToJson(game));
            PlayerPrefs.Save();
        }

        /// <summary>Loads and applies the stored save. Returns false when absent/corrupt.</summary>
        public static bool TryLoadInto(GameManager game, double nowUnix)
        {
            if (!HasSave())
                return false;

            if (!TryParse(PlayerPrefs.GetString(PrefsKey), out SaveData data))
            {
                Debug.LogWarning("[MergeCafe] Corrupt save data ignored.");
                return false;
            }

            Apply(game, data, nowUnix);
            return true;
        }

        public static void Delete()
        {
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.Save();
        }
    }
}
