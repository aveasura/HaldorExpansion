using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System.Reflection;
using System;
using UnityEngine;

namespace HaldorExpansion.Features.StaminaCape
{
    internal static class StaminaCapeItemRegistration
    {
        internal const string PrefabName = "CapeFracturedBreath";
        internal const string SourcePrefab = "CapeLox";
        internal const int Price = 2500;

        internal const string ItemKey = "$item_cape_fracturedbreath";
        internal const string ItemDescKey = "$item_cape_fracturedbreath_desc";
        internal const string EffectNameKey = "$se_cape_fracturedbreath";
        internal const string EffectDescKey = "$se_cape_fracturedbreath_desc";

        internal static GameObject CapePrefab;
        internal static ItemDrop CapeItemDrop;
        

        internal static void ResetCaches()
        {
            CapePrefab = null;
            CapeItemDrop = null;
        }

        internal static bool EnsureReady()
        {
            try
            {

                if (ObjectDB.instance == null)
                    return false;

                GameObject existing = ObjectDB.instance.GetItemPrefab(PrefabName);
                if (existing != null)
                {
                    CapePrefab = existing;
                    CapeItemDrop = existing.GetComponent<ItemDrop>();

                    if (CapeItemDrop != null)
                    {
                        ItemDrop.ItemData.SharedData shared = CapeItemDrop.m_itemData.m_shared;
                        ConfigureShared(shared);

                        CapeItemDrop.m_itemData.m_dropPrefab = existing;
                        shared.m_canBeReparied = true;
                        shared.m_useDurability = true;
                    }

                    HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] Stamina cape already in ObjectDB: {PrefabName}");
                    return CapeItemDrop != null;
                }

                GameObject source = ObjectDB.instance.GetItemPrefab(SourcePrefab);
                if (source == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        $"[HaldorExpansion] Source cape prefab not found: {SourcePrefab}");
                    return false;
                }

                GameObject capeGo = PrefabManager.Instance.CreateClonedPrefab(PrefabName, source.name);
                if (capeGo == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        "[HaldorExpansion] Failed to clone stamina cape prefab");
                    return false;
                }

                ItemDrop drop = capeGo.GetComponent<ItemDrop>();
                if (drop == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        "[HaldorExpansion] ItemDrop missing on stamina cape");
                    return false;
                }

                ItemDrop.ItemData.SharedData sharedNew = drop.m_itemData.m_shared;

                ConfigureShared(sharedNew);
                drop.m_itemData.m_dropPrefab = capeGo;

                ItemManager.Instance.AddItem(new CustomItem(capeGo, true));

                CapePrefab = capeGo;
                CapeItemDrop = drop;

                HaldorExpansionPlugin.DebugLog(
                    $"[HaldorExpansion] Stamina cape registered: {PrefabName} (source={source.name})");
                return true;
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError($"[HaldorExpansion] EnsureReady stamina cape error: {e}");
                return false;
            }
        }

        internal static void RegisterRecipe()
        {
            try
            {
                string recipeName = "Recipe_" + PrefabName;

                if (ItemManager.Instance.GetRecipe(recipeName) != null)
                {
                    HaldorExpansionPlugin.DebugLog(
                        $"[HaldorExpansion] Recipe already registered: {recipeName}");
                    return;
                }

                GameObject sourcePrefab = ObjectDB.instance?.GetItemPrefab(SourcePrefab);
                if (sourcePrefab == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        "[HaldorExpansion] Source cape prefab not found for recipe");
                    return;
                }

                ItemDrop sourceDrop = sourcePrefab.GetComponent<ItemDrop>();
                if (sourceDrop == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        "[HaldorExpansion] Source cape ItemDrop not found for recipe");
                    return;
                }

                Recipe sourceRecipe = ObjectDB.instance.GetRecipe(sourceDrop.m_itemData);
                if (sourceRecipe == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        "[HaldorExpansion] Source cape recipe not found");
                    return;
                }

                string craftingStation = sourceRecipe.m_craftingStation != null
                    ? sourceRecipe.m_craftingStation.name
                    : null;

                string repairStation = "forge";
                
                var recipeConfig = new RecipeConfig
                {
                    Name = recipeName,
                    Item = PrefabName,
                    Amount = 1,
                    CraftingStation = craftingStation,
                    RepairStation = repairStation,
                    MinStationLevel = 1,
                    Enabled = false,
                    Requirements = Array.Empty<RequirementConfig>()
                };

                ItemManager.Instance.AddRecipe(new CustomRecipe(recipeConfig));
                HaldorExpansionPlugin.DebugLog(
                    $"[HaldorExpansion] Registered recipe via Jotunn for {PrefabName}");
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError(
                    $"[HaldorExpansion] RegisterRecipe stamina cape error: {e}");
            }
        }

        private static void ConfigureShared(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            shared.m_name = ItemKey;
            shared.m_description = ItemDescKey;

            shared.m_armor = 4f;
            shared.m_maxQuality = 1;
            shared.m_weight = 1f;
            
            shared.m_maxDurability = 1800f;
            shared.m_durabilityPerLevel = 0f;
            
            shared.m_value = 0;
            shared.m_canBeReparied = true;
            shared.m_useDurability = true;

            shared.m_setName = "";
            shared.m_setSize = 0;
            shared.m_setStatusEffect = null;

            // Убрать морозостойкость
            if (shared.m_damageModifiers != null)
            {
                shared.m_damageModifiers.RemoveAll(mod => mod.m_type == HitData.DamageType.Frost);
            }
            
            ApplyVisualEffect(shared);
        }

        private static void ApplyVisualEffect(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            SE_Stats se = ScriptableObject.CreateInstance<SE_Stats>();
            se.name = "SE_FracturedBreathCapeVisual";

            SetFieldIfExists(se, "m_name", EffectNameKey);
            SetFieldIfExists(se, "m_tooltip", EffectDescKey);

            shared.m_equipStatusEffect = se;
        }

        private static void SetFieldIfExists(object obj, string fieldName, object value)
        {
            FieldInfo f = obj.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (f != null)
            {
                f.SetValue(obj, value);
            }
        }
    }
}
