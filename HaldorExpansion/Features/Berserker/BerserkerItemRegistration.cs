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
    internal static class BerserkerItemRegistration
    {
        internal const string SourcePrefabName = "BattleaxeGold_BloodLightning";
        internal const int Price = 2500;
        internal const string IconFileName = "berserker_axe.png";
        internal const float BaseSlashDamage = 95f;
        internal const float SecondaryBaseDamage = 130f;
        internal const float MovementModifier = -0.15f;
        internal const float PrimaryStamina = 14f;
        internal const float AttackForce = 45f;
        internal const float BlockPower = 14f;
        internal const float DeflectionForce = 20f;
        internal const float PrimaryStaggerMultiplier = 1.2f;
        internal const float SecondaryStaggerMultiplier = 3f;
        internal const string SecondaryAttackAnimation = "swing_sledge";

        internal static GameObject Prefab;
        internal static ItemDrop ItemDrop;

        internal static bool EnsureReady()
        {
            try
            {
                if (ObjectDB.instance == null) return false;

                GameObject existing = ItemPrefabLookup.Registered(BerserkerIds.AxePrefab);
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
                        BerserkerAppearance.Apply(existing, BerserkerAppearance.Style.Axe);
                    }

                    return ItemDrop != null;
                }

                GameObject source = ItemPrefabLookup.Source(SourcePrefabName);
                if (source == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        $"[Berserker] Source prefab not found: {SourcePrefabName}. " +
                        "This prototype requires the Valheim build containing the Deep North Thunderblood Greataxe.");
                    return false;
                }

                GameObject clone = PrefabManager.Instance.CreateClonedPrefab(BerserkerIds.AxePrefab, source.name);
                if (clone == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[Berserker] Failed to clone Thunderblood Greataxe prefab.");
                    return false;
                }

                ItemDrop drop = clone.GetComponent<ItemDrop>();
                if (drop == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[Berserker] ItemDrop missing on cloned greataxe.");
                    return false;
                }

                ConfigureShared(drop.m_itemData.m_shared);
                ApplyIcon(drop.m_itemData.m_shared);
                drop.m_itemData.m_dropPrefab = clone;
                BerserkerAppearance.Apply(clone, BerserkerAppearance.Style.Axe);
                ItemManager.Instance.AddItem(new CustomItem(clone, true));
                EnsureRegisteredInObjectDB(clone);

                Prefab = clone;
                ItemDrop = drop;
                return true;
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError($"[Berserker] EnsureReady error: {e}");
                return false;
            }
        }

        // An intentionally disabled recipe still tells Valheim which forge repairs the weapon.
        internal static void RegisterRecipe()
        {
            try
            {
                string recipeName = BerserkerIds.AxeRecipe;
                if (ItemManager.Instance.GetRecipe(recipeName) != null) return;

                var config = new RecipeConfig
                {
                    Name = recipeName,
                    Item = BerserkerIds.AxePrefab,
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
                HaldorExpansionPlugin.Log?.LogError($"[Berserker] Register axe recipe error: {e}");
            }
        }

        private static void ApplyIcon(ItemDrop.ItemData.SharedData shared)
        {
            // The embedded image is optional until the final illustration has been supplied.
            EmbeddedIconLoader.TrySetEmbeddedIcon(shared, IconFileName);
        }

        private static void ConfigureShared(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null) return;

            shared.m_name = BerserkerIds.AxeItemKey;
            shared.m_description = BerserkerIds.AxeDescriptionKey;
            shared.m_value = 0;
            shared.m_canBeReparied = true;
            shared.m_useDurability = true;

            // Keep the Thunderblood Greataxe model/moveset and its native missing-health damage scaling,
            // but tune the weapon itself around the Berserker risk/reward loop.
            shared.m_damages = new HitData.DamageTypes();
            shared.m_damages.m_slash = BaseSlashDamage;
            shared.m_damagesPerLevel = new HitData.DamageTypes();
            shared.m_maxQuality = 1;

            // Less raw safety/control than the vanilla Deep North greataxe: the build should prefer
            // taking real HP damage instead of leaning on blocking/parrying and heavy stagger-lock.
            shared.m_movementModifier = MovementModifier;
            shared.m_attackForce = AttackForce;
            shared.m_blockPower = BlockPower;
            shared.m_deflectionForce = DeflectionForce;
            shared.m_deflectionForcePerLevel = 0f;

            if (shared.m_attack != null)
            {
                shared.m_attack.m_attackStamina = PrimaryStamina;
                shared.m_attack.m_staggerMultiplier = PrimaryStaggerMultiplier;
            }

            if (shared.m_secondaryAttack != null)
            {
                shared.m_secondaryAttack.m_staggerMultiplier = SecondaryStaggerMultiplier;
                // Make the overhead secondary a deliberate execution hit: 130 base damage
                // from the shared 95 Slash weapon profile, before missing-HP scaling/Reckoning.
                shared.m_secondaryAttack.m_damageMultiplier = SecondaryBaseDamage / BaseSlashDamage;

                // Keep the Berserker secondary attack mechanics/hit data, but use the
                // vanilla Stagbreaker overhead slam animation for a heavier execution feel.
                shared.m_secondaryAttack.m_attackAnimation = SecondaryAttackAnimation;
            }

            // Thunderblood's chain-lightning proc is NOT an attack status effect in current Valheim.
            // It is spawned directly by both Attack objects through m_spawnOnHit / m_spawnOnHitChance.
            // Clear that inherited proc while deliberately leaving m_damageMultiplierPerMissingHP intact.
            shared.m_attackStatusEffect = null;
            ReflectionAccess.SetFieldIfExists(shared, "m_attackStatusEffectChance", 0f);
            ClearSpawnOnHit(shared.m_attack);
            ClearSpawnOnHit(shared.m_secondaryAttack);

            shared.m_setName = "";
            shared.m_setSize = 0;
            shared.m_setStatusEffect = null;
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

        private static void ClearSpawnOnHit(Attack attack)
        {
            if (attack == null) return;

            // Reflection matches the defensive cleanup pattern used by Norn Thread and avoids
            // binding the mod to field visibility changes between Valheim builds.
            ReflectionAccess.SetFieldIfExists(attack, "m_spawnOnHit", null);
            ReflectionAccess.SetFieldIfExists(attack, "m_spawnOnHitChance", 0f);
        }
    }
}