using System;

namespace HaldorExpansion.Features.Berserker
{
    internal static class BerserkerRagnarokMath
    {
        internal const double DebtPerVisualStack = 100d;
        internal const double MinimumDebt = 500d;

        internal const int MinimumMeteorCount = 12;
        internal const int MaximumMeteorCount = 30;

        internal const float MinimumMeteorImpactRadius = 7f;
        internal const float MaximumMeteorImpactRadius = 11f;
        internal const float MinimumMeteorExplosionRadius = 5f;
        internal const float MaximumMeteorExplosionRadius = 7f;

        private const double MinimumDamageMultiplier = 2.75d;
        private const double MaximumDamageMultiplier = 7.2875d;

        // 0 at x5, 1 at x15 and above. Fractional Debt scales radii smoothly.
        private static double Progress(double debt)
        {
            if (double.IsNaN(debt) || double.IsInfinity(debt)) return 0d;
            return Math.Max(0d, Math.Min(1d, (debt / DebtPerVisualStack - 5d) / 10d));
        }

        internal static int MeteorCount(double debt)
        {
            double count = MinimumMeteorCount +
                           (MaximumMeteorCount - MinimumMeteorCount) * Progress(debt);
            return (int)Math.Round(count, MidpointRounding.AwayFromZero);
        }

        internal static float MeteorImpactRadius(double debt)
        {
            return (float)(MinimumMeteorImpactRadius +
                           (MaximumMeteorImpactRadius - MinimumMeteorImpactRadius) * Progress(debt));
        }

        internal static float MeteorExplosionRadius(double debt)
        {
            return (float)(MinimumMeteorExplosionRadius +
                           (MaximumMeteorExplosionRadius - MinimumMeteorExplosionRadius) * Progress(debt));
        }

        private static double DamageMultiplier(double debt)
        {
            double progress = Progress(debt);
            return MinimumDamageMultiplier +
                   (MaximumDamageMultiplier - MinimumDamageMultiplier) * progress * progress;
        }

        internal static float TotalDamage(double debt)
        {
            if (double.IsNaN(debt) || double.IsInfinity(debt) || debt < MinimumDebt)
                return 0f;

            double stacks = debt / DebtPerVisualStack;
            double n = Math.Max(0d, Math.Min(10d, stacks - 5d));
            double total = 800d + 175d * n + 25d * n * n;

            // Beyond x15, number/radii/multiplier are capped, but the old soft
            // +250 base damage for each additional stack is preserved.
            if (stacks > 15d)
                total += (stacks - 15d) * 250d;

            total *= DamageMultiplier(debt);
            return double.IsNaN(total) || double.IsInfinity(total) || total <= 0d
                ? 0f
                : total >= float.MaxValue
                    ? float.MaxValue
                    : (float)total;
        }

        internal static float PerMeteorDamage(double debt)
        {
            int count = MeteorCount(debt);
            return count <= 0 ? 0f : TotalDamage(debt) / count;
        }
    }
}