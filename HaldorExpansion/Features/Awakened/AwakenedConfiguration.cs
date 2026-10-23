using System;
using BepInEx.Configuration;
using HaldorExpansion.Configuration;
using Jotunn.Extensions;

namespace HaldorExpansion.Features.Awakened
{
    internal static class AwakenedConfiguration
    {
        internal const int DefaultQualityThreshold = 10;
        private const int MinimumQualityThreshold = 1;
        private const int MaximumQualityThreshold = 1000;
        internal static ConfigEntry<int> QualityThreshold;

        // Register against ConfigurationRuntime.Balance so server balance synchronization applies.
        internal static void Register(ConfigFile config)
        {
            QualityThreshold = config.BindConfig(ItemNames.ConfigSection("Awakening", "Пробуждение"),
                "Quality threshold", DefaultQualityThreshold,
                "Minimum item upgrade quality required to awaken any artifact. Applies to equipped items and upgrade previews. " +
                "Changes apply immediately. / Минимальный уровень улучшения для пробуждения всех артефактов. Изменения применяются сразу.",
                synced: true,
                acceptableValues: new AcceptableValueRange<int>(MinimumQualityThreshold, MaximumQualityThreshold));
        }

        internal static int QualityThresholdValue => Math.Max(MinimumQualityThreshold,
            Math.Min(MaximumQualityThreshold,
                QualityThreshold == null ? DefaultQualityThreshold : QualityThreshold.Value));
    }
}