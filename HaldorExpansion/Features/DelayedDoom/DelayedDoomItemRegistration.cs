using HaldorExpansion.Compatibility;
using HaldorExpansion.Presentation;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace HaldorExpansion.Features.DelayedDoom
{
    internal static class DelayedDoomItemRegistration
    {
        internal const string DelayedDoomChestPrefabName = "ArmorDelayedDoomChest";
        internal const string DelayedDoomChestSourcePrefab = "ArmorPaddedCuirass";
        internal const int DelayedDoomChestPrice = 4000;
        internal const float DelayedDoomChestFoodRegenBonus = 5f;

        internal static GameObject DelayedDoomChestPrefab;
        internal static ItemDrop DelayedDoomChestItemDrop;
        
        private static void ApplyDelayedDoomChestVisualEffect(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            SE_Stats se = ScriptableObject.CreateInstance<SE_Stats>();
            se.name = "SE_DelayedDoomChestVisual";

            ReflectionAccess.SetFieldIfExists(se, "m_name", "$se_delayeddoom_regen");
            ReflectionAccess.SetFieldIfExists(se, "m_tooltip", "$se_delayeddoom_regen_desc");

            shared.m_equipStatusEffect = se;
        }

        internal static bool EnsureDelayedDoomChestReady()
        {
            try
            {
                if (ObjectDB.instance == null)
                    return false;

                GameObject existing = ObjectDB.instance.GetItemPrefab(DelayedDoomChestPrefabName);
                if (existing != null)
                {
                    DelayedDoomChestPrefab = existing;
                    DelayedDoomChestItemDrop = existing.GetComponent<ItemDrop>();

                    if (DelayedDoomChestItemDrop != null)
                    {
                        ItemDrop.ItemData.SharedData shared = DelayedDoomChestItemDrop.m_itemData.m_shared;
                        ConfigureDelayedDoomChestShared(shared);
                        ApplyDelayedDoomChestIcon(shared);

                        DelayedDoomChestItemDrop.m_itemData.m_dropPrefab = existing;
                        shared.m_canBeReparied = true;
                        shared.m_useDurability = true;
                    }

                    HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] Delayed Doom chest already in ObjectDB: {DelayedDoomChestPrefabName}");
                    return DelayedDoomChestItemDrop != null;
                }

                GameObject source = ObjectDB.instance.GetItemPrefab(DelayedDoomChestSourcePrefab);
                if (source == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning($"[HaldorExpansion] Source chest prefab not found: {DelayedDoomChestSourcePrefab}");
                    return false;
                }

                GameObject chestGo = PrefabManager.Instance.CreateClonedPrefab(DelayedDoomChestPrefabName, source.name);
                if (chestGo == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] Failed to clone delayed doom chest prefab");
                    return false;
                }

                ItemDrop drop = chestGo.GetComponent<ItemDrop>();
                if (drop == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] ItemDrop missing on delayed doom chest");
                    return false;
                }

                ItemDrop.ItemData.SharedData sharedNew = drop.m_itemData.m_shared;

                ConfigureDelayedDoomChestShared(sharedNew);
                ApplyDelayedDoomChestIcon(sharedNew);
                drop.m_itemData.m_dropPrefab = chestGo;

                ItemManager.Instance.AddItem(new CustomItem(chestGo, true));

                DelayedDoomChestPrefab = chestGo;
                DelayedDoomChestItemDrop = drop;

                HaldorExpansionPlugin.DebugLog(
                    $"[HaldorExpansion] Delayed Doom chest registered: {DelayedDoomChestPrefabName} (source={source.name})");
                return true;
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError($"[HaldorExpansion] EnsureDelayedDoomChestReady error: {e}");
                return false;
            }
        }

        // Статы 
        private static void ConfigureDelayedDoomChestShared(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            shared.m_name = "$item_chest_delayeddoom";
            shared.m_description = "$item_chest_delayeddoom_desc";

            shared.m_armor = 32f;
            shared.m_maxQuality = 1;
            shared.m_weight = 5f;
            shared.m_value = 0;

            shared.m_maxDurability = 2100f;
            shared.m_durabilityPerLevel = 0f;

            shared.m_useDurability = true;
            shared.m_canBeReparied = true;

            shared.m_setName = "";
            shared.m_setSize = 0;
            shared.m_setStatusEffect = null;

            ApplyDelayedDoomChestVisualEffect(shared);
        }

        internal static void RegisterDelayedDoomChestRecipe()
        {
            try
            {
                if (ItemManager.Instance.GetRecipe("Recipe_" + DelayedDoomChestPrefabName) != null)
                {
                    HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] Recipe already registered: Recipe_{DelayedDoomChestPrefabName}");
                    return;
                }

                GameObject sourcePrefab = ObjectDB.instance?.GetItemPrefab(DelayedDoomChestSourcePrefab);
                if (sourcePrefab == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] Source prefab not found for recipe");
                    return;
                }

                ItemDrop sourceDrop = sourcePrefab.GetComponent<ItemDrop>();
                if (sourceDrop == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] Source ItemDrop not found for recipe");
                    return;
                }

                Recipe sourceRecipe = ObjectDB.instance.GetRecipe(sourceDrop.m_itemData);
                if (sourceRecipe == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] Source recipe not found");
                    return;
                }

                string craftingStation = sourceRecipe.m_craftingStation != null
                    ? sourceRecipe.m_craftingStation.name
                    : null;

                string repairStation = "forge";

                var recipeConfig = new RecipeConfig
                {
                    Name = "Recipe_" + DelayedDoomChestPrefabName,
                    Item = DelayedDoomChestPrefabName,
                    Amount = 1,
                    CraftingStation = craftingStation,
                    RepairStation = repairStation,
                    MinStationLevel = 1,
                    Enabled = false,
                    Requirements = Array.Empty<RequirementConfig>()
                };

                ItemManager.Instance.AddRecipe(new CustomRecipe(recipeConfig));
                HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] Registered recipe via Jotunn for {DelayedDoomChestPrefabName}");
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError($"[HaldorExpansion] RegisterDelayedDoomChestRecipe error: {e}");
            }
        }
        
        private static void ApplyDelayedDoomChestIcon(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            if (!EmbeddedIconLoader.TrySetEmbeddedIcon(shared, "delayed_doom_chest.png"))
            {
                HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] Embedded icon missing for Delayed doom chest: delayed_doom_chest.png");
            }
        }
    }
}
