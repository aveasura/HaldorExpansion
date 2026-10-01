using HaldorExpansion.Compatibility;
using HaldorExpansion.Presentation;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace HaldorExpansion.Features.HelOath
{
    internal static class HelOathItemRegistration
    {
        internal const string HelOathPrefabName = "BowHelOath";
        internal const string HelOathSourcePrefab = "BowSpineSnap";
        internal const int HelOathPrice = 2500;

        internal const string HelOathItemKey = "$item_bow_heloath";
        internal const string HelOathItemDescKey = "$item_bow_heloath_desc";
        internal const string HelOathPassiveNameKey = "$se_heloath_sacrifice";
        internal const string HelOathPassiveDescKey = "$se_heloath_sacrifice_desc";

        internal const float HelOathBasePierceDamage = 30f;
        internal const float HelOathMovementModifier = 0.01f;
        internal const float HelOathBackstabBonus = 3f;

        internal static GameObject HelOathPrefab;
        internal static ItemDrop HelOathItemDrop;

        internal static bool EnsureHelOathReady()
        {
            try
            {
                if (ObjectDB.instance == null)
                    return false;

                GameObject existing = ObjectDB.instance.GetItemPrefab(HelOathPrefabName);
                if (existing != null)
                {
                    HelOathAppearance.Apply(existing);
                    HelOathPrefab = existing;
                    HelOathItemDrop = existing.GetComponent<ItemDrop>();

                    if (HelOathItemDrop != null)
                    {
                        ConfigureHelOathShared(HelOathItemDrop.m_itemData.m_shared);
                        ApplyHelOathIcon(HelOathItemDrop.m_itemData.m_shared);
                        HelOathItemDrop.m_itemData.m_dropPrefab = existing;
                    }

                    HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] Hel Oath already in ObjectDB: {HelOathPrefabName}");
                    return HelOathItemDrop != null;
                }

                GameObject source = ObjectDB.instance.GetItemPrefab(HelOathSourcePrefab);
                if (source == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        $"[HaldorExpansion] Source bow(helOath) prefab not found: {HelOathSourcePrefab}");
                    return false;
                }

                GameObject helOathGo =
                    PrefabManager.Instance.CreateClonedPrefab(HelOathPrefabName, source.name);
                if (helOathGo == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] Failed to clone Hel Oath prefab");
                    return false;
                }

                ItemDrop drop = helOathGo.GetComponent<ItemDrop>();
                if (drop == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] ItemDrop missing on Hel Oath");
                    return false;
                }

                ConfigureHelOathShared(drop.m_itemData.m_shared);
                ApplyHelOathIcon(drop.m_itemData.m_shared);
                
                drop.m_itemData.m_dropPrefab = helOathGo;

                HelOathAppearance.Apply(helOathGo);
                ItemManager.Instance.AddItem(new CustomItem(helOathGo, true));

                HelOathPrefab = helOathGo;
                HelOathItemDrop = drop;

                HaldorExpansionPlugin.DebugLog(
                    $"[HaldorExpansion] Hel Oath registered: {HelOathPrefabName} (source={source.name})");
                return true;
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError($"[HaldorExpansion] EnsureHelOathReady error: {e}");
                return false;
            }
        }

        private static void ConfigureHelOathShared(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            shared.m_name = HelOathItemKey;
            shared.m_description = HelOathItemDescKey;

            shared.m_maxQuality = 1;
            shared.m_weight = 2f;
            shared.m_value = 0;

            shared.m_skillType = Skills.SkillType.Bows;
            shared.m_movementModifier = HelOathMovementModifier;
            shared.m_backstabBonus = HelOathBackstabBonus;
            shared.m_attackForce = 20f;
            
            shared.m_attack.m_attackStamina = 10f;

            shared.m_damages = new HitData.DamageTypes();
            shared.m_damages.m_pierce = HelOathBasePierceDamage;
            shared.m_damages.m_lightning = 35f;
            shared.m_damages.m_frost = 15f;
            shared.m_damagesPerLevel = new HitData.DamageTypes();

            shared.m_maxDurability = 1800f;
            shared.m_durabilityPerLevel = 0f;
            shared.m_useDurability = true;
            shared.m_canBeReparied = true;

            // Do not inherit gameplay status effects from the source bow or another mod's edits.
            // Hel's Sacrifice lives on the bow's equip status effect so its skill bonus is
            // applied only while the bow is equipped and remains visible in the highlighted tooltip.
            ApplyHelOathPassiveTooltip(shared);
            shared.m_attackStatusEffect = null;
            shared.m_fullAdrenalineSE = null;
            shared.m_setName = "";
            shared.m_setSize = 0;
            shared.m_setStatusEffect = null;
        }


        private static void ApplyHelOathPassiveTooltip(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null) return;

            SE_Stats se = ScriptableObject.CreateInstance<SE_Stats>();
            se.name = "SE_HelOathSacrificeTooltip";
            ReflectionAccess.SetFieldIfExists(se, "m_name", HelOathPassiveNameKey);
            ReflectionAccess.SetFieldIfExists(se, "m_tooltip", HelOathPassiveDescKey);
            se.m_skillLevel = Skills.SkillType.Bows;
            se.m_skillLevelModifier = 20f;
            shared.m_equipStatusEffect = se;
        }

        internal static void RegisterHelOathRecipe()
        {
            try
            {
                string recipeName = "Recipe_" + HelOathPrefabName;
                if (ItemManager.Instance.GetRecipe(recipeName) != null)
                {
                    HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] Recipe already registered: {recipeName}");
                    return;
                }

                GameObject sourcePrefab = ObjectDB.instance?.GetItemPrefab(HelOathSourcePrefab);
                if (sourcePrefab == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] Source prefab not found for Hel Oath recipe");
                    return;
                }

                ItemDrop sourceDrop = sourcePrefab.GetComponent<ItemDrop>();
                if (sourceDrop == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] Source ItemDrop not found for Hel Oath recipe");
                    return;
                }

                Recipe sourceRecipe = ObjectDB.instance.GetRecipe(sourceDrop.m_itemData);
                if (sourceRecipe == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] Source recipe not found for Hel Oath");
                    return;
                }

                string craftingStation = sourceRecipe.m_craftingStation != null
                    ? sourceRecipe.m_craftingStation.name
                    : null;

                string repairStation = "forge";

                var recipeConfig = new RecipeConfig
                {
                    Name = recipeName,
                    Item = HelOathPrefabName,
                    Amount = 1,
                    CraftingStation = craftingStation,
                    RepairStation = repairStation,
                    MinStationLevel = 1,
                    Enabled = false,
                    Requirements = Array.Empty<RequirementConfig>()
                };

                ItemManager.Instance.AddRecipe(new CustomRecipe(recipeConfig));
                HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] Registered recipe via Jotunn for {HelOathPrefabName}");
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError($"[HaldorExpansion] RegisterHelOathRecipe error: {e}");
            }
        }
        
        private static void ApplyHelOathIcon(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            if (!EmbeddedIconLoader.TrySetEmbeddedIcon(shared, "Hel_oath.png"))
            {
                HaldorExpansionPlugin.Log?.LogWarning(
                    "[HaldorExpansion] Embedded icon missing for Hel Oath: Hel_oath.png"
                );
            }
        }
    }
}
