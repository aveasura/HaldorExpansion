using System.Reflection;
using HaldorExpansion.Trading;
using HarmonyLib;

namespace HaldorExpansion.Patches
{
    // Resolve custom stock after ordinary Interact/Show extensions have populated
    // the trader. Never recreate TradeItems during selection or purchase lookup.
    [HarmonyPatch(typeof(StoreGui), "FillList")]
    internal static class CustomTradingFillPatch
    {
        private static readonly FieldInfo TraderField = AccessTools.Field(typeof(StoreGui), "m_trader");

        internal static Trader GetTrader(StoreGui gui) =>
            gui != null && TraderField != null ? TraderField.GetValue(gui) as Trader : null;

        [HarmonyPriority(Priority.Last)]
        private static void Prefix(StoreGui __instance) =>
            CustomShopItems.Refresh(GetTrader(__instance));
    }

    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.Show))]
    internal static class CustomTradingShowPatch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(Trader __0)
        {
            if (HaldorShopUpdater.IsHaldor(__0)) CustomShopItems.RemoveOwned(__0);
        }
    }

    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.Hide))]
    internal static class CustomTradingHidePatch
    {
        // Read m_trader before the native Hide implementation can clear it.
        private static void Prefix(StoreGui __instance)
        {
            Trader trader = CustomTradingFillPatch.GetTrader(__instance);
            if (HaldorShopUpdater.IsHaldor(trader)) CustomShopItems.RemoveOwned(trader);
        }
    }
}