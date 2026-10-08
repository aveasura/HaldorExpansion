using HaldorExpansion.Presentation;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HaldorExpansion.Patches
{
    [HarmonyPatch(typeof(UITooltip), nameof(UITooltip.OnHoverStart))]
    internal static class ItemTooltipHoverPatch
    {
        private static void Prefix() => ItemTooltipController.Reset();

        private static void Postfix(UITooltip __instance, UITooltip ___m_current, GameObject ___m_tooltip)
        {
            if (__instance == ___m_current) ItemTooltipController.Attach(__instance, ___m_tooltip);
        }
    }

    [HarmonyPatch(typeof(UITooltip), "UpdateTextElements")]
    internal static class ItemTooltipTextPatch
    {
        private static void Postfix(UITooltip __instance, UITooltip ___m_current, GameObject ___m_tooltip)
        {
            if (__instance == ___m_current) ItemTooltipController.Refresh(__instance, ___m_tooltip);
        }
    }

    [HarmonyPatch(typeof(UITooltip), "LateUpdate")]
    internal static class ItemTooltipUpdatePatch
    {
        private static void Postfix(UITooltip __instance, UITooltip ___m_current, GameObject ___m_tooltip)
        {
            if (__instance == ___m_current) ItemTooltipController.Tick(__instance, ___m_tooltip);
        }
    }

    [HarmonyPatch(typeof(UITooltip), nameof(UITooltip.HideTooltip))]
    internal static class ItemTooltipHidePatch
    {
        private static void Prefix() => ItemTooltipController.Reset();
    }

    [HarmonyPatch(typeof(Utils), nameof(Utils.ClampUIToScreen))]
    internal static class ItemTooltipClampPatch
    {
        private static bool Prefix(RectTransform transform)
        {
            LongItemTooltipView view = ItemTooltipController.Current;
            // Never change clamping for other windows or another mod's tooltip.
            return !ItemTooltipController.Enabled || view == null || !view.Alive || view.OriginalPanel != transform;
        }
    }

    [HarmonyPatch(typeof(ScrollRect), nameof(ScrollRect.OnScroll))]
    internal static class ItemTooltipScrollPatch
    {
        private static bool Prefix(ScrollRect __instance, PointerEventData __0)
        {
            LongItemTooltipView view = ItemTooltipController.Current;
            if (!ItemTooltipController.Enabled || view == null || !view.HasScroll || __0 == null ||
                !view.Owner.transform.IsChildOf(__instance.transform) ||
                !view.PointerOnOwner(__0.position)) return true;
            // EventSystem delivers scroll during Update, before the tooltip's
            // LateUpdate polling. Wheel() deduplicates those two paths by frame.
            view.Wheel(__0.scrollDelta.y);
            __0.Use();
            return false;
        }
    }
}