using System;

namespace HaldorExpansion.Features.HelOath
{
    internal static class HelOathChargeCreditMath
    {
        internal static float Select(float outgoingDamage, float actualDamage, bool validEnemy, bool special)
        {
            if (special || !validEnemy) return 0f;
            if (!Finite(outgoingDamage) || outgoingDamage <= 0f) return 0f;
            // Require the hit to have actually connected for damage. This prevents immune/invalid
            // targets from becoming charge batteries, while resistance/armor no longer shrink credit.
            if (!Finite(actualDamage) || actualDamage <= 0f) return 0f;
            return outgoingDamage;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
