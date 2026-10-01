using System;
using HaldorExpansion.Features.HelOath;

internal static class ChargeCreditTests
{
    internal static void Run()
    {
        Check(HelOathChargeCreditMath.Select(180f, 180f, true, false), 180f, "Neutral target credits full outgoing damage");
        Check(HelOathChargeCreditMath.Select(180f, 90f, true, false), 180f, "Resistance does not reduce charge credit");
        Check(HelOathChargeCreditMath.Select(180f, 45f, true, false), 180f, "Very resistant target still credits full outgoing damage");
        Check(HelOathChargeCreditMath.Select(180f, 0f, true, false), 0f, "Immune/no-damage target cannot be used as a charge battery");
        Check(HelOathChargeCreditMath.Select(180f, 90f, true, true), 0f, "Special shot never charges Embrace");
        Check(HelOathChargeCreditMath.Select(180f, 90f, false, false), 0f, "Non-enemy target does not charge Embrace");
        Check(HelOathChargeCreditMath.Select(float.NaN, 90f, true, false), 0f, "Malformed outgoing damage is rejected");
        Check(HelOathChargeCreditMath.Select(180f, float.PositiveInfinity, true, false), 0f, "Malformed actual damage is rejected");
        Console.WriteLine("PASS: pre-defense Hel Oath charge credit and special/immune gating");
    }

    private static void Check(float value, float expected, string message)
    {
        if (Math.Abs(value - expected) > 0.001f)
            throw new Exception(message + $": expected {expected}, got {value}");
    }
}
