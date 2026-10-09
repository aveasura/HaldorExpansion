using System.Collections.Generic;

namespace HaldorExpansion.Features.Berserker
{
    internal static class BerserkerCloakRuntime
    {
        internal const float IncomingDamageMultiplier = 1.30f;
        internal const float DebtGenerationMultiplier = 1.50f;

        internal static bool HasEquipped(Player player)
        {
            if (player == null) return false;

            Inventory inventory = player.GetInventory();
            if (inventory == null) return false;

            List<ItemDrop.ItemData> items = inventory.GetAllItems();
            if (items == null) return false;

            foreach (ItemDrop.ItemData item in items)
            {
                if (item == null || !item.m_equipped || item.m_shared == null) continue;

                string prefab = item.m_dropPrefab != null ? item.m_dropPrefab.name : null;
                if (prefab == BerserkerIds.CloakPrefab ||
                    item.m_shared.m_name == BerserkerIds.CloakItemKey)
                    return true;
            }

            return false;
        }

        internal static float DebtMultiplier(Player player)
            => HasEquipped(player) ? DebtGenerationMultiplier : 1f;
    }
}