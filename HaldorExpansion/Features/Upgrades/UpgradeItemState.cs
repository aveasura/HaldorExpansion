using System;
using HaldorExpansion.Features.Berserker;
using HaldorExpansion.Features.Brisingamen;
using HaldorExpansion.Features.Cestus;
using HaldorExpansion.Features.HelOath;
using HaldorExpansion.Features.ShadowCrossbow;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Features.Upgrades
{
    internal static class UpgradeItemState
    {
        // Crossbow and bow did not previously have an InventoryRebind. Reattach loaded
        // instances to current prefabs without replacing ItemData or its saved metadata.
        // Also normalize the other two weapons; their existing rebinds remain compatible.
        private static readonly string[] WeaponPrefabs =
        {
            BerserkerIds.AxePrefab,
            CestusItemRegistration.GritCestusPrefabName,
            HelOathItemRegistration.HelOathPrefabName,
            ShadowCrossbowItemRegistration.ShadowCrossbowPrefabName
        };

        internal static void RebindWeapons(Player player)
        {
            if (player == null || ObjectDB.instance == null) return;
            var items = player.GetInventory()?.GetAllItems();
            if (items == null) return;

            foreach (ItemDrop.ItemData item in items)
            {
                if (item?.m_shared == null) continue;
                foreach (string prefabName in WeaponPrefabs)
                {
                    GameObject prefab = ObjectDB.instance.GetItemPrefab(prefabName);
                    ItemDrop canonical = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                    if (canonical?.m_itemData?.m_shared == null ||
                        item.m_shared.m_name != canonical.m_itemData.m_shared.m_name) continue;

                    item.m_dropPrefab = prefab;
                    item.m_shared = canonical.m_itemData.m_shared;
                    item.m_quality = Math.Max(1, item.m_quality);
                    Normalize(item); // Never increases an item's remaining durability.
                    break;
                }
            }
        }

        internal static void Normalize(ItemDrop.ItemData item)
        {
            if (!UpgradeRecipes.IsSupported(item) ||
                item.m_dropPrefab.name == BrisingamenItemRegistration.RingPrefabName) return;

            // Loaded temporary items may not have their final SharedData yet.
            ItemDrop.ItemData canonical = item.m_dropPrefab.GetComponent<ItemDrop>()?.m_itemData;
            if (canonical == null) return;
            item.m_quality = Math.Max(1, item.m_quality); // No upper bound: native refinement is unbounded.
            float maximum = canonical.GetMaxDurability(item.m_quality);
            if (float.IsNaN(maximum) || float.IsInfinity(maximum) || maximum <= 0f) return;
            // A balance change may lower the maximum. Never repair a worn/broken item on load.
            item.m_durability = float.IsNaN(item.m_durability)
                ? 0f
                : Math.Max(0f, Math.Min(item.m_durability, maximum));
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), new[] { typeof(ItemDrop.ItemData) })]
    internal static class UpgradeInventoryItemStatePatch
    {
        private static void Prefix(ItemDrop.ItemData __0) => UpgradeItemState.Normalize(__0);
    }

    [HarmonyPatch(typeof(Inventory), "AddItem", new[]
        { typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int), typeof(bool) })]
    internal static class UpgradeInventorySlotStatePatch
    {
        private static void Prefix(ItemDrop.ItemData __0) => UpgradeItemState.Normalize(__0);
    }

    [HarmonyPatch(typeof(ItemDrop), "LoadFromZDO", new[]
        { typeof(ItemDrop.ItemData), typeof(ZDO), typeof(int) })]
    internal static class UpgradeWorldItemStatePatch
    {
        private static void Postfix(ItemDrop.ItemData __0) => UpgradeItemState.Normalize(__0);
    }
}