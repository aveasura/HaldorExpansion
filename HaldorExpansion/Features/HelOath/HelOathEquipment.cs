using System.Collections.Generic;

namespace HaldorExpansion.Features.HelOath
{
    internal static class HelOathEquipment
    {
        private const string HelmetPrefab = "HelmetFenring";
        private const string ChestPrefab = "ArmorFenringChest";
        private const string LegsPrefab = "ArmorFenringLegs";

        private const string HelmetKey = "$item_helmet_fenris";
        private const string ChestKey = "$item_chest_fenris";
        private const string LegsKey = "$item_legs_fenris";

        internal static bool HasFullFenrisSet(Player player)
        {
            if (player == null) return false;
            Inventory inventory = player.GetInventory();
            if (inventory == null) return false;
            List<ItemDrop.ItemData> items = inventory.GetAllItems();
            if (items == null) return false;

            bool helmet = false, chest = false, legs = false;
            foreach (ItemDrop.ItemData item in items)
            {
                if (item == null || !item.m_equipped || item.m_shared == null) continue;
                string prefab = item.m_dropPrefab != null ? item.m_dropPrefab.name : null;
                string key = item.m_shared.m_name;
                if (prefab == HelmetPrefab || key == HelmetKey) helmet = true;
                else if (prefab == ChestPrefab || key == ChestKey) chest = true;
                else if (prefab == LegsPrefab || key == LegsKey) legs = true;
                if (helmet && chest && legs) return true;
            }
            return false;
        }
    }
}
