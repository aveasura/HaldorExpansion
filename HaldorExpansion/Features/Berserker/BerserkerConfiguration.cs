using BepInEx.Configuration;
using Jotunn.Extensions;
using UnityEngine;

namespace HaldorExpansion.Features.Berserker
{
    internal static class BerserkerConfiguration
    {
        internal static ConfigEntry<float> HitsPerWrathStage;
        internal static ConfigEntry<float> WrathDecayInterval;
        internal static ConfigEntry<float> FrenzyDrainPercentPerSecond;
        internal static ConfigEntry<float> FrenzyMinimumHealthPercent;
        internal static ConfigEntry<float> BloodDebtEnemyMultiplier;
        internal static ConfigEntry<float> BloodDebtFrenzyMultiplier;
        internal static ConfigEntry<float> BloodDebtDecayDelay;
        internal static ConfigEntry<float> BloodDebtDecayPercentPerSecond;
        internal static ConfigEntry<float> BloodDebtBarCycle;
        internal static ConfigEntry<float> BloodDebtHudOffsetX;
        internal static ConfigEntry<float> BloodDebtHudOffsetY;
        internal static ConfigEntry<float> BloodDebtHudWidth;

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

            FrenzyDrainPercentPerSecond = config.BindConfig(
                "Berserker",
                "Frenzy max health drain per second percent",
                0.25f,
                "Percent of maximum health drained per second while Wrath V / Frenzy is active. " +
                "The first prototype intentionally uses a very small value.",
                synced: true);

            BloodDebtEnemyMultiplier = config.BindConfig(
                "Berserker", "Blood Debt from enemy HP loss multiplier", 2f,
                "Real HP lost to hostile creatures (after armor, resistance, and blocks) is multiplied by this value. " +
                "Works only while the Berserker Greataxe is equipped.", synced: true);

            BloodDebtFrenzyMultiplier = config.BindConfig(
                "Berserker", "Blood Debt from Frenzy HP loss multiplier", 0.10f,
                "Small separate coefficient applied to actual HP removed by Frenzy. " +
                "Does not restart the Blood Debt decay timer.", synced: true);

            BloodDebtDecayDelay = config.BindConfig(
                "Berserker", "Blood Debt decay delay seconds", 10f,
                "Seconds without real HP loss from a hostile creature before Blood Debt begins to decay.", synced: true);

            BloodDebtDecayPercentPerSecond = config.BindConfig(
                "Berserker", "Blood Debt decay percent per second", 4f,
                "Exponential percent-per-second decrease while out of combat. No maximum Blood Debt limit.", synced: true);

            BloodDebtBarCycle = config.BindConfig(
                "Berserker", "Blood Debt HUD amount per cycle", 100f,
                "Visual-only threshold for one completed Blood Debt bar cycle. The center label shows x0, x1, x2, etc. Accumulating beyond it is unlimited.", synced: true);

            // Client-only layout tuning relative to the health bar.
            // Keep configurable until the best in-game position is established.
            BloodDebtHudOffsetX = config.BindConfig(
                "Berserker", "Blood Debt HUD offset X from HP", 8f,
                "Horizontal gap from the RIGHT edge of the HP bar. Negative values move left. " +
                "Client-only visual adjustment.", synced: false);

            BloodDebtHudOffsetY = config.BindConfig(
                "Berserker", "Blood Debt HUD offset Y from HP", 0f,
                "Vertical adjustment relative to the BOTTOM edge of the HP bar. Positive moves up. " +
                "Client-only visual adjustment.", synced: false);

            BloodDebtHudWidth = config.BindConfig(
                "Berserker", "Blood Debt HUD bar width", 32f,
                "Fixed width of the Blood Debt meter in UI pixels. Client-only. " +
                "Height always follows the actual health bar automatically.", synced: false);

            FrenzyMinimumHealthPercent = config.BindConfig(
                "Berserker",
                "Frenzy minimum health percent",
                5f,
                "Frenzy self-drain cannot reduce current health below this percent of maximum health.",
                synced: true);
        }

        internal static float HitsPerStageValue => FiniteClamp(HitsPerWrathStage.Value, 0.25f, 100f, 2f);
        internal static float DecayIntervalValue => FiniteClamp(WrathDecayInterval.Value, 0.5f, 300f, 10f);
        internal static float FrenzyDrainFractionPerSecond => FiniteClamp(FrenzyDrainPercentPerSecond.Value, 0f, 20f, 0.25f) * 0.01f;
        internal static float FrenzyMinimumHealthFraction => FiniteClamp(FrenzyMinimumHealthPercent.Value, 0f, 100f, 5f) * 0.01f;

        internal static float EnemyDebtMultiplierValue => FiniteClamp(BloodDebtEnemyMultiplier.Value, 0f, 100f, 2f);
        internal static float FrenzyDebtMultiplierValue => FiniteClamp(BloodDebtFrenzyMultiplier.Value, 0f, 5f, 0.10f);
        internal static float DebtDecayDelayValue => FiniteClamp(BloodDebtDecayDelay.Value, 0.1f, 600f, 10f);
        internal static float DebtDecayRateValue => FiniteClamp(BloodDebtDecayPercentPerSecond.Value, 0f, 100f, 4f);
        internal static float DebtCycleValue => FiniteClamp(BloodDebtBarCycle.Value, 10f, 100000f, 100f);
        internal static float DebtHudOffsetXValue => FiniteClamp(BloodDebtHudOffsetX.Value, -500f, 500f, 8f);
        internal static float DebtHudOffsetYValue => FiniteClamp(BloodDebtHudOffsetY.Value, -500f, 500f, 0f);
        internal static float DebtHudWidthValue => FiniteClamp(BloodDebtHudWidth.Value, 5f, 80f, 32f);

        private static float FiniteClamp(float value, float min, float max, float fallback)
            => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);
    }
}
