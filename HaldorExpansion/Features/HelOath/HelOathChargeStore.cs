using System;
using System.Collections.Generic;
using System.Globalization;

namespace HaldorExpansion.Features.HelOath
{
    /// <summary>
    /// Persists Hel's Embrace charge on the concrete bow instance rather than on the player runtime.
    /// ItemData custom data is serialized together with the item, so charge follows that bow through
    /// unequip/re-equip, teleports, inventory serialization and drop/pickup cycles.
    /// </summary>
    internal static class HelOathChargeStore
    {
        private const string ChargeKey = "HaldorExpansion.HelOath.Charge";

        internal static float Read(ItemDrop.ItemData item)
        {
            if (item == null || item.m_customData == null) return 0f;
            if (!item.m_customData.TryGetValue(ChargeKey, out string raw) || string.IsNullOrEmpty(raw)) return 0f;

            if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float charge))
                return 0f;

            return Normalize(charge);
        }

        internal static void Write(ItemDrop.ItemData item, float charge)
        {
            if (item == null) return;
            charge = Normalize(charge);

            if (item.m_customData == null)
                item.m_customData = new Dictionary<string, string>();

            // Absence means zero. Removing zero keeps ordinary bows free of unnecessary custom data.
            if (charge <= 0f)
            {
                item.m_customData.Remove(ChargeKey);
                return;
            }

            item.m_customData[ChargeKey] = charge.ToString("R", CultureInfo.InvariantCulture);
        }

        private static float Normalize(float charge)
        {
            if (float.IsNaN(charge) || float.IsInfinity(charge)) return 0f;
            return Math.Max(0f, Math.Min(100f, charge));
        }
    }
}
