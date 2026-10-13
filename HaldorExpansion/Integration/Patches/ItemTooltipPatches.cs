using HaldorExpansion.Features.Awakened;
using HaldorExpansion.Presentation;
using HaldorExpansion.Trading;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HaldorExpansion.Patches
{
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip),
        new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool) })]
    internal static class ItemTooltipLorePatch
    {
        private static void Postfix(ItemDrop.ItemData __0, int __1, bool __5, ref string __result)
        {
            // Appended item tooltips have no leading description of their own.
            // Haldor's lore-only preview uses ShopItemTooltip independently.
            if (__5 || __0?.m_shared == null || Localization.instance == null) return;
            string loreKey = ShopLoreCatalog.ForItem(__0.m_shared.m_name);
            string lore = loreKey == null ? null : Localization.instance.Localize(loreKey);
            string descriptionKey = __0.m_shared.m_description;
            if (lore != null && !string.IsNullOrEmpty(descriptionKey))
                __result = ItemLoreTooltip.MoveToEnd(__result, descriptionKey,
                    Localization.instance.Localize(descriptionKey), lore);
            __result = AwakenedDescriptions.InsertBeforeLore(__result,
                AwakenedDescriptions.ForItem(__0, __1), lore);
        }
    }

    [HarmonyPatch(typeof(UITooltip), nameof(UITooltip.OnHoverStart))]
    internal static class ItemTooltipHoverPatch
    {
        private static void Prefix(UITooltip __instance)
        {
            ItemTooltipController.Reset();
            ShopItemTooltip.Prepare(__instance);
        }

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