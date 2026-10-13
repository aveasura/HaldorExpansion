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

            // Facebreaker takes priority over RMB/block ONLY while aiming with Bone Crushers.
            // During its cooldown the original vanilla block input must remain untouched.
            string cestusSection = ItemNames.ConfigSection(ItemNames.BoneCrushersEn, ItemNames.BoneCrushersRu);
            FacebreakerKeyConfig = Config.Bind(
                cestusSection,
                "Facebreaker Key", KeyCode.Mouse1,
                "Hold to aim Facebreaker, release to pull. Default: Mouse1 (right mouse button). " +
                "While Bone Crushers is equipped and Facebreaker is ready, RMB replaces blocking. " +
                "While on cooldown, RMB blocks normally. Change the binding if preferred.");

            // Existing players have Facebreaker Key = E saved in their BepInEx config.
            // Migrate that old DEFAULT only once, without resetting other custom keys
            // or preventing the player from explicitly choosing E again afterwards.
            ConfigEntry<bool> migrated = Config.Bind(
                cestusSection,
                "Facebreaker RMB Default Migrated", false,
                "Internal one-time migration of the old Facebreaker E default to Mouse1. " +
                "Leave enabled to preserve subsequent manual key changes.");
            if (!migrated.Value)
            {
                if (FacebreakerKeyConfig.Value == KeyCode.E)
                    FacebreakerKeyConfig.Value = KeyCode.Mouse1;
                migrated.Value = true;
            }
            FacebreakerButton = new ButtonConfig
            {
                Name = "CestusFacebreaker",
                Config = FacebreakerKeyConfig,
                // Must stay true for remapped keys like E/Mouse0: this suppresses
                // conflicting vanilla actions, but ONLY when Facebreaker explicitly
                // polls its button during eligible aiming (never on cooldown).
                BlockOtherInputs = true
            };
            InputManager.Instance.AddButton(HaldorExpansionPlugin.ModGuid, FacebreakerButton);
        }
    }
}