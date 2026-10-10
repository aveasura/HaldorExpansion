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

        // 85% of a health bar should provide ~350 Debt with the default 411.765/full-bar economy.
        debt.AddEnemyDamage(0f, 85f, 100f, 411.765f);
        Check(Math.Abs(debt.Amount - 350.00025d) < 0.01d,
            "Incoming Debt is normalized by Max HP instead of absolute HP loss");

        // Outgoing combat contributes a fixed amount and holds decay for five seconds.
        debt.RegisterAxeHit(1f, 7f);
        Check(Math.Abs(debt.Amount - 357.00025d) < 0.01d, "Primary axe hit adds seven Debt");
        debt.Tick(9.99f, 4f, 10f, 5f);
        Check(Math.Abs(debt.Amount - 357.00025d) < 0.01d,
            "Debt does not decay while either incoming or outgoing grace window is still active");

        // Incoming grace expires at t=10, outgoing at t=6, so decay starts at t=10.
        debt.Tick(11f, 4f, 10f, 5f);
        Check(Math.Abs(debt.Amount - 357.00025d * Math.Exp(-0.04d)) < 0.01d,
            "Decay begins only after both incoming and outgoing grace windows expire");

        // A new axe hit, even with zero Debt gain (Reckoning), refreshes outgoing combat QoL.
        double beforeReckoningActivity = debt.Amount;
        debt.RegisterAxeHit(12f, 0f);
        debt.Tick(16.99f, 4f, 10f, 5f);
        Check(Math.Abs(debt.Amount - beforeReckoningActivity) < 0.01d,
            "Reckoning-style zero-gain axe activity refreshes the five-second outgoing grace");

        debt.RegisterAxeHit(17f, 11f);
        Check(debt.Amount > beforeReckoningActivity + 10.9d, "Secondary axe hit adds eleven Debt");

        double half = debt.Amount * 0.5d;
        double spent = debt.Spend(half);
        Check(Math.Abs(spent - half) < 0.0001d && Math.Abs(debt.Amount - half) < 0.0001d,
            "Partial Debt spending leaves the unspent half intact");

        debt.Reset();
        debt.AddEnemyDamage(0f, 1000000f, 1f, 411.765f);
        debt.AddEnemyDamage(1f, 1000000f, 1f, 411.765f);
        Check(debt.Amount >= 823000000d, "Debt has no gameplay maximum");
        debt.AddEnemyDamage(2f, float.NaN, 100f, 411.765f);
        Check(debt.Amount >= 823000000d, "Nonfinite inputs do not poison debt");
        debt.Reset();
        Check(debt.Amount == 0d, "Death/owner reset clears debt");
        Console.WriteLine(
            "PASS: Blood Debt Max-HP normalization, dual grace decay, outgoing hit gains, partial spending and unlimited growth");
    }


    private static void VerifyReckoning()
    {
        Check(Nearly(BerserkerReckoningMath.BonusDamage(100d), 120f), "x1 Reckoning bonus is 120");
        Check(Nearly(BerserkerReckoningMath.BonusDamage(300d), 360f), "x3 Reckoning bonus is 360");
        Check(Nearly(BerserkerReckoningMath.BonusDamage(500d), 470f), "x5 Reckoning bonus is 470");
        Check(Nearly(BerserkerReckoningMath.BonusDamage(600d), 525f), "x6 Reckoning bonus is 525");
        Check(Nearly(BerserkerReckoningMath.BonusDamage(1000d), 665f), "x10 Reckoning bonus is 665");
        Check(Nearly(BerserkerReckoningMath.BonusDamage(2000d), 865f), "x20 Reckoning bonus is 865");

        Check(Nearly(BerserkerReckoningMath.HealAmount(500f, 0.20f), 100f),
            "Reckoning heals twenty percent of final direct damage");
        Check(Nearly(BerserkerReckoningMath.HealAmount(1000f, 0.20f), 200f),
            "Reckoning lifesteal has no artificial max-health cap");

        Check(BerserkerRagnarokMath.MeteorCount(500d) == 10, "x5 Ragnarok spawns 10 meteors");
        Check(BerserkerRagnarokMath.MeteorCount(600d) == 12, "x6 Ragnarok spawns 12 meteors");
        Check(BerserkerRagnarokMath.MeteorCount(800d) == 16, "x8 Ragnarok spawns 16 meteors");
        Check(BerserkerRagnarokMath.MeteorCount(1500d) == 16, "Ragnarok meteor count caps at 16");
        Check(Nearly(BerserkerRagnarokMath.TotalDamage(400d), 0f), "Below x5 Ragnarok deals no damage");
        Check(Nearly(BerserkerRagnarokMath.TotalDamage(500d), 1440f), "x5 Ragnarok total is 1440 after +80%");
        Check(Nearly(BerserkerRagnarokMath.TotalDamage(600d), 1800f), "x6 Ragnarok total is 1800 after +80%");
        Check(Nearly(BerserkerRagnarokMath.TotalDamage(800d), 2790f), "x8 Ragnarok total is 2790 after +80%");
        Check(Nearly(BerserkerRagnarokMath.TotalDamage(1000d), 4140f), "x10 Ragnarok total is 4140 after +80%");
        Check(Nearly(BerserkerRagnarokMath.TotalDamage(1500d), 9090f), "x15 Ragnarok total is 9090 after +80%");
        Check(Nearly(BerserkerRagnarokMath.TotalDamage(2000d), 11340f), "x20 Ragnarok soft-scales to 11340");
        Check(Nearly(BerserkerRagnarokMath.PerMeteorDamage(500d), 144f), "x5 damage splits across 10 meteors");
        Check(Nearly(BerserkerRagnarokMath.PerMeteorDamage(800d), 174.375f), "x8 damage splits across 16 meteors");

        var debt = new BerserkerDebtState();
        debt.AddEnemyDamage(0f, 195f, 100f, 100f);
        double reckoningSpend = debt.Amount * 0.5d;
        double spent = debt.Spend(reckoningSpend);
        Check(NearlyDouble(spent, 97.5d) && NearlyDouble(debt.Amount, 97.5d),
            "Reckoning can spend exactly half of 195 Debt and leave 97.5");

        debt.RegisterAxeHit(1f, 402.5f);
        double consumed = debt.ConsumeAll();
        Check(NearlyDouble(consumed, 500d) && debt.Amount == 0d,
            "Ragnarok still consumes the entire exact Blood Debt amount");
        Console.WriteLine(
            "PASS: Reckoning softer segmented bonus, half-spend resource model, uncapped lifesteal and Ragnarok full consumption");
    }

    private static bool NearlyDouble(double a, double b)
        => Math.Abs(a - b) <= 0.0001d;

    private static bool Nearly(float a, float b) => Math.Abs(a - b) <= 0.0001f;

    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }
}
