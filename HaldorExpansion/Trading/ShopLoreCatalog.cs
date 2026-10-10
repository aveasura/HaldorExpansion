using System.Collections.Generic;
using HaldorExpansion.Features.Berserker;
using HaldorExpansion.Features.Brisingamen;
using HaldorExpansion.Features.Cestus;
using HaldorExpansion.Features.DelayedDoom;
using HaldorExpansion.Features.HelOath;
using HaldorExpansion.Features.NornThread;
using HaldorExpansion.Features.PeltOfHelheim;
using HaldorExpansion.Features.PitKing;
using HaldorExpansion.Features.ShadowCrossbow;
using HaldorExpansion.Features.StaminaCape;
using HaldorExpansion.Features.WoundedBeast;

namespace HaldorExpansion.Trading
{
    internal static class ShopLoreCatalog
    {
        // Only these HaldorExpansion relics have lore-only shop descriptions.
        // An item with no entry keeps its normal shop tooltip (including vanilla items).
        private static readonly Dictionary<string, string> Descriptions = new Dictionary<string, string>
        {
            [BerserkerIds.AxeItemKey] = BerserkerIds.AxeItemKey + "_lore",
            [BerserkerIds.CloakItemKey] = BerserkerIds.CloakItemKey + "_lore",
            [BerserkerIds.HelmetItemKey] = BerserkerIds.HelmetItemKey + "_lore",
            [HelOathItemRegistration.HelOathItemKey] = HelOathItemRegistration.HelOathItemKey + "_lore",
            [DelayedDoomIds.ItemKey] = DelayedDoomIds.ItemKey + "_lore",
            [CestusItemRegistration.CestusItemKey] = CestusItemRegistration.CestusItemKey + "_lore",
            [PeltOfHelheimItemRegistration.ItemKey] = PeltOfHelheimItemRegistration.ItemKey + "_lore",
            [StaminaCapeItemRegistration.ItemKey] = StaminaCapeItemRegistration.ItemKey + "_lore",
            [BrisingamenIds.ItemKey] = BrisingamenIds.DescriptionKey,
            [NornThreadItemRegistration.ItemKey] = NornThreadItemRegistration.ItemDescKey,
            [PitKingItemRegistration.PitKingChestItemKey] = PitKingItemRegistration.PitKingChestItemDescKey,
            [WoundedBeastItemRegistration.WoundedBeastCapeItemKey] = WoundedBeastItemRegistration.WoundedBeastCapeItemDescKey,
            [ShadowCrossbowItemRegistration.ShadowCrossbowItemKey] = ShadowCrossbowItemRegistration.ShadowCrossbowItemDescKey
        };

        internal static string ForItem(string name)
        {
            return name != null && Descriptions.TryGetValue(name, out string key) ? key : null;
        }

        internal static string DescriptionFor(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null) return string.Empty;
            return ForItem(shared.m_name) ?? shared.m_description ?? string.Empty;
        }
    }
}
