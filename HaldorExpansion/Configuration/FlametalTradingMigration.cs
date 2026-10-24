using System;

namespace HaldorExpansion.Configuration
{
    // One-time 1.3.3 -> later-version shop-ID migration. Flametal is the removed
    // Ancient Metal prefab. FlametalNew is the usable Ashlands Flametal prefab.
    // Keep this for players upgrading directly from an older installation.
    internal static class FlametalTradingMigration
    {
        private const string OldId = "Flametal";
        private const string NewId = "FlametalNew";

        internal static bool NeedsUpgrade(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            string group = null;

            foreach (string line in text.Replace("\r", "").Split('\n'))
            {
                // Recognize exactly the parser's shop mapping header, including
                // harmless whitespace and trailing YAML comments.
                string token = line.Split('#')[0].TrimEnd();
                int indent = token.Length - token.TrimStart().Length;
                string entry = token.Trim();
                int colon = entry.IndexOf(':');
                if (colon < 1 || entry.Substring(colon + 1).Trim().Length != 0) continue;
                string key = entry.Substring(0, colon).Trim();

                if (indent == 0 && (key == "shop" || key == "sell"))
                {
                    group = key;
                    continue;
                }

                if (group == "shop" && indent == 2 && key == OldId)
                {
                    return true;
                }
            }

            return false;
        }

        internal static TradingDocument LegacyDefaults(TradingDocument current)
        {
            var legacy = current.Copy();

            TradeSetting row;
            if (!legacy.Shop.TryGetValue(NewId, out row))
                throw new FormatException("Missing FlametalNew catalog entry for migration.");

            legacy.Shop.Remove(NewId);
            row.Id = OldId;
            legacy.Shop.Add(OldId, row);

            return legacy;
        }

        internal static TradingDocument Upgrade(TradingDocument previous, TradingDocument current)
        {
            var upgraded = current.Copy();

            foreach (var pair in previous.Shop)
            {
                string id = pair.Key == OldId ? NewId : pair.Key;

                if (!upgraded.Shop.ContainsKey(id))
                    throw new FormatException("Unknown shop item during Flametal migration: " + pair.Key);

                TradeSetting row = pair.Value.Copy();
                row.Id = id;
                upgraded.Shop[id] = row;
            }

            foreach (var pair in previous.Sell)
            {
                if (!upgraded.Sell.ContainsKey(pair.Key))
                    throw new FormatException("Unknown sell item during Flametal migration: " + pair.Key);

                upgraded.Sell[pair.Key] = pair.Value.Copy();
            }

            return upgraded;
        }
    }
}