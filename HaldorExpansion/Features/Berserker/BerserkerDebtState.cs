using System;

namespace HaldorExpansion.Features.Berserker
{
    // Player-owned combat resource. Separate from the Wrath state so a future helmet or
    // other equipment condition can be introduced without rewriting debt accumulation.
    // Deliberately no gameplay hard cap; double precision keeps repeated accumulation stable.
    internal sealed class BerserkerDebtState
    {
        internal double Amount { get; private set; }

        private float decayStartAt;
        private float lastTickAt;
        private bool clockInitialized;

        internal void AddEnemyDamage(float now, float actualHpLost, float coefficient, float delay)
        {
            if (!Finite(now) || !Finite(actualHpLost) || actualHpLost <= 0f ||
                !Finite(coefficient) || coefficient <= 0f) return;

            Add(actualHpLost * (double)coefficient);
            decayStartAt = now + Positive(delay, 10f);
            lastTickAt = now;
            clockInitialized = true;
        }

        internal void Tick(float now, float decayPercentPerSecond)
        {
            if (!Finite(now)) return;
            if (!clockInitialized)
            {
                lastTickAt = now;
                clockInitialized = true;
                return;
            }

            float start = Math.Max(lastTickAt, decayStartAt);
            lastTickAt = now;
            if (Amount <= 0d || now <= start || !Finite(decayPercentPerSecond)) return;

            double rate = Math.Max(0d, decayPercentPerSecond) * 0.01d;
            if (rate > 0d)
            {
                Amount *= Math.Exp(-rate * (now - start));
                if (Amount < 0.01d) Amount = 0d;
            }
        }

        internal double ConsumeAll()
        {
            double consumed = Amount;
            Amount = 0d;
            decayStartAt = 0f;
            lastTickAt = 0f;
            clockInitialized = false;
            return consumed;
        }

        internal void Reset()
        {
            Amount = 0d;
            decayStartAt = 0f;
            lastTickAt = 0f;
            clockInitialized = false;
        }

        private void Add(double addition)
        {
            if (double.IsNaN(addition) || double.IsInfinity(addition) || addition <= 0d) return;
            double next = Amount + addition;
            if (!double.IsInfinity(next)) Amount = next;
        }

        private static float Positive(float value, float fallback)
            => Finite(value) && value > 0f ? value : fallback;
        private static bool Finite(float value)
            => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
