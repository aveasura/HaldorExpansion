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

            Console.WriteLine("PASS: Berserker Wrath hit progression, V/Frenzy, timer restart, staged decay, partial reset and fractional AoE credit");
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
        debt.AddFrenzyLoss(22f, 10f, 0.1f, 10f);
        double withFrenzy = debt.Amount;
        debt.Tick(23f, 4f);
        Check(debt.Amount < withFrenzy, "Frenzy drain never refreshes enemy-hit delay");

        debt.Reset();
        debt.AddFrenzyLoss(2f, 10f, 0.1f, 10f);
        Check(NearlyDouble(debt.Amount, 1d), "Frenzy self-drain accrues only 10 percent");
        debt.Tick(11f, 4f);
        Check(NearlyDouble(debt.Amount, 1d), "Frenzy-only debt uses its own first decay delay");
        debt.Tick(13f, 4f);
        Check(debt.Amount < 1d, "Frenzy-only debt decays without enemy hits");

        debt.Reset();
        debt.AddEnemyDamage(0f, 1000000f, 100f, 10f);
        debt.AddEnemyDamage(1f, 1000000f, 100f, 10f);
        Check(debt.Amount >= 200000000d, "Debt has no gameplay maximum");
        debt.AddEnemyDamage(2f, float.NaN, 2f, 10f);
        Check(debt.Amount >= 200000000d, "Nonfinite inputs do not poison debt");
        debt.Reset();
        Check(debt.Amount == 0d, "Death/owner reset clears debt");
        Console.WriteLine("PASS: Blood Debt actual-HP coefficients, delay, exponential decay, Frenzy-only credit, unlimited growth and reset");
    }

    private static bool NearlyDouble(double a, double b)
        => Math.Abs(a - b) <= 0.0001d;

    private static bool Nearly(float a, float b) => Math.Abs(a - b) <= 0.0001f;
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }
}
