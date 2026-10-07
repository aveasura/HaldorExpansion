using HaldorExpansion.Compatibility;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Reflection;
using UnityEngine;

namespace HaldorExpansion.Features.Berserker
{
    internal static class BerserkerCloakItemRegistration
    {
        internal const string PrefabName = "CapeBerserker";
        internal const string SourcePrefabName = "CapeDeerHide";
        internal const string ItemKey = "$item_cape_berserker";
        internal const string ItemDescKey = "$item_cape_berserker_desc";
        internal const string EffectNameKey = "$se_cape_berserker";
        internal const string EffectDescKey = "$se_cape_berserker_desc";

        // Working shop value for the prototype. The existing synchronized shop config can override it.
        internal const int Price = 2500;
        internal const float Armor = 4f;

        internal static GameObject Prefab;
        internal static ItemDrop ItemDrop;

        internal static void ResetCaches()
        {
            Prefab = null;
            ItemDrop = null;
        }

        internal static bool EnsureReady()
        {
            try
            {
                if (ObjectDB.instance == null) return false;

                GameObject existing = ObjectDB.instance.GetItemPrefab(PrefabName);
                if (existing != null)
                {
                    Prefab = existing;
                    ItemDrop = existing.GetComponent<ItemDrop>();
                    if (ItemDrop != null)
                    {
                        ConfigureShared(ItemDrop.m_itemData.m_shared);
                        ItemDrop.m_itemData.m_dropPrefab = existing;
                    }
                    return ItemDrop != null;
                }

                GameObject source = ObjectDB.instance.GetItemPrefab(SourcePrefabName);
                if (source == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        $"[Berserker] Source cape prefab not found: {SourcePrefabName}");
                    return false;
                }

                GameObject cloak = PrefabManager.Instance.CreateClonedPrefab(PrefabName, source.name);
                if (cloak == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[Berserker] Failed to clone deer-hide cape for Berserker cloak.");
                    return false;
                }

                ItemDrop drop = cloak.GetComponent<ItemDrop>();
                if (drop == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[Berserker] ItemDrop missing on Berserker cloak prefab.");
                    return false;
                }

                ConfigureShared(drop.m_itemData.m_shared);
                drop.m_itemData.m_dropPrefab = cloak;

                ItemManager.Instance.AddItem(new CustomItem(cloak, true));
                EnsureRegisteredInObjectDB(cloak);

                Prefab = cloak;
                ItemDrop = drop;
                HaldorExpansionPlugin.DebugLog(
                    $"[Berserker] Cloak registered: {PrefabName} (source={source.name})");
                return true;
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError($"[Berserker] Ensure cloak error: {e}");
                return false;
            }
        }

        internal static void RegisterRecipe()
        {
            try
            {
                string recipeName = "Recipe_" + PrefabName;
                if (ItemManager.Instance.GetRecipe(recipeName) != null) return;

                GameObject source = ObjectDB.instance?.GetItemPrefab(SourcePrefabName);
                ItemDrop sourceDrop = source != null ? source.GetComponent<ItemDrop>() : null;
                if (sourceDrop == null) return;

                Recipe sourceRecipe = ObjectDB.instance?.GetRecipe(sourceDrop.m_itemData);
                if (sourceRecipe == null) return;

                string craftingStation = sourceRecipe.m_craftingStation != null
                    ? sourceRecipe.m_craftingStation.name
                    : null;
                string repairStation = sourceRecipe.m_repairStation != null
                    ? sourceRecipe.m_repairStation.name
                    : craftingStation;

                var config = new RecipeConfig
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

                ItemManager.Instance.AddRecipe(new CustomRecipe(config));
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError($"[Berserker] Register cloak recipe error: {e}");
            }
        }

        private static void ConfigureShared(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null) return;

            shared.m_name = ItemKey;
            shared.m_description = ItemDescKey;
            shared.m_armor = Armor;
            shared.m_armorPerLevel = 0f;
            shared.m_maxQuality = 1;
            shared.m_value = 0;
            shared.m_movementModifier = 0f;

            // The deer cape is only the physical/visual base. Remove inherited equipment/set
            // effects so all gameplay comes from the Berserker feature itself.
            shared.m_setName = string.Empty;
            shared.m_setSize = 0;
            shared.m_setStatusEffect = null;
            shared.m_damageModifiers?.Clear();

            SE_Stats se = ScriptableObject.CreateInstance<SE_Stats>();
            se.name = "SE_BerserkerCloakVisual";
            ReflectionAccess.SetFieldIfExists(se, "m_name", EffectNameKey);
            ReflectionAccess.SetFieldIfExists(se, "m_tooltip", EffectDescKey);
            shared.m_equipStatusEffect = se;
        }

        private static void EnsureRegisteredInObjectDB(GameObject itemPrefab)
        {
            if (ObjectDB.instance == null || itemPrefab == null) return;

            if (ObjectDB.instance.m_items != null && !ObjectDB.instance.m_items.Contains(itemPrefab))
                ObjectDB.instance.m_items.Add(itemPrefab);

            MethodInfo updateItemHashes = typeof(ObjectDB).GetMethod(
                "UpdateItemHashes",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            updateItemHashes?.Invoke(ObjectDB.instance, null);
        }
    }
}
