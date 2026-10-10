using BepInEx.Configuration;
using HaldorExpansion.Configuration;
using Jotunn.Configs;
using Jotunn.Extensions;
using Jotunn.Managers;
using UnityEngine;

namespace HaldorExpansion.Features.HelOath
{
    internal static class HelOathConfiguration
    {
        internal static ConfigEntry<float> ChargeDamage;
        internal static ConfigEntry<float> Radius;
        internal static ConfigEntry<float> EmbraceCoefficient;
        internal static ConfigEntry<float> TouchVEmbraceCoefficient;
        internal static ConfigEntry<float> TouchStackInterval;
        internal static ButtonConfig Button;

        internal static void Register(ConfigFile config, ConfigFile input)
        {
            ChargeDamage = config.BindConfig(ItemNames.ConfigSection(ItemNames.HelOathEn, ItemNames.HelOathRu),
                "Damage for full charge", 650f,
                "Base outgoing bow damage required for a full charge. The real requirement scales with world boss progression: " +
                "the world progression curve defined by the mod. " +
                "Normal shots are credited from their pre-defense damage payload, so enemy resistance/armor does not slow charge. " +
                "DoT channels are credited once with the originating hit and are not counted again on later ticks; the special shot never charges the ability.",
                synced: true, acceptableValues: new AcceptableValueRange<float>(1f, 1000000f));

            Radius = config.BindConfig(ItemNames.ConfigSection(ItemNames.HelOathEn, ItemNames.HelOathRu),
                "Explosion radius", 9f,
                "Hel's Embrace explosion radius in metres. Damage does not fall off with distance.", synced: true,
                acceptableValues: new AcceptableValueRange<float>(0.5f, 50f));

            EmbraceCoefficient = config.BindConfig(ItemNames.ConfigSection(ItemNames.HelOathEn, ItemNames.HelOathRu),
                "Embrace coefficient", 0.85f,
                "HelPower multiplier for the normal Hel's Embrace splash (80% Lightning / 20% Frost).", synced: true,
                acceptableValues: new AcceptableValueRange<float>(0f, 10f));

            TouchVEmbraceCoefficient = config.BindConfig(
                ItemNames.ConfigSection(ItemNames.HelOathEn, ItemNames.HelOathRu), "Touch V Embrace coefficient", 2.15f,
                "HelPower multiplier for Hel's Embrace when activated at Hel's Touch V.", synced: true,
                acceptableValues: new AcceptableValueRange<float>(0f, 10f));

            TouchStackInterval = config.BindConfig(ItemNames.ConfigSection(ItemNames.HelOathEn, ItemNames.HelOathRu),
                "Touch stack interval", 3.0f,
                "Seconds without receiving a combat hit required to gain one Hel's Touch stack after Hel's Oath has damaged an enemy.",
                synced: true, acceptableValues: new AcceptableValueRange<float>(0.25f, 10f));
            var key = input.Bind(ItemNames.ConfigSection(ItemNames.HelOathEn, ItemNames.HelOathRu), "Ability Key",
                KeyCode.Mouse2,
                "Activate Hel's Embrace at full charge. Activation is instant. Lightning on the bow marks the empowered next arrow.");
            Button = new ButtonConfig
            {
                Name = "HE_HelEmbrace", Config = key,
                HintToken = HelOathIds.AbilityKey, BlockOtherInputs = true
            };
            InputManager.Instance.AddButton(HaldorExpansionPlugin.ModGuid, Button);
        }

        private static float FiniteClamp(float value, float min, float max, float fallback)
            => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);

        internal static float BaseRequiredDamage => FiniteClamp(ChargeDamage.Value, 1f, 1000000f, 650f);
        internal static float RequiredDamage => HelOathWorldProgression.ScaleRequiredDamage(BaseRequiredDamage);
        internal static float ExplosionRadius => FiniteClamp(Radius.Value, 0.5f, 50f, 9f);
        internal static float NormalEmbraceCoefficient => FiniteClamp(EmbraceCoefficient.Value, 0f, 10f, 0.85f);

        internal static float TouchVEmbraceCoefficientValue =>
            FiniteClamp(TouchVEmbraceCoefficient.Value, 0f, 10f, 2.15f);

        internal static float TouchStackIntervalValue => FiniteClamp(TouchStackInterval.Value, 0.25f, 10f, 3.0f);
    }
}