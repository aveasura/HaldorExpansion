using HaldorExpansion.Compatibility;
using HaldorExpansion.Features.DelayedDoom;
using System.Collections.Generic;
using UnityEngine;

namespace HaldorExpansion.Features.WoundedBeast
{
    internal static class WoundedBeastRuntime
    {
        // С Кирасой безмолвной расплаты эффект хила от плаща в 2 раза слабее
        private const float WoundedBeastCapeRegenWithDelayedDoomMultiplier = 1f / 2f; 

        internal const float WoundedBeastCapeIncomingDamageMultiplier = 1.18f;
        internal const float WoundedBeastCapeDotRegenSuppressDuration = 2f;
        private const float WoundedBeastCapeRegenTickInterval = 1f;

        private static readonly Dictionary<long, float> WoundedBeastCapeNextRegenTickTimes = new Dictionary<long, float>();
        private static readonly Dictionary<long, float> WoundedBeastCapeRegenSuppressedUntil = new Dictionary<long, float>();

        internal static void ResetWoundedBeastCapeState()
        {
            WoundedBeastItemRegistration.WoundedBeastCapePrefab = null;
            WoundedBeastItemRegistration.WoundedBeastCapeItemDrop = null;
            WoundedBeastCapeNextRegenTickTimes.Clear();
            WoundedBeastCapeRegenSuppressedUntil.Clear();
        }

        internal static bool HasWoundedBeastCapeEquipped(Player player)
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
                if (item == null || !item.m_equipped || item.m_shared == null)
                    continue;

                if (item.m_shared.m_name == WoundedBeastItemRegistration.WoundedBeastCapeItemKey)
                    return true;
            }

            return false;
        }

        internal static void UpdateWoundedBeastCapeRegen(Player player)
        {
            if (player == null)
                return;

            long playerId = player.GetPlayerID();

            if (!HasWoundedBeastCapeEquipped(player) || player.IsDead())
            {
                WoundedBeastCapeNextRegenTickTimes.Remove(playerId);
                return;
            }

            float now = Time.time;
            if (WoundedBeastCapeRegenSuppressedUntil.TryGetValue(playerId, out float suppressedUntil) && now < suppressedUntil)
                return;

            if (!WoundedBeastCapeNextRegenTickTimes.TryGetValue(playerId, out float nextTick) || now >= nextTick)
            {
                WoundedBeastCapeNextRegenTickTimes[playerId] = now + WoundedBeastCapeRegenTickInterval;
                TryApplyWoundedBeastCapeRegenTick(player);
            }
        }

        internal static void ApplyWoundedBeastCapeIncomingDamagePenalty(Player player, HitData hit)
        {
            if (player == null || hit == null || !HasWoundedBeastCapeEquipped(player))
                return;

            ScaleWoundedBeastCapeDamageTypes(ref hit.m_damage, WoundedBeastCapeIncomingDamageMultiplier);

            if (HasWoundedBeastCapeSuppressionDamage(hit))
            {
                long playerId = player.GetPlayerID();
                WoundedBeastCapeRegenSuppressedUntil[playerId] = Time.time + WoundedBeastCapeDotRegenSuppressDuration;
            }
        }

        private static void TryApplyWoundedBeastCapeRegenTick(Player player)
        {
            float currentHealth = CharacterHealthAccess.GetCharacterHealth(player);
            float maxHealth = CharacterHealthAccess.GetCharacterMaxHealth(player);

            if (currentHealth <= 0f || maxHealth <= 0f || currentHealth >= maxHealth)
                return;

            float regenAmount = GetWoundedBeastCapeRegenPerSecond(currentHealth, maxHealth);
            if (regenAmount <= 0f)
                return;

            if (DelayedDoomEquipment.HasDelayedDoomEquipped(player))
            {
                regenAmount *= WoundedBeastCapeRegenWithDelayedDoomMultiplier;
            }

            float newHealth = Mathf.Min(maxHealth, currentHealth + regenAmount);
            if (newHealth <= currentHealth + 0.001f)
                return;

            CharacterHealthAccess.SetCharacterHealth(player, newHealth);

            HaldorExpansionPlugin.DebugLog(
                $"[HaldorExpansion] Wounded Beast regen tick: " +
                $"current={currentHealth:0.##}, " +
                $"max={maxHealth:0.##}, " +
                $"regen={regenAmount:0.##}, " +
                $"new={newHealth:0.##}, " +
                $"delayedDoom={DelayedDoomEquipment.HasDelayedDoomEquipped(player)}");
        }

        private static float GetWoundedBeastCapeRegenPerSecond(float currentHealth, float maxHealth)
        {
            float hpFraction = currentHealth / maxHealth;

            if (hpFraction > 0.80f) return 0f;
            if (hpFraction > 0.75f) return 1f;
            if (hpFraction > 0.65f) return 2f;
            if (hpFraction > 0.60f) return 3f;
            if (hpFraction > 0.45f) return 5f;
            if (hpFraction > 0.30f) return 6f;
            if (hpFraction > 0.20f) return 7f;
            return 10f;
        }

        private static bool HasWoundedBeastCapeSuppressionDamage(HitData hit)
        {
            HitData.DamageTypes damage = hit.m_damage;
            return damage.m_fire > 0f || damage.m_poison > 0f || damage.m_spirit > 0f;
        }

        private static void ScaleWoundedBeastCapeDamageTypes(ref HitData.DamageTypes damage, float multiplier)
        {
            damage.m_damage *= multiplier;
            damage.m_blunt *= multiplier;
            damage.m_slash *= multiplier;
            damage.m_pierce *= multiplier;
            damage.m_fire *= multiplier;
            damage.m_frost *= multiplier;
            damage.m_lightning *= multiplier;
            damage.m_poison *= multiplier;
            damage.m_spirit *= multiplier;
        }
    }
}
