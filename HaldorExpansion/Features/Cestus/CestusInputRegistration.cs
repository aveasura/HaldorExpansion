using BepInEx.Configuration;
using HaldorExpansion.Configuration;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace HaldorExpansion.Features.Cestus
{
    internal static class CestusInputRegistration
    {
        internal static ConfigEntry<KeyCode> CestusAbilityKeyConfig;
        internal static ButtonConfig CestusAbilityButton;
        internal static ConfigEntry<KeyCode> FacebreakerKeyConfig;
        internal static ButtonConfig FacebreakerButton;

        internal static void RegisterCestusInput(ConfigFile Config)
        {
            CestusAbilityKeyConfig = Config.Bind(
                ItemNames.ConfigSection(ItemNames.BoneCrushersEn, ItemNames.BoneCrushersRu),
                "Ability Key",
                KeyCode.Mouse2,
                "Key for activating Bone Crushers / Костоломы.\n" +
                "Mouse0 = Left Mouse Button.\n" +
                "Mouse1 = Right Mouse Button.\n" +
                "Mouse2 = Middle Mouse Button.\n" +
                "Uses UnityEngine.KeyCode names. " +
                "Examples: Mouse0-Mouse6, A-Z, Alpha0-Alpha9, F1-F15, Space, Tab, LeftShift, RightShift, LeftControl, RightControl, LeftAlt, RightAlt.");

            CestusAbilityButton = new ButtonConfig
            {
                Name = "CestusAbility",
                Config = CestusAbilityKeyConfig,
                HintToken = "$cestus_ability",
                BlockOtherInputs = true
            };

            InputManager.Instance.AddButton(HaldorExpansionPlugin.ModGuid, CestusAbilityButton);

            // Poll this binding only while Bone Crushers is equipped and the ability is ready.
            // Jotunn then suppresses the vanilla Use (E) for those frames, not globally.
            FacebreakerKeyConfig = Config.Bind(
                ItemNames.ConfigSection(ItemNames.BoneCrushersEn, ItemNames.BoneCrushersRu),
                "Facebreaker Key", KeyCode.E,
                "Hold to aim Facebreaker, release to pull. While Bone Crushers is equipped and Facebreaker is ready, this key takes priority over vanilla interaction. Change the binding if preferred.");
            FacebreakerButton = new ButtonConfig
            {
                Name = "CestusFacebreaker",
                Config = FacebreakerKeyConfig,
                BlockOtherInputs = true
            };
            InputManager.Instance.AddButton(HaldorExpansionPlugin.ModGuid, FacebreakerButton);
        }
    }
}