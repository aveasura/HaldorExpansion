using BepInEx.Configuration;
using HaldorExpansion.Configuration;
using Jotunn.Configs;
using Jotunn.Extensions;
using Jotunn.Managers;
using UnityEngine;

namespace HaldorExpansion.Features.Berserker
{
    internal static class BerserkerConfiguration
    {
        internal static ConfigEntry<float> HitsPerWrathStage;
        internal static ConfigEntry<float> WrathDecayInterval;
        internal static ConfigEntry<float> BloodDebtPerFullHealthBar;
        internal static ConfigEntry<float> BloodDebtIncomingGraceSeconds;
        internal static ConfigEntry<float> BloodDebtOutgoingGraceSeconds;
        internal static ConfigEntry<float> BloodDebtPrimaryHitGain;
        internal static ConfigEntry<float> BloodDebtSecondaryHitGain;
        internal static ConfigEntry<float> BloodDebtDecayPercentPerSecond;
        internal static ConfigEntry<float> ReckoningMinimumDebt;
        internal static ConfigEntry<float> ReckoningSpendPercent;
        internal static ConfigEntry<float> ReckoningLifeStealPercent;
        internal static ConfigEntry<float> WrathAttackSpeedPercentPerStage;
        internal static ConfigEntry<float> WrathStaminaCostReductionPercentPerStage;
        internal static ConfigEntry<float> RagnarokMinimumDebt;
        internal static ConfigEntry<float> RagnarokCastDuration;
        internal static ConfigEntry<float> RagnarokExhaustionDuration;
        internal static ConfigEntry<float> RagnarokExhaustionMovePenaltyPercent;
        internal static ConfigEntry<float> RagnarokExhaustionAttackPenaltyPercent;
        internal static ConfigEntry<float> RagnarokExhaustionStaminaRegenPenaltyPercent;
        internal static ButtonConfig RagnarokButton;

        internal static void Register(ConfigFile config, ConfigFile input)
        {
            HitsPerWrathStage = config.BindConfig(
                ItemNames.ConfigSection(ItemNames.BerserkerGreataxeEn, ItemNames.BerserkerGreataxeRu),
                "Hits per Wrath stage",
                2f,
                "Valid enemy hits with the Berserker Greataxe required to gain one Wrath stage. " +
                "Applies to subsequent hits. / Применяется к последующим попаданиям.",
                synced: true, acceptableValues: new AcceptableValueRange<float>(0.25f, 100f));

            WrathDecayInterval = config.BindConfig(
                ItemNames.ConfigSection(ItemNames.BerserkerGreataxeEn, ItemNames.BerserkerGreataxeRu),
                "Wrath decay interval",
                10f,
                "Seconds without a valid Berserker Greataxe hit before one Wrath stage is lost. " +
                "Each lost stage restarts the same interval.",
                synced: true, acceptableValues: new AcceptableValueRange<float>(0.5f, 300f));

            BloodDebtPerFullHealthBar = config.BindConfig(
                ItemNames.ConfigSection(ItemNames.BerserkerGreataxeEn, ItemNames.BerserkerGreataxeRu),
                "Blood Debt per full Max HP lost", 411.765f,
                "Blood Debt granted for losing 100% of Max HP cumulatively to hostile creatures, measured from real post-defense HP loss. " +
                "This Max-HP-normalized value keeps Debt generation consistent across progression.", synced: true,
                acceptableValues: new AcceptableValueRange<float>(0f, 1000000f));

            BloodDebtIncomingGraceSeconds = config.BindConfig(
                ItemNames.ConfigSection(ItemNames.BerserkerGreataxeEn, ItemNames.BerserkerGreataxeRu),
                "Blood Debt incoming damage grace seconds", 10f,
                "Blood Debt cannot decay until this many seconds have passed since the last eligible hostile-creature HP loss.",
                synced: true, acceptableValues: new AcceptableValueRange<float>(0f, 600f));

            BloodDebtOutgoingGraceSeconds = config.BindConfig(
                ItemNames.ConfigSection(ItemNames.BerserkerGreataxeEn, ItemNames.BerserkerGreataxeRu),
                "Blood Debt axe hit grace seconds", 5f,
                "Blood Debt cannot decay until this many seconds have passed since the last valid Berserker Greataxe hit.",
                synced: true, acceptableValues: new AcceptableValueRange<float>(0f, 600f));

            BloodDebtPrimaryHitGain = config.BindConfig(
                ItemNames.ConfigSection(ItemNames.BerserkerGreataxeEn, ItemNames.BerserkerGreataxeRu),
                "Blood Debt per primary axe hit", 7f,
                "Blood Debt granted by each valid primary Berserker Greataxe hit. Reckoning never grants this Debt.",
                synced: true, acceptableValues: new AcceptableValueRange<float>(0f, 1000000f));

            BloodDebtSecondaryHitGain = config.BindConfig(
                ItemNames.ConfigSection(ItemNames.BerserkerGreataxeEn, ItemNames.BerserkerGreataxeRu),
                "Blood Debt per secondary axe hit", 11f,
                "Blood Debt granted by each valid normal secondary Berserker Greataxe hit. Reckoning never grants this Debt.",
                synced: true, acceptableValues: new AcceptableValueRange<float>(0f, 1000000f));

            BloodDebtDecayPercentPerSecond = config.BindConfig(
                ItemNames.ConfigSection(ItemNames.BerserkerGreataxeEn, ItemNames.BerserkerGreataxeRu),
                "Blood Debt decay percent per second", 2.4f,
                "Exponential percent-per-second decrease while out of combat. No maximum Blood Debt limit.",
                synced: true, acceptableValues: new AcceptableValueRange<float>(0f, 100f));

            ReckoningMinimumDebt = config.BindConfig(
                ItemNames.ConfigSection(ItemNames.BerserkerGreataxeEn, ItemNames.BerserkerGreataxeRu),
                "Reckoning minimum Blood Debt", 100f,
                "Minimum Blood Debt required for the secondary greataxe attack to become Reckoning at Wrath II or higher.",
                synced: true, acceptableValues: new AcceptableValueRange<float>(1f, 1000000f));

            ReckoningSpendPercent = config.BindConfig(
                ItemNames.ConfigSection(ItemNames.BerserkerGreataxeEn, ItemNames.BerserkerGreataxeRu),
                "Reckoning Blood Debt spend percent", 50f,
                "Percent of the current Blood Debt consumed on a successful Reckoning hit. Bonus damage is calculated from the amount actually spent.",
                synced: true, acceptableValues: new AcceptableValueRange<float>(0f, 100f));

            ReckoningLifeStealPercent = config.BindConfig(
                ItemNames.ConfigSection(ItemNames.BerserkerGreataxeEn, ItemNames.BerserkerGreataxeRu),
                "Reckoning lifesteal percent", 30f,
                "Percent of the main target's final direct post-defense Reckoning damage restored as health. Overkill damage still counts.",
                synced: true, acceptableValues: new AcceptableValueRange<float>(0f, 100f));

            WrathAttackSpeedPercentPerStage = config.BindConfig(
                ItemNames.ConfigSection(ItemNames.BerserkerGreataxeEn, ItemNames.BerserkerGreataxeRu),
                "Wrath attack speed percent per stage", 10f,
                "Base speed bonus for the non-linear Wrath curve while wielding the Berserker Greataxe. " +
                "Default 10 gives Wrath I–V: +20/+30/+38/+45/+50% attack speed. " +
                "Changing this value scales all five stage bonuses proportionally.",
                synced: true, acceptableValues: new AcceptableValueRange<float>(0f, 100f));

            WrathStaminaCostReductionPercentPerStage = config.BindConfig(
                ItemNames.ConfigSection(ItemNames.BerserkerGreataxeEn, ItemNames.BerserkerGreataxeRu),
                "Wrath stamina cost reduction percent per stage", 5f,
                "Stamina cost reduction for all normal actions per active Wrath stage while wielding the Berserker Greataxe.",
                synced: true, acceptableValues: new AcceptableValueRange<float>(0f, 19f));

            RagnarokMinimumDebt = config.BindConfig(
                ItemNames.ConfigSection(ItemNames.BerserkerGreataxeEn, ItemNames.BerserkerGreataxeRu),
                "Ragnarok minimum Blood Debt", 500f,
                "Minimum Blood Debt required to cast the Ragnarok ability at Wrath V.", synced: true,
                acceptableValues: new AcceptableValueRange<float>(100f, 1000000f));

            RagnarokCastDuration = config.BindConfig(
                ItemNames.ConfigSection(ItemNames.BerserkerGreataxeEn, ItemNames.BerserkerGreataxeRu),
                "Ragnarok cast duration seconds", 1.5f,
                "Cast time before the meteor rain is released.", synced: true,
                acceptableValues: new AcceptableValueRange<float>(0.1f, 10f));

            RagnarokExhaustionDuration = config.BindConfig(
                ItemNames.ConfigSection(ItemNames.BerserkerGreataxeEn, ItemNames.BerserkerGreataxeRu),
                "Ragnarok exhaustion duration seconds", 7f,
                "Duration of Exhaustion after Ragnarok resolves.", synced: true,
                acceptableValues: new AcceptableValueRange<float>(0f, 30f));

            RagnarokExhaustionMovePenaltyPercent = config.BindConfig(
                ItemNames.ConfigSection(ItemNames.BerserkerGreataxeEn, ItemNames.BerserkerGreataxeRu),
                "Ragnarok exhaustion movement penalty percent", 40f,
                "Movement-speed penalty applied by Exhaustion after Ragnarok.", synced: true,
                acceptableValues: new AcceptableValueRange<float>(0f, 90f));

            RagnarokExhaustionAttackPenaltyPercent = config.BindConfig(
                ItemNames.ConfigSection(ItemNames.BerserkerGreataxeEn, ItemNames.BerserkerGreataxeRu),
                "Ragnarok exhaustion attack speed penalty percent", 25f,
                "Attack-speed penalty applied by Exhaustion after Ragnarok. Subtracts from Wrath's attack-speed bonus.",
                synced: true, acceptableValues: new AcceptableValueRange<float>(0f, 90f));

            RagnarokExhaustionStaminaRegenPenaltyPercent = config.BindConfig(
                ItemNames.ConfigSection(ItemNames.BerserkerGreataxeEn, ItemNames.BerserkerGreataxeRu),
                "Ragnarok exhaustion stamina regeneration penalty percent", 40f,
                "Stamina-regeneration penalty applied by Exhaustion after Ragnarok.",
                synced: true, acceptableValues: new AcceptableValueRange<float>(0f, 90f));

            ConfigEntry<KeyCode> ragnarokKey = input.Bind(
                ItemNames.ConfigSection(ItemNames.BerserkerGreataxeEn, ItemNames.BerserkerGreataxeRu),
                "Ragnarok Ability Key", KeyCode.E,
                "Ragnarok key. Requires Wrath V, the configured Blood Debt minimum and Berserker Cloak.");
            RagnarokButton = new ButtonConfig
            {
                Name = BerserkerIds.RagnarokButtonName,
                Config = ragnarokKey,
                HintToken = BerserkerIds.RagnarokAbilityKey,
                BlockOtherInputs = true
            };
            InputManager.Instance.AddButton(HaldorExpansionPlugin.ModGuid, RagnarokButton);
        }

        internal static float HitsPerStageValue => FiniteClamp(HitsPerWrathStage.Value, 0.25f, 100f, 2f);
        internal static float DecayIntervalValue => FiniteClamp(WrathDecayInterval.Value, 0.5f, 300f, 10f);

        internal static float DebtPerFullHealthBarValue =>
            FiniteClamp(BloodDebtPerFullHealthBar.Value, 0f, 1000000f, 411.765f);

        internal static float DebtIncomingGraceValue => FiniteClamp(BloodDebtIncomingGraceSeconds.Value, 0f, 600f, 10f);
        internal static float DebtOutgoingGraceValue => FiniteClamp(BloodDebtOutgoingGraceSeconds.Value, 0f, 600f, 5f);
        internal static float PrimaryDebtGainValue => FiniteClamp(BloodDebtPrimaryHitGain.Value, 0f, 1000000f, 7f);
        internal static float SecondaryDebtGainValue => FiniteClamp(BloodDebtSecondaryHitGain.Value, 0f, 1000000f, 11f);
        internal static float DebtDecayRateValue => FiniteClamp(BloodDebtDecayPercentPerSecond.Value, 0f, 100f, 2.4f);
        internal static float ReckoningMinimumDebtValue => FiniteClamp(ReckoningMinimumDebt.Value, 1f, 1000000f, 100f);
        internal static float ReckoningSpendFraction => FiniteClamp(ReckoningSpendPercent.Value, 0f, 100f, 50f) * 0.01f;

        internal static float ReckoningLifeStealFraction =>
            FiniteClamp(ReckoningLifeStealPercent.Value, 0f, 100f, 30) * 0.01f;

        internal static float WrathAttackSpeedFractionPerStage =>
            FiniteClamp(WrathAttackSpeedPercentPerStage.Value, 0f, 100f, 10f) * 0.01f;

        // Keep the existing config key for compatibility: its value scales the curve.
        // The defaults are 20/30/38/45/50% for Wrath I/II/III/IV/V.
        private static readonly float[] WrathSpeedStageFactors = { 0f, 2f, 3f, 3.8f, 4.5f, 5f };

        internal static float WrathAttackSpeedBonusFraction(int stage) =>
            Mathf.Clamp(WrathSpeedStageFactors[Mathf.Clamp(stage, 0, BerserkerState.MaxWrathStage)] *
                        WrathAttackSpeedFractionPerStage, 0f, 2f);

        internal static float WrathStaminaReductionFractionPerStage =>
            FiniteClamp(WrathStaminaCostReductionPercentPerStage.Value, 0f, 19f, 5f) * 0.01f;

        internal static float RagnarokMinimumDebtValue => FiniteClamp(RagnarokMinimumDebt.Value, 100f, 1000000f, 500f);
        internal static float RagnarokCastDurationValue => FiniteClamp(RagnarokCastDuration.Value, 0.1f, 10f, 1.5f);

        internal static float RagnarokExhaustionDurationValue =>
            FiniteClamp(RagnarokExhaustionDuration.Value, 0f, 30f, 7f);

        internal static float RagnarokExhaustionMovePenaltyFraction =>
            FiniteClamp(RagnarokExhaustionMovePenaltyPercent.Value, 0f, 90f, 40f) * 0.01f;

        internal static float RagnarokExhaustionAttackPenaltyFraction =>
            FiniteClamp(RagnarokExhaustionAttackPenaltyPercent.Value, 0f, 90f, 25f) * 0.01f;

        internal static float RagnarokExhaustionStaminaRegenPenaltyFraction =>
            FiniteClamp(RagnarokExhaustionStaminaRegenPenaltyPercent.Value, 0f, 90f, 40f) * 0.01f;

        private static float FiniteClamp(float value, float min, float max, float fallback)
            => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);
    }
}