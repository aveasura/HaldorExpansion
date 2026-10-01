using System.Collections.Generic;

namespace HaldorExpansion.Features.PitKing
{
    internal static class PitKingEquipment
    {

        private static readonly HashSet<string> PitKingChestItemKeys = new HashSet<string>
        {
            PitKingItemRegistration.PitKingChestItemKey
        };

        internal static bool HasPitKingChestEquipped(Player player)
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

                string itemKey = item.m_shared.m_name;
                if (!string.IsNullOrEmpty(itemKey) && PitKingChestItemKeys.Contains(itemKey))
                    return true;
            }

            return false;
        }
    }
}
