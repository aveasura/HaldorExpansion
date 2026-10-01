using System;
using HaldorExpansion.Features.HelOath;
internal static class DotTests
{
    internal static void Run()
    {
        var ledger = new HelOathDotLedger();
        ledger.Add("player-a:epoch", 30f);
        ledger.Add("player-b:epoch", 10f);
        // Pool also contains 60 damage from other weapons/special shots, never credited.
        var tick = ledger.Consume(100f, 20f);
        Check(tick["player-a:epoch"], 6f);
        Check(tick["player-b:epoch"], 2f);
        tick = ledger.Consume(80f, 20f);
        Check(tick["player-a:epoch"], 6f);
        Check(tick["player-b:epoch"], 2f);
        // Poison replacement from an uncredited source discards old attribution.
        ledger.Clear();
        if (ledger.Consume(50f, 10f).Count != 0) throw new Exception("Replaced poison retained credit");
        ledger.Add("normal", 5f);
        Check(ledger.Consume(5f, 10f)["normal"], 5f);
        if (ledger.Consume(0f, 10f).Count != 0) throw new Exception("Spent DoT counted twice");
        Console.WriteLine("PASS: mixed-source DoT, proportional ticks, replacement and exhaustion");
    }
    static void Check(float value, float expected) { if(Math.Abs(value-expected)>0.001f) throw new Exception("Wrong DoT ownership share"); }
}
