using System.Collections.Generic;
using System.Reflection;
using HaldorExpansion.Features.PitKing;

namespace HaldorExpansion.Features.Cestus
{
    internal static class CestusEquipment
    {
        private static readonly FieldInfo RightHand = typeof(Humanoid).GetField(
            "m_rightItem", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly FieldInfo LeftHand = typeof(Humanoid).GetField(
            "m_leftItem", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly HashSet<string> CestusItemKeys = new HashSet<string>
        {
            "$item_weapon_gritcestus"
        };

        internal static bool HasCestusEquipped(Player player)
        {
            if (player == null)
                return false;

            ItemDrop.ItemData currentWeapon = player.GetCurrentWeapon();
            if (IsCestusWeapon(currentWeapon))
                return true;

            ItemDrop.ItemData rightItem = GetPlayerHandItem(player, "m_rightItem");
            if (IsCestusWeapon(rightItem))
                return true;

            ItemDrop.ItemData leftItem = GetPlayerHandItem(player, "m_leftItem");
            if (IsCestusWeapon(leftItem))
                return true;

            Inventory inventory = player.GetInventory();
            if (inventory == null)
                return false;

            List<ItemDrop.ItemData> items = inventory.GetAllItems();
            if (items == null)
                return false;

            foreach (ItemDrop.ItemData item in items)
            {
                if (item == null || !item.m_equipped)
                    continue;

                if (IsCestusWeapon(item))
                    return true;

                if (item.m_shared == null)
                    continue;

                string itemKey = item.m_shared.m_name;
                if (!string.IsNullOrEmpty(itemKey) && CestusItemKeys.Contains(itemKey))
                    return true;
            }

            return false;
        }

        internal static ItemDrop.ItemData GetPlayerHandItem(Player player, string fieldName)
        {
            if (player == null || string.IsNullOrWhiteSpace(fieldName))
                return null;

            FieldInfo field = fieldName == "m_rightItem" ? RightHand :
                fieldName == "m_leftItem" ? LeftHand : typeof(Humanoid).GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            return field?.GetValue(player) as ItemDrop.ItemData;
        }

        internal static bool HasCestusSetBonus(Player player, bool? cestusEquipped = null)
        {
            return player != null &&
                   (cestusEquipped ?? HasCestusEquipped(player)) &&
                   PitKingEquipment.HasPitKingChestEquipped(player);
        }

        internal static bool IsCestusWeapon(ItemDrop.ItemData item)
        {
            if (item == null)
                return false;

            if (item.m_dropPrefab != null && item.m_dropPrefab.name == CestusItemRegistration.GritCestusPrefabName)
                return true;

            if (item.m_shared != null && item.m_shared.m_name == CestusItemRegistration.CestusItemKey)
                return true;

            return false;
        }

        internal static bool HasEquippedCestus(Player player)
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
                if (item == null)
                    continue;

                if (!item.m_equipped)
                    continue;

                if (IsCestusWeapon(item))
                    return true;
            }

            return false;
        }
    }
}