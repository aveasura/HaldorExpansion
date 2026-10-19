using System;

namespace HaldorExpansion.Features.HelOath
{
    // Gameplay tuning, independent of the approved appearance settings.
    internal static class HelOathGhostRules
    {
        internal const int RequiredQuality = 10;
        internal const int Limit = 3;
        internal const float SoulCostFraction = 1f / 3f;
        internal const float InactivitySeconds = 45f;
        internal const float SoulDecaySeconds = 10f;
        internal const float NormalDamageMultiplier = 0.33f;
        internal const float ActiveDamageMultiplier = 0.50f;
        internal const float ReactionMin = 0.15f;
        internal const float ReactionMax = 0.40f;
    }

    internal sealed class HelOathGhostState
    {
        internal int Count { get; private set; }
        internal float Progress { get; private set; }
        internal int Generation { get; private set; }
        private float lastUpdate;
        private float inactivityRemaining = HelOathGhostRules.InactivitySeconds;
        private float hiddenRemaining = HelOathGhostRules.SoulDecaySeconds;
        private bool equipped = true;
        private bool hasClock;

        internal void Hit(float now, bool eligible, float damage, float fullChargeCost)
        {
            if (!eligible || !Finite(now) || !Finite(damage) || damage <= 0f
                || !Finite(fullChargeCost) || fullChargeCost <= 0f) return;
            Update(now, true);
            inactivityRemaining = HelOathGhostRules.InactivitySeconds;
            hiddenRemaining = HelOathGhostRules.SoulDecaySeconds;
            if (Count == HelOathGhostRules.Limit)
            {
                Progress = 0f;
                return;
            }

            // Store fractional souls, so a progression change cannot multiply banked damage.
            Progress += damage / (fullChargeCost * HelOathGhostRules.SoulCostFraction);
            int earned = (int)Math.Min(HelOathGhostRules.Limit - Count, Math.Floor(Progress + 0.000001f));
            Count += earned;
            Progress = Count == HelOathGhostRules.Limit ? 0f : Math.Max(0f, Progress - earned);
        }

        // The previous equipment state owns the elapsed interval. Toggling the bow
        // pauses/resumes the hidden budget; only a confirmed hit refreshes either budget.
        internal void Update(float now, bool isEquipped)
        {
            if (!Finite(now) || (hasClock && now < lastUpdate))
            {
                Reset();
                return;
            }

            float elapsed = hasClock ? now - lastUpdate : 0f;
            lastUpdate = now;
            hasClock = true;
            while ((Count > 0 || Progress > 0f) && elapsed >= 0f)
            {
                float untilLoss = equipped ? inactivityRemaining : Math.Min(inactivityRemaining, hiddenRemaining);
                if (elapsed < untilLoss)
                {
                    inactivityRemaining -= elapsed;
                    if (!equipped) hiddenRemaining -= elapsed;
                    break;
                }

                elapsed -= untilLoss;
                if (Count > 0) Count--;
                Progress = 0f;
                Generation++;
                // A hidden loss must not shorten the original 45-second inactivity
                // grace when the bow returns. Also keep at least ten seconds between
                // losses, so overlapping deadlines cannot charge the same period twice.
                inactivityRemaining = Math.Max(inactivityRemaining - untilLoss, HelOathGhostRules.SoulDecaySeconds);
                hiddenRemaining = HelOathGhostRules.SoulDecaySeconds;
            }

            equipped = isEquipped;
        }

        internal int Consume()
        {
            int result = Count;
            Reset();
            return result;
        }

        internal void Reset()
        {
            if (Count != 0 || Progress != 0f) Generation++;
            Count = 0;
            Progress = 0f;
            lastUpdate = 0f;
            hasClock = false;
            equipped = true;
            inactivityRemaining = HelOathGhostRules.InactivitySeconds;
            hiddenRemaining = HelOathGhostRules.SoulDecaySeconds;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}