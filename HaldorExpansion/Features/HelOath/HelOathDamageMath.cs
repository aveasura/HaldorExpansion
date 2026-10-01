using System;

namespace HaldorExpansion.Features.HelOath
{
    internal static class HelOathDamageMath
    {
        internal static float CalculateSplash(float finalMaxStamina, float finalMaxHealth, float coefficient)
        {
            if (!Finite(finalMaxStamina) || !Finite(finalMaxHealth) || !Finite(coefficient) || coefficient <= 0f) return 0f;

            // With Hel's exact 50% HP -> stamina transfer, converted HP cancels out here.
            // HelPower therefore reflects real pre-conversion stamina investment.
            float helPower = Math.Max(0f, finalMaxStamina - finalMaxHealth);
            return helPower * coefficient * GlassCannonMultiplier(finalMaxHealth, helPower);
        }

        internal static float GlassCannonMultiplier(float finalMaxHealth, float helPower)
        {
            if (!Finite(finalMaxHealth) || !Finite(helPower)) return 1f;

            // finalMaxHealth is half of the pre-sacrifice max HP, while HelPower is the
            // pre-sacrifice max stamina. Their ratio remains meaningful across biome tiers.
            float originalMaxHealth = finalMaxHealth * 2f;
            float ratio = helPower / Math.Max(1f, originalMaxHealth);

            if (ratio >= 2.70f) return 1.75f;
            if (ratio <= 0.75f) return 0.50f;

            if (ratio >= 2.50f)
                return SmoothSegment(ratio, 2.70f, 2.50f, 1.75f, 1.45f);

            if (ratio >= 2.00f)
                return SmoothSegment(ratio, 2.50f, 2.00f, 1.45f, 1.15f);

            if (ratio >= 1.50f)
                return SmoothSegment(ratio, 2.00f, 1.50f, 1.15f, 0.80f);

            if (ratio >= 1.00f)
                return SmoothSegment(ratio, 1.50f, 1.00f, 0.80f, 0.60f);

            return SmoothSegment(ratio, 1.00f, 0.75f, 0.60f, 0.50f);
        }

        private static float SmoothSegment(float value, float highPoint, float lowPoint, float highMultiplier, float lowMultiplier)
        {
            float t = (highPoint - value) / (highPoint - lowPoint);
            t = Math.Max(0f, Math.Min(1f, t));
            t = t * t * (3f - 2f * t);
            return highMultiplier + (lowMultiplier - highMultiplier) * t;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
