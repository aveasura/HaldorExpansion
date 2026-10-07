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

        // Ragnarok is intentionally a separate payoff curve rather than Reckoning × N.
        // At the unlock threshold x5 it starts at 1500 total damage. Up to x10 each extra
        // Debt point adds 1 damage.
        // beyond x10 the marginal gain softens to 0.5 per Debt.
        internal static float TotalDamage(double debt)
        {
            if (double.IsNaN(debt) || double.IsInfinity(debt) || debt < MinimumDebt) return 0f;

            double total = debt <= 1000d
                ? 1000d + debt
                : 2000d + (debt - 1000d) * 0.50d;

            return double.IsNaN(total) || double.IsInfinity(total) || total <= 0d
                ? 0f
                : total >= float.MaxValue ? float.MaxValue : (float)total;
        }

        internal static float PerMeteorDamage(double debt)
        {
            int count = MeteorCount(debt);
            return count <= 0 ? 0f : TotalDamage(debt) / count;
        }
    }
}
