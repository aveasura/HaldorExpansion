using System;
using System.Collections.Generic;
using HaldorExpansion.Features.Berserker;
using HaldorExpansion.Features.Brisingamen;
using HaldorExpansion.Features.Cestus;
using HaldorExpansion.Features.DelayedDoom;
using HaldorExpansion.Features.HelOath;
using HaldorExpansion.Features.PeltOfHelheim;
using HaldorExpansion.Features.PitKing;
using HaldorExpansion.Features.ShadowCrossbow;
using HaldorExpansion.Features.StaminaCape;
using HaldorExpansion.Features.WoundedBeast;
using UnityEngine;

namespace HaldorExpansion.Features.Upgrades
{
    internal static class UpgradeRecipes
    {
        private static readonly string[] SupportedPrefabs =
        {
            BerserkerIds.HelmetPrefab,
            DelayedDoomItemRegistration.DelayedDoomChestPrefabName,
            PitKingItemRegistration.PitKingChestPrefabName,
            BrisingamenItemRegistration.RingPrefabName,
            BerserkerIds.AxePrefab,
            CestusItemRegistration.GritCestusPrefabName,
            HelOathItemRegistration.HelOathPrefabName,
            ShadowCrossbowItemRegistration.ShadowCrossbowPrefabName,
            BerserkerIds.CloakPrefab,
            PeltOfHelheimItemRegistration.PrefabName,
            WoundedBeastItemRegistration.WoundedBeastCapePrefabName,
            StaminaCapeItemRegistration.PrefabName
        };

        internal static bool IsSupported(ItemDrop.ItemData item)
        {
            GameObject prefab = item?.m_dropPrefab;
            if (prefab == null) return false;
            for (int i = 0; i < SupportedPrefabs.Length; i++)
                if (prefab.name == SupportedPrefabs[i])
                    return true;
            return false;
        }

        internal static bool IsSupportedItemName(string name)
        {
            if (string.IsNullOrEmpty(name) || ObjectDB.instance == null) return false;
            foreach (string prefabName in SupportedPrefabs)
            {
                ItemDrop drop = ObjectDB.instance.GetItemPrefab(prefabName)?.GetComponent<ItemDrop>();
                if (drop?.m_itemData?.m_shared?.m_name == name) return true;
            }

            return false;
        }

        internal static void Register()
        {
            ObjectDB db = ObjectDB.instance;
            if (db == null || IdolRegistration.ItemDrop == null) return;
            foreach (string name in SupportedPrefabs)
            {
                ItemDrop drop = db.GetItemPrefab(name)?.GetComponent<ItemDrop>();
                if (drop == null) continue;
                Recipe recipe = db.GetRecipe(drop.m_itemData);
                if (recipe == null)
                {
                    // Brisingamen has no ordinary crafting/repair recipe.
                    // Armor recipes must already exist so their repair station is preserved.
                    if (name != BrisingamenItemRegistration.RingPrefabName) continue;
                    recipe = ScriptableObject.CreateInstance<Recipe>();
                    recipe.name = "recipe_aveasura_he_brisingamen_refinement";
                    recipe.m_item = drop;
                    db.m_recipes.Add(recipe);
                }

                recipe.m_enabled = false;
                recipe.m_noCraftOnlyUpgrade = true;
                recipe.m_resources = new[]
                {
                    new Piece.Requirement
                    {
                        m_resItem = IdolRegistration.ItemDrop,
                        m_amount = 1,
                        m_amountPerLevel = 0,
                        m_upgraderResource = true,
                        m_recover = false
                    }
                };
            }
        }

        internal static void AppendAvailable(Player player, List<Recipe> available)
        {
            if (player == null || available == null || player.GetCurrentCraftingStation()?.m_upgrader != true)
                return;
            ObjectDB db = ObjectDB.instance;
            if (db == null || IdolRegistration.ItemDrop == null) return;
            foreach (ItemDrop.ItemData item in player.GetInventory().GetAllItems())
            {
                if (!IsSupported(item) || !MatchesFilter(item)) continue;
                Recipe recipe = db.GetRecipe(item);
                if (!IsReady(recipe) || available.Contains(recipe)) continue;
                available.Add(recipe);
            }
        }

        private static bool MatchesFilter(ItemDrop.ItemData item)
        {
            if (Player.s_FilterCraft.Count == 0) return true;
            string localized = Localization.instance?.Localize(item.m_shared.m_name) ?? item.m_shared.m_name;
            foreach (string filter in Player.s_FilterCraft)
            {
                if (string.IsNullOrEmpty(filter)) continue;
                if (item.m_dropPrefab.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.m_shared.m_name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    localized.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }

            return false;
        }

        private static bool IsReady(Recipe recipe)
        {
            return recipe != null && recipe.m_resources != null && recipe.m_resources.Length == 1 &&
                   recipe.m_resources[0].m_upgraderResource &&
                   recipe.m_resources[0].m_resItem == IdolRegistration.ItemDrop;
        }

        internal static bool CanRefine(Player player, Recipe recipe, ItemDrop.ItemData item)
        {
            return player != null && player.GetCurrentCraftingStation()?.m_upgrader == true &&
                   IsReady(recipe) && IsSupported(item) && player.GetInventory().ContainsItem(item) &&
                   recipe.m_item.m_itemData.m_shared.m_name == item.m_shared.m_name;
        }
    }
}