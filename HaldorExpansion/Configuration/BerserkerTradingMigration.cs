using System;
using System.Collections.Generic;
using HaldorExpansion.Features.Berserker;

namespace HaldorExpansion.Configuration
{
    // Converts unpublished 1.3.0 test shop IDs without losing customized prices or unlocks.
    // Only used at startup, after verifying the previous file against its schema fingerprint.
    internal static class BerserkerTradingMigration
    {
        private static readonly Dictionary<string, string> LegacyIds = new Dictionary<string, string>
        {
            { "BattleaxeBerserker", BerserkerIds.AxePrefab },
            { "CapeBerserker", BerserkerIds.CloakPrefab },
            { "HelmetBerserker", BerserkerIds.HelmetPrefab }
        };

        internal static bool NeedsUpgrade(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (string line in text.Replace("\r", "").Split('\n'))
            {
                if (!line.StartsWith("  ", StringComparison.Ordinal) ||
                    line.StartsWith("    ", StringComparison.Ordinal)) continue;
                string id = line.Trim();
                if (id.EndsWith(":", StringComparison.Ordinal) &&
                    LegacyIds.ContainsKey(id.Substring(0, id.Length - 1))) return true;
            }

            return false;
        }

        internal static TradingDocument LegacyDefaults(TradingDocument current)
        {
            var legacy = current.Copy();
            foreach (var pair in LegacyIds)
            {
                TradeSetting row;
                if (!legacy.Shop.TryGetValue(pair.Value, out row)) continue;
                legacy.Shop.Remove(pair.Value);
                row.Id = pair.Key;
                legacy.Shop.Add(pair.Key, row);
            }

            return legacy;
        }

        internal static TradingDocument Upgrade(TradingDocument previous, TradingDocument current)
        {
            var upgraded = current.Copy();
            foreach (var pair in previous.Shop)
            {
                string id;
                if (!LegacyIds.TryGetValue(pair.Key, out id)) id = pair.Key;
                if (!upgraded.Shop.ContainsKey(id))
                    throw new FormatException("Unknown trading item during Berserker migration: " + pair.Key);
                TradeSetting row = pair.Value.Copy();
                row.Id = id;
                upgraded.Shop[id] = row;
            }

            foreach (var pair in previous.Sell)
                upgraded.Sell[pair.Key] = pair.Value.Copy();
            return upgraded;
        }
    }
}
