using System;
using System.Collections.Generic;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace HaldorExpansion
{
    public partial class HaldorExpansionPlugin
    {
        internal const string GritCestusPrefabName = "Bone_crushers";

        // ключи эффектов
        private const string CestusEffectNameKey = "$se_weapon_gritcestus";
        private const string CestusEffectDescKey = "$se_weapon_gritcestus_desc";

        // todo Пока возьму когти за основу, потом можно переделать на кулаки
        internal const string GritCestusSourcePrefab = "FistFenrirClaw";

        internal const int GritCestusPrice = 2500;

        private const string CestusItemKey = "$item_weapon_gritcestus";
        private const string CestusItemDescKey = "$item_weapon_gritcestus_desc";
        
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
        private const float GritCestusAttackSpeedMultiplier = 1.9f; // +90% скорости атаки пока кастет в руках
        private const float GritCestusArmorPenalty = 15f; // плоский минус к броне

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

                    DebugLog($"[HaldorExpansion] Cestus already in ObjectDB: {GritCestusPrefabName}");
                    return GritCestusItemDrop != null;
                }

                GameObject source = ObjectDB.instance.GetItemPrefab(GritCestusSourcePrefab);
                if (source == null)
                {
                    Log?.LogWarning(
                        $"[HaldorExpansion] Source cestus prefab not found: {GritCestusSourcePrefab}");
                    return false;
                }

                GameObject cestusGo =
                    PrefabManager.Instance.CreateClonedPrefab(GritCestusPrefabName, source.name);

                if (cestusGo == null)
                {
                    Log?.LogWarning("[HaldorExpansion] Failed to clone cestus prefab");
                    return false;
                }

                ItemDrop drop = cestusGo.GetComponent<ItemDrop>();
                if (drop == null)
                {
                    Log?.LogWarning("[HaldorExpansion] ItemDrop missing on cloned cestus prefab");
                    return false;
                }

                ConfigureCestusShared(drop.m_itemData.m_shared);
                ApplyBoneCrushersIcon(drop.m_itemData.m_shared);
                drop.m_itemData.m_dropPrefab = cestusGo;

                ItemManager.Instance.AddItem(new CustomItem(cestusGo, true));

                GritCestusPrefab = cestusGo;
                GritCestusItemDrop = drop;

                DebugLog(
                    $"[HaldorExpansion] Cestus registered: {GritCestusPrefabName} (source={source.name})");

                return true;
            }
            catch (Exception e)
            {
                Log?.LogError($"[HaldorExpansion] EnsureCestusReady error: {e}");
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
            
            ApplySteelHeartSet(shared);
        }

        private static void ApplyCestusEquipBonuses(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            // Реальные бонусы остаются в Harmony patch / другой логике,
            // а этот эффект нужен только для красивого отдельного блока в описании.
            ApplyCestusVisualEffect(shared);
        }

        private static bool IsCestusWeapon(ItemDrop.ItemData item)
        {
            if (item == null)
                return false;

            if (item.m_dropPrefab != null && item.m_dropPrefab.name == GritCestusPrefabName)
                return true;

            if (item.m_shared != null && item.m_shared.m_name == CestusItemKey)
                return true;

            return false;
        }

        // скрытый рецепт(тупо для ремонта)
        internal static void RegisterCestusRecipe()
        {
            try
            {
                string recipeName = "Recipe_" + GritCestusPrefabName;

                if (ItemManager.Instance.GetRecipe(recipeName) != null)
                {
                    DebugLog($"[HaldorExpansion] Recipe already registered: {recipeName}");
                    return;
                }

                GameObject sourcePrefab = ObjectDB.instance?.GetItemPrefab(GritCestusSourcePrefab);
                if (sourcePrefab == null)
                {
                    Log?.LogWarning("[HaldorExpansion] Source prefab not found for cestus recipe");
                    return;
                }

                ItemDrop sourceDrop = sourcePrefab.GetComponent<ItemDrop>();
                if (sourceDrop == null)
                {
                    Log?.LogWarning("[HaldorExpansion] Source ItemDrop not found for cestus recipe");
                    return;
                }

                Recipe sourceRecipe = ObjectDB.instance.GetRecipe(sourceDrop.m_itemData);
                if (sourceRecipe == null)
                {
                    Log?.LogWarning("[HaldorExpansion] Source recipe not found for cestus");
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
                DebugLog($"[HaldorExpansion] Registered recipe via Jotunn for {GritCestusPrefabName}");
            }
            catch (Exception e)
            {
                Log?.LogError($"[HaldorExpansion] RegisterCestusRecipe error: {e}");
            }
        }

        // fix для уже существующих кастетов в инвентаре
        internal static bool FixCestusItemsInInventory(Player player)
        {
            try
            {
                if (player == null || ObjectDB.instance == null)
                    return false;

                GameObject cestusPrefab = ObjectDB.instance.GetItemPrefab(GritCestusPrefabName);
                if (cestusPrefab == null)
                    return false;

                ItemDrop prefabDrop = cestusPrefab.GetComponent<ItemDrop>();
                if (prefabDrop == null)
                    return false;

                Inventory inventory = player.GetInventory();
                if (inventory == null)
                    return false;

                bool found = false;

                List<ItemDrop.ItemData> items = inventory.GetAllItems();
                if (items == null)
                    return false;

                foreach (ItemDrop.ItemData item in items)
                {
                    if (item == null || item.m_shared == null)
                        continue;

                    if (item.m_shared.m_name != CestusItemKey)
                        continue;

                    found = true;

                    int oldQuality = Mathf.Max(1, item.m_quality);
                    float oldDurability = item.m_durability;
                    bool oldEquipped = item.m_equipped;
                    int oldVariant = item.m_variant;
                    long oldCrafterID = item.m_crafterID;
                    string oldCrafterName = item.m_crafterName;

                    item.m_dropPrefab = cestusPrefab;
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
                }

                return found;
            }
            catch (Exception e)
            {
                Log?.LogError($"[HaldorExpansion] FixCestusItemsInInventory error: {e}");
                return false;
            }
        }

        private static bool HasEquippedCestus(Player player)
        {
            if (player == null)
                return false;

            Inventory inventory = player.GetInventory();
            if (inventory == null)
                return false;

            List<ItemDrop.ItemData> items = inventory.GetAllItems();
            if (items == null)
                return false;

            foreach (ItemDrop.ItemData item in items)
            {
                if (item == null)
                    continue;

                if (!item.m_equipped)
                    continue;

                if (IsCestusWeapon(item))
                    return true;
            }

            return false;
        }

        private static void ApplyCestusVisualEffect(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            SE_Stats se = ScriptableObject.CreateInstance<SE_Stats>();
            se.name = "SE_GritCestusVisual";

            SetFieldIfExists(se, "m_name", CestusEffectNameKey);
            SetFieldIfExists(se, "m_tooltip", CestusEffectDescKey);

            shared.m_equipStatusEffect = se;
        }

        [HarmonyPatch(typeof(Player), "GetTotalFoodValue")]
        private static class CestusMaxHealthPatch
        {
            private static void Postfix(Player __instance, ref float hp, ref float stamina, ref float eitr)
            {
                if (__instance == null)
                    return;

                if (HasEquippedCestus(__instance))
                    hp += GritCestusBonusMaxHealth;

                DebugLog(
                    $"[HaldorExpansion] Cestus HP bonus: equipped={HasEquippedCestus(__instance)}, hpBeforeBonus={hp}");
            }
        }
        
        [HarmonyPatch(typeof(Player), "GetBodyArmor")]
        private static class CestusArmorPenaltyPatch
        {
            private static void Postfix(Player __instance, ref float __result)
            {
                if (__instance == null)
                    return;

                bool equipped = HasEquippedCestus(__instance);
                if (equipped)
                    __result = Mathf.Max(0f, __result - GritCestusArmorPenalty);

                DebugLog($"[HaldorExpansion] Cestus armor penalty: equipped={equipped}, armorAfterPenalty={__result}");
            }
        }
        
        private static void ApplyBoneCrushersIcon(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            if (!TrySetEmbeddedIcon(shared, "Bone_crushers.png"))
            {
                Log?.LogWarning("[HaldorExpansion] Embedded icon missing for Bone crushers: Bone_crushers.png");
            }
        }
    }
}