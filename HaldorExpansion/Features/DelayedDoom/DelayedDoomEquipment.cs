using System.Collections.Generic;

namespace HaldorExpansion.Features.DelayedDoom
{
    internal static class DelayedDoomEquipment
    {
        private static readonly HashSet<string> DelayedDoomItemKeys = new HashSet<string>
        {
            "$item_chest_delayeddoom"
        };

        internal static bool HasDelayedDoomEquipped(Player player)
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

                if (!string.IsNullOrEmpty(itemKey) && DelayedDoomItemKeys.Contains(itemKey))
                    return true;
            }

            return false;
        }
    }
}
