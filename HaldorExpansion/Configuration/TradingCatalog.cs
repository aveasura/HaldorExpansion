using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace HaldorExpansion.Configuration
{
    internal static class TradingCatalog
    {
        internal static readonly TradingDocument Defaults = new TradingDocument();
        internal static TradingDocument Current;

        private static readonly Dictionary<string, ConfigEntry<int>>
            Prices = new Dictionary<string, ConfigEntry<int>>();

        private static readonly Dictionary<string, ConfigEntry<string>> Keys =
            new Dictionary<string, ConfigEntry<string>>();

        internal static void Shop(string id, string label, string category, ConfigEntry<int> price,
            ConfigEntry<string> key)
        {
            Prices.Add("shop/" + id, price);
            Keys.Add(id, key);
            int stack = 1;
            switch (id)
            {
                case "Iron":
                case "Copper":
                case "Tin":
                case "Silver":
                case "Flametal": stack = 30; break;
                case "Feathers": stack = 100; break;
                default:
                    if (category != "Shop Progression - Custom Items") stack = 0;
                    break;
            }

            Defaults.Shop.Add(id, new TradeSetting
            {
                Id = id, Label = ItemNames.Label(label), Category = category,
                Price = price.Value, RequiredKey = key.Value, Stack = stack
            });
        }

        internal static void Sell(string id, string label, string category, ConfigEntry<int> price)
        {
            Prices.Add("sell/" + id, price);
            Defaults.Sell.Add(id,
                new TradeSetting { Id = id, Label = label, Category = category, Price = price.Value });
        }

        internal static TradingDocument FromEntries()
        {
            var result = Defaults.Copy();
            foreach (var row in result.Shop.Values)
            {
                row.Price = Math.Max(0, Prices["shop/" + row.Id].Value);
                row.RequiredKey = Keys[row.Id].Value.Trim();
            }

            foreach (var row in result.Sell.Values) row.Price = Math.Max(0, Prices["sell/" + row.Id].Value);
            return result;
        }

        internal static void Apply(TradingDocument document)
        {
            Current = document;
            foreach (var row in document.Shop.Values)
            {
                Prices["shop/" + row.Id].Value = row.Price;
                Keys[row.Id].Value = row.RequiredKey;
            }

            foreach (var row in document.Sell.Values) Prices["sell/" + row.Id].Value = row.Price;
            ConfigurationRuntime.RequestTradeRefresh();
        }

        internal static bool Enabled(string id) =>
            Current == null || !Current.Shop.ContainsKey(id) || Current.Shop[id].Enabled;

        internal static int Stack(string id, int fallback, int maxStack)
        {
            TradeSetting row;
            if (Current == null || !Current.Shop.TryGetValue(id, out row)) return fallback;
            return row.Stack == 0
                ? Math.Max(1, maxStack)
                : Math.Min(row.Stack, Math.Max(1, maxStack));
        }
    }
}