using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace HaldorExpansion.Features.Cestus
{
    internal static class CestusInputRegistration
    {

        internal static ConfigEntry<KeyCode> CestusAbilityKeyConfig;
        internal static ButtonConfig CestusAbilityButton;

        internal static void RegisterCestusInput(BepInEx.Configuration.ConfigFile Config)
        {
            CestusAbilityKeyConfig = Config.Bind(
                "Cestus",
                "Ability Key",
                KeyCode.Mouse2,
                "Key for activating the Cestus ability.\n" +
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
        }
    }
}
