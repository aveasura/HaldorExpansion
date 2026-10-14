using System.Collections.Generic;
using HaldorExpansion.Configuration;
using UnityEngine;

namespace HaldorExpansion.Trading
{
    internal static class TrophyValueUpdater
    {
        internal const int DefaultTrophyValue = 10;

        // Отдельно: не трофеи, но Хальдор может купить
        private static readonly HashSet<string> ExtraSellableItems = new HashSet<string>
        {
            "SurtlingCore", "BlackCore", "MoltenCore"
        };

        internal static void ApplyTrophyValues(ObjectDB db)
        {
            if (db == null || db.m_items == null)
                return;

            foreach (GameObject go in db.m_items)
            {
                if (go == null)
                    continue;

                ItemDrop drop = go.GetComponent<ItemDrop>();
                if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null)
                    continue;

                ItemDrop.ItemData.SharedData shared = drop.m_itemData.m_shared;

                // Non-trophies bought by Haldor, including all 17 idols.
                if (ExtraSellableItems.Contains(go.name) || HaldorIdolCatalog.IsIdol(go.name))
                {
                    int configuredExtraValue = SellPriceConfiguration.GetConfiguredHaldorSellPrice(go.name, 0);
                    shared.m_value = configuredExtraValue;
                    HaldorExpansionPlugin.DebugLog(
                        $"[HaldorExpansion] Extra sellable item value set: prefab={go.name}, key={shared.m_name}, value={configuredExtraValue}");
                    continue;
                }

                // Дальше работаем только с трофеями
                if (shared.m_itemType != ItemDrop.ItemData.ItemType.Trophy)
                    continue;

                int value = SellPriceConfiguration.GetDefaultTrophySellPrice();

                value = SellPriceConfiguration.GetConfiguredHaldorSellPrice(go.name, value);

                shared.m_value = value;

                HaldorExpansionPlugin.DebugLog(
                    $"[HaldorExpansion] Trophy value set: prefab={go.name}, key={shared.m_name}, value={value}");
            }
        }

        internal static bool IsSellableToHaldor(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null)
                return false;

            string prefabName = item.m_dropPrefab != null ? item.m_dropPrefab.name : null;
            if (string.IsNullOrEmpty(prefabName))
                return false;

            if (ExtraSellableItems.Contains(prefabName) || HaldorIdolCatalog.IsIdol(prefabName))
                return item.m_shared.m_value > 0;

            return item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy
                   && item.m_shared.m_value > 0;
        }
    }
}