using System;

namespace HaldorExpansion.Features.Berserker
{
    internal static class BerserkerRagnarokMath
    {
        internal const double DebtPerVisualStack = 100d;
        internal const double MinimumDebt = 500d;
        // Wrath V is consumed on release: double the meteor count (5-8 -> 10-16).
        internal const int MinimumMeteorCount = 10;
        internal const int MaximumMeteorCount = 16;
        private const double TotalDamageMultiplier = 1.80d;

        internal static int MeteorCount(double debt)
        {
            if (double.IsNaN(debt) || double.IsInfinity(debt) || debt <= 0d)
                return MinimumMeteorCount;

            // Clamp before converting to int to handle large, uncapped Blood Debt.
            double originalCount = Math.Max(5d, Math.Min(8d, Math.Floor(debt / DebtPerVisualStack)));
            return (int)originalCount * 2;
        }

        internal static float TotalDamage(double debt)
        {
            if (double.IsNaN(debt) || double.IsInfinity(debt) || debt < MinimumDebt)
                return 0f;

            double stacks = debt / DebtPerVisualStack;

            // x5 -> x15:
            // x5  = 800
            // x6  = 1000
            // x7  = 1250
            // x8  = 1550
            // x9  = 1900
            // x10 = 2300
            // x11 = 2750
            // x12 = 3250
            // x13 = 3800
            // x14 = 4400
            // x15 = 5050
            double n = Math.Max(0d, Math.Min(10d, stacks - 5d));

            double total = 800d
                           + 175d * n
                           + 25d * n * n;

            // После x15 рост становится мягким:
            // +250 total damage за каждый следующий x1.
            if (stacks > 15d)
                total += (stacks - 15d) * 250d;

            // Apply the +80% buff to the TOTAL damage budget, not to every
            // meteor: PerMeteorDamage divides this budget by the doubled count.
            total *= TotalDamageMultiplier;

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