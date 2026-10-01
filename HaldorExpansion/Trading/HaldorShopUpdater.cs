using HaldorExpansion.Features.HelOath;
using HaldorExpansion.Configuration;
using HaldorExpansion.Features.Brisingamen;
using HaldorExpansion.Features.NornThread;
using HaldorExpansion.Features.Cestus;
using HaldorExpansion.Features.DelayedDoom;
using HaldorExpansion.Features.PitKing;
using HaldorExpansion.Features.ShadowCrossbow;
using HaldorExpansion.Features.WoundedBeast;
using UnityEngine;

namespace HaldorExpansion.Trading
{
    internal static class HaldorShopUpdater
    {
        private sealed class ShopItemDefinition
        {
            public string PrefabName { get; private set; }
            public int Price { get; private set; }
            public int Stack { get; private set; }
            public bool UseMaxStack { get; private set; }

            public ShopItemDefinition(string prefabName, int price, int stack, bool useMaxStack = false)
            {
                PrefabName = prefabName;
                Price = price;
                Stack = stack;
                UseMaxStack = useMaxStack;
            }
        }

        private static readonly ShopItemDefinition[] ShopItems =
        {
            new ShopItemDefinition("Iron", 225, 30),
            new ShopItemDefinition("Copper", 310, 30),
            new ShopItemDefinition("Tin", 180, 30),
            new ShopItemDefinition("Silver", 550, 30),
            new ShopItemDefinition("Flametal", 800, 30),

            new ShopItemDefinition("Stone", 20, 1, true),
            new ShopItemDefinition("Grausten", 25, 1, true),
            new ShopItemDefinition("BlackMarble", 40, 1, true),

            new ShopItemDefinition("Wood", 10, 1, true),
            new ShopItemDefinition("RoundLog", 10, 1, true),
            new ShopItemDefinition("ElderBark", 30, 1, true),
            new ShopItemDefinition("FineWood", 150, 1, true),
            new ShopItemDefinition("YggdrasilWood", 250, 1, true),
            new ShopItemDefinition("Ashwood", 30, 1, true),
            new ShopItemDefinition("Blackwood", 100, 1, true),
            new ShopItemDefinition("CharredWood", 50, 1, true),

            new ShopItemDefinition("JuteRed", 250, 1, true),
            new ShopItemDefinition("JuteBlue", 250, 1, true),

            new ShopItemDefinition("Resin", 100, 1, true),
            new ShopItemDefinition("Feathers", 150, 100),
            new ShopItemDefinition("GreydwarfEye", 200, 1, true),
            new ShopItemDefinition("Tar", 250, 1, true),
            new ShopItemDefinition("Crystal", 250, 1, true),
            new ShopItemDefinition("Needle", 250, 1, true)
        };
        
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

            foreach (ShopItemDefinition item in ShopItems)
            {
                TryAddConfiguredItem(trader, item);
            }
        }

        private static void TryAddConfiguredItem(Trader trader, ShopItemDefinition item)
        {
            if (ObjectDB.instance == null)
                return;

            if (HasPrefab(trader, item.PrefabName))
                return;

            GameObject prefab = ObjectDB.instance.GetItemPrefab(item.PrefabName);
            if (prefab == null)
            {
                HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Prefab not found: " + item.PrefabName);
                return;
            }

            ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
            if (itemDrop == null)
            {
                HaldorExpansionPlugin.Log?.LogWarning(
                    "[HaldorExpansion] ItemDrop missing on prefab: " + item.PrefabName);
                return;
            }

            int stack = ResolveStack(itemDrop, item);

            AddTradeItem(trader, itemDrop, item.Price, stack);
            int configuredPrice = ShopConfiguration.GetShopPrice(item.PrefabName, item.Price);
            HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Added " + item.PrefabName + " x" + stack + " for " +
                                               configuredPrice + " coins");
        }

        private static int ResolveStack(ItemDrop itemDrop, ShopItemDefinition item)
        {
            if (!item.UseMaxStack)
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

        private static bool HasPrefab(Trader trader, string prefabName)
        {
            string requiredGlobalKey = ShopConfiguration.GetShopRequiredGlobalKey(prefabName);

            foreach (Trader.TradeItem tradeItem in trader.m_items)
            {
                if (tradeItem.m_prefab != null && tradeItem.m_prefab.name == prefabName)
                {
                    // Keep already-added entries in sync as well. This matters when a
                    // server config is synchronized/changed after the TradeItem exists.
                    tradeItem.m_requiredGlobalKey = requiredGlobalKey;
                    tradeItem.m_price = ShopConfiguration.GetShopPrice(prefabName, tradeItem.m_price);
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
            trader.m_items.Add(new Trader.TradeItem
            {
                m_prefab = itemDrop,
                m_price = ShopConfiguration.GetShopPrice(prefabName, price),
                m_stack = stack,
                m_requiredGlobalKey = ShopConfiguration.GetShopRequiredGlobalKey(prefabName),
                m_levelUpEffect = false,
                m_buyPlayerEffects = new EffectList(),
                m_icon = null,
                m_name = shared != null ? shared.m_name ?? string.Empty : string.Empty,
                m_tooltip = shared != null ? shared.m_description ?? string.Empty : string.Empty,
                m_buyKey = string.Empty,
                m_incrementKey = string.Empty,
                m_incrementAmount = 0
            });
        }
    }
}
