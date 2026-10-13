using System;
using HaldorExpansion.Features.Awakened;
class AbilityMathTests
{
    static int checks;
    static void Equal(float expected, float actual, string label)
    {
        checks++;
        if (Math.Abs(expected - actual) > .0001f || float.IsNaN(actual))
            throw new Exception(label + ": expected " + expected + ", got " + actual);
    }
    static void Main()
    {
        Equal(2f, AwakenedAbilityMath.DebtRegeneration(25f, 100f, 1f, true), "inclusive threshold");
        Equal(0f, AwakenedAbilityMath.DebtRegeneration(25.01f, 100f, 1f, true), "above threshold");
        Equal(0f, AwakenedAbilityMath.DebtRegeneration(20f, 100f, 1f, false), "no real debt");
        Equal(0f, AwakenedAbilityMath.DebtRegeneration(0f, 100f, 1f, true), "no resurrection");
        Equal(0f, AwakenedAbilityMath.DebtRegeneration(-1f, 100f, 1f, true), "lethal health");
        Equal(0f, AwakenedAbilityMath.DebtRegeneration(20f, 100f, float.NaN, true), "invalid dt");
        Equal(0f, AwakenedAbilityMath.DebtRegeneration(20f, 100f, -1f, true), "negative dt");
        Equal(1f, AwakenedAbilityMath.DebtRegeneration(40f, 200f, .25f, true), "max health and dt scaling");
        Equal(.75f, AwakenedAbilityMath.ShieldRetainedFraction(1.5f), "lose awakened share only");
        Equal(150f, 100f * AwakenedBalance.PitKingChest.ShieldMultiplier * AwakenedAbilityMath.ShieldRetainedFraction(1.5f), "normal set restored");
        Equal(520f, 100f * AwakenedBalance.PitKingChest.ShieldMultiplier * 2.6f, "Nova scaled once");
        Console.WriteLine("PASS awakened ability math: " + checks + " checks");
    }
}
// Enum boundary only; no game behaviour is simulated by these arithmetic tests.
public class HitData { public enum DamageModifier { SlightlyResistant } }
