using System;
using BepInEx.Configuration;
using HaldorExpansion.Trading;

namespace HaldorExpansion.Configuration
{
    internal static class TradingUiConfiguration
    {
        private static ConfigEntry<bool> enabled, hideItems;

        private static readonly ConfigEntry<bool>[] categories =
            new ConfigEntry<bool>[Enum.GetValues(typeof(ShopCategory)).Length];

        internal static bool Enabled => enabled != null && enabled.Value;
        internal static bool HideItems => hideItems != null && hideItems.Value;

        internal static bool IsCategoryEnabled(ShopCategory category)
        {
            int index = (int)category;
            return category == ShopCategory.All || index < 0 || index >= categories.Length ||
                   categories[index] == null || categories[index].Value;
        }

        // Poll on Unity's update, not from configuration callback threads.
        internal static int State
        {
            get
            {
                int state = (Enabled ? 1 : 0) | (HideItems ? 2 : 0);
                for (int i = 1; i < categories.Length; i++)
                    if (IsCategoryEnabled((ShopCategory)i))
                        state |= 1 << (i + 1);
                return state;
            }
        }

        internal static void Register(ConfigFile config)
        {
            enabled = config.Bind("Trading UI", "Enable Haldor tabs", true,
                "Show category tabs in Haldor's shop. Disabling only changes the interface; category sales restrictions remain active. " +
                "Показывать вкладки у Хальдора. Выключение меняет только интерфейс; запреты продажи категорий продолжают действовать.");
            hideItems = config.Bind("Trading UI", "Hide items from disabled tabs", false,
                "Hides items from disabled shop categories, including Haldor's original vanilla items.\n" +
                "This setting works even when \"Enable Haldor Tabs\" is disabled.\n" +
                "If all categories are disabled, Haldor's shop will be completely empty.");
            BindCategory(config, ShopCategory.Equipment, "Снаряжение / Equipment");
            BindCategory(config, ShopCategory.Wood, "Древесина / Wood");
            BindCategory(config, ShopCategory.Metals, "Руды и металлы / Ores and metals");
            BindCategory(config, ShopCategory.Resources, "Ресурсы / Resources");
            BindCategory(config, ShopCategory.Other, "Прочее / Other");
            BindCategory(config, ShopCategory.Idols, "Идолы / Idols");
        }

        private static void BindCategory(ConfigFile config, ShopCategory category, string label)
        {
            categories[(int)category] = config.Bind("Trading UI", "Show " + category + " tab", true,
                label +
                ". Show this tab. Its position in the category order is preserved. Items remain in ALL unless 'Hide items from disabled tabs' is enabled. " +
                "Показывать вкладку. Её место в порядке категорий сохраняется. Товары остаются во ВСЕ, если не включено 'Hide items from disabled tabs' (убирать товары отключённых вкладок).");
        }
    }
}