using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace HaldorExpansion.Configuration
{
    // ConfigurationManager views over trading.yml. These entries are in-memory only:
    // trading.yml remains the only persisted trading configuration.
    internal sealed class TradingEditorSettings
    {
        private sealed class Field
        {
            internal string Group, Id, Name, DisplayLabel;
            internal ConfigEntryBase Entry;
        }

        private readonly ConfigurationRuntime runtime;
        private readonly List<Field> fields = new List<Field>();
        private readonly Dictionary<ConfigEntryBase, Field> byEntry = new Dictionary<ConfigEntryBase, Field>();
        private TradingDocument displayed;
        private bool refreshing;

        internal IEnumerable<ConfigEntryBase> Entries
        {
            get
            {
                foreach (Field field in fields) yield return field.Entry;
            }
        }

        internal TradingEditorSettings(ConfigurationRuntime owner, ConfigFile config, TradingDocument document)
        {
            runtime = owner;
            foreach (TradeSetting row in document.Shop.Values)
            {
                string section = "Trading - Buy - " + row.Category.Replace("Shop Progression - ", "");
                Add(section, row.Id + " Price", row.Price,
                    "Price for the WHOLE stack / Цена за всю пачку. " + row.Label,
                    "shop", row.Id, "price", new AcceptableValueRange<int>(0, int.MaxValue), config, row.Label);
                Add(section, row.Id + " Enabled", row.Enabled, "Show item in Haldor's shop. " + row.Label,
                    "shop", row.Id, "enabled", null, config, row.Label);
                Add(section, row.Id + " Stack", row.Stack,
                    "0 = item's maximum stack, otherwise 1..10000 (capped to the prefab maximum). " + row.Label,
                    "shop", row.Id, "stack", new AcceptableValueRange<int>(0, 10000), config, row.Label);
                Add(section, row.Id + " RequiredGlobalKey", row.RequiredKey,
                    "World global key required for this item. Empty = always available. " + row.Label,
                    "shop", row.Id, "requiredGlobalKey", null, config, row.Label);
            }

            foreach (TradeSetting row in document.Sell.Values)
                Add("Trading - Sell - " + row.Category, row.Id + " Price", row.Price,
                    "Coins Haldor pays for this item; 0 disables selling. " + row.Label,
                    "sell", row.Id, "price", new AcceptableValueRange<int>(0, int.MaxValue), config);

            displayed = document;
            config.SettingChanged += OnSettingChanged;
        }

        private void Add<T>(string section, string key, T value, string description, string group,
            string id, string name, AcceptableValueBase acceptable, ConfigFile config, string label = null)
        {
            ConfigEntry<T> entry = config.Bind(section, key, value, new ConfigDescription(description, acceptable));
            var field = new Field
            {
                Group = group, Id = id, Name = name, Entry = entry,
                DisplayLabel = section == "Trading - Buy - Custom Items" ? label : null
            };
            fields.Add(field);
            byEntry.Add(entry, field);
        }

        // Only rename custom-item fields in F1. ConfigEntry keys and YAML item IDs remain stable.
        // Names are resolved when ConfigurationManager builds its window, after game localization loads.
        internal string GetDisplayName(ConfigEntryBase entry, bool russian)
        {
            Field field;
            if (!byEntry.TryGetValue(entry, out field) || string.IsNullOrEmpty(field.DisplayLabel)) return null;

            string label = field.DisplayLabel;
            int separator = label.IndexOf(" / ", StringComparison.Ordinal);
            if (separator >= 0)
                label = russian ? label.Substring(0, separator) : label.Substring(separator + 3);

            string suffix;
            switch (field.Name)
            {
                case "price": suffix = russian ? "Цена" : "Price"; break;
                case "enabled": suffix = russian ? "Доступен" : "Enabled"; break;
                case "stack": suffix = russian ? "Количество" : "Stack"; break;
                case "requiredGlobalKey": suffix = russian ? "Условие открытия" : "Required Key"; break;
                default: return null;
            }

            return label + " — " + suffix;
        }

        private void OnSettingChanged(object sender, SettingChangedEventArgs e)
        {
            Field field;
            if (refreshing || !byEntry.TryGetValue(e.ChangedSetting, out field)) return;
            if (!runtime.TryEditTrading(field.Group, field.Id, field.Name, field.Entry.BoxedValue))
                Refresh(TradingCatalog.Current, true);
        }

        internal void Refresh(TradingDocument document, bool force = false)
        {
            if (document == null || (!force && ReferenceEquals(displayed, document))) return;
            refreshing = true;
            try
            {
                foreach (Field field in fields)
                {
                    TradeSetting row = field.Group == "shop" ? document.Shop[field.Id] : document.Sell[field.Id];
                    object value;
                    switch (field.Name)
                    {
                        case "price": value = row.Price; break;
                        case "enabled": value = row.Enabled; break;
                        case "stack": value = row.Stack; break;
                        default: value = row.RequiredKey; break;
                    }

                    if (!Equals(field.Entry.BoxedValue, value)) field.Entry.BoxedValue = value;
                }

                displayed = document;
            }
            finally
            {
                refreshing = false;
            }
        }
    }
}