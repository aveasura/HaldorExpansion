using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HaldorExpansion.Configuration
{
    internal sealed class TradeSetting
    {
        internal string Id, Label, Category, RequiredKey = "";
        internal int Price, Stack = 1;
        internal bool Enabled = true;
        internal TradeSetting Copy() => (TradeSetting)MemberwiseClone();
    }

    // Deliberately restricted YAML mapping schema: no aliases, tags or executable values.
    internal sealed class TradingDocument
    {
        internal readonly Dictionary<string, TradeSetting> Shop = new Dictionary<string, TradeSetting>();
        internal readonly Dictionary<string, TradeSetting> Sell = new Dictionary<string, TradeSetting>();

        internal TradingDocument Copy()
        {
            var copy = new TradingDocument();
            foreach (var pair in Shop) copy.Shop.Add(pair.Key, pair.Value.Copy());
            foreach (var pair in Sell) copy.Sell.Add(pair.Key, pair.Value.Copy());
            return copy;
        }

        internal static TradingDocument Parse(string text, TradingDocument defaults)
        {
            if (text == null || text.Length > 262144) throw new FormatException("Trading file exceeds 256 KiB.");
            var result = defaults.Copy();
            var seen = new HashSet<string>();
            string group = null, id = null;
            int lineNumber = 0;
            foreach (string raw in text.Replace("\r", "").Split('\n'))
            {
                lineNumber++;
                try
                {
                    string line = StripComment(raw).TrimEnd();
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line.Contains("\t")) throw new FormatException("Use spaces, not tabs.");
                    int indent = line.Length - line.TrimStart().Length;
                    string token = line.Trim();
                    int colon = token.IndexOf(':');
                    if (colon < 1) throw new FormatException("Expected key: value.");
                    string key = token.Substring(0, colon).Trim();
                    string value = token.Substring(colon + 1).Trim();
                    if (indent == 0)
                    {
                        if ((key != "shop" && key != "sell") || value != "")
                            throw new FormatException("Expected shop: or sell:.");
                        group = key;
                        id = null;
                        if (!seen.Add(group)) throw new FormatException("Duplicate section.");
                    }
                    else if (indent == 2 && group != null)
                    {
                        if (value != "") throw new FormatException("Item must be a mapping.");
                        id = key;
                        var rows = group == "shop" ? result.Shop : result.Sell;
                        if (!rows.ContainsKey(id)) throw new FormatException("Unknown item ID: " + id);
                        if (!seen.Add(group + "/" + id)) throw new FormatException("Duplicate item.");
                    }
                    else if (indent == 4 && group != null && id != null)
                    {
                        if (!seen.Add(group + "/" + id + "/" + key)) throw new FormatException("Duplicate field.");
                        TradeSetting row = (group == "shop" ? result.Shop : result.Sell)[id];
                        if (key == "price") row.Price = Number(value, 0, int.MaxValue);
                        else if (group == "shop" && key == "stack") row.Stack = Number(value, 0, 10000);
                        else if (group == "shop" && key == "enabled")
                        {
                            if (value != "true" && value != "false")
                                throw new FormatException("Expected true or false.");
                            row.Enabled = value == "true";
                        }
                        else if (group == "shop" && key == "requiredGlobalKey") row.RequiredKey = Unquote(value);
                        else throw new FormatException("Unknown field: " + key);
                    }
                    else throw new FormatException("Expected indentation of 0, 2 or 4 spaces.");
                }
                catch (FormatException e)
                {
                    throw new FormatException("trading.yml line " + lineNumber + ": " + e.Message);
                }
            }

            if (!seen.Contains("shop") || !seen.Contains("sell"))
                throw new FormatException("Both shop: and sell: sections are required.");
            foreach (string section in new[] { "shop", "sell" })
            foreach (string item in (section == "shop" ? defaults.Shop : defaults.Sell).Keys)
            foreach (string field in (section == "shop"
                         ? new[] { "price", "enabled", "stack", "requiredGlobalKey" }
                         : new[] { "price" }))
                if (!seen.Contains(section + "/" + item + "/" + field))
                    throw new FormatException("Incomplete trading file: missing " + section + "/" + item + "/" + field);
            return result;
        }

        private static int Number(string value, int min, int max)
        {
            int n;
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out n) || n < min || n > max)
                throw new FormatException("Expected integer " + min + ".." + max + ".");
            return n;
        }

        private static string StripComment(string value)
        {
            bool quoted = false, escaped = false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (quoted && c == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (c == '"') quoted = !quoted;
                if (!quoted && c == '#') return value.Substring(0, i);
            }

            if (quoted) throw new FormatException("Unclosed quote.");
            return value;
        }

        private static string Unquote(string value)
        {
            if (value.StartsWith("\""))
            {
                if (!value.EndsWith("\"") || value.Length < 2) throw new FormatException("Unclosed quote.");
                var result = new StringBuilder();
                for (int i = 1; i < value.Length - 1; i++)
                {
                    char c = value[i];
                    if (c == '\\')
                    {
                        if (++i >= value.Length - 1 || (value[i] != '\\' && value[i] != '"'))
                            throw new FormatException("Unsupported string escape.");
                        c = value[i];
                    }
                    else if (c == '"') throw new FormatException("Unexpected quote.");

                    result.Append(c);
                }

                return result.ToString();
            }

            foreach (char c in value)
                if (!(char.IsLetterOrDigit(c) || c == '_' || c == '-'))
                    throw new FormatException("Quote non-identifier global keys.");
            return value;
        }

        private static string Quote(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

        internal string Serialize()
        {
            var b = new StringBuilder();
            b.AppendLine(
                "# HaldorExpansion trading / Торговля. Server controls multiplayer / В сети настройки сервера.");
            b.AppendLine("# shop: игрок покупает / player buys. Price is for the WHOLE stack / Цена за ВСЮ пачку.");
            b.AppendLine("# enabled: false hides shop item. price: 0 means FREE in shop; disables selling in sell.");
            b.AppendLine(
                "# stack: 0 = prefab maximum stack / максимальный стак. Otherwise 1..10000, capped to prefab maximum / не больше максимума предмета.");
            b.AppendLine("# requiredGlobalKey: \"\" = always available / доступен сразу.");
            b.AppendLine("# Boss keys: defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon,");
            b.AppendLine("# defeated_goblinking, defeated_queen, defeated_fader, defeated_frozenking.");
            b.AppendLine("# Edit values, keep IDs. Known items only. 2/4 spaces, no tabs, aliases or YAML tags.");
            b.AppendLine("# Меняйте значения, сохраняйте ID. Только перечисленные предметы. Отступы 2/4 пробела.");
            foreach (string group in new[] { "shop", "sell" })
            {
                b.AppendLine(group + ":");
                foreach (TradeSetting row in (group == "shop" ? Shop : Sell).Values)
                {
                    b.AppendLine("  # " + row.Category + " — " + row.Label);
                    b.AppendLine("  " + row.Id + ":");
                    b.AppendLine("    price: " + row.Price.ToString(CultureInfo.InvariantCulture));
                    if (group == "shop")
                    {
                        b.AppendLine("    enabled: " + (row.Enabled ? "true" : "false"));
                        b.AppendLine("    stack: " + row.Stack.ToString(CultureInfo.InvariantCulture));
                        b.AppendLine("    requiredGlobalKey: " + Quote(row.RequiredKey));
                    }
                }
            }

            return b.ToString();
        }
    }
}