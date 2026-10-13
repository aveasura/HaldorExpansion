using System;

namespace HaldorExpansion.Features.Upgrades
{
    // Edit each item's group independently, then rebuild the mod.
    // PerLevel values apply once for every level AFTER level 1.
    internal static class ItemUpgradeBalance
    {
        internal static class BerserkerHelmet
        {
            internal const float BaseDurability = 1200f;
            internal const float DurabilityPerLevel = 200f;
            internal const float ArmorPerLevel = 2f;
        }

        internal static class DelayedDoomChest
        {
            internal const float BaseDurability = 1200f;
            internal const float DurabilityPerLevel = 200f;
            internal const float ArmorPerLevel = 2f;
        }

        internal static class PitKingChest
        {
            internal const float BaseDurability = 1200f;
            internal const float DurabilityPerLevel = 200f;
            internal const float ArmorPerLevel = 2f;
        }

        internal static class Brisingamen
        {
            internal const float BaseCarryWeight = 150f;
            internal const float CarryWeightPerLevel = 15f;
        }

        internal static class BerserkerAxe
        {
            internal const float SlashPerLevel = 10f;
            internal const float DurabilityPerLevel = 120f;
        }

        internal static class BoneCrushers
        {
            internal const float BluntPerLevel = 3f;
            internal const float LightningPerLevel = 1f;
            internal const float DurabilityPerLevel = 110f;
        }

        internal static class HelOath
        {
            internal const float PiercePerLevel = 2f;
            internal const float LightningPerLevel = 4f;
            internal const float FrostPerLevel = 2f;
            internal const float DurabilityPerLevel = 90f;
        }

        internal static class ShadowCrossbow
        {
            internal const float PiercePerLevel = 16f;
            internal const float DurabilityPerLevel = 30f;
        }

        internal static class BerserkerCloak
        {
            internal const float ArmorPerLevel = 2f;
            internal const float DurabilityPerLevel = 120f;
        }

        internal static class HelheimPelt
        {
            internal const float ArmorPerLevel = 1f;
            internal const float DurabilityPerLevel = 50f;
        }

        internal static class WoundedBeastCloak
        {
            // Intentionally no armor scaling for this high-risk cloak.
            internal const float DurabilityPerLevel = 60f;
        }

        internal static class ScorchedEnduranceCloak
        {
            internal const float ArmorPerLevel = 1f;
            internal const float DurabilityPerLevel = 90f;
        }

        internal static float CarryWeight(int quality)
        {
            return Brisingamen.BaseCarryWeight + Math.Max(0, quality - 1) * Brisingamen.CarryWeightPerLevel;
        }
    }
}