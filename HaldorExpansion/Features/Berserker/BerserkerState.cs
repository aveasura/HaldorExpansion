using System;

namespace HaldorExpansion.Features.Berserker
{
    // Pure state machine: no Unity dependency, so Wrath timing can be regression-tested separately.
    internal sealed class BerserkerState
    {
        internal const int MaxWrathStage = 5;

        internal int WrathStage { get; private set; }
        internal float HitProgress { get; private set; }
        internal bool Frenzy => WrathStage >= MaxWrathStage;

        private bool decayArmed;
        private float nextDecayAt;

        internal void RegisterHit(float now, float credit, float hitsPerStage, float decayInterval)
        {
            hitsPerStage = NormalizePositive(hitsPerStage, 2f);
            decayInterval = NormalizePositive(decayInterval, 10f);
            if (!Finite(now) || !Finite(credit) || credit <= 0f) return;

            if (WrathStage < MaxWrathStage)
            {
                HitProgress += credit;
                while (WrathStage < MaxWrathStage && HitProgress + 0.0001f >= hitsPerStage)
                {
                    HitProgress -= hitsPerStage;
                    if (HitProgress < 0f) HitProgress = 0f;
                    WrathStage++;
                }

                if (WrathStage >= MaxWrathStage)
                    HitProgress = 0f;
            }

            decayArmed = WrathStage > 0;
            nextDecayAt = now + decayInterval;
        }

        internal void Tick(float now, float decayInterval)
        {
            decayInterval = NormalizePositive(decayInterval, 10f);
            if (!Finite(now) || WrathStage <= 0 || !decayArmed) return;

            while (WrathStage > 0 && now + 0.0001f >= nextDecayAt)
            {
                WrathStage--;
                HitProgress = 0f;

                if (WrathStage <= 0)
                {
                    decayArmed = false;
                    nextDecayAt = 0f;
                    return;
                }

                nextDecayAt += decayInterval;
            }
        }

        internal void Reset()
        {
            WrathStage = 0;
            HitProgress = 0f;
            decayArmed = false;
            nextDecayAt = 0f;
        }

        private static float NormalizePositive(float value, float fallback)
            => !Finite(value) || value <= 0f ? fallback : value;

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
