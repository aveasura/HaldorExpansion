using System;
using System.Collections.Generic;
using HaldorExpansion.Features.HelOath;

internal static class ProgressionTests
{
    internal static void Run()
    {
        CheckTier(new string[0], 0.65f, 422.5f, "pre-Eikthyr");
        CheckTier(new[] { "defeated_eikthyr" }, 0.80f, 520f, "Eikthyr");
        CheckTier(new[] { "defeated_gdking" }, 1.00f, 650f, "Elder");
        CheckTier(new[] { "defeated_bonemass" }, 1.25f, 812.5f, "Bonemass");
        CheckTier(new[] { "defeated_dragon" }, 1.55f, 1007.5f, "Moder");
        CheckTier(new[] { "defeated_goblinking" }, 1.85f, 1202.5f, "Yagluth");
        CheckTier(new[] { "defeated_queen" }, 2.20f, 1430f, "Queen");
        CheckTier(new[] { "defeated_fader" }, 2.60f, 1690f, "Fader");

        // Highest known key wins even in an unusual world missing earlier keys.
        CheckTier(new[] { "defeated_eikthyr", "defeated_queen" }, 2.20f, 1430f, "highest defeated boss wins");

        float fallback = HelOathProgressionMath.ResolveMultiplier(null);
        Check(fallback, 1.00f, "null key reader falls back to Elder baseline");
        Check(HelOathProgressionMath.ScaleRequiredDamage(650f, float.NaN), 650f, "invalid multiplier falls back to x1");

        Console.WriteLine("PASS: Hel's Embrace world-progression charge curve");
    }

    private static void CheckTier(IEnumerable<string> keys, float expectedMultiplier, float expectedDamage, string name)
    {
        var set = new HashSet<string>(keys, StringComparer.Ordinal);
        float multiplier = HelOathProgressionMath.ResolveMultiplier(set.Contains);
        Check(multiplier, expectedMultiplier, name + " multiplier");
        Check(HelOathProgressionMath.ScaleRequiredDamage(650f, multiplier), expectedDamage, name + " required damage");
    }

    private static void Check(float actual, float expected, string name)
    {
        if (Math.Abs(actual - expected) > 0.001f)
            throw new Exception(name + " mismatch: " + actual + " != " + expected);
    }
}
