using UnityEngine;

namespace HaldorExpansion.Features.Berserker
{
    internal static class BerserkerEquipment
    {
        internal static bool IsAxe(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null) return false;
            if (item.m_shared.m_name == BerserkerItemRegistration.ItemKey) return true;

            GameObject dropPrefab = item.m_dropPrefab;
            return dropPrefab != null && dropPrefab.name == BerserkerItemRegistration.PrefabName;
        }

        internal static bool Equipped(Player player)
            => player != null && IsAxe(player.GetCurrentWeapon());
    }
}
