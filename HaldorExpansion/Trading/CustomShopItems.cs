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
            internal string Id;
            internal string Category;
        }

        private static readonly ConditionalWeakTable<Trader.TradeItem, OwnedEntry> Owned =
            new ConditionalWeakTable<Trader.TradeItem, OwnedEntry>();

        // Only bridge the full built-in refresh to the next FillList. Weak trader
        // keys avoid retaining traders/worlds if the store never fills again.
        private static readonly ConditionalWeakTable<Trader, Dictionary<string, Trader.TradeItem>> Pending =
            new ConditionalWeakTable<Trader, Dictionary<string, Trader.TradeItem>>();

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

        // Detach before built-in duplicate checks, but preserve matching instances
        // for the next FillList. Repeated refreshes must keep the same pending rows.
        internal static void RemoveOwnedForRefresh(Trader trader)
        {
            if (trader == null) return;
            var reusable = CollectReusable(trader);
            RemoveOwned(trader);
            if (reusable.Count != 0) Pending.Add(trader, reusable);
        }

        private static Dictionary<string, Trader.TradeItem> CollectReusable(Trader trader)
        {
            Dictionary<string, Trader.TradeItem> reusable;
            if (!Pending.TryGetValue(trader, out reusable))
                reusable = new Dictionary<string, Trader.TradeItem>(StringComparer.Ordinal);
            if (trader.m_items == null) return reusable;
            foreach (Trader.TradeItem current in trader.m_items)
            {
                OwnedEntry owner;
                if (current != null && Owned.TryGetValue(current, out owner) &&
                    !string.IsNullOrEmpty(owner.Id))
                    reusable[owner.Id] = current;
            }

            return reusable;
        }

        // Full cleanup for Hide/Show and after collecting candidates in Refresh.
        internal static void RemoveOwned(Trader trader)
        {
            if (trader == null) return;
            Pending.Remove(trader);
            if (trader.m_items == null) return;
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
            // StoreGui.FillList is called again after a purchase. Valheim keeps the
            // selected TradeItem by reference, so recreating our TradeItems here
            // would silently move selection to a different row.
            // Keep the previous instances for matching IDs, but still remove our
            // entries before checking foreign/built-in stock for collisions.
            var reusable = CollectReusable(trader);

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
                Trader.TradeItem item;
                // Preserve object identity across repeated FillList calls (including
                // the one triggered by Buy). If an alias now resolves to another
                // prefab, replace the old row instead of keeping the wrong item.
                if (reusable.TryGetValue(row.Id, out item) && ReferenceEquals(item.m_prefab, drop))
                {
                    item.m_price = row.Price;
                    item.m_stack = Math.Max(1, Math.Min(row.Amount, maximum));
                    item.m_requiredGlobalKey = row.RequiredKey;
                    item.m_name = shared.m_name ?? string.Empty;
                    item.m_tooltip = ShopLoreCatalog.DescriptionFor(shared);
                }
                else
                {
                    item = new Trader.TradeItem
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
                }

                trader.m_items.Add(item);
                existingIds.Add(drop.name);
                existingPrefabs.Add(drop);
                Owned.Add(item, new OwnedEntry { Id = row.Id, Category = row.Category });
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