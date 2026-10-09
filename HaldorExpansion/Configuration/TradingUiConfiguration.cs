using BepInEx.Configuration;

namespace HaldorExpansion.Configuration
{
    internal static class TradingUiConfiguration
    {
        private static ConfigEntry<bool> enabled;
        internal static bool Enabled => enabled != null && enabled.Value;

        internal static void Register(ConfigFile config)
        {
            enabled = config.Bind("Trading UI", "Enable Haldor tabs", true,
                "Show All, Equipment, Resources and Other categories in Haldor's shop. " +
                "Disable to restore the original shop interface. " +
                "Вкладки категорий у Хальдора. Выключение возвращает обычный интерфейс, сохраняя товары и цены.");
        }
    }
}
