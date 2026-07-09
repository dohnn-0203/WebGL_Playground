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
        public const string PrefsKey = "MergeCafe.Save.v2";

        public static SaveData Capture(GameManager game)
        {
            var data = new SaveData
            {
                gold = game.Economy.Gold,
                unlockedCells = game.Board.GetUnlockedCells().ToArray(),
                expandedCellCount = game.Upgrades.ExpandedCellCount,
                energyUpgradeCount = game.Upgrades.EnergyUpgradeCount,
                orderCounter = game.Orders.OrderCounter,
                energy = new SavedEnergy
                {
                    current = game.Generators.Energy.Current,
                    max = game.Generators.Energy.Max,
                    lastRecoveryUnix = game.Generators.Energy.LastRecoveryUnix
                }
            };

            for (int i = 0; i < Board.BoardManager.CellCount; i++)
            {
                if (game.Board.HasGenerator(i))
                {
                    data.generators.Add(new SavedGenerator
                    {
                        cellIndex = i,
                        itemType = (int)game.Board.GetGenerator(i)
                    });
                    continue;
                }

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

        public static string ToJson(GameManager game) => JsonUtility.ToJson(Capture(game));

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

            if (data == null || data.version < 2 ||
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
            game.Upgrades.EnergyUpgradeCount = data.energyUpgradeCount;

            game.Board.ResetForLoad(data.unlockedCells);

            // Generators first (they occupy cells), then items into the free cells.
            foreach (SavedGenerator saved in data.generators)
                game.Board.TryPlaceGenerator(saved.cellIndex, (ItemType)saved.itemType);

            foreach (SavedItem saved in data.items)
            {
                var type = (ItemType)saved.itemType;
                if (ItemCatalog.IsValid(type, saved.level))
                    game.Board.TryPlaceItem(saved.cellIndex, new ItemInstance(type, saved.level));
            }

            EnergyPool pool = game.Generators.Energy;
            game.Upgrades.ApplyEnergyUpgradesTo(pool);
            if (data.energy != null && data.energy.max > 0)
                pool.Max = data.energy.max;
            pool.Current = Mathf.Clamp(data.energy != null ? data.energy.current : pool.Max, 0, pool.Max);
            pool.LastRecoveryUnix = data.energy != null ? data.energy.lastRecoveryUnix : nowUnix;

            var orders = new List<CafeOrder>();
            foreach (SavedOrder saved in data.orders)
                orders.Add(new CafeOrder(saved.orderId, (ItemType)saved.itemType, saved.level, saved.rewardGold));
            game.Orders.LoadOrders(orders, data.orderCounter);

            // Energy earned while the page was closed.
            game.Generators.Tick(nowUnix);
            game.Generators.RaiseStatesChanged();
        }

        public static bool HasSave() => PlayerPrefs.HasKey(PrefsKey);

        public static void Save(GameManager game)
        {
            PlayerPrefs.SetString(PrefsKey, ToJson(game));
            PlayerPrefs.Save();
        }

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
