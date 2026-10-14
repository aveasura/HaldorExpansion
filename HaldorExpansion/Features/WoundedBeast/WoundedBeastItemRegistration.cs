using System;
using System.Reflection;
using HaldorExpansion.Compatibility;
using HaldorExpansion.Features.Upgrades;
using HaldorExpansion.Integration;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace HaldorExpansion.Features.WoundedBeast
{
    internal static class WoundedBeastItemRegistration
    {
        internal const string WoundedBeastCapePrefabName = "CapeWoundedBeast";
        internal const string WoundedBeastCapeSourcePrefab = "CapeWolf";
        internal const int WoundedBeastCapePrice = 2500;

        internal const string WoundedBeastCapeItemKey = "$item_cape_woundedbeast";
        internal const string WoundedBeastCapeItemDescKey = "$item_cape_woundedbeast_desc";
        internal const string WoundedBeastCapeEffectNameKey = "$se_cape_woundedbeast";
        internal const string WoundedBeastCapeEffectDescKey = "$se_cape_woundedbeast_desc";

        internal static GameObject WoundedBeastCapePrefab;
        internal static ItemDrop WoundedBeastCapeItemDrop;

        internal static bool EnsureWoundedBeastCapeReady()
        {
            try
            {
                if (ObjectDB.instance == null)
                    return false;

                GameObject existing = ItemPrefabLookup.Registered(WoundedBeastCapePrefabName);
                if (existing != null && existing == WoundedBeastCapePrefab && WoundedBeastCapeItemDrop != null)
                    return true;

                if (existing != null)
                {
                    WoundedBeastCapePrefab = existing;
                    WoundedBeastCapeItemDrop = existing.GetComponent<ItemDrop>();

                    if (WoundedBeastCapeItemDrop != null)
                    {
                        ConfigureWoundedBeastCapeShared(WoundedBeastCapeItemDrop.m_itemData.m_shared);
                        WoundedBeastCapeItemDrop.m_itemData.m_dropPrefab = existing;
                    }

                    HaldorExpansionPlugin.DebugLog(
                        $"[HaldorExpansion] Wounded Beast cape already in ObjectDB: {WoundedBeastCapePrefabName}");
                    return WoundedBeastCapeItemDrop != null;
                }

                GameObject source = ItemPrefabLookup.Source(WoundedBeastCapeSourcePrefab);
                if (source == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        $"[HaldorExpansion] Source cape prefab not found: {WoundedBeastCapeSourcePrefab}");
                    return false;
                }

                GameObject capeGo = PrefabManager.Instance.CreateClonedPrefab(WoundedBeastCapePrefabName, source.name);
                if (capeGo == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        "[HaldorExpansion] Failed to clone Wounded Beast cape prefab");
                    return false;
                }

                ItemDrop drop = capeGo.GetComponent<ItemDrop>();
                if (drop == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] ItemDrop missing on Wounded Beast cape");
                    return false;
                }

                ConfigureWoundedBeastCapeShared(drop.m_itemData.m_shared);
                drop.m_itemData.m_dropPrefab = capeGo;

                ItemManager.Instance.AddItem(new CustomItem(capeGo, fixReference: false));
                EnsureWoundedBeastCapeRegisteredInObjectDB(capeGo);

                WoundedBeastCapePrefab = capeGo;
                WoundedBeastCapeItemDrop = drop;

                HaldorExpansionPlugin.DebugLog(
                    $"[HaldorExpansion] Wounded Beast cape registered: {WoundedBeastCapePrefabName} (source={source.name})");
                return true;
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError($"[HaldorExpansion] EnsureWoundedBeastCapeReady error: {e}");
                return false;
            }
        }

        internal static void RegisterWoundedBeastCapeRecipe()
        {
            try
            {
                string recipeName = "Recipe_" + WoundedBeastCapePrefabName;
                if (ItemManager.Instance.GetRecipe(recipeName) != null)
                {
                    HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] Recipe already registered: {recipeName}");
                    return;
                }

                GameObject sourcePrefab = ObjectDB.instance?.GetItemPrefab(WoundedBeastCapeSourcePrefab);
                if (sourcePrefab == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        "[HaldorExpansion] Source cape prefab not found for Wounded Beast recipe");
                    return;
                }

                ItemDrop sourceDrop = sourcePrefab.GetComponent<ItemDrop>();
                if (sourceDrop == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        "[HaldorExpansion] Source cape ItemDrop not found for Wounded Beast recipe");
                    return;
                }

                Recipe sourceRecipe = ObjectDB.instance.GetRecipe(sourceDrop.m_itemData);
                if (sourceRecipe == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        "[HaldorExpansion] Source cape recipe not found for Wounded Beast cape");
                    return;
                }

                string craftingStation = sourceRecipe.m_craftingStation != null
                    ? sourceRecipe.m_craftingStation.name
                    : null;

                string repairStation = sourceRecipe.m_repairStation != null
                    ? sourceRecipe.m_repairStation.name
                    : (craftingStation ?? "forge");

                var recipeConfig = new RecipeConfig
                {
                    Name = recipeName,
                    Item = WoundedBeastCapePrefabName,
                    Amount = 1,
                    CraftingStation = craftingStation,
                    RepairStation = repairStation,
                    MinStationLevel = 1,
                    Enabled = false,
                    Requirements = Array.Empty<RequirementConfig>()
                };

                ItemManager.Instance.AddRecipe(new CustomRecipe(recipeConfig));
                HaldorExpansionPlugin.DebugLog(
                    $"[HaldorExpansion] Registered recipe via Jotunn for {WoundedBeastCapePrefabName}");
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError($"[HaldorExpansion] RegisterWoundedBeastCapeRecipe error: {e}");
            }
        }


        private static void EnsureWoundedBeastCapeRegisteredInObjectDB(GameObject itemPrefab)
        {
            try
            {
                if (ObjectDB.instance == null || itemPrefab == null)
                    return;

                if (ObjectDB.instance.m_items != null && !ObjectDB.instance.m_items.Contains(itemPrefab))
                    ObjectDB.instance.m_items.Add(itemPrefab);

                MethodInfo updateItemHashes = typeof(ObjectDB).GetMethod(
                    "UpdateItemHashes",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                updateItemHashes?.Invoke(ObjectDB.instance, null);
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError(
                    $"[HaldorExpansion] EnsureWoundedBeastCapeRegisteredInObjectDB error: {e}");
            }
        }

        private static void ConfigureWoundedBeastCapeShared(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            shared.m_name = WoundedBeastCapeItemKey;
            shared.m_description = WoundedBeastCapeItemDescKey;

            shared.m_armor = 1f;
            shared.m_armorPerLevel = 0f; // This cloak gains durability only.
            shared.m_maxQuality = 2; // Native upgrader eligibility; saved levels have no cap.
            shared.m_weight = 1f;
            shared.m_value = 0;

            shared.m_maxDurability = ItemUpgradeBalance.WoundedBeastCloak.BaseDurability;
            shared.m_durabilityPerLevel = ItemUpgradeBalance.WoundedBeastCloak.DurabilityPerLevel;
            shared.m_canBeReparied = true;
            shared.m_useDurability = true;

            shared.m_setName = "";
            shared.m_setSize = 0;
            shared.m_setStatusEffect = null;

            if (shared.m_damageModifiers != null)
            {
                shared.m_damageModifiers.RemoveAll(mod => mod.m_type == HitData.DamageType.Frost);
            }

            ApplyWoundedBeastCapeVisualEffect(shared);
        }

        private static void ApplyWoundedBeastCapeVisualEffect(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            SE_Stats se = ScriptableObject.CreateInstance<SE_Stats>();
            se.name = "SE_WoundedBeastCapeVisual";

            ReflectionAccess.SetFieldIfExists(se, "m_name", WoundedBeastCapeEffectNameKey);
            ReflectionAccess.SetFieldIfExists(se, "m_tooltip", WoundedBeastCapeEffectDescKey);

            shared.m_equipStatusEffect = se;
        }
    }
}