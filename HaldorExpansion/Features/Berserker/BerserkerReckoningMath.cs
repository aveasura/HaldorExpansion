using System;

namespace HaldorExpansion.Features.Berserker
{
    internal static class BerserkerReckoningMath
    {
        // Reckoning is the frequent, reliable Blood Debt cash-out. Early Debt remains valuable,
        // but the curve softens quickly so x5 does not rival the much rarer Ragnarok payoff.
        internal static float BonusDamage(double debt)
        {
            if (double.IsNaN(debt) || double.IsInfinity(debt) || debt <= 0d) return 0f;

            double remaining = debt;
            double damage = 0d;
            damage += Take(ref remaining, 300d) * 1.00d;
            damage += Take(ref remaining, 300d) * 0.45d;
            damage += Take(ref remaining, 400d) * 0.30d;

            if (remaining > 0d)
                damage += remaining * 0.15d;

            if (double.IsNaN(damage) || double.IsInfinity(damage) || damage <= 0d) return 0f;
            return damage >= float.MaxValue ? float.MaxValue : (float)damage;
        }

        // No artificial max-health cap: the only cap is the player's missing health.
        // This keeps lifesteal proportional to the final direct post-defense damage, including overkill.
        internal static float HealAmount(float finalDamage, float lifeStealFraction)
        {
            if (!Finite(finalDamage) || finalDamage <= 0f || !Finite(lifeStealFraction)) return 0f;
            return Math.Max(0f, finalDamage * Math.Max(0f, lifeStealFraction));
        }

        private static double Take(ref double remaining, double width)
        {
            if (remaining <= 0d) return 0d;
            double taken = Math.Min(remaining, width);
            remaining -= taken;
            return taken;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}