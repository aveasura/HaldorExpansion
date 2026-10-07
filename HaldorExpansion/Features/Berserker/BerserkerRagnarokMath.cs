using System;

namespace HaldorExpansion.Features.Berserker
{
    internal static class BerserkerRagnarokMath
    {
        internal const double DebtPerVisualStack = 100d;
        internal const double MinimumDebt = 500d;
        internal const int MinimumMeteorCount = 5;
        internal const int MaximumMeteorCount = 8;

        internal static int MeteorCount(double debt)
        {
            if (double.IsNaN(debt) || double.IsInfinity(debt) || debt <= 0d)
                return MinimumMeteorCount;

            int completedStacks = (int)Math.Floor(debt / DebtPerVisualStack);
            return Math.Max(MinimumMeteorCount, Math.Min(MaximumMeteorCount, completedStacks));
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