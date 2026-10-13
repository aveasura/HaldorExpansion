using System;

namespace HaldorExpansion.Features.Awakened
{
    internal static class AwakenedAbilityMath
    {
        internal static float DebtRegeneration(float health, float maxHealth, float dt, bool hasDebt)
        {
            if (!hasDebt || !Finite(health) || !Finite(maxHealth) || !Finite(dt) ||
                health <= 0f || maxHealth <= 0f || dt <= 0f ||
                health > maxHealth * AwakenedBalance.DelayedDoomChest.HealthThreshold) return 0f;
            return Math.Min(maxHealth - health, maxHealth * AwakenedBalance.DelayedDoomChest.HealthPerSecond * dt);
        }

        internal static float ShieldRetainedFraction(float normalSetMultiplier)
            => normalSetMultiplier / AwakenedBalance.PitKingChest.ShieldMultiplier;

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}