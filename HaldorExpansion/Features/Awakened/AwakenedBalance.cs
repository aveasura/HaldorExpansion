namespace HaldorExpansion.Features.Awakened
{
    // Quality is checked on the equipped ItemData, never SharedData.
    internal static class AwakenedBalance
    {
        internal static int QualityThreshold => AwakenedConfiguration.QualityThresholdValue;
        internal const float SkillCap = 100f;

        internal static class DelayedDoomChest
        {
            internal const float HealthThreshold = .25f, HealthPerSecond = .02f;
        }

        internal static class BerserkerHelmet
        {
            internal const float MissingHealthOnKill = .10f;
        }

        internal static class BerserkerAxe
        {
            internal const float DebtOnKill = 30f;
        }

        internal static class BoneCrushers
        {
            internal const float LifeSteal = .03f;
        }

        internal static class WoundedBeastCloak
        {
            internal const float Health = 30f, Unarmed = 30f;
        }

        internal static class HelheimPelt
        {
            internal const float Stamina = 80f, FallDamageReduction = .80f;
        }

        internal static class PitKingChest
        {
            internal const float ShieldMultiplier = 2f;
        }

        internal static class ShadowCrossbow
        {
            internal const float Sneak = 60f, Crossbows = 30f;
        }

        internal static class Brisingamen
        {
            internal const float StaminaRegenBonus = .40f;
        }

        internal static class ScorchedEnduranceCloak
        {
            internal const float SlashReduction = .25f;
            internal const HitData.DamageModifier SlashResistance = HitData.DamageModifier.SlightlyResistant;
        }
    }
}