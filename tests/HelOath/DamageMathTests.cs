using System;
using HaldorExpansion.Features.HelOath;

internal static class DamageMathTests
{
    internal static void Run()
    {
        // Hold final HP at 100 so helPower = ratio * originalHP (200).
        Check(HelOathDamageMath.GlassCannonMultiplier(100f, 150f), 0.50f, "0.75 ratio multiplier");
        Check(HelOathDamageMath.GlassCannonMultiplier(100f, 200f), 0.60f, "1.00 ratio multiplier");
        Check(HelOathDamageMath.GlassCannonMultiplier(100f, 300f), 0.80f, "1.50 ratio multiplier");
        Check(HelOathDamageMath.GlassCannonMultiplier(100f, 400f), 1.15f, "2.00 ratio multiplier");
        Check(HelOathDamageMath.GlassCannonMultiplier(100f, 500f), 1.45f, "2.50 ratio multiplier");
        Check(HelOathDamageMath.GlassCannonMultiplier(100f, 540f), 1.95f, "2.70 ratio multiplier");
        Check(HelOathDamageMath.GlassCannonMultiplier(100f, 700f), 1.95f, "ratio cap");

        // Deep-North-ish full stamina build after Hel's exact transfer:
        // 70 final HP, 460 final stamina => HelPower 390 => ratio 390 / 140 > 2.70.
        Check(HelOathDamageMath.CalculateSplash(460f, 70f, 0.85f), 646.425f, "glass normal Embrace target");
        Check(HelOathDamageMath.CalculateSplash(460f, 70f, 2.15f), 1635.075f, "glass Touch V target");

        float previous = HelOathDamageMath.GlassCannonMultiplier(100f, 150f);
        for (int hpPower = 160; hpPower <= 540; hpPower += 10)
        {
            float current = HelOathDamageMath.GlassCannonMultiplier(100f, hpPower);
            if (current + 0.0001f < previous) throw new Exception("Ratio glass-cannon curve is not monotonic");
            previous = current;
        }

        if (HelOathDamageMath.CalculateSplash(100f, 150f, 1.5f) != 0f)
            throw new Exception("Negative HelPower was not clamped");

        Console.WriteLine("PASS: HelPower, coefficients and biome-stable ratio glass-cannon curve");
    }

    private static void Check(float actual, float expected, string name)
    {
        if (Math.Abs(actual - expected) > 0.001f) throw new Exception(name + " mismatch: " + actual + " != " + expected);
    }
}
