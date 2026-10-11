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
        internal static ConfigEntry<float> WrathCloakRunSpeedBonusAtMaxPercent;
        internal static ConfigEntry<float> CloakIncomingDamageBonusPercent;
        internal static ConfigEntry<float> CloakBloodDebtGainBonusPercent;
        internal static ConfigEntry<float> RagnarokMinimumDebt;
        internal static ConfigEntry<float> RagnarokCastDuration;
        internal static ConfigEntry<float> RagnarokExhaustionDuration;
        internal static ConfigEntry<float> RagnarokExhaustionMovePenaltyPercent;
        internal static ConfigEntry<float> RagnarokExhaustionAttackPenaltyPercent;
        internal static ConfigEntry<float> RagnarokExhaustionStaminaRegenPenaltyPercent;
        internal static ButtonConfig RagnarokButton;
        internal static ConfigEntry<bool> BloodDebtHudEnabled;
        internal static ConfigEntry<int> BloodDebtHudOffsetX;
        internal static ConfigEntry<int> BloodDebtHudOffsetY;
        internal static ConfigEntry<float> BloodDebtHudScale;
        internal static ConfigEntry<bool> BloodDebtHudMatchHealthSize;
        internal static ConfigEntry<float> BloodDebtHudCustomWidth;
        internal static ConfigEntry<float> BloodDebtHudCustomHeight;

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
                "Blood Debt per full Max HP lost", 320f,
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
                "Blood Debt per primary axe hit", 6f,
                "Blood Debt granted by each valid primary Berserker Greataxe hit. Reckoning never grants this Debt.",
                synced: true, acceptableValues: new AcceptableValueRange<float>(0f, 1000000f));

            BloodDebtSecondaryHitGain = config.BindConfig(
                ItemNames.ConfigSection(ItemNames.BerserkerGreataxeEn, ItemNames.BerserkerGreataxeRu),
                "Blood Debt per secondary axe hit", 15f,
                "Blood Debt granted by each valid normal secondary Berserker Greataxe hit. Reckoning never grants this Debt.",
                synced: true, acceptableValues: new AcceptableValueRange<float>(0f, 1000000f));

            BloodDebtDecayPercentPerSecond = config.BindConfig(
                ItemNames.ConfigSection(ItemNames.BerserkerGreataxeEn, ItemNames.BerserkerGreataxeRu),
                "Blood Debt decay percent per second", 1.6f,
                "Exponential percent-per-second decrease while out of combat. No maximum Blood Debt limit.",
                synced: true, acceptableValues: new AcceptableValueRange<float>(0f, 100f));

            ReckoningMinimumDebt = config.BindConfig(
                ItemNames.ConfigSection(ItemNames.BerserkerGreataxeEn, ItemNames.BerserkerGreataxeRu),
                "Reckoning minimum Blood Debt", 100f,
                "Minimum Blood Debt required for the secondary greataxe attack to become Reckoning at Wrath II or higher.",
                synced: true, acceptableValues: new AcceptableValueRange<float>(1f, 1000000f));

            ReckoningSpendPercent = config.BindConfig(
                ItemNames.ConfigSection(ItemNames.BerserkerGreataxeEn, ItemNames.BerserkerGreataxeRu),
                "Reckoning Blood Debt spend percent", 35f,
                "Percent of the current Blood Debt consumed on a successful Reckoning hit. Bonus damage is calculated from the amount actually spent.",
                synced: true, acceptableValues: new AcceptableValueRange<float>(0f, 100f));

            ReckoningLifeStealPercent = config.BindConfig(
                ItemNames.ConfigSection(ItemNames.BerserkerGreataxeEn, ItemNames.BerserkerGreataxeRu),
                "Reckoning lifesteal percent", 40f,
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
                "Base of the non-linear stamina-cost reduction curve while wielding the Berserker Greataxe. " +
                "Default 5 gives Wrath I-V: -14/-21/-26.6/-31.5/-35%. " +
                "Changing this value scales all stages proportionally (max 90%).",
                synced: true, acceptableValues: new AcceptableValueRange<float>(0f, 19f));

            WrathCloakRunSpeedBonusAtMaxPercent = config.BindConfig(
                ItemNames.ConfigSection(ItemNames.BerserkerCloakEn, ItemNames.BerserkerCloakRu),
                "Wrath V running speed bonus percent", 15f,
                "Non-linear running (sprint) speed bonus with the Berserker Cloak and Greataxe equipped. " +
                "The configured bonus is reached at Wrath V; does not modify walking or jogging speed.",
                synced: true, acceptableValues: new AcceptableValueRange<float>(0f, 100f));

            CloakIncomingDamageBonusPercent = config.BindConfig(
                ItemNames.ConfigSection(ItemNames.BerserkerCloakEn, ItemNames.BerserkerCloakRu),
                "Incoming damage increase percent", 30f,
                "Extra incoming damage taken while wearing the Berserker Cloak. " +
                "Default 30 means 130% of incoming damage, after armor and resistances.",
                synced: true, acceptableValues: new AcceptableValueRange<float>(0f, 500f));

            CloakBloodDebtGainBonusPercent = config.BindConfig(
                ItemNames.ConfigSection(ItemNames.BerserkerCloakEn, ItemNames.BerserkerCloakRu),
                "Blood Debt generation bonus percent", 50f,
                "Extra Blood Debt generation while wearing the Berserker Cloak. " +
                "Default 50 means 150% Blood Debt gain.",
                synced: true, acceptableValues: new AcceptableValueRange<float>(0f, 500f));

            RagnarokMinimumDebt = config.BindConfig(
                ItemNames.ConfigSection(ItemNames.BerserkerGreataxeEn, ItemNames.BerserkerGreataxeRu),
                "Ragnarok minimum Blood Debt", 600f,
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

            // These are client-only visual preferences in general.cfg. Plain Bind
            // does not mark them IsAdminOnly, so Jotunn never server-syncs them.
            BloodDebtHudEnabled = input.Bind("Blood Debt HUD / HUD Кровавого долга", "Enable HUD", true,
                "Show the Blood Debt gauge. Local setting: each player can hide it without disabling the mechanic. / " +
                "Показывать шкалу Кровавого долга. Личная настройка, не влияет на механику.");

            BloodDebtHudOffsetX = input.Bind("Blood Debt HUD / HUD Кровавого долга", "Position X", -88,
                new ConfigDescription(
                    "Horizontal offset in UI units from the LEFT edge of the vanilla health bar. " +
                    "Negative moves left; default -88 positions the gauge left of the food icons. " +
                    "Личный сдвиг по горизонтали от левого края полосы здоровья (отрицательный — влево).",
                    new AcceptableValueRange<int>(-600, 600)));

            BloodDebtHudOffsetY = input.Bind("Blood Debt HUD / HUD Кровавого долга", "Position Y", 0,
                new ConfigDescription(
                    "Vertical offset in UI units from the BOTTOM of the vanilla health bar. " +
                    "Positive moves up. / Личный сдвиг по вертикали от низа полосы здоровья (плюс — вверх).",
                    new AcceptableValueRange<int>(-400, 400)));

            BloodDebtHudScale = input.Bind("Blood Debt HUD / HUD Кровавого долга", "Scale", 1f,
                new ConfigDescription(
                    "Scale multiplier for the gauge in both auto-size and custom-size modes (default 1.0). " +
                    "Масштаб шкалы в обоих режимах размера (по умолчанию 1.0).",
                    new AcceptableValueRange<float>(0.5f, 2f)));

            BloodDebtHudMatchHealthSize = input.Bind("Blood Debt HUD / HUD Кровавого долга", "Match HP Bar Size", true,
                "Automatically match the width and height of the vanilla HP bar (default). " +
                "Disable to use Custom Width and Custom Height. Local-only setting. / " +
                "Подстраивать ширину и высоту под колонку здоровья (по умолчанию включено). " +
                "Выключите для ручной настройки ширины и высоты; не синхронизируется с сервером.");

            BloodDebtHudCustomWidth = input.Bind("Blood Debt HUD / HUD Кровавого долга", "Custom Width", 32f,
                new ConfigDescription(
                    "Manual Blood Debt bar width in UI units. Used only when Match HP Bar Size is false. / " +
                    "Ширина шкалы в единицах интерфейса; работает при выключенной привязке к HP.",
                    new AcceptableValueRange<float>(8f, 800f)));

            BloodDebtHudCustomHeight = input.Bind("Blood Debt HUD / HUD Кровавого долга", "Custom Height", 220f,
                new ConfigDescription(
                    "Manual Blood Debt bar height in UI units. Used only when Match HP Bar Size is false. / " +
                    "Высота шкалы в единицах интерфейса; работает при выключенной привязке к HP.",
                    new AcceptableValueRange<float>(16f, 1600f)));

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

        internal static bool BloodDebtHudVisible => BloodDebtHudEnabled == null || BloodDebtHudEnabled.Value;
        internal static float BloodDebtHudX => BloodDebtHudOffsetX != null ? BloodDebtHudOffsetX.Value : -88f;
        internal static float BloodDebtHudY => BloodDebtHudOffsetY != null ? BloodDebtHudOffsetY.Value : 0f;

        internal static float BloodDebtHudScaleValue => BloodDebtHudScale != null
            ? FiniteClamp(BloodDebtHudScale.Value, 0.5f, 2f, 1f)
            : 1f;

        internal static bool BloodDebtHudAutoSize =>
            BloodDebtHudMatchHealthSize == null || BloodDebtHudMatchHealthSize.Value;

        internal static float BloodDebtHudWidth => BloodDebtHudCustomWidth != null
            ? FiniteClamp(BloodDebtHudCustomWidth.Value, 8f, 800f, 32f)
            : 32f;

        internal static float BloodDebtHudHeight => BloodDebtHudCustomHeight != null
            ? FiniteClamp(BloodDebtHudCustomHeight.Value, 16f, 1600f, 220f)
            : 220f;

        internal static float HitsPerStageValue => FiniteClamp(HitsPerWrathStage.Value, 0.25f, 100f, 2f);
        internal static float DecayIntervalValue => FiniteClamp(WrathDecayInterval.Value, 0.5f, 300f, 10f);

        internal static float DebtPerFullHealthBarValue =>
            FiniteClamp(BloodDebtPerFullHealthBar.Value, 0f, 1000000f, 320f);

        internal static float DebtIncomingGraceValue => FiniteClamp(BloodDebtIncomingGraceSeconds.Value, 0f, 600f, 10f);
        internal static float DebtOutgoingGraceValue => FiniteClamp(BloodDebtOutgoingGraceSeconds.Value, 0f, 600f, 5f);
        internal static float PrimaryDebtGainValue => FiniteClamp(BloodDebtPrimaryHitGain.Value, 0f, 1000000f, 6f);
        internal static float SecondaryDebtGainValue => FiniteClamp(BloodDebtSecondaryHitGain.Value, 0f, 1000000f, 15f);
        internal static float DebtDecayRateValue => FiniteClamp(BloodDebtDecayPercentPerSecond.Value, 0f, 100f, 1.6f);
        internal static float ReckoningMinimumDebtValue => FiniteClamp(ReckoningMinimumDebt.Value, 1f, 1000000f, 100f);
        internal static float ReckoningSpendFraction => FiniteClamp(ReckoningSpendPercent.Value, 0f, 100f, 35f) * 0.01f;

        internal static float ReckoningLifeStealFraction =>
            FiniteClamp(ReckoningLifeStealPercent.Value, 0f, 100f, 40) * 0.01f;

        internal static float WrathAttackSpeedFractionPerStage =>
            FiniteClamp(WrathAttackSpeedPercentPerStage.Value, 0f, 100f, 10f) * 0.01f;

        // Keep the existing config key for compatibility: its value scales the curve.
        // The defaults are 20/30/38/45/50% for Wrath I/II/III/IV/V.
        private static readonly float[] WrathSpeedStageFactors = { 0f, 2f, 3f, 3.8f, 4.5f, 5f };

        internal static float WrathAttackSpeedBonusFraction(int stage) =>
            Mathf.Clamp(WrathSpeedStageFactors[Mathf.Clamp(stage, 0, BerserkerState.MaxWrathStage)] *
                        WrathAttackSpeedFractionPerStage, 0f, 2f);

        // Equipment shares the attack-speed curve: 40/60/76/90/100% of its maximum.
        // The first stage is immediately useful; later stages give smaller gains.
        internal static float WrathProgressFraction(int stage) =>
            WrathSpeedStageFactors[Mathf.Clamp(stage, 0, BerserkerState.MaxWrathStage)] / 5f;

        internal static float WrathStaminaReductionFraction(int stage) =>
            Mathf.Clamp(WrathProgressFraction(stage) * 7f *
                        FiniteClamp(WrathStaminaCostReductionPercentPerStage.Value, 0f, 19f, 5f) * 0.01f, 0f, 0.9f);

        internal static float WrathCloakRunSpeedBonusFraction(int stage) =>
            WrathProgressFraction(stage) *
            FiniteClamp(WrathCloakRunSpeedBonusAtMaxPercent.Value, 0f, 100f, 15f) * 0.01f;

        internal static float CloakIncomingDamageBonusFraction =>
            FiniteClamp(CloakIncomingDamageBonusPercent.Value, 0f, 500f, 30f) * 0.01f;

        internal static float CloakBloodDebtGainBonusFraction =>
            FiniteClamp(CloakBloodDebtGainBonusPercent.Value, 0f, 500f, 50f) * 0.01f;

        internal static float RagnarokMinimumDebtValue => FiniteClamp(RagnarokMinimumDebt.Value, 100f, 1000000f, 600f);
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