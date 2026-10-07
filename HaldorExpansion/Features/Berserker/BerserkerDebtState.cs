using System;

namespace HaldorExpansion.Features.Berserker
{
    // Player-owned combat resource. Separate from the Wrath state so equipment modifiers
    // can be layered on later without changing the core accumulation/decay rules.
    // Deliberately no gameplay hard cap; double precision keeps repeated accumulation stable.
    internal sealed class BerserkerDebtState
    {
        internal double Amount { get; private set; }

        private float lastIncomingAt;
        private float lastOutgoingAt;
        private float lastTickAt;
        private bool hasIncomingActivity;
        private bool hasOutgoingActivity;
        private bool clockInitialized;

        internal void AddEnemyDamage(float now, float actualHpLost, float maxHealth, float debtPerFullHealthBar)
        {
            if (!Finite(now) || !Finite(actualHpLost) || actualHpLost <= 0f ||
                !Finite(maxHealth) || maxHealth <= 0f ||
                !Finite(debtPerFullHealthBar) || debtPerFullHealthBar <= 0f) return;

            double healthFractionLost = actualHpLost / (double)maxHealth;
            Add(healthFractionLost * debtPerFullHealthBar);
            lastIncomingAt = now;
            lastTickAt = now;
            hasIncomingActivity = true;
            clockInitialized = true;
        }

        // Every real Berserker Greataxe hit counts as outgoing combat activity for decay QoL.
        // debtGain may be zero for Reckoning: it keeps the fight alive without refunding Debt.
        internal void RegisterAxeHit(float now, float debtGain)
        {
            if (!Finite(now)) return;
            if (Finite(debtGain) && debtGain > 0f) Add(debtGain);

            lastOutgoingAt = now;
            lastTickAt = now;
            hasOutgoingActivity = true;
            clockInitialized = true;
        }

        internal void Tick(float now, float decayPercentPerSecond, float incomingGrace, float outgoingGrace)
        {
            if (!Finite(now)) return;
            if (!clockInitialized)
            {
                lastTickAt = now;
                clockInitialized = true;
                return;
            }

            float decayStartAt = float.NegativeInfinity;
            if (hasIncomingActivity)
                decayStartAt = Math.Max(decayStartAt, lastIncomingAt + NonNegative(incomingGrace, 10f));
            if (hasOutgoingActivity)
                decayStartAt = Math.Max(decayStartAt, lastOutgoingAt + NonNegative(outgoingGrace, 5f));

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

        internal double Spend(double amount)
        {
            if (double.IsNaN(amount) || double.IsInfinity(amount) || amount <= 0d || Amount <= 0d) return 0d;
            double spent = Math.Min(Amount, amount);
            Amount -= spent;
            if (Amount < 0.000001d) Amount = 0d;
            return spent;
        }

        internal double ConsumeAll()
        {
            double consumed = Amount;
            Amount = 0d;
            ClearClock();
            return consumed;
        }

        internal void Reset()
        {
            Amount = 0d;
            ClearClock();
        }

        private void Add(double addition)
        {
            if (double.IsNaN(addition) || double.IsInfinity(addition) || addition <= 0d) return;
            double next = Amount + addition;
            if (!double.IsInfinity(next)) Amount = next;
        }

        private void ClearClock()
        {
            lastIncomingAt = 0f;
            lastOutgoingAt = 0f;
            lastTickAt = 0f;
            hasIncomingActivity = false;
            hasOutgoingActivity = false;
            clockInitialized = false;
        }

        private static float NonNegative(float value, float fallback)
            => Finite(value) && value >= 0f ? value : fallback;
        private static bool Finite(float value)
            => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
