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

        private static float FiniteClamp(float value, float min, float max, float fallback)
            => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);
    }
}
