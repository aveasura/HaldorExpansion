using System;
using HaldorExpansion.Features.Berserker;
using HaldorExpansion.Features.Cestus;
using HaldorExpansion.Features.HelOath;
using HaldorExpansion.Features.NornThread;
using HaldorExpansion.Features.PeltOfHelheim;
using HaldorExpansion.Features.PitKing;
using HaldorExpansion.Features.ShadowCrossbow;
using HaldorExpansion.Features.StaminaCape;
using HaldorExpansion.Features.WoundedBeast;
using UnityEngine;

namespace HaldorExpansion.Presentation
{
    internal static class ItemTooltipController
    {
        private static readonly string[] ItemKeys =
        {
            "$item_brisingamen", "$item_chest_delayeddoom",
            NornThreadItemRegistration.ItemKey, PitKingItemRegistration.PitKingChestItemKey,
            CestusItemRegistration.CestusItemKey, HelOathItemRegistration.HelOathItemKey,
            ShadowCrossbowItemRegistration.ShadowCrossbowItemKey, WoundedBeastItemRegistration.WoundedBeastCapeItemKey,
            StaminaCapeItemRegistration.ItemKey, PeltOfHelheimItemRegistration.ItemKey,
            BerserkerItemRegistration.ItemKey, BerserkerCloakItemRegistration.ItemKey,
            BerserkerHelmetItemRegistration.ItemKey
        };

        internal static LongItemTooltipView Current { get; private set; }
        private static int lastWidth, lastHeight;
        private static Vector3 lastScale;
        private static bool warned;

        private static bool IsModItem(UITooltip owner)
        {
            if (owner == null || string.IsNullOrEmpty(owner.m_topic)) return false;
            foreach (string key in ItemKeys)
            {
                if (owner.m_topic == key) return true;
                // StoreGui supplies a localized topic; InventoryGrid supplies the key.
                if (Localization.instance != null && owner.m_topic == Localization.instance.Localize(key)) return true;
            }

            return false;
        }

        internal static void Attach(UITooltip owner, GameObject tooltip)
        {
            if (Current != null || tooltip == null) return;
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            lastScale = tooltip.transform.lossyScale;
            if (!IsModItem(owner)) return;
            try
            {
                Current = LongItemTooltipView.TryCreate(owner, tooltip);
            }
            catch (Exception e)
            {
                Failed(e);
            }
        }

        internal static void Refresh(UITooltip owner, GameObject tooltip)
        {
            if (Current != null && (Current.Owner != owner || !IsModItem(owner))) Reset();
            try
            {
                if (Current != null)
                {
                    if (!Current.Refresh()) Reset();
                }
                else Attach(owner, tooltip);
            }
            catch (Exception e)
            {
                Failed(e);
            }
        }

        internal static void Tick(UITooltip owner, GameObject tooltip)
        {
            if (Current != null && (!Current.Alive || Current.Owner != owner)) Reset();
            try
            {
                if (Current != null)
                {
                    if (!Current.Tick()) Reset();
                }
                else if (tooltip != null && (lastWidth != Screen.width || lastHeight != Screen.height ||
                                             lastScale != tooltip.transform.lossyScale)) Attach(owner, tooltip);
            }
            catch (Exception e)
            {
                Failed(e);
            }
        }

        internal static void Reset()
        {
            LongItemTooltipView old = Current;
            Current = null;
            old?.Dispose();
        }

        private static void Failed(Exception error)
        {
            Reset();
            if (warned) return;
            warned = true;
            HaldorExpansionPlugin.Log?.LogWarning(
                "[HaldorExpansion] Long tooltip unavailable; using vanilla tooltip. " + error);
        }
    }
}