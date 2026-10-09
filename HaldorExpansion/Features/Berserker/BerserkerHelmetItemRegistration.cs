using System;
using System.Reflection;
using HaldorExpansion.Compatibility;
using HaldorExpansion.Integration;
using HaldorExpansion.Presentation;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace HaldorExpansion.Features.Berserker
{
    internal static class BerserkerHelmetItemRegistration
    {
        internal const string PrefabName = "HelmetBerserker";
        internal const string SourcePrefabName = "HelmetFlametal";
        internal const string ItemKey = "$item_helmet_berserker";
        internal const string ItemDescKey = "$item_helmet_berserker_desc";
        internal const string EffectNameKey = "$se_helmet_berserker";
        internal const string EffectDescKey = "$se_helmet_berserker_desc";

        internal const int Price = 2500;
        internal const string IconFileName = "berserker_helmet.png";
        internal const float BaseArmor = 18f;

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

                GameObject existing = ItemPrefabLookup.Registered(PrefabName);
                if (existing != null && existing == Prefab && ItemDrop != null)
                    return true;

                if (existing != null)
                {
                    Prefab = existing;
                    ItemDrop = existing.GetComponent<ItemDrop>();
                    if (ItemDrop != null)
                    {
                        ConfigureShared(ItemDrop.m_itemData.m_shared);
                        ApplyIcon(ItemDrop.m_itemData.m_shared);
                        ItemDrop.m_itemData.m_dropPrefab = existing;
                        BerserkerAppearance.Apply(existing, BerserkerAppearance.Style.Helmet);
                    }

                    return ItemDrop != null;
                }

                GameObject source = ItemPrefabLookup.Source(SourcePrefabName);
                if (source == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        $"[Berserker] Source Flametal helmet prefab not found: {SourcePrefabName}");
                    return false;
                }

                GameObject helmet = PrefabManager.Instance.CreateClonedPrefab(PrefabName, source.name);
                if (helmet == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        "[Berserker] Failed to clone Flametal helmet for Berserker helmet.");
                    return false;
                }

                ItemDrop drop = helmet.GetComponent<ItemDrop>();
                if (drop == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[Berserker] ItemDrop missing on Berserker helmet prefab.");
                    return false;
                }

                ConfigureShared(drop.m_itemData.m_shared);
                ApplyIcon(drop.m_itemData.m_shared);
                drop.m_itemData.m_dropPrefab = helmet;
                BerserkerAppearance.Apply(helmet, BerserkerAppearance.Style.Helmet);

                ItemManager.Instance.AddItem(new CustomItem(helmet, true));
                EnsureRegisteredInObjectDB(helmet);

                Prefab = helmet;
                ItemDrop = drop;
                HaldorExpansionPlugin.DebugLog(
                    $"[Berserker] Helmet registered: {PrefabName} (source={source.name})");
                return true;
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError($"[Berserker] Ensure helmet error: {e}");
                return false;
            }
        }

        internal static void RegisterRecipe()
        {
            try
            {
                string recipeName = "Recipe_" + PrefabName;
                if (ItemManager.Instance.GetRecipe(recipeName) != null) return;

                var config = new RecipeConfig
                {
                    Name = recipeName,
                    Item = PrefabName,
                    Amount = 1,
                    CraftingStation = "forge",
                    RepairStation = "forge",
                    MinStationLevel = 1,
                    Enabled = false,
                    Requirements = Array.Empty<RequirementConfig>()
                };

                ItemManager.Instance.AddRecipe(new CustomRecipe(config));
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError($"[Berserker] Register helmet recipe error: {e}");
            }
        }

        private static void ConfigureShared(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null) return;

            shared.m_name = ItemKey;
            shared.m_description = ItemDescKey;
            shared.m_armor = BaseArmor;
            shared.m_armorPerLevel = 0f;
            shared.m_maxQuality = 1;
            shared.m_value = 0;
            shared.m_movementModifier = 0f;
            shared.m_canBeReparied = true;
            shared.m_useDurability = true;
            BerserkerAppearance.ApplyArmorMaterial(shared, BerserkerAppearance.Style.Helmet);

            // The Flametal helmet supplies only the model/slot. Berserker mechanics are handled
            // by the feature runtime so they can scale from the owner's current Wrath stage.
            shared.m_setName = string.Empty;
            shared.m_setSize = 0;
            shared.m_setStatusEffect = null;
            shared.m_damageModifiers?.Clear();

            SE_Stats se = ScriptableObject.CreateInstance<SE_Stats>();
            se.name = "SE_BerserkerHelmetVisual";
            ReflectionAccess.SetFieldIfExists(se, "m_name", EffectNameKey);
            ReflectionAccess.SetFieldIfExists(se, "m_tooltip", EffectDescKey);
            shared.m_equipStatusEffect = se;
        }

        private static void ApplyIcon(ItemDrop.ItemData.SharedData shared)
        {
            EmbeddedIconLoader.TrySetEmbeddedIcon(shared, IconFileName);
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