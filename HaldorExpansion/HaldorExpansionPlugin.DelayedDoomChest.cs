using System;
using System.Collections.Generic;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace HaldorExpansion
{
    public partial class HaldorExpansionPlugin
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

            SetFieldIfExists(se, "m_name", "$se_delayeddoom_regen");
            SetFieldIfExists(se, "m_tooltip", "$se_delayeddoom_regen_desc");

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

                    DebugLog($"[HaldorExpansion] Delayed Doom chest already in ObjectDB: {DelayedDoomChestPrefabName}");
                    return DelayedDoomChestItemDrop != null;
                }

                GameObject source = ObjectDB.instance.GetItemPrefab(DelayedDoomChestSourcePrefab);
                if (source == null)
                {
                    Log?.LogWarning($"[HaldorExpansion] Source chest prefab not found: {DelayedDoomChestSourcePrefab}");
                    return false;
                }

                GameObject chestGo = PrefabManager.Instance.CreateClonedPrefab(DelayedDoomChestPrefabName, source.name);
                if (chestGo == null)
                {
                    Log?.LogWarning("[HaldorExpansion] Failed to clone delayed doom chest prefab");
                    return false;
                }

                ItemDrop drop = chestGo.GetComponent<ItemDrop>();
                if (drop == null)
                {
                    Log?.LogWarning("[HaldorExpansion] ItemDrop missing on delayed doom chest");
                    return false;
                }

                ItemDrop.ItemData.SharedData sharedNew = drop.m_itemData.m_shared;

                ConfigureDelayedDoomChestShared(sharedNew);
                ApplyDelayedDoomChestIcon(sharedNew);
                drop.m_itemData.m_dropPrefab = chestGo;

                ItemManager.Instance.AddItem(new CustomItem(chestGo, true));

                DelayedDoomChestPrefab = chestGo;
                DelayedDoomChestItemDrop = drop;

                DebugLog(
                    $"[HaldorExpansion] Delayed Doom chest registered: {DelayedDoomChestPrefabName} (source={source.name})");
                return true;
            }
            catch (Exception e)
            {
                Log?.LogError($"[HaldorExpansion] EnsureDelayedDoomChestReady error: {e}");
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

        internal static bool FixDelayedDoomItemsInInventory(Player player)
        {
            try
            {
                if (player == null || ObjectDB.instance == null)
                    return false;

                GameObject chestPrefab = ObjectDB.instance.GetItemPrefab(DelayedDoomChestPrefabName);
                if (chestPrefab == null)
                    return false;

                ItemDrop prefabDrop = chestPrefab.GetComponent<ItemDrop>();
                if (prefabDrop == null)
                    return false;

                Inventory inventory = player.GetInventory();
                if (inventory == null)
                    return false;

                bool found = false;
                bool changed = false;

                List<ItemDrop.ItemData> items = inventory.GetAllItems();
                if (items == null)
                    return false;

                foreach (ItemDrop.ItemData item in items)
                {
                    if (item == null || item.m_shared == null)
                        continue;

                    if (item.m_shared.m_name != "$item_chest_delayeddoom")
                        continue;

                    found = true;

                    int oldQuality = Mathf.Max(1, item.m_quality);
                    float oldDurability = item.m_durability;
                    bool oldEquipped = item.m_equipped;
                    int oldVariant = item.m_variant;
                    long oldCrafterID = item.m_crafterID;
                    string oldCrafterName = item.m_crafterName;
                    
                    item.m_dropPrefab = chestPrefab;
                    item.m_shared = prefabDrop.m_itemData.m_shared;

                    item.m_quality = Mathf.Clamp(oldQuality, 1, Mathf.Max(1, item.m_shared.m_maxQuality));
                    item.m_variant = oldVariant;
                    item.m_crafterID = oldCrafterID;
                    item.m_crafterName = oldCrafterName;
                    item.m_equipped = oldEquipped;

                    float maxDurability = item.m_shared.m_maxDurability +
                                          item.m_shared.m_durabilityPerLevel * (item.m_quality - 1);
                    item.m_durability = Mathf.Clamp(oldDurability, 0f, Mathf.Max(1f, maxDurability));

                    item.m_shared.m_canBeReparied = true;
                    item.m_shared.m_useDurability = true;

                    changed = true;
                }

                if (found)
                {
                    Recipe recipe = ObjectDB.instance.GetRecipe(prefabDrop.m_itemData);

                    DebugLog(
                        $"[HaldorExpansion] Inventory rebind checked: " +
                        $"changed={changed}, " +
                        $"recipe={(recipe != null ? recipe.name : "null")}, " +
                        $"craft={(recipe != null ? recipe.m_craftingStation?.name : "null")}, " +
                        $"repair={(recipe != null ? recipe.m_repairStation?.name : "null")}, " +
                        $"dropPrefab={chestPrefab.name}");
                }

                return found;
            }
            catch (Exception e)
            {
                Log?.LogError($"[HaldorExpansion] FixDelayedDoomItemsInInventory error: {e}");
                return false;
            }
        }

        internal static void RegisterDelayedDoomChestRecipe()
        {
            try
            {
                if (ItemManager.Instance.GetRecipe("Recipe_" + DelayedDoomChestPrefabName) != null)
                {
                    DebugLog($"[HaldorExpansion] Recipe already registered: Recipe_{DelayedDoomChestPrefabName}");
                    return;
                }

                GameObject sourcePrefab = ObjectDB.instance?.GetItemPrefab(DelayedDoomChestSourcePrefab);
                if (sourcePrefab == null)
                {
                    Log?.LogWarning("[HaldorExpansion] Source prefab not found for recipe");
                    return;
                }

                ItemDrop sourceDrop = sourcePrefab.GetComponent<ItemDrop>();
                if (sourceDrop == null)
                {
                    Log?.LogWarning("[HaldorExpansion] Source ItemDrop not found for recipe");
                    return;
                }

                Recipe sourceRecipe = ObjectDB.instance.GetRecipe(sourceDrop.m_itemData);
                if (sourceRecipe == null)
                {
                    Log?.LogWarning("[HaldorExpansion] Source recipe not found");
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
                DebugLog($"[HaldorExpansion] Registered recipe via Jotunn for {DelayedDoomChestPrefabName}");
            }
            catch (Exception e)
            {
                Log?.LogError($"[HaldorExpansion] RegisterDelayedDoomChestRecipe error: {e}");
            }
        }
        
        private static void ApplyDelayedDoomChestIcon(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            if (!TrySetEmbeddedIcon(shared, "delayed_doom_chest.png"))
            {
                Log?.LogWarning("[HaldorExpansion] Embedded icon missing for Delayed doom chest: delayed_doom_chest.png");
            }
        }
    }
}