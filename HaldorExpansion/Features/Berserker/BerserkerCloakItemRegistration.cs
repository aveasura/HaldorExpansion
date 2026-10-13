using System;
using System.Reflection;
using HaldorExpansion.Compatibility;
using HaldorExpansion.Features.Upgrades;
using HaldorExpansion.Integration;
using HaldorExpansion.Presentation;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace HaldorExpansion.Features.Berserker
{
    internal static class BerserkerCloakItemRegistration
    {
        // Use the Ashen Cape as the appearance source only. All gameplay stats are assigned below.
        private static readonly string[] SourcePrefabs = { "CapeAsh" };

        internal const int Price = 2500;
        internal const string IconFileName = "berserker_cloak.png";
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

                GameObject existing = ItemPrefabLookup.Registered(BerserkerIds.CloakPrefab);
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
                        BerserkerAppearance.Apply(existing, BerserkerAppearance.Style.Cloak);
                    }

                    return ItemDrop != null;
                }

                GameObject source = FindSourcePrefab();
                if (source == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        $"[Berserker] Source Ashen Cape prefab not found (CapeAsh)");
                    return false;
                }

                GameObject cloak = PrefabManager.Instance.CreateClonedPrefab(BerserkerIds.CloakPrefab, source.name);
                if (cloak == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        "[Berserker] Failed to clone Ashen Cape for Berserker cloak.");
                    return false;
                }

                ItemDrop drop = cloak.GetComponent<ItemDrop>();
                if (drop == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[Berserker] ItemDrop missing on Berserker cloak prefab.");
                    return false;
                }

                ConfigureShared(drop.m_itemData.m_shared);
                ApplyIcon(drop.m_itemData.m_shared);
                drop.m_itemData.m_dropPrefab = cloak;
                BerserkerAppearance.Apply(cloak, BerserkerAppearance.Style.Cloak);

                ItemManager.Instance.AddItem(new CustomItem(cloak, fixReference: false));
                EnsureRegisteredInObjectDB(cloak);

                Prefab = cloak;
                ItemDrop = drop;
                HaldorExpansionPlugin.DebugLog(
                    $"[Berserker] Cloak registered: {BerserkerIds.CloakPrefab} (source={source.name})");
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
                string recipeName = BerserkerIds.CloakRecipe;
                if (ItemManager.Instance.GetRecipe(recipeName) != null) return;

                var config = new RecipeConfig
                {
                    Name = recipeName,
                    Item = BerserkerIds.CloakPrefab,
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
                HaldorExpansionPlugin.Log?.LogError($"[Berserker] Register cloak recipe error: {e}");
            }
        }

        private static GameObject FindSourcePrefab()
        {
            if (ObjectDB.instance == null) return null;
            foreach (string name in SourcePrefabs)
            {
                GameObject source = ItemPrefabLookup.Source(name);
                if (source != null) return source;
            }

            return null;
        }

        private static void ConfigureShared(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null) return;

            shared.m_name = BerserkerIds.CloakItemKey;
            shared.m_description = BerserkerIds.CloakDescriptionKey;
            shared.m_armor = Armor;
            shared.m_armorPerLevel = ItemUpgradeBalance.BerserkerCloak.ArmorPerLevel;
            shared.m_maxQuality = 2; // Native upgrader eligibility; saved levels have no cap.
            shared.m_value = 0;
            shared.m_movementModifier = 0f;
            shared.m_eitrRegenModifier = 0f;
            // CapeAsh grants -10% attack stamina use and -20% block stamina use.
            // They belong to the vanilla Ashen Cape and must NOT carry over.
            // Reflection keeps this compatible with Valheim builds where these
            // optional Ashlands fields are absent or have moved.
            ReflectionAccess.SetFieldIfExists(shared, "m_attackStaminaModifier", 0f);
            ReflectionAccess.SetFieldIfExists(shared, "m_blockStaminaModifier", 0f);
            shared.m_canBeReparied = true;
            shared.m_useDurability = true;

            // Keep Berserker durability and weight unchanged when switching models.
            // This is a visual-only change, not a progression/stat adjustment.
            shared.m_maxDurability = 1200f;
            shared.m_durabilityPerLevel = ItemUpgradeBalance.BerserkerCloak.DurabilityPerLevel;
            shared.m_weight = 4f;
            BerserkerAppearance.ApplyArmorMaterial(shared, BerserkerAppearance.Style.Cloak);

            // CapeAsh supplies only the visual prefab. Strip inherited set bonuses and
            // damage modifiers (including frost resistance); equip the Berserker
            // status effect below. Berserker gameplay stays in its runtime patches.
            shared.m_setName = string.Empty;
            shared.m_setSize = 0;
            shared.m_setStatusEffect = null;
            shared.m_damageModifiers?.Clear();

            SE_Stats se = ScriptableObject.CreateInstance<SE_Stats>();
            se.name = BerserkerIds.CloakVisualStatus;
            ReflectionAccess.SetFieldIfExists(se, "m_name", BerserkerIds.CloakEffectKey);
            ReflectionAccess.SetFieldIfExists(se, "m_tooltip", BerserkerIds.CloakEffectDescriptionKey);
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