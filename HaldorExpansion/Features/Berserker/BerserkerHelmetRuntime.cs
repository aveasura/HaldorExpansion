using System.Collections.Generic;
using UnityEngine;

namespace HaldorExpansion.Features.Berserker
{
    internal static class BerserkerHelmetRuntime
    {
        internal const float ArmorPerWrathStage = 4f;
        internal const float StaggerReductionPerWrathStage = 0.15f;
        internal const float DamageReductionPerWrathStage = 0.01f;

        internal static bool IsHelmet(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null) return false;
            if (item.m_shared.m_name == BerserkerHelmetItemRegistration.ItemKey) return true;

            GameObject dropPrefab = item.m_dropPrefab;
            return dropPrefab != null && dropPrefab.name == BerserkerHelmetItemRegistration.PrefabName;
        }

        internal static bool HasEquipped(Player player)
        {
            if (player == null) return false;

            Inventory inventory = player.GetInventory();
            if (inventory == null) return false;

            List<ItemDrop.ItemData> items = inventory.GetAllItems();
            if (items == null) return false;

            foreach (ItemDrop.ItemData item in items)
            {
                if (item != null && item.m_equipped && IsHelmet(item))
                    return true;
            }

            return false;
        }

        internal static int WrathStage(Player player)
        {
            if (!HasEquipped(player)) return 0;
            return Mathf.Clamp(BerserkerRuntime.EffectiveWrathStage(player), 0, BerserkerState.MaxWrathStage);
        }

        internal static float ArmorBonus(Player player)
            => WrathStage(player) * ArmorPerWrathStage;

        internal static float IncomingDamageMultiplier(Player player)
        {
            int stage = WrathStage(player);
            return Mathf.Clamp(1f - stage * DamageReductionPerWrathStage, 0f, 1f);
        }

        internal static float IncomingStaggerMultiplier(Player player)
        {
            int stage = WrathStage(player);
            return Mathf.Clamp(1f - stage * StaggerReductionPerWrathStage, 0f, 1f);
        }
    }
}
