using System;
using HaldorExpansion.Features.Berserker;

internal static class BerserkerStateTests
{
    static int Main(string[] args)
    {
        try
        {
            var s = new BerserkerState();

            s.RegisterHit(0f, 1f, 2f, 10f);
            Check(s.WrathStage == 0 && Nearly(s.HitProgress, 1f), "First hit stores half a stage");
            s.RegisterHit(1f, 1f, 2f, 10f);
            Check(s.WrathStage == 1 && Nearly(s.HitProgress, 0f), "Two hits reach Wrath I");

            for (int i = 0; i < 8; i++)
                s.RegisterHit(2f + i, 1f, 2f, 10f);
            Check(s.WrathStage == 5 && s.Frenzy, "Ten total hits reach Wrath V / Frenzy");

            // Last hit was at t=9, so the first stage must survive until t=19.
            s.Tick(18.99f, 10f);
            Check(s.WrathStage == 5, "Wrath V survives until ten seconds after the latest hit");
            s.Tick(19f, 10f);
            Check(s.WrathStage == 4 && !s.Frenzy, "First decay removes exactly one stage and exits Frenzy");
            s.Tick(29f, 10f);
            Check(s.WrathStage == 3, "Decay continues one stage per interval");

            // A new hit restarts the full ten-second decay interval even when it does not grant a stage.
            s.RegisterHit(30f, 1f, 2f, 10f);
            Check(s.WrathStage == 3 && Nearly(s.HitProgress, 1f), "Partial stage progress is retained while fighting");
            s.Tick(39.99f, 10f);
            Check(s.WrathStage == 3, "New hit restarts decay timer");
            s.Tick(40f, 10f);
            Check(s.WrathStage == 2 && Nearly(s.HitProgress, 0f), "Losing a stage clears hidden partial progress");

            // Future AoE tuning can use fractional hit credit without changing state architecture.
            s.Reset();
            s.RegisterHit(0f, 0.5f, 2f, 10f);
            s.RegisterHit(1f, 0.5f, 2f, 10f);
            s.RegisterHit(2f, 1f, 2f, 10f);
            Check(s.WrathStage == 1, "Fractional AoE credit composes correctly");

            s.Tick(100f, 10f);
            Check(s.WrathStage == 0 && Nearly(s.HitProgress, 0f), "Long inactivity fully clears Wrath");

            s.RegisterHit(float.NaN, 1f, 2f, 10f);
            s.RegisterHit(0f, float.PositiveInfinity, 2f, 10f);
            Check(s.WrathStage == 0, "Malformed hit inputs are ignored");

            VerifyDebt();
            VerifyReckoning();

            Console.WriteLine("PASS: Berserker Wrath, Blood Debt and Reckoning pure-state/math regressions");
            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("FAIL: " + e.Message);
            return 1;
        }
    }

    private static void VerifyDebt()
    {
        var debt = new BerserkerDebtState();
        debt.AddEnemyDamage(0f, 20f, 2f, 10f);
        Check(NearlyDouble(debt.Amount, 40d), "Twenty real HP lost gives forty debt at x2");
        debt.Tick(9.99f, 4f);
        Check(NearlyDouble(debt.Amount, 40d), "No decay before ten seconds");
        debt.Tick(11f, 4f);
        Check(NearlyDouble(debt.Amount, 40d * Math.Exp(-0.04d)), "Four percent exponential decay begins after delay");

        debt.AddEnemyDamage(12f, 10f, 2f, 10f);
        double refreshed = debt.Amount;
        debt.Tick(21f, 4f);
        Check(NearlyDouble(debt.Amount, refreshed), "Fresh enemy damage restarts idle delay");
        debt.Reset();
        debt.AddEnemyDamage(0f, 1000000f, 100f, 10f);
        debt.AddEnemyDamage(1f, 1000000f, 100f, 10f);
        Check(debt.Amount >= 200000000d, "Debt has no gameplay maximum");
        debt.AddEnemyDamage(2f, float.NaN, 2f, 10f);
        Check(debt.Amount >= 200000000d, "Nonfinite inputs do not poison debt");
        debt.Reset();
        Check(debt.Amount == 0d, "Death/owner reset clears debt");
        Console.WriteLine("PASS: Blood Debt actual-HP coefficients, delay, exponential decay, unlimited growth and reset");
    }


    private static void VerifyReckoning()
    {
        Check(Nearly(BerserkerReckoningMath.BonusDamage(100d), 120f), "x1 Reckoning bonus is 120");
        Check(Nearly(BerserkerReckoningMath.BonusDamage(300d), 360f), "x3 Reckoning bonus is 360");
        Check(Nearly(BerserkerReckoningMath.BonusDamage(500d), 470f), "x5 Reckoning bonus is 470");
        Check(Nearly(BerserkerReckoningMath.BonusDamage(600d), 525f), "x6 Reckoning bonus is 525");
        Check(Nearly(BerserkerReckoningMath.BonusDamage(1000d), 665f), "x10 Reckoning bonus is 665");
        Check(Nearly(BerserkerReckoningMath.BonusDamage(2000d), 865f), "x20 Reckoning bonus is 865");

        Check(Nearly(BerserkerReckoningMath.HealAmount(500f, 0.25f), 125f),
            "Reckoning heals twenty-five percent of actual damage");
        Check(Nearly(BerserkerReckoningMath.HealAmount(1000f, 0.25f), 250f),
            "Reckoning lifesteal has no artificial max-health cap");

        Check(Nearly(BerserkerRagnarokMath.TotalDamage(500d), 1500f), "x5 Ragnarok total is 1500");
        Check(Nearly(BerserkerRagnarokMath.TotalDamage(600d), 1600f), "x6 Ragnarok total is 1600");
        Check(Nearly(BerserkerRagnarokMath.TotalDamage(800d), 1800f), "x8 Ragnarok total is 1800");
        Check(Nearly(BerserkerRagnarokMath.TotalDamage(1000d), 2000f), "x10 Ragnarok total is 2000");
        Check(Nearly(BerserkerRagnarokMath.TotalDamage(2000d), 2500f), "x20 Ragnarok soft-scales to 2500");

        var debt = new BerserkerDebtState();
        debt.AddEnemyDamage(0f, 125f, 2f, 10f);
        double consumed = debt.ConsumeAll();
        Check(NearlyDouble(consumed, 250d) && debt.Amount == 0d,
            "Reckoning consumes the entire exact Blood Debt amount");
        Console.WriteLine("PASS: Reckoning softer segmented bonus, uncapped lifesteal, Ragnarok independent curve and full debt consumption");
    }

    private static bool NearlyDouble(double a, double b)
        => Math.Abs(a - b) <= 0.0001d;

    private static bool Nearly(float a, float b) => Math.Abs(a - b) <= 0.0001f;
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }
}
