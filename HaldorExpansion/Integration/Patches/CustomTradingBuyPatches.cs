using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HaldorExpansion.Trading;
using HarmonyLib;

namespace HaldorExpansion.Patches
{
    // The vanilla coin button calls GetSellableItem to find a valuable inventory stack.
    // Temporarily supply per-unit values while previewing and selling one stack.
    // Shallow-clone SharedData only for affected inventory item references so the
    // prefab and other items are NEVER mutated. Restore each reference in the finalizer.
    internal static class CustomTradingSellScope
    {
        private sealed class CloneHolder
        {
            internal readonly Dictionary<int, ItemDrop.ItemData.SharedData> ByPrice =
                new Dictionary<int, ItemDrop.ItemData.SharedData>();
        }

        private static readonly MethodInfo CloneShared = typeof(object).GetMethod("MemberwiseClone",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly ConditionalWeakTable<ItemDrop.ItemData.SharedData, CloneHolder> Clones =
            new ConditionalWeakTable<ItemDrop.ItemData.SharedData, CloneHolder>();

        private static readonly Dictionary<ItemDrop.ItemData, ItemDrop.ItemData.SharedData> Originals =
            new Dictionary<ItemDrop.ItemData, ItemDrop.ItemData.SharedData>();

        private static int depth;
        private static bool warned;

        internal static void SafeBegin(StoreGui gui)
        {
            try
            {
                Begin(gui);
            }
            catch (Exception error)
            {
                End();
                if (warned) return;
                warned = true;
                HaldorExpansionPlugin.Log?.LogWarning("[Custom trading] Sell preview fallback: " + error.Message);
            }
        }

        private static ItemDrop.ItemData.SharedData PricedClone(ItemDrop.ItemData.SharedData original, int price)
        {
            if (CloneShared == null) return null;
            CloneHolder holder = Clones.GetValue(original, _ => new CloneHolder());
            ItemDrop.ItemData.SharedData shared;
            if (!holder.ByPrice.TryGetValue(price, out shared))
            {
                shared = (ItemDrop.ItemData.SharedData)CloneShared.Invoke(original, null);
                shared.m_value = price;
                holder.ByPrice.Add(price, shared);
            }

            return shared;
        }

        internal static void Begin(StoreGui gui)
        {
            if (depth++ != 0) return;
            Originals.Clear();
            Trader trader = CustomTradingFillPatch.GetTrader(gui);
            if (!CustomBuyItems.HasOffers || !HaldorShopUpdater.IsHaldor(trader) ||
                Player.m_localPlayer == null) return;
            var inventory = Player.m_localPlayer.GetInventory();
            if (inventory == null) return;
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (item == null || item.m_shared == null || item.m_dropPrefab == null) continue;
                string id = item.m_dropPrefab.name;
                // No extra work for ordinary inventory items.
                int price;
                if (!CustomBuyItems.TryGetPrice(id, item.m_stack, out price)) continue;
                var clone = PricedClone(item.m_shared, price);
                if (clone == null) continue;
                Originals.Add(item, item.m_shared);
                item.m_shared = clone;
            }
        }

        internal static void End()
        {
            if (depth == 0) return;
            if (--depth != 0) return;
            foreach (var entry in Originals)
                entry.Key.m_shared = entry.Value;
            Originals.Clear();
        }
    }

    [HarmonyPatch(typeof(StoreGui), "UpdateSellButton")]
    internal static class CustomTradingPreviewPatch
    {
        private static void Prefix(StoreGui __instance) => CustomTradingSellScope.SafeBegin(__instance);

        private static Exception Finalizer(Exception __exception)
        {
            CustomTradingSellScope.End();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(StoreGui), "SellItem")]
    internal static class CustomTradingSellPatch
    {
        private static void Prefix(StoreGui __instance) => CustomTradingSellScope.SafeBegin(__instance);

        private static Exception Finalizer(Exception __exception)
        {
            CustomTradingSellScope.End();
            return __exception;
        }
    }
}