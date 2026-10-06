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

            Console.WriteLine("PASS: Berserker Wrath hit progression, V/Frenzy, timer restart, staged decay, partial reset and fractional AoE credit");
            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("FAIL: " + e.Message);
            return 1;
        }
    }

    private static bool Nearly(float a, float b) => Math.Abs(a - b) <= 0.0001f;
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }
}
