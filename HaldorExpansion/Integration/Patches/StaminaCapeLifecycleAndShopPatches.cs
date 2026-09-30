using HaldorExpansion.Configuration;
using HaldorExpansion.Features.StaminaCape;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion
{
[HarmonyPatch(typeof(ObjectDB), "Awake")]
    internal static class StaminaCapeObjectDBAwakePatch
    {
        private static void Postfix()
        {
            StaminaCapeItemRegistration.ResetCaches();

            if (HaldorExpansionPlugin.Instance != null)
            {
                HaldorExpansionPlugin.Instance.StartCoroutine(DelayedEnsure());
            }
        }

        private static IEnumerator DelayedEnsure()
        {
            yield return null;

            yield return new WaitUntil(() =>
                ObjectDB.instance != null &&
                ObjectDB.instance.m_items != null &&
                ObjectDB.instance.m_items.Count > 0 &&
                ObjectDB.instance.m_recipes != null &&
                ObjectDB.instance.m_recipes.Count > 0);
            
            StaminaCapeItemRegistration.EnsureReady();
            StaminaCapeItemRegistration.RegisterRecipe();
        }
    }

    [HarmonyPatch(typeof(Player), "Update")]
    internal static class StaminaCapeInventoryFixPatch
    {
        private static readonly HashSet<int> ProcessedPlayers = new HashSet<int>();

        static void Postfix(Player __instance)
        {
            if (__instance == null || ObjectDB.instance == null)
                return;

            int id = __instance.GetInstanceID();
            if (ProcessedPlayers.Contains(id))
                return;

            StaminaCapeInventoryRebind.FixItemsInInventory(__instance);
            ProcessedPlayers.Add(id);
        }
    }

    [HarmonyPatch]
    internal static class HaldorStaminaCapeShopPatch
    {
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
            
            StaminaCapeItemRegistration.EnsureReady();
            TryAddCape(__instance);
        }

        private static void TryAddCape(Trader trader)
        {
            ItemDrop capeDrop = StaminaCapeItemRegistration.CapeItemDrop;
            if (capeDrop == null)
                return;

            if (HasPrefab(trader, StaminaCapeItemRegistration.PrefabName))
                return;

            AddTradeItem(trader, capeDrop, StaminaCapeItemRegistration.Price, 1);
            HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Stamina cape added to Haldor shop");
        }

        private static bool IsHaldor(Trader trader)
        {
            return trader != null
                   && trader.gameObject != null
                   && trader.gameObject.name.StartsWith("Haldor");
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