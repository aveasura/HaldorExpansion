using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HaldorExpansion.Configuration;

namespace HaldorExpansion.Trading
{
    // Custom rows own only their TradeItem instances, never prefab/shared item data.
    internal static class CustomShopItems
    {
        private sealed class OwnedEntry
        {
            internal string Category;
        }

        private static readonly ConditionalWeakTable<Trader.TradeItem, OwnedEntry> Owned =
            new ConditionalWeakTable<Trader.TradeItem, OwnedEntry>();

        private static readonly List<CustomTradeSetting> Settings = new List<CustomTradeSetting>();
        private static readonly HashSet<string> Warned = new HashSet<string>(StringComparer.Ordinal);
        private static ObjectDB warningDatabase;
        private static bool enabled = true;

        internal static void SetEnabled(bool value) => enabled = value;

        internal static void Apply(CustomTradingDocument document)
        {
            Settings.Clear();
            Warned.Clear();
            if (document == null) return;
            foreach (CustomTradeSetting row in document.Sells.Values)
            {
                if (row == null || string.IsNullOrEmpty(row.Id)) continue;
                Settings.Add(new CustomTradeSetting
                {
                    Id = row.Id, Price = row.Price, Amount = row.Amount, Enabled = row.Enabled,
                    RequiredKey = row.RequiredKey ?? string.Empty, Category = row.Category ?? "Auto"
                });
            }
        }

        // Run before the built-in shop refresh so its duplicate checks never see stale custom rows.
        internal static void RemoveOwned(Trader trader)
        {
            if (trader == null || trader.m_items == null) return;
            for (int i = trader.m_items.Count - 1; i >= 0; i--)
            {
                Trader.TradeItem item = trader.m_items[i];
                OwnedEntry entry;
                if (item == null || !Owned.TryGetValue(item, out entry)) continue;
                trader.m_items.RemoveAt(i);
                Owned.Remove(item);
            }
        }

        // Resolve on opening/refresh, after other mods have had an opportunity to register prefabs.
        // Missing lookups are deliberately not cached: late registration in the same ObjectDB works.
        internal static void Refresh(Trader trader)
        {
            if (!HaldorShopUpdater.IsHaldor(trader) || trader.m_items == null) return;
            RemoveOwned(trader);
            if (!enabled || Settings.Count == 0) return;
            ObjectDB database = ObjectDB.instance;
            if (database == null) return;
            if (!ReferenceEquals(warningDatabase, database))
            {
                warningDatabase = database;
                Warned.Clear();
            }

            // Build once instead of scanning Trader.m_items for every custom entry.
            var existingIds = new HashSet<string>(StringComparer.Ordinal);
            var existingPrefabs = new HashSet<ItemDrop>();
            foreach (Trader.TradeItem current in trader.m_items)
            {
                if (current == null || current.m_prefab == null) continue;
                existingIds.Add(current.m_prefab.name);
                existingPrefabs.Add(current.m_prefab);
            }

            foreach (CustomTradeSetting row in Settings)
            {
                if (!row.Enabled) continue;
                if (IsBuiltIn(row.Id))
                {
                    WarnBuiltIn(row.Id);
                    continue;
                }

                if (existingIds.Contains(row.Id))
                {
                    WarnExisting(row.Id);
                    continue;
                }

                var prefab = database.GetItemPrefab(row.Id);
                if (prefab == null)
                {
                    Warn(row.Id, "Prefab not found; custom shop entry skipped: " + row.Id);
                    continue;
                }

                ItemDrop drop = prefab.GetComponent<ItemDrop>();
                if (drop == null)
                {
                    Warn(row.Id, "ItemDrop missing on prefab; custom shop entry skipped: " + row.Id);
                    continue;
                }

                // Check the resolved identity too, in case a registration exposes an alias.
                if (IsBuiltIn(drop.name))
                {
                    WarnBuiltIn(row.Id);
                    continue;
                }

                if (existingIds.Contains(drop.name) || existingPrefabs.Contains(drop))
                {
                    WarnExisting(row.Id);
                    continue;
                }

                ItemDrop.ItemData.SharedData shared = drop.m_itemData != null ? drop.m_itemData.m_shared : null;
                if (shared == null)
                {
                    Warn(row.Id, "ItemDrop has missing item data or shared data; custom shop entry skipped: " + row.Id);
                    continue;
                }

                int maximum = Math.Max(1, shared.m_maxStackSize);
                var item = new Trader.TradeItem
                {
                    m_prefab = drop,
                    m_price = row.Price,
                    m_stack = Math.Max(1, Math.Min(row.Amount, maximum)),
                    m_requiredGlobalKey = row.RequiredKey,
                    m_levelUpEffect = false,
                    m_buyPlayerEffects = new EffectList(),
                    m_icon = null,
                    m_name = shared.m_name ?? string.Empty,
                    m_tooltip = ShopLoreCatalog.DescriptionFor(shared),
                    m_buyKey = string.Empty,
                    m_incrementKey = string.Empty,
                    m_incrementAmount = 0
                };
                trader.m_items.Add(item);
                existingIds.Add(drop.name);
                existingPrefabs.Add(drop);
                Owned.Add(item, new OwnedEntry { Category = row.Category });
            }
        }

        internal static bool TryGetCategory(Trader.TradeItem item, out ShopCategory category)
        {
            category = ShopCategory.All;
            OwnedEntry entry;
            if (item == null || !Owned.TryGetValue(item, out entry)) return false;
            switch (entry.Category)
            {
                case "Equipment":
                    category = ShopCategory.Equipment;
                    return true;
                case "Wood":
                    category = ShopCategory.Wood;
                    return true;
                case "Metals":
                    category = ShopCategory.Metals;
                    return true;
                case "Resources":
                    category = ShopCategory.Resources;
                    return true;
                case "Idols":
                    category = ShopCategory.Idols;
                    return true;
                case "Other":
                    category = ShopCategory.Other;
                    return true;
                default: return false; // Auto delegates to the existing classifier.
            }
        }

        private static bool IsBuiltIn(string id) =>
            id != null && (TradingCatalog.Defaults.Shop.ContainsKey(id) || HaldorIdolCatalog.IsIdol(id));

        private static void WarnBuiltIn(string id) =>
            Warn(id,
                "Custom shop entry conflicts with a built-in item: " + id + ". Configure it in trading.yml instead.");

        private static void WarnExisting(string id) =>
            Warn(id, "Custom shop entry already belongs to Haldor or another mod; existing entry preserved: " + id);

        private static void Warn(string id, string message)
        {
            if (Warned.Add(id)) HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] " + message);
        }
    }
}