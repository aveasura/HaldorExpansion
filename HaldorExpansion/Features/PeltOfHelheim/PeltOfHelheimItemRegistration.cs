using System;
using System.Reflection;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using HaldorExpansion.Presentation;

namespace HaldorExpansion.Features.PeltOfHelheim
{
    internal static class PeltOfHelheimItemRegistration
    {
        internal const string PrefabName = "CapePeltOfHelheim";
        internal const int Price = 2500;

        internal const string ItemKey = "$item_cape_peltofhelheim";
        internal const string ItemDescKey = "$item_cape_peltofhelheim_desc";
        internal const string EffectNameKey = "$se_peltofhelheim";
        internal const string EffectDescKey = "$se_peltofhelheim_desc";

        private static readonly string[] SourcePrefabs = { "CapeTroll", "CapeTrollHide" };

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
                if (ObjectDB.instance == null) return false;

                GameObject existing = ObjectDB.instance.GetItemPrefab(PrefabName);
                if (existing != null)
                {
                    CapePrefab = existing;
                    CapeItemDrop = existing.GetComponent<ItemDrop>();
                    if (CapeItemDrop != null)
                    {
                        ConfigureShared(CapeItemDrop.m_itemData.m_shared);
                        CapeItemDrop.m_itemData.m_dropPrefab = existing;
                        PeltOfHelheimAppearance.ApplyToPrefab(existing);
                    }
                    return CapeItemDrop != null;
                }

                GameObject source = FindSourcePrefab();
                if (source == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        "[HaldorExpansion] Troll cape source prefab not found for Pelt of Helheim.");
                    return false;
                }

                GameObject cape = PrefabManager.Instance.CreateClonedPrefab(PrefabName, source.name);
                if (cape == null) return false;

                ItemDrop drop = cape.GetComponent<ItemDrop>();
                if (drop == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        "[HaldorExpansion] ItemDrop missing on Pelt of Helheim prefab.");
                    return false;
                }

                ConfigureShared(drop.m_itemData.m_shared);
                drop.m_itemData.m_dropPrefab = cape;
                PeltOfHelheimAppearance.ApplyToPrefab(cape);

                ItemManager.Instance.AddItem(new CustomItem(cape, true));
                EnsureRegisteredInObjectDB(cape);

                CapePrefab = cape;
                CapeItemDrop = drop;
                HaldorExpansionPlugin.DebugLog(
                    $"[HaldorExpansion] Pelt of Helheim registered: {PrefabName} (source={source.name})");
                return true;
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError(
                    $"[HaldorExpansion] Ensure Pelt of Helheim error: {e}");
                return false;
            }
        }

        internal static void RegisterRecipe()
        {
            try
            {
                string recipeName = "Recipe_" + PrefabName;
                if (ItemManager.Instance.GetRecipe(recipeName) != null) return;

                GameObject source = FindSourcePrefab();
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
                HaldorExpansionPlugin.Log?.LogError(
                    $"[HaldorExpansion] Register Pelt of Helheim recipe error: {e}");
            }
        }

        private static GameObject FindSourcePrefab()
        {
            if (ObjectDB.instance == null) return null;
            foreach (string name in SourcePrefabs)
            {
                GameObject source = ObjectDB.instance.GetItemPrefab(name);
                if (source != null) return source;
            }
            return null;
        }

        private static void ConfigureShared(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null) return;

            shared.m_name = ItemKey;
            shared.m_description = ItemDescKey;
            
            shared.m_armor = 4f;
            shared.m_maxDurability = 1800f;
            
            shared.m_maxQuality = 1;
            shared.m_armorPerLevel = 0f;
            shared.m_durabilityPerLevel = 0f;
            shared.m_value = 0;
            shared.m_canBeReparied = true;
            shared.m_useDurability = true;

            shared.m_setName = string.Empty;
            shared.m_setSize = 0;
            shared.m_setStatusEffect = null;
            shared.m_equipStatusEffect = null;
            shared.m_movementModifier = 0f;

            // The Troll Cape is only the visual/physical base. It must not carry any inherited
            // resistance or set modifier; the Pierce resistance is granted dynamically at Touch V.
            shared.m_damageModifiers?.Clear();

            if (shared.m_armorMaterial != null)
                shared.m_armorMaterial = PeltOfHelheimAppearance.MakeBlackMaterial(shared.m_armorMaterial);
            
            ApplyPeltIcon(shared);
        }
        
        private static void ApplyPeltIcon(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null) return;

            if (!EmbeddedIconLoader.TrySetEmbeddedIcon(shared, "Pelt_of_Helheim.png"))
            {
                HaldorExpansionPlugin.Log?.LogWarning(
                    "[HaldorExpansion] Embedded icon missing for Pelt of Helheim: Pelt_of_Helheim.png");
            }
        }

        private static void EnsureRegisteredInObjectDB(GameObject itemPrefab)
        {
            if (ObjectDB.instance == null || itemPrefab == null) return;
            if (ObjectDB.instance.m_items != null && !ObjectDB.instance.m_items.Contains(itemPrefab))
                ObjectDB.instance.m_items.Add(itemPrefab);

            MethodInfo updateItemHashes = typeof(ObjectDB).GetMethod(
                "UpdateItemHashes", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            updateItemHashes?.Invoke(ObjectDB.instance, null);
        }
    }
}
