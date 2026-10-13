using System;
using System.Collections.Generic;
using System.Reflection;
using HaldorExpansion.Configuration;
using HaldorExpansion.Trading;
using HarmonyLib;

namespace HaldorExpansion.Presentation
{
    internal static class HaldorTabsController
    {
        private static readonly FieldInfo TraderField = AccessTools.Field(typeof(StoreGui), "m_trader");
        private static readonly FieldInfo SelectedField = AccessTools.Field(typeof(StoreGui), "m_selectedItem");
        private static readonly MethodInfo FillList = AccessTools.Method(typeof(StoreGui), "FillList");
        private static HaldorTabsView view;
        private static StoreGui failedGui;
        private static int lastState = -1;
        internal static ShopCategory Category { get; private set; }

        internal static bool Applies(StoreGui gui)
        {
            // Keep row/index mapping stable until Sync rebuilds the visible list.
            return view != null && view.Gui == gui &&
                   gui != null && gui.m_rootPanel.activeSelf &&
                   HaldorShopUpdater.IsHaldor(TraderField.GetValue(gui) as Trader);
        }

        internal static void Sync(StoreGui gui)
        {
            int state = TradingUiConfiguration.State;
            bool settingsChanged = state != lastState;
            lastState = state;
            if (!TradingUiConfiguration.IsCategoryEnabled(Category)) Category = ShopCategory.All;
            bool wanted = TradingUiConfiguration.Enabled && gui != null && gui.m_rootPanel.activeSelf &&
                          HaldorShopUpdater.IsHaldor(TraderField.GetValue(gui) as Trader);
            if (view != null && (!wanted || view.Gui != gui))
            {
                Reset();
                if (gui != null && gui.m_rootPanel.activeSelf) Refresh(gui);
            }

            if (!wanted || view != null || failedGui == gui)
            {
                if (settingsChanged && IsHaldorGui(gui))
                {
                    view?.Select(Category);
                    Refresh(gui);
                }

                return;
            }

            try
            {
                view = HaldorTabsView.Create(gui);
                Category = ShopCategory.All;
                view.Select(Category);
                Refresh(gui);
            }
            catch (Exception error)
            {
                Reset();
                failedGui = gui;
                HaldorExpansionPlugin.Log?.LogWarning(
                    "[HaldorExpansion] Haldor tabs unavailable; using original shop. " + error);
            }
        }

        internal static void Tick(StoreGui gui)
        {
            Sync(gui);
            if (!Applies(gui)) return;
            view.UpdateLayout();
            if (ZInput.GetButtonDown("JoyTabLeft")) view.Cycle(Category, -1);
            else if (ZInput.GetButtonDown("JoyTabRight")) view.Cycle(Category, 1);
        }

        internal static void Select(ShopCategory category)
        {
            if (view == null || Category == category || !TradingUiConfiguration.IsCategoryEnabled(category)) return;
            Category = category;
            view.Select(category);
            Refresh(view.Gui);
        }

        private static void Refresh(StoreGui gui)
        {
            SelectedField.SetValue(gui, null);
            FillList.Invoke(gui, null);
            if (gui.m_listScroll != null) gui.m_listScroll.value = 1f;
        }

        // Called only by the three StoreGui methods which map visible rows to purchases.
        // Never replace Trader.m_items or affect other callers of GetAvailableItems.
        internal static List<Trader.TradeItem> GetVisibleItems(Trader trader)
        {
            List<Trader.TradeItem> items = trader.GetAvailableItems();
            if (!HaldorShopUpdater.IsHaldor(trader)) return items;
            bool tabsApply = Applies(StoreGui.instance) && (Trader)TraderField.GetValue(StoreGui.instance) == trader;
            return ShopCategoryFilter.Filter(items, tabsApply ? Category : ShopCategory.All, Classify,
                TradingUiConfiguration.HideItems
                    ? (Func<ShopCategory, bool>)TradingUiConfiguration.IsCategoryEnabled
                    : null);
        }

        private static ShopCategory Classify(Trader.TradeItem item)
        {
            var shared = item?.m_prefab != null ? item.m_prefab.m_itemData?.m_shared : null;
            return ShopCategoryFilter.Classify(shared != null ? shared.m_itemType.ToString() : null,
                item?.m_prefab != null ? item.m_prefab.name : null, shared?.m_name);
        }

        private static bool IsHaldorGui(StoreGui gui)
        {
            return gui != null && gui.m_rootPanel.activeSelf &&
                   HaldorShopUpdater.IsHaldor(TraderField.GetValue(gui) as Trader);
        }

        internal static void ClampSelection(StoreGui gui, ref int index)
        {
            if (!IsHaldorGui(gui)) return;
            var trader = TraderField.GetValue(gui) as Trader;
            index = ShopCategoryFilter.ClampSelection(index, GetVisibleItems(trader).Count);
        }

        internal static void Reset()
        {
            HaldorTabsView old = view;
            view = null;
            Category = ShopCategory.All;
            failedGui = null;
            old?.Dispose();
        }
    }
}