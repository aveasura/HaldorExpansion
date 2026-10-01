using BepInEx.Configuration;
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

        internal static void Register(ConfigFile config)
        {
            ChargeDamage = config.BindConfig("Hel Oath", "Damage for full charge", 650f,
                "Base actual bow damage required for a full charge. The real requirement scales with world boss progression: " +
                "pre-Eikthyr x0.65, Eikthyr x0.80, Elder x1.00, Bonemass x1.25, Moder x1.55, Yagluth x1.85, Queen x2.20, Fader x2.60. " +
                "Attributed damage over time counts; the special shot never charges the ability.", synced: true);
            Radius = config.BindConfig("Hel Oath", "Explosion radius", 9f,
                "Hel's Embrace explosion radius in metres. Damage does not fall off with distance.", synced: true);
            EmbraceCoefficient = config.BindConfig("Hel Oath", "Embrace coefficient", 0.85f,
                "HelPower multiplier for the normal Hel's Embrace splash (80% Lightning / 20% Frost).", synced: true);
            TouchVEmbraceCoefficient = config.BindConfig("Hel Oath", "Touch V Embrace coefficient", 2.15f,
                "HelPower multiplier for Hel's Embrace when activated at Hel's Touch V.", synced: true);
            TouchStackInterval = config.BindConfig("Hel Oath", "Touch stack interval", 3.0f,
                "Seconds without receiving a combat hit required to gain one Hel's Touch stack after Hel's Oath has damaged an enemy.", synced: true);
            var key = config.Bind("Hel Oath", "Ability Key", KeyCode.Mouse2,
                "Activate Hel's Embrace at full charge. Activation is instant. Lightning on the bow marks the empowered next arrow.");
            Button = new ButtonConfig { Name = "HE_HelEmbrace", Config = key,
                HintToken = "$he_hel_ability", BlockOtherInputs = true };
            InputManager.Instance.AddButton(HaldorExpansionPlugin.ModGuid, Button);
        }

        private static float FiniteClamp(float value, float min, float max, float fallback)
            => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);

        internal static float BaseRequiredDamage => FiniteClamp(ChargeDamage.Value, 1f, 1000000f, 650f);
        internal static float RequiredDamage => HelOathWorldProgression.ScaleRequiredDamage(BaseRequiredDamage);
        internal static float ExplosionRadius => FiniteClamp(Radius.Value, 0.5f, 50f, 9f);
        internal static float NormalEmbraceCoefficient => FiniteClamp(EmbraceCoefficient.Value, 0f, 10f, 0.85f);
        internal static float TouchVEmbraceCoefficientValue => FiniteClamp(TouchVEmbraceCoefficient.Value, 0f, 10f, 2.15f);
        internal static float TouchStackIntervalValue => FiniteClamp(TouchStackInterval.Value, 0.25f, 10f, 3.0f);
    }
}
