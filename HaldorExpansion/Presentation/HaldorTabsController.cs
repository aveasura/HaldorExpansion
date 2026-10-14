using System;
using System.Collections.Generic;
using System.Reflection;
using HaldorExpansion.Configuration;
using HaldorExpansion.Trading;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

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
        private static StoreGui pendingScrollGui;
        private static int pendingScrollFrame;
        private static StoreGui scrollRectGui;
        private static ScrollRect linkedScrollRect;
        internal static ShopCategory Category { get; private set; }

        internal static bool Applies(StoreGui gui)
        {
            // Keep row/index mapping stable until Sync rebuilds the visible list.
            return view != null && view.Gui == gui &&
                   gui != null && gui.m_rootPanel.activeSelf &&
                   HaldorShopUpdater.IsHaldor(TraderField.GetValue(gui) as Trader);
        }

        internal static void Sync(StoreGui gui, bool listAlreadyFilled = false)
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
                // Show has already filled All (including the hidden-category filter).
                // Creating the tab buttons does not change that row/index mapping.
                // Keep selection from the native fill and only reset the scroll.
                if (listAlreadyFilled) ResetScrollAfterRefresh(gui);
                else Refresh(gui);
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
            // Do this once after Unity has had a frame to update the new list's layout.
            if (pendingScrollGui == gui && Time.frameCount > pendingScrollFrame)
            {
                pendingScrollGui = null;
                ResetListScroll(gui);
            }

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
            ResetScrollAfterRefresh(gui);
        }

        private static void ResetScrollAfterRefresh(StoreGui gui)
        {
            ResetListScroll(gui);
            // A short list may not have updated the ScrollRect bounds yet. Only the
            // most recent refresh gets one deferred correction; no layout rebuilds.
            if (view != null && view.Gui == gui)
            {
                pendingScrollGui = gui;
                pendingScrollFrame = Time.frameCount;
            }
        }

        private static void ResetListScroll(StoreGui gui)
        {
            if (gui == null || gui.m_listScroll == null) return;
            gui.m_listScroll.value = 1f;
            if (view == null || view.Gui != gui) return;

            // The Scrollbar may already read 1 while the content is still offset.
            // Reset the linked ScrollRect directly, without touching unrelated UI.
            if (scrollRectGui != gui)
            {
                scrollRectGui = gui;
                linkedScrollRect = null;
                foreach (ScrollRect rect in gui.GetComponentsInChildren<ScrollRect>(true))
                {
                    if (rect != null && rect.verticalScrollbar == gui.m_listScroll && rect.vertical)
                    {
                        linkedScrollRect = rect;
                        break;
                    }
                }
            }

            if (linkedScrollRect == null) return;
            linkedScrollRect.StopMovement();
            linkedScrollRect.verticalNormalizedPosition = 1f;
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
            pendingScrollGui = null;
            scrollRectGui = null;
            linkedScrollRect = null;
            HaldorTabsView old = view;
            view = null;
            Category = ShopCategory.All;
            failedGui = null;
            old?.Dispose();
        }
    }
}