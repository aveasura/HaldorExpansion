using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace HaldorExpansion.Features.Upgrades
{
    // Local UI alternatives only: never replace or register a recipe in ObjectDB.
    internal static class AdamantineRefinement
    {
        private static ObjectDB database;
        private static readonly Dictionary<Recipe, Recipe> Alternatives = new Dictionary<Recipe, Recipe>();
        private static readonly Dictionary<Recipe, Recipe> Originals = new Dictionary<Recipe, Recipe>();
        private static bool refining;

        internal static bool IsAlternative(Recipe recipe) => recipe != null && Originals.ContainsKey(recipe);

        private static bool Eligible(Recipe recipe)
        {
            if (recipe?.m_item?.m_itemData?.m_shared == null || recipe.m_item.m_itemData.m_shared.m_maxQuality <= 1 ||
                recipe.m_resources == null || ObjectDB.instance == null ||
                !ObjectDB.instance.m_recipes.Contains(recipe)) return false;
            // Respect existing disabled-recipe rules, including HE's upgrade-only artifacts.
            if (!recipe.m_enabled && !UpgradeRecipes.IsSupported(recipe.m_item.m_itemData)) return false;
            foreach (Piece.Requirement requirement in recipe.m_resources)
                if (requirement?.m_resItem != null && requirement.m_upgraderResource &&
                    (requirement.m_resItem == IdolRegistration.ItemDrop ||
                     IdolRegistration.IsNativeIdol(requirement.m_resItem.gameObject)))
                    return true;
            return false;
        }

        internal static void AppendAvailable(Player player, List<Recipe> available)
        {
            if (player == null || available == null || player.GetCurrentCraftingStation()?.m_upgrader != true ||
                AdamantineIdolRegistration.ItemDrop == null) return;
            if (database != ObjectDB.instance)
            {
                foreach (Recipe variant in Originals.Keys) Object.Destroy(variant);
                Originals.Clear();
                Alternatives.Clear();
                database = ObjectDB.instance;
            }

            // Only recipes already offered by Valheim/HE acquire an alternative. Keeping the
            // row without an idol makes the cost visible and avoids silently switching choices.
            int count = available.Count;
            for (int i = 0; i < count; i++)
            {
                Recipe source = available[i];
                if (!Eligible(source)) continue;
                if (!Alternatives.TryGetValue(source, out Recipe variant))
                {
                    variant = Object.Instantiate(source);
                    variant.name = source.name + "_he_adamantine";
                    variant.m_enabled = false;
                    variant.m_noCraftOnlyUpgrade = true;
                    variant.m_amount = 1;
                    variant.m_requireOnlyOneIngredient = false;
                    variant.m_resources = new[]
                    {
                        new Piece.Requirement
                        {
                            m_resItem = AdamantineIdolRegistration.ItemDrop,
                            m_amount = 1, m_amountPerLevel = 0, m_upgraderResource = true, m_recover = false
                        }
                    };
                    Alternatives.Add(source, variant);
                    Originals.Add(variant, source);
                }

                if (!available.Contains(variant)) available.Add(variant);
            }
        }

        internal static bool TryRefine(Player player, Recipe recipe, ItemDrop.ItemData item, float roll,
            out bool success)
        {
            success = false;
            if (refining || player == null || item == null || !IsAlternative(recipe) ||
                player.GetCurrentCraftingStation()?.m_upgrader != true || !Eligible(Originals[recipe]) ||
                item.m_dropPrefab != Originals[recipe].m_item.gameObject || item.m_quality < 1 ||
                item.m_quality == int.MaxValue || !player.GetInventory().ContainsItem(item) ||
                !player.HaveRequirements(recipe, false, item.m_quality + 1)) return false;
            ItemDrop.ItemData idol = null;
            foreach (ItemDrop.ItemData candidate in player.GetInventory().GetAllItems())
                if (candidate?.m_dropPrefab == AdamantineIdolRegistration.Prefab && candidate.m_stack > 0 &&
                    candidate.m_worldLevel >= Game.m_worldLevel)
                {
                    idol = candidate;
                    break;
                }

            if (idol == null || ReferenceEquals(idol, item)) return false;

            int previous = item.m_quality;
            refining = true;
            try
            {
                // The native comparison and the registered idol's live chance; no second RNG roll.
                success = AdamantineIdolRegistration.ItemDrop.m_itemData.m_shared.m_upgradeChance >= roll;
                item.m_quality = success ? previous + 1 : Math.Max(1, previous - 1);
                // Keep ItemData, equipment slots, durability, author, custom data and all other fields.
                // RemoveItem notifies inventory listeners after BOTH mutations. No native crafting
                // follows this path, including in no-cost mode; there is exactly one consumption.
                if (!player.GetInventory().RemoveItem(idol, 1))
                {
                    item.m_quality = previous;
                    success = false;
                    return false;
                }

                return true;
            }
            finally
            {
                refining = false;
            }
        }
    }
}