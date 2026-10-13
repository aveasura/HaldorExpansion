using System.Collections.Generic;
using HarmonyLib;

namespace HaldorExpansion.Features.Upgrades
{
    // The forge localizes its outcome template, then inserts the raw item-name token
    // as a format argument. Resolve only our supported item names before substitution;
    // never replace SharedData.m_name, which is also used as an item identifier.
    [HarmonyPatch(typeof(Localization), nameof(Localization.Localize),
        new[] { typeof(string), typeof(string[]) })]
    internal static class UpgradeOutcomeItemNamePatch
    {
        private static void Prefix(string text, ref string[] words)
        {
            if (text != "$msg_upgrader_success" && text != "$msg_upgrader_broke" &&
                text != "$msg_upgrader_failed") return;
            if (words == null || words.Length < 2 ||
                !UpgradeRecipes.IsSupportedItemName(words[0]) || Localization.instance == null) return;

            string localized = Localization.instance.Localize(words[0]);
            if (string.IsNullOrEmpty(localized) || localized == words[0]) return;
            string[] resolved = (string[])words.Clone();
            resolved[0] = localized;
            words = resolved;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetAvailableRecipes))]
    internal static class UpgradeAvailableRecipesPatch
    {
        private static void Postfix(Player __instance, List<Recipe> __0)
        {
            UpgradeRecipes.AppendAvailable(__instance, __0);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
    internal static class UpgradeCraftingGuardPatch
    {
        private static bool Prefix(Player __0, Recipe ___m_craftRecipe, ItemDrop.ItemData ___m_craftUpgradeItem)
        {
            if (___m_craftRecipe == null || !UpgradeRecipes.IsSupported(___m_craftRecipe.m_item?.m_itemData))
                return true;
            // Guard the operation, not only the list: no-cost mode must not create free copies.
            return UpgradeRecipes.CanRefine(__0, ___m_craftRecipe, ___m_craftUpgradeItem);
        }
    }
}