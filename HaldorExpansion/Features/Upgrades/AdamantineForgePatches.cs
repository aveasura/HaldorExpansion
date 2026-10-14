using System;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace HaldorExpansion.Features.Upgrades
{
    internal static class AdamantineForge
    {
        private static readonly MethodInfo Refresh = AccessTools.Method(typeof(InventoryGui), "UpdateCraftingPanel");

        internal static void Craft(InventoryGui gui, Player player, Recipe recipe, ItemDrop.ItemData item)
        {
            // Called exclusively for our alternative. Never fall through to destructive vanilla crafting.
            try
            {
                if (!AdamantineRefinement.TryRefine(player, recipe, item, UnityEngine.Random.Range(0f, 1f),
                        out bool success))
                {
                    player?.Message(MessageHud.MessageType.Center, "$msg_missingrequirement");
                    Refresh.Invoke(gui, new object[] { false });
                    return;
                }
                player.Message(MessageHud.MessageType.Center, Localization.instance.Localize(
                    success ? "$msg_upgrader_success" : "$msg_upgrader_failed",
                    Localization.instance.Localize(item.m_shared.m_name), item.m_quality.ToString()), 0, null, true);
                CraftingStation station = player.GetCurrentCraftingStation();
                (success ? station.m_craftItemDoneEffects : station.m_craftItemDoneFailEffects)
                    .Create(player.transform.position, Quaternion.identity);
                Refresh.Invoke(gui, new object[] { false });
            }
            catch (Exception error)
            {
                HaldorExpansionPlugin.Log?.LogError("[Adamantine Idol] Upgrade interrupted; vanilla fallback suppressed: " + error);
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "AddRecipeToList")]
    internal static class AdamantineRecipeLabelPatch
    {
        private static void Postfix(Recipe recipe, RectTransform ___m_recipeListRoot)
        {
            if (!AdamantineRefinement.IsAlternative(recipe) || ___m_recipeListRoot.childCount == 0) return;
            Transform row = ___m_recipeListRoot.GetChild(___m_recipeListRoot.childCount - 1);
            TMP_Text label = row.Find("name")?.GetComponent<TMP_Text>();
            if (label != null)
                label.text = "<color=#30D8E6>" + Localization.instance.Localize(AdamantineIdolRegistration.ForgeLabelKey) + "</color> " + label.text;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "UpdateRecipe")]
    internal static class AdamantineForgeWarningPatch
    {
        // RecipeDataPair is a private value type. Field injection as object produces
        // invalid IL on the game's Harmony/Mono runtime; reflection boxes it safely.
        private static readonly FieldInfo SelectedRecipe = typeof(InventoryGui).GetField(
            "m_selectedRecipe", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly PropertyInfo RecipeProperty = SelectedRecipe?.FieldType.GetProperty("Recipe");
        private static void Postfix(InventoryGui __instance, TMP_Text ___m_itemCraftType)
        {
            if (__instance == null || ___m_itemCraftType == null || RecipeProperty == null) return;
            object selected = SelectedRecipe.GetValue(__instance);
            Recipe recipe = RecipeProperty.GetValue(selected, null) as Recipe;
            if (AdamantineRefinement.IsAlternative(recipe))
                ___m_itemCraftType.text = Localization.instance.Localize(AdamantineIdolRegistration.ForgeWarningKey);
        }
    }
}
