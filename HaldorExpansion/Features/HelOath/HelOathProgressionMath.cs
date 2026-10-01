using System;

namespace HaldorExpansion.Features.HelOath
{
    // Pure progression rules: no Unity/Valheim dependency, so the curve can be regression-tested.
    internal static class HelOathProgressionMath
    {
        internal const float PreEikthyrMultiplier = 0.90f;
        internal const float EikthyrMultiplier    = 1.05f;
        internal const float ElderMultiplier      = 1.25f;
        internal const float BonemassMultiplier   = 1.50f;
        internal const float ModerMultiplier      = 1.75f;
        internal const float YagluthMultiplier    = 2.00f;
        internal const float QueenMultiplier      = 2.25f;
        internal const float FaderMultiplier      = 2.60f;

        internal static float ResolveMultiplier(Func<string, bool> hasGlobalKey)
        {
            if (hasGlobalKey == null)
                return ElderMultiplier;

            // Resolve from newest to oldest so imported/edited worlds still use their
            // highest known progression even if an earlier key is unexpectedly absent.
            if (hasGlobalKey("defeated_fader"))
                return FaderMultiplier;
            if (hasGlobalKey("defeated_queen"))
                return QueenMultiplier;
            if (hasGlobalKey("defeated_goblinking"))
                return YagluthMultiplier;
            if (hasGlobalKey("defeated_dragon"))
                return ModerMultiplier;
            if (hasGlobalKey("defeated_bonemass"))
                return BonemassMultiplier;
            if (hasGlobalKey("defeated_gdking"))
                return ElderMultiplier;
            if (hasGlobalKey("defeated_eikthyr"))
                return EikthyrMultiplier;

            return PreEikthyrMultiplier;
        }

        internal static float ScaleRequiredDamage(float baseDamage, float multiplier)
        {
            if (float.IsNaN(baseDamage) || float.IsInfinity(baseDamage) || baseDamage <= 0f)
                return 0f;
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier) || multiplier <= 0f)
                multiplier = ElderMultiplier;

            return baseDamage * multiplier;
        }
    }
}
