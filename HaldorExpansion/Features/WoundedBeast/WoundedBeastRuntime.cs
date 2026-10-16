using System.Collections.Generic;
using HaldorExpansion.Compatibility;
using HaldorExpansion.Features.DelayedDoom;
using UnityEngine;

namespace HaldorExpansion.Features.WoundedBeast
{
    internal static class WoundedBeastRuntime
    {
        // С Кирасой безмолвной расплаты эффект хила от плаща в 2 раза слабее
        private const float WoundedBeastCapeRegenWithDelayedDoomMultiplier = 1f / 2f;
        private const float WoundedBeastCapeRegenWithDotMultiplier = 1f / 2f;

        internal const float WoundedBeastCapeIncomingDamageMultiplier = 1.20f;
        private const float WoundedBeastCapeRegenTickInterval = 1f;

        private static readonly Dictionary<long, float> WoundedBeastCapeNextRegenTickTimes =
            new Dictionary<long, float>();

        internal static void ResetWoundedBeastCapeState()
        {
            WoundedBeastItemRegistration.WoundedBeastCapePrefab = null;
            WoundedBeastItemRegistration.WoundedBeastCapeItemDrop = null;
            WoundedBeastCapeNextRegenTickTimes.Clear();
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
            if (!WoundedBeastCapeNextRegenTickTimes.TryGetValue(playerId, out float nextTick) || now >= nextTick)
            {
                WoundedBeastCapeNextRegenTickTimes[playerId] = now + WoundedBeastCapeRegenTickInterval;
                TryApplyWoundedBeastCapeRegenTick(player);
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

            bool dotActive = HasWoundedBeastCapeSuppressingDot(player);
            if (dotActive)
                regenAmount *= WoundedBeastCapeRegenWithDotMultiplier;

            float newHealth = Mathf.Min(maxHealth, currentHealth + regenAmount);
            if (newHealth <= currentHealth + 0.001f)
                return;

            CharacterHealthAccess.SetCharacterHealth(player, newHealth);

            if (HaldorExpansionPlugin.DebugLogging)
            {
                HaldorExpansionPlugin.DebugLog(
                    $"[HaldorExpansion] Wounded Beast regen tick: " +
                    $"current={currentHealth:0.##}, " +
                    $"max={maxHealth:0.##}, " +
                    $"regen={regenAmount:0.##}, " +
                    $"new={newHealth:0.##}, " +
                    $"delayedDoom={DelayedDoomEquipment.HasDelayedDoomEquipped(player)}, dotActive={dotActive}");
            }
        }

        private static float GetWoundedBeastCapeRegenPerSecond(float currentHealth, float maxHealth)
        {
            float hpFraction = currentHealth / maxHealth;

            if (hpFraction > 0.80f) return 0f;
            if (hpFraction > 0.75f) return 1f;
            if (hpFraction > 0.65f) return 1.5f;
            if (hpFraction > 0.60f) return 2f;
            if (hpFraction > 0.45f) return 4f;
            if (hpFraction > 0.30f) return 5.5f;
            if (hpFraction > 0.20f) return 7f;
            return 10f;
        }

        private static bool HasWoundedBeastCapeSuppressingDot(Player player)
        {
            List<StatusEffect> effects = player.GetSEMan()?.GetStatusEffects();
            if (effects == null)
                return false;

            foreach (StatusEffect effect in effects)
            {
                // Valheim uses SE_Burning for both fire and spirit, and SE_Poison for poison.
                if (effect is SE_Burning || effect is SE_Poison)
                    return true;
            }

            return false;
        }
    }
}