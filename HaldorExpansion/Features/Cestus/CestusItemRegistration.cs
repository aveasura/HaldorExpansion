using HaldorExpansion.Compatibility;
using HaldorExpansion.Features.SteelHeart;
using HaldorExpansion.Presentation;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace HaldorExpansion.Features.Cestus
{
    internal static class CestusItemRegistration
    {
        internal const string GritCestusPrefabName = "Bone_crushers";

        // ключи эффектов
        internal const string CestusEffectNameKey = "$se_weapon_gritcestus";
        internal const string CestusEffectDescKey = "$se_weapon_gritcestus_desc";

        // todo Пока возьму когти за основу, потом можно переделать на кулаки
        internal const string GritCestusSourcePrefab = "FistFenrirClaw";

        internal const int GritCestusPrice = 2500;

        internal const string CestusItemKey = "$item_weapon_gritcestus";
        internal const string CestusItemDescKey = "$item_weapon_gritcestus_desc";
        
        private const float GritCestusWeight = 5f;
        
        private const int GritCestusMaxQuality = 1;
        private const float GritCestusMaxDurability = 1800f;
        private const float GritCestusDurabilityPerLevel = 0f;

        // Урон
        private const float GritCestusBluntDamage = 24f;
        private const float GritCestusLightningDamage = 4f;

        // Особые статы
        internal const float GritCestusBonusMaxHealth = 30f;
        private const float GritCestusPrimaryStaggerMultiplier = 1.35f; // ускоренное настакивание оглушения ударами
        private const float GritCestusSecondaryStaggerMultiplier = 2.2f;
        internal const float GritCestusAttackSpeedMultiplier = 1.9f; // +90% скорости атаки пока кастет в руках
        internal const float GritCestusArmorPenalty = 15f; // плоский минус к броне

        // блок/парирование
        private const float GritCestusBlockPower = 2f;
        private const float GritCestusBlockPowerPerLevel = 0f;
        private const float GritCestusDeflectionForce = 15f;
        private const float GritCestusDeflectionForcePerLevel = 0f;
        private const float GritCestusTimedBlockBonus = 2.0f;

        internal static GameObject GritCestusPrefab;
        internal static ItemDrop GritCestusItemDrop;

        internal static bool EnsureCestusReady()
        {
            try
            {
                if (ObjectDB.instance == null)
                    return false;

                GameObject existing = ObjectDB.instance.GetItemPrefab(GritCestusPrefabName);
                if (existing != null)
                {
                    GritCestusPrefab = existing;
                    GritCestusItemDrop = existing.GetComponent<ItemDrop>();

                    if (GritCestusItemDrop != null)
                    {
                        ConfigureCestusShared(GritCestusItemDrop.m_itemData.m_shared);
                        ApplyBoneCrushersIcon(GritCestusItemDrop.m_itemData.m_shared);
                        GritCestusItemDrop.m_itemData.m_dropPrefab = existing;
                    }

                    HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] Cestus already in ObjectDB: {GritCestusPrefabName}");
                    return GritCestusItemDrop != null;
                }

                GameObject source = ObjectDB.instance.GetItemPrefab(GritCestusSourcePrefab);
                if (source == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        $"[HaldorExpansion] Source cestus prefab not found: {GritCestusSourcePrefab}");
                    return false;
                }

                GameObject cestusGo =
                    PrefabManager.Instance.CreateClonedPrefab(GritCestusPrefabName, source.name);

                if (cestusGo == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] Failed to clone cestus prefab");
                    return false;
                }

                ItemDrop drop = cestusGo.GetComponent<ItemDrop>();
                if (drop == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] ItemDrop missing on cloned cestus prefab");
                    return false;
                }

                ConfigureCestusShared(drop.m_itemData.m_shared);
                ApplyBoneCrushersIcon(drop.m_itemData.m_shared);
                drop.m_itemData.m_dropPrefab = cestusGo;

                ItemManager.Instance.AddItem(new CustomItem(cestusGo, true));

                GritCestusPrefab = cestusGo;
                GritCestusItemDrop = drop;

                HaldorExpansionPlugin.DebugLog(
                    $"[HaldorExpansion] Cestus registered: {GritCestusPrefabName} (source={source.name})");

                return true;
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError($"[HaldorExpansion] EnsureCestusReady error: {e}");
                return false;
            }
        }

        private static void ConfigureCestusShared(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            shared.m_name = CestusItemKey;
            shared.m_description = CestusItemDescKey;

            shared.m_maxStackSize = 1;
            shared.m_weight = GritCestusWeight;
            shared.m_value = 0;

            shared.m_maxQuality = GritCestusMaxQuality;

            shared.m_canBeReparied = true;
            shared.m_useDurability = true;
            shared.m_maxDurability = GritCestusMaxDurability;
            shared.m_durabilityPerLevel = GritCestusDurabilityPerLevel;

            // Обычный урон
            shared.m_damages.m_slash = 0f;
            shared.m_damages.m_blunt = GritCestusBluntDamage;

            shared.m_damages.m_pierce = 0f;
            shared.m_damages.m_fire = 0f;
            shared.m_damages.m_frost = 0f;
            shared.m_damages.m_lightning = GritCestusLightningDamage;
            shared.m_damages.m_poison = 0f;
            shared.m_damages.m_spirit = 0f;

            shared.m_damagesPerLevel.m_slash = 0f;
            shared.m_damagesPerLevel.m_blunt = 0f;
            shared.m_damagesPerLevel.m_pierce = 0f;
            shared.m_damagesPerLevel.m_fire = 0f;
            shared.m_damagesPerLevel.m_frost = 0f;
            shared.m_damagesPerLevel.m_lightning = 0f;
            shared.m_damagesPerLevel.m_poison = 0f;
            shared.m_damagesPerLevel.m_spirit = 0f;

            // блок / парирование
            shared.m_blockPower = GritCestusBlockPower;
            shared.m_blockPowerPerLevel = GritCestusBlockPowerPerLevel;
            shared.m_deflectionForce = GritCestusDeflectionForce;
            shared.m_deflectionForcePerLevel = GritCestusDeflectionForcePerLevel;
            shared.m_timedBlockBonus = GritCestusTimedBlockBonus;

            // потребление выносливости на удары
            if (shared.m_attack != null)
            {
                shared.m_attack.m_attackStamina = 12f;
                shared.m_attack.m_staggerMultiplier = GritCestusPrimaryStaggerMultiplier;
            }

            if (shared.m_secondaryAttack != null)
            {
                shared.m_secondaryAttack.m_attackStamina = 20;
                shared.m_secondaryAttack.m_staggerMultiplier = GritCestusSecondaryStaggerMultiplier;
            }

            // скрытный/внезапный удар
            shared.m_backstabBonus = 3f;

            ApplyCestusEquipBonuses(shared);

            shared.m_canBeReparied = true;
            shared.m_useDurability = true;
            
            SteelHeartSet.ApplySteelHeartSet(shared);
        }

        private static void ApplyCestusEquipBonuses(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            // Реальные бонусы остаются в Harmony patch / другой логике,
            // а этот эффект нужен только для красивого отдельного блока в описании.
            ApplyCestusVisualEffect(shared);
        }

        // скрытый рецепт(тупо для ремонта)
        internal static void RegisterCestusRecipe()
        {
            try
            {
                string recipeName = "Recipe_" + GritCestusPrefabName;

                if (ItemManager.Instance.GetRecipe(recipeName) != null)
                {
                    HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] Recipe already registered: {recipeName}");
                    return;
                }

                GameObject sourcePrefab = ObjectDB.instance?.GetItemPrefab(GritCestusSourcePrefab);
                if (sourcePrefab == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] Source prefab not found for cestus recipe");
                    return;
                }

                ItemDrop sourceDrop = sourcePrefab.GetComponent<ItemDrop>();
                if (sourceDrop == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] Source ItemDrop not found for cestus recipe");
                    return;
                }

                Recipe sourceRecipe = ObjectDB.instance.GetRecipe(sourceDrop.m_itemData);
                if (sourceRecipe == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] Source recipe not found for cestus");
                    return;
                }

                string craftingStation = sourceRecipe.m_craftingStation != null
                    ? sourceRecipe.m_craftingStation.name
                    : "forge";

                var recipeConfig = new RecipeConfig
                {
                    Name = recipeName,
                    Item = GritCestusPrefabName,
                    Amount = 1,
                    CraftingStation = craftingStation,
                    RepairStation = "forge",
                    MinStationLevel = 1,
                    Enabled = false,
                    Requirements = Array.Empty<RequirementConfig>()
                };

                ItemManager.Instance.AddRecipe(new CustomRecipe(recipeConfig));
                HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] Registered recipe via Jotunn for {GritCestusPrefabName}");
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError($"[HaldorExpansion] RegisterCestusRecipe error: {e}");
            }
        }

        private static void ApplyCestusVisualEffect(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            SE_Stats se = ScriptableObject.CreateInstance<SE_Stats>();
            se.name = "SE_GritCestusVisual";

            ReflectionAccess.SetFieldIfExists(se, "m_name", CestusEffectNameKey);
            ReflectionAccess.SetFieldIfExists(se, "m_tooltip", CestusEffectDescKey);

            shared.m_equipStatusEffect = se;
        }
        
        private static void ApplyBoneCrushersIcon(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            if (!EmbeddedIconLoader.TrySetEmbeddedIcon(shared, "Bone_crushers.png"))
            {
                HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] Embedded icon missing for Bone crushers: Bone_crushers.png");
            }
        }
    }
}
