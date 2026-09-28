using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Patches
{
    [HarmonyPatch]
    internal static class HaldorShopExpansionPatch
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

        static MethodBase TargetMethod()
        {
            MethodInfo fallback = null;

            MethodInfo[] methods =
                typeof(Trader).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (MethodInfo method in methods)
            {
                if (method.Name != "Interact")
                    continue;

                if (fallback == null)
                    fallback = method;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length >= 1 && typeof(Humanoid).IsAssignableFrom(parameters[0].ParameterType))
                    return method;
            }

            return fallback;
        }

        static void Prefix(Trader __instance)
        {
            if (!IsHaldor(__instance))
                return;

            if (__instance.m_items == null)
                __instance.m_items = new List<Trader.TradeItem>();

            // добавляет Хальдору обычные предметы из массива ShopItems
            TryAddConfiguredItems(__instance);
            
            // Аксессуары
            TryAddRing(__instance);
            
            // Плащи
            TryAddWoundedBeastCape(__instance);
            
            // Доспехи
            TryAddDelayedDoomChest(__instance);
            TryAddPitKingChest(__instance);
            
            // Оружие
            TryAddCestus(__instance);
            TryAddShadowCrossbow(__instance);
        }
        
        private static void TryAddWoundedBeastCape(Trader trader)
        {
            HaldorExpansionPlugin.EnsureWoundedBeastCapeReady();

            ItemDrop capeDrop = HaldorExpansionPlugin.WoundedBeastCapeItemDrop;
            if (capeDrop == null)
                return;

            if (HasPrefab(trader, HaldorExpansionPlugin.WoundedBeastCapePrefabName))
                return;

            AddTradeItem(trader, capeDrop, HaldorExpansionPlugin.WoundedBeastCapePrice, 1);
            HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Wounded Beast cape added to Haldor shop");
        }
        
        private static void TryAddShadowCrossbow(Trader trader)
        {
            HaldorExpansionPlugin.EnsureShadowCrossbowReady();

            ItemDrop crossbowDrop = HaldorExpansionPlugin.ShadowCrossbowItemDrop;
            if (crossbowDrop == null)
                return;

            if (HasPrefab(trader, HaldorExpansionPlugin.ShadowCrossbowPrefabName))
                return;

            AddTradeItem(trader, crossbowDrop, HaldorExpansionPlugin.ShadowCrossbowPrice, 1);
            HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Shadow crossbow added to Haldor shop");
        }
        
        private static void TryAddPitKingChest(Trader trader)
        {
            HaldorExpansionPlugin.EnsurePitKingChestReady();

            ItemDrop chestDrop = HaldorExpansionPlugin.PitKingChestItemDrop;
            if (chestDrop == null)
                return;

            if (HasPrefab(trader, HaldorExpansionPlugin.PitKingChestPrefabName))
                return;

            AddTradeItem(trader, chestDrop, HaldorExpansionPlugin.PitKingChestPrice, 1);
            HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Pit King chest added to Haldor shop");
        }
        
        private static void TryAddCestus(Trader trader)
        {
            HaldorExpansionPlugin.EnsureCestusReady();

            ItemDrop cestusDrop = HaldorExpansionPlugin.GritCestusItemDrop;
            if (cestusDrop == null)
                return;

            if (HasPrefab(trader, HaldorExpansionPlugin.GritCestusPrefabName))
                return;

            AddTradeItem(trader, cestusDrop, HaldorExpansionPlugin.GritCestusPrice, 1);
            HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Cestus added to Haldor shop");
        }

        private static void TryAddDelayedDoomChest(Trader trader)
        {
            HaldorExpansionPlugin.EnsureDelayedDoomChestReady();

            ItemDrop chestDrop = HaldorExpansionPlugin.DelayedDoomChestItemDrop;
            if (chestDrop == null)
                return;

            if (HasPrefab(trader, HaldorExpansionPlugin.DelayedDoomChestPrefabName))
                return;

            AddTradeItem(trader, chestDrop, HaldorExpansionPlugin.DelayedDoomChestPrice, 1);
            HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Delayed Doom chest added to Haldor shop");
        }

        private static bool IsHaldor(Trader trader)
        {
            return trader != null
                   && trader.gameObject != null
                   && trader.gameObject.name.StartsWith("Haldor");
        }

        private static void TryAddConfiguredItems(Trader trader)
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
            HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Added " + item.PrefabName + " x" + stack + " for " +
                                               item.Price + " coins");
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

        private static void TryAddRing(Trader trader)
        {
            HaldorExpansionPlugin.EnsureRingReady();

            ItemDrop ringDrop = HaldorExpansionPlugin.RingItemDrop;
            if (ringDrop == null)
                return;

            if (HasPrefab(trader, HaldorExpansionPlugin.RingPrefabName))
                return;

            AddTradeItem(trader, ringDrop, HaldorExpansionPlugin.RingPrice, 1);
            HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Ring added to Haldor shop");
        }

        private static bool HasPrefab(Trader trader, string prefabName)
        {
            string requiredGlobalKey = HaldorExpansionPlugin.GetShopRequiredGlobalKey(prefabName);

            foreach (Trader.TradeItem tradeItem in trader.m_items)
            {
                if (tradeItem.m_prefab != null && tradeItem.m_prefab.name == prefabName)
                {
                    // Keep already-added entries in sync as well. This matters when a
                    // server config is synchronized/changed after the TradeItem exists.
                    tradeItem.m_requiredGlobalKey = requiredGlobalKey;
                    return true;
                }
            }

            return false;
        }

        private static void AddTradeItem(Trader trader, ItemDrop itemDrop, int price, int stack)
        {
            ItemDrop.ItemData itemData = itemDrop != null ? itemDrop.m_itemData : null;
            ItemDrop.ItemData.SharedData shared = itemData != null ? itemData.m_shared : null;

            // Valheim 1.0 expanded Trader.TradeItem with UI/progression fields.
            // StoreGui.FillList expects these reference fields to be initialized;
            // leaving them null (the pre-1.0 behavior) causes a NullReferenceException.
            trader.m_items.Add(new Trader.TradeItem
            {
                m_prefab = itemDrop,
                m_price = price,
                m_stack = stack,
                m_requiredGlobalKey = HaldorExpansionPlugin.GetShopRequiredGlobalKey(
                    itemDrop != null ? itemDrop.name : string.Empty),
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