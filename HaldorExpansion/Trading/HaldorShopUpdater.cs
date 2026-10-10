using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HaldorExpansion.Configuration;
using HaldorExpansion.Features.Berserker;
using HaldorExpansion.Features.Brisingamen;
using HaldorExpansion.Features.Cestus;
using HaldorExpansion.Features.DelayedDoom;
using HaldorExpansion.Features.HelOath;
using HaldorExpansion.Features.NornThread;
using HaldorExpansion.Features.PeltOfHelheim;
using HaldorExpansion.Features.PitKing;
using HaldorExpansion.Features.ShadowCrossbow;
using HaldorExpansion.Features.StaminaCape;
using HaldorExpansion.Features.WoundedBeast;
using UnityEngine;

namespace HaldorExpansion.Trading
{
    internal static class HaldorShopUpdater
    {
        internal static void TryAddStaminaCape(Trader trader)
        {
            StaminaCapeItemRegistration.EnsureReady();
            var drop = StaminaCapeItemRegistration.CapeItemDrop;
            if (drop == null || HasPrefab(trader, StaminaCapeItemRegistration.PrefabName)) return;
            AddTradeItem(trader, drop, StaminaCapeItemRegistration.Price, 1);
        }

        internal static void TryAddBerserkerAxe(Trader trader)
        {
            BerserkerItemRegistration.EnsureReady();

            ItemDrop drop = BerserkerItemRegistration.ItemDrop;
            if (drop == null) return;
            if (HasPrefab(trader, BerserkerIds.AxePrefab)) return;

            AddTradeItem(trader, drop, BerserkerItemRegistration.Price, 1);
            HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Berserker axe added to Haldor shop");
        }

        internal static void TryAddBerserkerCloak(Trader trader)
        {
            BerserkerCloakItemRegistration.EnsureReady();

            ItemDrop drop = BerserkerCloakItemRegistration.ItemDrop;
            if (drop == null) return;
            if (HasPrefab(trader, BerserkerIds.CloakPrefab)) return;

            AddTradeItem(trader, drop, BerserkerCloakItemRegistration.Price, 1);
            HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Berserker cloak added to Haldor shop");
        }

        internal static void TryAddBerserkerHelmet(Trader trader)
        {
            BerserkerHelmetItemRegistration.EnsureReady();

            ItemDrop drop = BerserkerHelmetItemRegistration.ItemDrop;
            if (drop == null) return;
            if (HasPrefab(trader, BerserkerIds.HelmetPrefab)) return;

            AddTradeItem(trader, drop, BerserkerHelmetItemRegistration.Price, 1);
            HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Berserker helmet added to Haldor shop");
        }

        internal static void TryAddPeltOfHelheim(Trader trader)
        {
            PeltOfHelheimItemRegistration.EnsureReady();

            ItemDrop drop = PeltOfHelheimItemRegistration.CapeItemDrop;
            if (drop == null) return;
            if (HasPrefab(trader, PeltOfHelheimItemRegistration.PrefabName)) return;

            AddTradeItem(trader, drop, PeltOfHelheimItemRegistration.Price, 1);
            HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Pelt of Helheim added to Haldor shop");
        }

        internal static void TryAddWoundedBeastCape(Trader trader)
        {
            WoundedBeastItemRegistration.EnsureWoundedBeastCapeReady();

            ItemDrop capeDrop = WoundedBeastItemRegistration.WoundedBeastCapeItemDrop;
            if (capeDrop == null)
                return;

            if (HasPrefab(trader, WoundedBeastItemRegistration.WoundedBeastCapePrefabName))
                return;

            AddTradeItem(trader, capeDrop, WoundedBeastItemRegistration.WoundedBeastCapePrice, 1);
            HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Wounded Beast cape added to Haldor shop");
        }

        internal static void TryAddHelOath(Trader trader)
        {
            HelOathItemRegistration.EnsureHelOathReady();

            ItemDrop crossbowDrop = HelOathItemRegistration.HelOathItemDrop;
            if (crossbowDrop == null)
                return;

            if (HasPrefab(trader, HelOathItemRegistration.HelOathPrefabName))
                return;

            AddTradeItem(trader, crossbowDrop, HelOathItemRegistration.HelOathPrice, 1);
            HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Shadow crossbow added to Haldor shop");
        }

        internal static void TryAddShadowCrossbow(Trader trader)
        {
            ShadowCrossbowItemRegistration.EnsureShadowCrossbowReady();

            ItemDrop crossbowDrop = ShadowCrossbowItemRegistration.ShadowCrossbowItemDrop;
            if (crossbowDrop == null)
                return;

            if (HasPrefab(trader, ShadowCrossbowItemRegistration.ShadowCrossbowPrefabName))
                return;

            AddTradeItem(trader, crossbowDrop, ShadowCrossbowItemRegistration.ShadowCrossbowPrice, 1);
            HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Shadow crossbow added to Haldor shop");
        }

        internal static void TryAddPitKingChest(Trader trader)
        {
            PitKingItemRegistration.EnsurePitKingChestReady();

            ItemDrop chestDrop = PitKingItemRegistration.PitKingChestItemDrop;
            if (chestDrop == null)
                return;

            if (HasPrefab(trader, PitKingItemRegistration.PitKingChestPrefabName))
                return;

            AddTradeItem(trader, chestDrop, PitKingItemRegistration.PitKingChestPrice, 1);
            HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Pit King chest added to Haldor shop");
        }

        internal static void TryAddCestus(Trader trader)
        {
            CestusItemRegistration.EnsureCestusReady();

            ItemDrop cestusDrop = CestusItemRegistration.GritCestusItemDrop;
            if (cestusDrop == null)
                return;

            if (HasPrefab(trader, CestusItemRegistration.GritCestusPrefabName))
                return;

            AddTradeItem(trader, cestusDrop, CestusItemRegistration.GritCestusPrice, 1);
            HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Cestus added to Haldor shop");
        }

        internal static void TryAddDelayedDoomChest(Trader trader)
        {
            DelayedDoomItemRegistration.EnsureDelayedDoomChestReady();

            ItemDrop chestDrop = DelayedDoomItemRegistration.DelayedDoomChestItemDrop;
            if (chestDrop == null)
                return;

            if (HasPrefab(trader, DelayedDoomItemRegistration.DelayedDoomChestPrefabName))
                return;

            AddTradeItem(trader, chestDrop, DelayedDoomItemRegistration.DelayedDoomChestPrice, 1);
            HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Delayed Doom chest added to Haldor shop");
        }

        internal static bool IsHaldor(Trader trader)
        {
            return trader != null
                   && trader.gameObject != null
                   && trader.gameObject.name.StartsWith("Haldor");
        }

        internal static void TryAddConfiguredItems(Trader trader)
        {
            if (ObjectDB.instance == null)
                return;

            foreach (TradeSetting item in TradingCatalog.Defaults.Shop.Values)
            {
                if (item.Category != "Shop Progression - Custom Items") TryAddConfiguredItem(trader, item);
            }
        }

        private static void TryAddConfiguredItem(Trader trader, TradeSetting item)
        {
            if (ObjectDB.instance == null)
                return;

            if (HasPrefab(trader, item.Id))
                return;

            GameObject prefab = ObjectDB.instance.GetItemPrefab(item.Id);
            if (prefab == null)
            {
                HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Prefab not found: " + item.Id);
                return;
            }

            ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
            if (itemDrop == null)
            {
                HaldorExpansionPlugin.Log?.LogWarning(
                    "[HaldorExpansion] ItemDrop missing on prefab: " + item.Id);
                return;
            }

            int stack = ResolveStack(itemDrop, item);

            AddTradeItem(trader, itemDrop, item.Price, stack);
            int configuredPrice = ShopConfiguration.GetShopPrice(item.Id, item.Price);
            HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Added " + item.Id + " x" + stack + " for " +
                                           configuredPrice + " coins");
        }

        private static int ResolveStack(ItemDrop itemDrop, TradeSetting item)
        {
            if (item.Stack != 0)
                return item.Stack;

            if (itemDrop.m_itemData != null && itemDrop.m_itemData.m_shared != null)
            {
                int maxStack = itemDrop.m_itemData.m_shared.m_maxStackSize;
                if (maxStack > 0)
                    return maxStack;
            }

            return 1;
        }

        internal static void TryAddRing(Trader trader)
        {
            BrisingamenItemRegistration.EnsureRingReady();

            ItemDrop ringDrop = BrisingamenItemRegistration.RingItemDrop;
            if (ringDrop == null)
                return;

            if (HasPrefab(trader, BrisingamenItemRegistration.RingPrefabName))
                return;

            AddTradeItem(trader, ringDrop, BrisingamenItemRegistration.RingPrice, 1);
            HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Ring added to Haldor shop");
        }

        internal static void TryAddNornThread(Trader trader)
        {
            NornThreadItemRegistration.EnsureReady();

            ItemDrop drop = NornThreadItemRegistration.NornThreadItemDrop;
            if (drop == null)
                return;

            if (HasPrefab(trader, NornThreadItemRegistration.PrefabName))
                return;

            AddTradeItem(trader, drop, NornThreadItemRegistration.Price, 1);
            HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Thread of the Norns added to Haldor shop");
        }

        // Only remove entries created by this mod; other traders/mods keep ownership.
        private static readonly ConditionalWeakTable<Trader.TradeItem, object> Owned =
            new ConditionalWeakTable<Trader.TradeItem, object>();

        internal static bool IsOwned(Trader.TradeItem item)
        {
            object value;
            return Owned.TryGetValue(item, out value);
        }

        internal static void RemoveDisabledItems(Trader trader)
        {
            trader.m_items.RemoveAll(item => IsOwned(item) && item.m_prefab != null &&
                                             !TradingCatalog.Enabled(item.m_prefab.name));
        }

        // The fixed catalog order is independent of when an item was re-enabled.
        // Keep non-HaldorExpansion entries in their own order and never sort their internals.
        internal static void RestoreOrder(Trader trader)
        {
            var positions = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (TradeSetting item in TradingCatalog.Defaults.Shop.Values)
                if (item.Category != "Shop Progression - Custom Items" && !positions.ContainsKey(item.Id))
                    positions.Add(item.Id, positions.Count);

            foreach (string prefab in new[]
                     {
                         BrisingamenItemRegistration.RingPrefabName, NornThreadItemRegistration.PrefabName,
                         StaminaCapeItemRegistration.PrefabName,
                         WoundedBeastItemRegistration.WoundedBeastCapePrefabName,
                         PeltOfHelheimItemRegistration.PrefabName, BerserkerIds.CloakPrefab,
                         BerserkerIds.HelmetPrefab,
                         DelayedDoomItemRegistration.DelayedDoomChestPrefabName,
                         PitKingItemRegistration.PitKingChestPrefabName, BerserkerIds.AxePrefab,
                         CestusItemRegistration.GritCestusPrefabName,
                         ShadowCrossbowItemRegistration.ShadowCrossbowPrefabName,
                         HelOathItemRegistration.HelOathPrefabName
                     })
                if (!positions.ContainsKey(prefab))
                    positions.Add(prefab, positions.Count);

            var other = new List<Trader.TradeItem>();
            var owned = new List<Trader.TradeItem>();
            int firstOwned = -1;
            foreach (Trader.TradeItem item in trader.m_items)
            {
                if (IsOwned(item))
                {
                    if (firstOwned < 0) firstOwned = other.Count;
                    owned.Add(item);
                }
                else other.Add(item);
            }

            if (owned.Count < 2) return;

            owned.Sort((left, right) =>
            {
                int l, r;
                if (!positions.TryGetValue(left.m_prefab != null ? left.m_prefab.name : "", out l)) l = int.MaxValue;
                if (!positions.TryGetValue(right.m_prefab != null ? right.m_prefab.name : "", out r)) r = int.MaxValue;
                return l.CompareTo(r);
            });
            other.InsertRange(firstOwned, owned);
            for (int i = 0; i < other.Count; i++)
                if (!ReferenceEquals(other[i], trader.m_items[i]))
                {
                    trader.m_items.Clear();
                    trader.m_items.AddRange(other);
                    break;
                }
        }

        private static bool HasPrefab(Trader trader, string prefabName)
        {
            if (!TradingCatalog.Enabled(prefabName)) return true;
            string requiredGlobalKey = ShopConfiguration.GetShopRequiredGlobalKey(prefabName);

            foreach (Trader.TradeItem tradeItem in trader.m_items)
            {
                if (tradeItem.m_prefab != null && tradeItem.m_prefab.name == prefabName)
                {
                    // Keep already-added entries in sync as well. This matters when a
                    // server config is synchronized/changed after the TradeItem exists.
                    tradeItem.m_requiredGlobalKey = requiredGlobalKey;
                    tradeItem.m_price = ShopConfiguration.GetShopPrice(prefabName, tradeItem.m_price);
                    tradeItem.m_stack = TradingCatalog.Stack(prefabName, tradeItem.m_stack,
                        tradeItem.m_prefab.m_itemData.m_shared.m_maxStackSize);
                    return true;
                }
            }

            return false;
        }

        private static void AddTradeItem(Trader trader, ItemDrop itemDrop, int price, int stack)
        {
            ItemDrop.ItemData itemData = itemDrop != null ? itemDrop.m_itemData : null;
            ItemDrop.ItemData.SharedData shared = itemData != null ? itemData.m_shared : null;
            string prefabName = itemDrop != null ? itemDrop.name : string.Empty;

            // Valheim 1.0 expanded Trader.TradeItem with UI/progression fields.
            // StoreGui.FillList expects these reference fields to be initialized;
            // leaving them null (the pre-1.0 behavior) causes a NullReferenceException.
            var tradeItem = new Trader.TradeItem
            {
                m_prefab = itemDrop,
                m_price = ShopConfiguration.GetShopPrice(prefabName, price),
                m_stack = TradingCatalog.Stack(prefabName, stack, shared != null ? shared.m_maxStackSize : stack),
                m_requiredGlobalKey = ShopConfiguration.GetShopRequiredGlobalKey(prefabName),
                m_levelUpEffect = false,
                m_buyPlayerEffects = new EffectList(),
                m_icon = null,
                m_name = shared != null ? shared.m_name ?? string.Empty : string.Empty,
                m_tooltip = shared != null ? shared.m_description ?? string.Empty : string.Empty,
                m_buyKey = string.Empty,
                m_incrementKey = string.Empty,
                m_incrementAmount = 0
            };
            trader.m_items.Add(tradeItem);
            Owned.Add(tradeItem, new object());
        }
    }
}