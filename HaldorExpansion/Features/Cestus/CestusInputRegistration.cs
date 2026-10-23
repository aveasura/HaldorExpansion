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

            // A saved Facebreaker Key always takes precedence over this default.
            // With E selected, vanilla interactions are handled first by AbilityInteractionPriority.
            // A manually selected Mouse1 still replaces blocking only while aiming.
            string cestusSection = ItemNames.ConfigSection(ItemNames.BoneCrushersEn, ItemNames.BoneCrushersRu);
            FacebreakerKeyConfig = Config.Bind(
                cestusSection,
                "Facebreaker Key", KeyCode.E,
                "Hold to aim Facebreaker, release to pull. Default: E. " +
                "Interactions take priority when using the same key. " +
                "With Mouse1 selected, aiming temporarily replaces blocking while Facebreaker is ready. " +
                "Existing key bindings are preserved.");

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