using System;
using System.Collections.Generic;
using HaldorExpansion.Configuration;

namespace HaldorExpansion.Trading
{
    // Server-originated rules are looked up only while selling to Haldor.
    // Never write custom prices to the ObjectDB prefab or to permanent shared item data.
    internal static class CustomBuyItems
    {
        private static readonly Dictionary<string, int> Prices =
            new Dictionary<string, int>(StringComparer.Ordinal);

        private static bool enabled = true;

        private static readonly HashSet<string> WarnedOverflow =
            new HashSet<string>(StringComparer.Ordinal);

        internal static bool HasOffers => enabled && Prices.Count != 0;

        internal static void SetEnabled(bool value) => enabled = value;

        internal static void Apply(CustomTradingDocument document)
        {
            Prices.Clear();
            WarnedOverflow.Clear();
            if (document == null) return;
            foreach (CustomBuySetting item in document.Buys.Values)
                if (item.Enabled && !string.IsNullOrEmpty(item.Id) && item.Price > 0)
                    Prices[item.Id] = item.Price;
        }

        internal static bool TryGetPrice(string prefabName, int count, out int price)
        {
            price = 0;
            int unitPrice;
            if (!HasOffers || string.IsNullOrEmpty(prefabName) || count <= 0 ||
                !Prices.TryGetValue(prefabName, out unitPrice)) return false;
            // GetValue() uses int. An impossible payout must NOT wrap or fall back
            // to an unrelated vanilla price. Value 0 makes this stack ineligible.
            if ((long)unitPrice * count > int.MaxValue)
            {
                if (WarnedOverflow.Add(prefabName))
                    HaldorExpansionPlugin.Log?.LogWarning("[Custom trading] Purchase price overflows int for " +
                                                          prefabName + " x" + count + ". Reduce price or stack size.");
                return true;
            }

            price = unitPrice;
            return true;
        }
    }
}