using System;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace HaldorExpansion
{
    public partial class HaldorExpansionPlugin
    {
        internal const string ShadowCrossbowPrefabName = "CrossbowSilentHunt";
        internal const string ShadowCrossbowSourcePrefab = "CrossbowArbalest";
        internal const int ShadowCrossbowPrice = 2500;

        internal const string ShadowCrossbowItemKey = "$item_crossbow_silenthunt";
        internal const string ShadowCrossbowItemDescKey = "$item_crossbow_silenthunt_desc";

        internal const float ShadowCrossbowBasePierceDamage = 210f;
        internal const float ShadowCrossbowMovementModifier = -0.35f;
        internal const float ShadowCrossbowBackstabBonus = 6f;

        internal static GameObject ShadowCrossbowPrefab;
        internal static ItemDrop ShadowCrossbowItemDrop;

        internal static bool EnsureShadowCrossbowReady()
        {
            try
            {
                if (ObjectDB.instance == null)
                    return false;

                GameObject existing = ObjectDB.instance.GetItemPrefab(ShadowCrossbowPrefabName);
                if (existing != null)
                {
                    ShadowCrossbowPrefab = existing;
                    ShadowCrossbowItemDrop = existing.GetComponent<ItemDrop>();

                    if (ShadowCrossbowItemDrop != null)
                    {
                        ConfigureShadowCrossbowShared(ShadowCrossbowItemDrop.m_itemData.m_shared);
                        ApplyShadowCrossbowIcon(ShadowCrossbowItemDrop.m_itemData.m_shared);
                    }

                    DebugLog($"[HaldorExpansion] Shadow crossbow already in ObjectDB: {ShadowCrossbowPrefabName}");
                    return ShadowCrossbowItemDrop != null;
                }

                GameObject source = ObjectDB.instance.GetItemPrefab(ShadowCrossbowSourcePrefab);
                if (source == null)
                {
                    Log?.LogWarning(
                        $"[HaldorExpansion] Source crossbow prefab not found: {ShadowCrossbowSourcePrefab}");
                    return false;
                }

                GameObject crossbowGo =
                    PrefabManager.Instance.CreateClonedPrefab(ShadowCrossbowPrefabName, source.name);
                if (crossbowGo == null)
                {
                    Log?.LogWarning("[HaldorExpansion] Failed to clone shadow crossbow prefab");
                    return false;
                }

                ItemDrop drop = crossbowGo.GetComponent<ItemDrop>();
                if (drop == null)
                {
                    Log?.LogWarning("[HaldorExpansion] ItemDrop missing on shadow crossbow");
                    return false;
                }

                ConfigureShadowCrossbowShared(drop.m_itemData.m_shared);
                ApplyShadowCrossbowIcon(drop.m_itemData.m_shared);
                drop.m_itemData.m_dropPrefab = crossbowGo;

                ItemManager.Instance.AddItem(new CustomItem(crossbowGo, true));

                ShadowCrossbowPrefab = crossbowGo;
                ShadowCrossbowItemDrop = drop;

                DebugLog(
                    $"[HaldorExpansion] Shadow crossbow registered: {ShadowCrossbowPrefabName} (source={source.name})");
                return true;
            }
            catch (Exception e)
            {
                Log?.LogError($"[HaldorExpansion] EnsureShadowCrossbowReady error: {e}");
                return false;
            }
        }

        private static void ConfigureShadowCrossbowShared(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            shared.m_name = ShadowCrossbowItemKey;
            shared.m_description = ShadowCrossbowItemDescKey;

            shared.m_maxQuality = 1;
            shared.m_weight = 4f;
            shared.m_value = 0;

            shared.m_skillType = Skills.SkillType.Crossbows;
            shared.m_movementModifier = ShadowCrossbowMovementModifier;
            shared.m_backstabBonus = ShadowCrossbowBackstabBonus;
            shared.m_attackForce = 80f;

            shared.m_damages = new HitData.DamageTypes();
            shared.m_damages.m_pierce = ShadowCrossbowBasePierceDamage;
            shared.m_damagesPerLevel = new HitData.DamageTypes();

            shared.m_maxDurability = 800f;
            shared.m_durabilityPerLevel = 0f;
            shared.m_useDurability = true;
            shared.m_canBeReparied = true;

            shared.m_setName = "";
            shared.m_setSize = 0;
            shared.m_setStatusEffect = null;
        }

        internal static void RegisterShadowCrossbowRecipe()
        {
            try
            {
                string recipeName = "Recipe_" + ShadowCrossbowPrefabName;
                if (ItemManager.Instance.GetRecipe(recipeName) != null)
                {
                    DebugLog($"[HaldorExpansion] Recipe already registered: {recipeName}");
                    return;
                }

                GameObject sourcePrefab = ObjectDB.instance?.GetItemPrefab(ShadowCrossbowSourcePrefab);
                if (sourcePrefab == null)
                {
                    Log?.LogWarning("[HaldorExpansion] Source prefab not found for shadow crossbow recipe");
                    return;
                }

                ItemDrop sourceDrop = sourcePrefab.GetComponent<ItemDrop>();
                if (sourceDrop == null)
                {
                    Log?.LogWarning("[HaldorExpansion] Source ItemDrop not found for shadow crossbow recipe");
                    return;
                }

                Recipe sourceRecipe = ObjectDB.instance.GetRecipe(sourceDrop.m_itemData);
                if (sourceRecipe == null)
                {
                    Log?.LogWarning("[HaldorExpansion] Source recipe not found for shadow crossbow");
                    return;
                }

                string craftingStation = sourceRecipe.m_craftingStation != null
                    ? sourceRecipe.m_craftingStation.name
                    : null;

                string repairStation = "forge";

                var recipeConfig = new RecipeConfig
                {
                    Name = recipeName,
                    Item = ShadowCrossbowPrefabName,
                    Amount = 1,
                    CraftingStation = craftingStation,
                    RepairStation = repairStation,
                    MinStationLevel = 1,
                    Enabled = false,
                    Requirements = Array.Empty<RequirementConfig>()
                };

                ItemManager.Instance.AddRecipe(new CustomRecipe(recipeConfig));
                DebugLog($"[HaldorExpansion] Registered recipe via Jotunn for {ShadowCrossbowPrefabName}");
            }
            catch (Exception e)
            {
                Log?.LogError($"[HaldorExpansion] RegisterShadowCrossbowRecipe error: {e}");
            }
        }

        private static void ApplyShadowCrossbowIcon(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            if (!TrySetEmbeddedIcon(shared, "Shadow_crossbow.png"))
            {
                Log?.LogWarning("[HaldorExpansion] Embedded icon missing for Shadow Crossbow: Shadow_crossbow.png");
            }
        }
    }
}