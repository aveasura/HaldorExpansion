using HaldorExpansion.Compatibility;
using HaldorExpansion.Features.SteelHeart;
using HaldorExpansion.Presentation;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace HaldorExpansion.Features.PitKing
{
    internal static class PitKingItemRegistration
    {
        internal const string PitKingChestPrefabName = "ArmorPitKingChest";
        internal const string PitKingChestSourcePrefab = "ArmorMageChest";
        internal const int PitKingChestPrice = 4000;

        internal const float PitKingChestBonusMaxHealth = 70f;

        internal const string PitKingChestItemKey = "$item_chest_pitking";
        private const string PitKingChestItemDescKey = "$item_chest_pitking_desc";

        private const string PitKingChestEffectNameKey = "$se_pitking_vigor";
        private const string PitKingChestEffectDescKey = "$se_pitking_vigor_desc";

        internal static GameObject PitKingChestPrefab;
        internal static ItemDrop PitKingChestItemDrop;

        private static void ApplyPitKingChestVisualEffect(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            SE_Stats se = ScriptableObject.CreateInstance<SE_Stats>();
            se.name = "SE_PitKingChestVisual";

            ReflectionAccess.SetFieldIfExists(se, "m_name", PitKingChestEffectNameKey);
            ReflectionAccess.SetFieldIfExists(se, "m_tooltip", PitKingChestEffectDescKey);

            shared.m_equipStatusEffect = se;
        }

        internal static bool EnsurePitKingChestReady()
        {
            try
            {
                if (PitKingChestItemDrop != null && PitKingChestPrefab != null)
                    return true;

                if (ObjectDB.instance == null)
                    return false;

                GameObject existing = ObjectDB.instance.GetItemPrefab(PitKingChestPrefabName);
                if (existing != null)
                {
                    PitKingChestPrefab = existing;
                    PitKingChestItemDrop = existing.GetComponent<ItemDrop>();

                    if (PitKingChestItemDrop != null)
                    {
                        ConfigurePitKingChestShared(PitKingChestItemDrop.m_itemData.m_shared);
                        ApplyPitKingChestIcon(PitKingChestItemDrop.m_itemData.m_shared);
                    }

                    HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] Pit King chest already in ObjectDB: {PitKingChestPrefabName}");
                    return PitKingChestItemDrop != null;
                }

                GameObject source = ObjectDB.instance.GetItemPrefab(PitKingChestSourcePrefab);
                if (source == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning($"[HaldorExpansion] Source chest prefab not found: {PitKingChestSourcePrefab}");
                    return false;
                }

                GameObject chestGo = PrefabManager.Instance.CreateClonedPrefab(PitKingChestPrefabName, source.name);
                if (chestGo == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] Failed to clone Pit King chest prefab");
                    return false;
                }

                ItemDrop drop = chestGo.GetComponent<ItemDrop>();
                if (drop == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] ItemDrop missing on cloned Pit King chest prefab");
                    return false;
                }

                ConfigurePitKingChestShared(drop.m_itemData.m_shared);
                ApplyPitKingChestIcon(drop.m_itemData.m_shared);
                drop.m_itemData.m_dropPrefab = chestGo;

                ItemManager.Instance.AddItem(new CustomItem(chestGo, true));

                PitKingChestPrefab = chestGo;
                PitKingChestItemDrop = drop;

                HaldorExpansionPlugin.DebugLog(
                    $"[HaldorExpansion] Pit King chest registered: {PitKingChestPrefabName} (source={source.name})");
                return true;
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError($"[HaldorExpansion] EnsurePitKingChestReady error: {e}");
                return false;
            }
        }

        private static void ConfigurePitKingChestShared(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            shared.m_name = PitKingChestItemKey;
            shared.m_description = PitKingChestItemDescKey;

            shared.m_armor = 12f;
            shared.m_maxQuality = 1;
            shared.m_weight = 5f;
            shared.m_value = 0;

            shared.m_maxDurability = 2100f;
            shared.m_durabilityPerLevel = 0f;

            shared.m_useDurability = true;
            shared.m_canBeReparied = true;

            shared.m_eitrRegenModifier = 0f;
            shared.m_movementModifier = -0.05f;

            // обнулить унаследованный набор
            shared.m_setName = "";
            shared.m_setSize = 0;
            shared.m_setStatusEffect = null;

            ApplyPitKingChestVisualEffect(shared);
            SteelHeartSet.ApplySteelHeartSet(shared);
        }

        internal static void RegisterPitKingChestRecipe()
        {
            try
            {
                if (ItemManager.Instance.GetRecipe("Recipe_" + PitKingChestPrefabName) != null)
                {
                    HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] Recipe already registered: Recipe_{PitKingChestPrefabName}");
                    return;
                }

                GameObject sourcePrefab = ObjectDB.instance?.GetItemPrefab(PitKingChestSourcePrefab);
                if (sourcePrefab == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] Source prefab not found for Pit King recipe");
                    return;
                }

                ItemDrop sourceDrop = sourcePrefab.GetComponent<ItemDrop>();
                if (sourceDrop == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] Source ItemDrop not found for Pit King recipe");
                    return;
                }

                Recipe sourceRecipe = ObjectDB.instance.GetRecipe(sourceDrop.m_itemData);
                if (sourceRecipe == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] Source recipe not found for Pit King chest");
                    return;
                }

                string craftingStation = sourceRecipe.m_craftingStation != null
                    ? sourceRecipe.m_craftingStation.name
                    : null;

                string repairStation = "forge";

                var recipeConfig = new RecipeConfig
                {
                    Name = "Recipe_" + PitKingChestPrefabName,
                    Item = PitKingChestPrefabName,
                    Amount = 1,
                    CraftingStation = craftingStation,
                    RepairStation = repairStation,
                    MinStationLevel = 1,
                    Enabled = false,
                    Requirements = Array.Empty<RequirementConfig>()
                };

                ItemManager.Instance.AddRecipe(new CustomRecipe(recipeConfig));
                HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] Registered recipe via Jotunn for {PitKingChestPrefabName}");
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError($"[HaldorExpansion] RegisterPitKingChestRecipe error: {e}");
            }
        }
        
        private static void ApplyPitKingChestIcon(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            if (!EmbeddedIconLoader.TrySetEmbeddedIcon(shared, "pit_king_chest.png"))
            {
                HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] Embedded icon missing for Pit King Chest: pit_king_chest.png");
            }
        }
    }
}
