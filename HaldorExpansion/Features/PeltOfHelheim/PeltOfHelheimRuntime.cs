using System.Collections.Generic;
using HaldorExpansion.Features.HelOath;

namespace HaldorExpansion.Features.PeltOfHelheim
{
    internal static class PeltOfHelheimRuntime
    {
        internal static bool HasCapeEquipped(Player player)
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
                if (prefab == PeltOfHelheimItemRegistration.PrefabName ||
                    item.m_shared.m_name == PeltOfHelheimItemRegistration.ItemKey)
                    return true;
            }
            return false;
        }

        internal static bool IsWardActive(Player player)
        {
            return player != null && player == Player.m_localPlayer && !player.IsDead() &&
                   HasCapeEquipped(player) &&
                   HelOathRuntime.EffectiveTouchStacks(player) >= HelOathState.MaxTouchStacks;
        }

        internal static void Update(Player player)
        {
            if (player == null || player != Player.m_localPlayer) return;
            PeltOfHelheimStatus.Sync(player, IsWardActive(player));
        }
    }
}
