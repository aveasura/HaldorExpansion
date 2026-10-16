using System;
using Random = UnityEngine.Random;

namespace HaldorExpansion.Features.Upgrades
{
    internal static class IdolLootAmount
    {
        internal static int Roll(int maximum)
        {
            int amount = Random.Range(1, maximum + 1);
            // The shadow result is only the success gate. Scale our fresh base exactly
            // once, including idols whose native drop entry/type would exempt them.
            double scaled = amount * (double)Game.m_resourceRate;
            if (!(scaled > 0d) || double.IsInfinity(scaled)) return 0;
            int whole = (int)Math.Min(scaled, int.MaxValue);
            double remainder = scaled - whole;
            // Fractional world rates retain their expected amount without an upward
            // rounding bias. Integer rates (e.g. base 2 at x3) need no extra RNG draw.
            if (whole < int.MaxValue && remainder > 0d && Random.value < remainder) whole++;
            return whole;
        }
    }
}