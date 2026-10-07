using BepInEx.Configuration;
using Jotunn.Extensions;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace HaldorExpansion.Features.Berserker
{
    internal static class BerserkerConfiguration
    {
        internal static ConfigEntry<float> HitsPerWrathStage;
        internal static ConfigEntry<float> WrathDecayInterval;
        internal static ConfigEntry<float> BloodDebtEnemyMultiplier;
        internal static ConfigEntry<float> BloodDebtDecayDelay;
        internal static ConfigEntry<float> BloodDebtDecayPercentPerSecond;
        internal static ConfigEntry<float> ReckoningMinimumDebt;
        internal static ConfigEntry<float> ReckoningLifeStealPercent;
        internal static ConfigEntry<float> WrathAttackSpeedPercentPerStage;
        internal static ConfigEntry<float> WrathStaminaCostReductionPercentPerStage;
        internal static ConfigEntry<float> RagnarokMinimumDebt;
        internal static ConfigEntry<float> RagnarokCastDuration;
        internal static ConfigEntry<float> RagnarokExhaustionDuration;
        internal static ConfigEntry<float> RagnarokExhaustionMovePenaltyPercent;
        internal static ButtonConfig RagnarokButton;

        internal static void Register(ConfigFile config)
        {
            HitsPerWrathStage = config.BindConfig(
                "Berserker",
                "Hits per Wrath stage",
                2f,
                "Valid enemy hits with the Berserker Greataxe required to gain one Wrath stage. " +
                "This is intentionally low for the first gameplay prototype.",
                synced: true);

            WrathDecayInterval = config.BindConfig(
                "Berserker",
                "Wrath decay interval",
                10f,
                "Seconds without a valid Berserker Greataxe hit before one Wrath stage is lost. " +
                "Each lost stage restarts the same interval.",
                synced: true);

            BloodDebtEnemyMultiplier = config.BindConfig(
                "Berserker", "Blood Debt from enemy HP loss multiplier", 2f,
                "Real HP lost to hostile creatures (after armor, resistance, and blocks) is multiplied by this value. " +
                "Works only while the Berserker Greataxe is equipped.", synced: true);

            BloodDebtDecayDelay = config.BindConfig(
                "Berserker", "Blood Debt decay delay seconds", 10f,
                "Seconds without real HP loss from a hostile creature before Blood Debt begins to decay.", synced: true);

            BloodDebtDecayPercentPerSecond = config.BindConfig(
                "Berserker", "Blood Debt decay percent per second", 2.4f,
                "Exponential percent-per-second decrease while out of combat. No maximum Blood Debt limit.", synced: true);

            ReckoningMinimumDebt = config.BindConfig(
                "Berserker", "Reckoning minimum Blood Debt", 100f,
                "Minimum Blood Debt required for the secondary greataxe attack to become Reckoning at Wrath V.", synced: true);

            ReckoningLifeStealPercent = config.BindConfig(
                "Berserker", "Reckoning lifesteal percent", 25f,
                "Percent of the main target's actual post-defense Reckoning damage restored as health.", synced: true);

            WrathAttackSpeedPercentPerStage = config.BindConfig(
                "Berserker", "Wrath attack speed percent per stage", 10f,
                "Attack animation speed gained per active Wrath stage while wielding the Berserker Greataxe.", synced: true);

            WrathStaminaCostReductionPercentPerStage = config.BindConfig(
                "Berserker", "Wrath stamina cost reduction percent per stage", 5f,
                "Stamina cost reduction for all normal actions per active Wrath stage while wielding the Berserker Greataxe.", synced: true);

            RagnarokMinimumDebt = config.BindConfig(
                "Berserker", "Ragnarok minimum Blood Debt", 500f,
                "Minimum Blood Debt required to cast the prototype Ragnarok ability at Wrath V.", synced: true);

            RagnarokCastDuration = config.BindConfig(
                "Berserker", "Ragnarok cast duration seconds", 1.5f,
                "Prototype cast time before the meteor rain is released.", synced: true);

            RagnarokExhaustionDuration = config.BindConfig(
                "Berserker", "Ragnarok exhaustion duration seconds", 3f,
                "Duration of Exhaustion after Ragnarok resolves.", synced: true);

            RagnarokExhaustionMovePenaltyPercent = config.BindConfig(
                "Berserker", "Ragnarok exhaustion movement penalty percent", 25f,
                "Movement-speed penalty applied by Exhaustion after Ragnarok.", synced: true);

            ConfigEntry<KeyCode> ragnarokKey = config.Bind(
                "Berserker", "Ragnarok Ability Key", KeyCode.Z,
                "Prototype key for casting Ragnarok at Wrath V with at least x5 Blood Debt. The final set requirement will be added later.");
            RagnarokButton = new ButtonConfig
            {
                Name = "HE_BerserkerRagnarok",
                Config = ragnarokKey,
                HintToken = "$he_berserker_ragnarok_ability",
                BlockOtherInputs = true
            };
            InputManager.Instance.AddButton(HaldorExpansionPlugin.ModGuid, RagnarokButton);

        }

        internal static float HitsPerStageValue => FiniteClamp(HitsPerWrathStage.Value, 0.25f, 100f, 2f);
        internal static float DecayIntervalValue => FiniteClamp(WrathDecayInterval.Value, 0.5f, 300f, 10f);

        internal static float EnemyDebtMultiplierValue => FiniteClamp(BloodDebtEnemyMultiplier.Value, 0f, 100f, 2f);
        internal static float DebtDecayDelayValue => FiniteClamp(BloodDebtDecayDelay.Value, 0.1f, 600f, 10f);
        internal static float DebtDecayRateValue => FiniteClamp(BloodDebtDecayPercentPerSecond.Value, 0f, 100f, 2.4f);
        internal static float ReckoningMinimumDebtValue => FiniteClamp(ReckoningMinimumDebt.Value, 1f, 1000000f, 100f);
        internal static float ReckoningLifeStealFraction => FiniteClamp(ReckoningLifeStealPercent.Value, 0f, 100f, 25f) * 0.01f;
        internal static float WrathAttackSpeedFractionPerStage => FiniteClamp(WrathAttackSpeedPercentPerStage.Value, 0f, 100f, 10f) * 0.01f;
        internal static float WrathStaminaReductionFractionPerStage => FiniteClamp(WrathStaminaCostReductionPercentPerStage.Value, 0f, 19f, 5f) * 0.01f;
        internal static float RagnarokMinimumDebtValue => FiniteClamp(RagnarokMinimumDebt.Value, 100f, 1000000f, 500f);
        internal static float RagnarokCastDurationValue => FiniteClamp(RagnarokCastDuration.Value, 0.1f, 10f, 1.5f);
        internal static float RagnarokExhaustionDurationValue => FiniteClamp(RagnarokExhaustionDuration.Value, 0f, 30f, 3f);
        internal static float RagnarokExhaustionMovePenaltyFraction => FiniteClamp(RagnarokExhaustionMovePenaltyPercent.Value, 0f, 90f, 25f) * 0.01f;

        private static float FiniteClamp(float value, float min, float max, float fallback)
            => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);
    }
}
