using System;
using System.Collections.Generic;
using System.Reflection;
using HaldorExpansion.Trading;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Presentation
{
    internal static class ShopItemTooltip
    {
        private static readonly FieldInfo TraderField = AccessTools.Field(typeof(StoreGui), "m_trader");
        private static readonly FieldInfo RowsField = AccessTools.Field(typeof(StoreGui), "m_itemList");
        private static bool preparing;

        internal static void Prepare(UITooltip tooltip)
        {
            var gui = StoreGui.instance;
            if (preparing || tooltip == null || gui == null || !gui.m_rootPanel.activeSelf ||
                !tooltip.transform.IsChildOf(gui.m_listRoot))
                return;

            var trader = TraderField.GetValue(gui) as Trader;
            if (!HaldorShopUpdater.IsHaldor(trader))
                return;

            var rows = RowsField.GetValue(gui) as List<GameObject>;
            int index = rows != null ? rows.IndexOf(tooltip.gameObject) : -1;
            if (index < 0)
                return;

            var items = HaldorTabsController.GetVisibleItems(trader);
            if (index >= items.Count)
                return;

            var trade = items[index];
            if (!HaldorShopUpdater.IsOwned(trade) || trade.m_prefab == null)
                return;

            var item = trade.m_prefab.m_itemData;
            if (item?.m_shared == null)
                return;

            try
            {
                preparing = true;
                // Show only the legend at Haldor. The full item tooltip belongs to the inventory.
                // Fall back to vanilla behavior for items without a dedicated shop legend.
                string loreKey = ShopLoreCatalog.ForItem(item.m_shared.m_name);
                string description = loreKey != null && Localization.instance != null
                    ? Localization.instance.Localize(loreKey)
                    : item.GetTooltip(trade.m_stack);
                tooltip.Set(item.m_shared.m_name, description, gui.m_tooltipAnchor, Vector2.zero);
            }
            catch (Exception error)
            {
                HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] Full shop tooltip unavailable: " +
                                                      error.Message);
            }
            finally
            {
                preparing = false;
            }
        }
    }
}