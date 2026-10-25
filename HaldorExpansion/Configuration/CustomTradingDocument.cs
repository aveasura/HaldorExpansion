using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HaldorExpansion.Configuration
{
    internal sealed class CustomTradeSetting
    {
        internal string Id, RequiredKey = "", Category = "Auto";
        internal int Price, Stack = 1;
        internal bool Enabled = true;
    }

    internal sealed class CustomTradingDocument
    {
        internal readonly Dictionary<string, CustomTradeSetting> Sells =
            new Dictionary<string, CustomTradeSetting>(StringComparer.Ordinal);

        internal static string DefaultText
        {
            get
            {
                return "# Optional extra items Haldor sells to players. Empty means no custom items.\n" +
                       "# Дополнительные товары Хальдора. Пустой список отключает добавленные товары.\n" +
                       "# Exact prefab IDs, including other mods; built-in HaldorExpansion items take priority.\n" +
                       "# Указывайте точное имя префаба. Встроенные товары мода имеют приоритет.\n" +
                       "# Use 2/4 spaces, no tabs, YAML aliases, tags or inline mappings.\n" +
                       "# price: 1 - 2147483647 coins for the whole stack.\n" +
                       "# stack: 1 - 10000 (runtime caps it to the prefab maximum); enabled defaults to true.\n" +
                       "# price - цена всей пачки (от 1 монеты); stack - количество, enabled: false - скрыть товар.\n" +
                       "# requiredGlobalKey defaults to \"\" (always available). Quote strings with punctuation.\n" +
                       "# category: Auto, Equipment, Wood, Metals, Resources or Other (default Auto).\n" +
                       "# Idols is reserved for known catalog idols..\n" +
                       "# Idols только для встроенных идолов.\n" +
                       "# Лимит файла и данных синхронизации: 256 КиБ UTF-8, включая значения по умолчанию.\n" +
                       "haldorSells:\n" +
                       "  # Raspberry:\n" +
                       "  #   enabled: true\n" +
                       "  #   price: 25\n" +
                       "  #   stack: 10\n" +
                       "  #   requiredGlobalKey: \"\"\n" +
                       "  #   category: Auto\n";
            }
        }

        // This deliberately small YAML subset accepts mappings and scalar values only.
        // Parsing stages a complete replacement; a malformed edit never mutates a live document.
        internal static CustomTradingDocument Parse(string text)
        {
            if (text == null) throw Error(1, "Document cannot be null.");
            if (Encoding.UTF8.GetByteCount(text) > 262144) throw Error(1, "Document exceeds 256 KiB of UTF-8 data.");
            var result = new CustomTradingDocument();
            var fields = new HashSet<string>(StringComparer.Ordinal);
            var itemLines = new Dictionary<string, int>(StringComparer.Ordinal);
            var prices = new HashSet<string>(StringComparer.Ordinal);
            bool section = false;
            CustomTradeSetting current = null;
            int lineNumber = 0;
            foreach (string raw in text.TrimStart('\uFEFF').Replace("\r\n", "\n").Split('\n'))
            {
                lineNumber++;
                try
                {
                    string line = StripComment(raw).TrimEnd(' ');
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line.IndexOf('\t') >= 0) throw new FormatException("Use spaces, not tabs.");
                    foreach (char c in line)
                        if (char.IsControl(c))
                            throw new FormatException("Unexpected control character.");
                    int indent = 0;
                    while (indent < line.Length && line[indent] == ' ') indent++;
                    string token = line.Substring(indent);
                    int colon = Separator(token);
                    if (colon < 1) throw new FormatException("Expected key: value.");
                    string key = token.Substring(0, colon).TrimEnd(' ');
                    string value = token.Substring(colon + 1).Trim(' ');
                    if (indent == 0)
                    {
                        if (key == "haldorBuys")
                            throw new FormatException("haldorBuys is not supported; configure only haldorSells.");
                        if (key != "haldorSells") throw new FormatException("Expected haldorSells:.");
                        if (value.Length != 0)
                            throw new FormatException("haldorSells must be a mapping on separate lines.");
                        if (section) throw new FormatException("Duplicate section: haldorSells.");
                        section = true;
                    }
                    else if (indent == 2 && section)
                    {
                        if (value.Length != 0) throw new FormatException("Item must be a mapping on separate lines.");
                        string id = Scalar(key);
                        if (string.IsNullOrWhiteSpace(id)) throw new FormatException("Prefab ID cannot be empty.");
                        if (result.Sells.ContainsKey(id)) throw new FormatException("Duplicate item: " + id);
                        current = new CustomTradeSetting { Id = id };
                        result.Sells.Add(id, current);
                        itemLines.Add(id, lineNumber);
                        fields.Clear();
                    }
                    else if (indent == 4 && section && current != null)
                    {
                        if (!fields.Add(key)) throw new FormatException("Duplicate field: " + key);
                        switch (key)
                        {
                            case "price":
                                current.Price = Number(value, 1, int.MaxValue);
                                prices.Add(current.Id);
                                break;
                            case "stack": current.Stack = Number(value, 1, 10000); break;
                            case "enabled":
                                if (value != "true" && value != "false")
                                    throw new FormatException("Expected true or false for enabled.");
                                current.Enabled = value == "true";
                                break;
                            case "requiredGlobalKey": current.RequiredKey = Scalar(value); break;
                            case "category":
                                string category = Scalar(value);
                                if (category == "Idols")
                                    throw new FormatException(
                                        "Category Idols is reserved for known catalog idols; use Auto.");
                                if (category != "Auto" && category != "Equipment" && category != "Wood" &&
                                    category != "Metals" && category != "Resources" && category != "Other")
                                    throw new FormatException("Unknown category: " + category +
                                                              ". Use Auto, Equipment, Wood, Metals, Resources or Other.");
                                current.Category = category;
                                break;
                            default: throw new FormatException("Unknown field: " + key);
                        }
                    }
                    else
                        throw new FormatException(
                            "Expected indentation of 0, 2 or 4 spaces below haldorSells and an item ID.");
                }
                catch (FormatException e)
                {
                    throw Error(lineNumber, e.Message);
                }
            }

            foreach (var item in result.Sells)
                if (!prices.Contains(item.Key))
                    throw Error(itemLines[item.Key], "Missing required price for " + item.Key + ".");
            // Sync sends the fully populated document, which can be larger than compact user input.
            if (Encoding.UTF8.GetByteCount(result.Serialize()) > 262144)
                throw Error(1, "Serialized document exceeds 256 KiB of UTF-8 data after expanding defaults; " +
                               "reduce the number of items or the length of prefab IDs and global keys.");
            return result;
        }

        private static FormatException Error(int line, string detail)
        {
            return new FormatException("custom-trading.yml line " + line + ": " + detail);
        }

        private static int Number(string value, int min, int max)
        {
            int number;
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out number) ||
                number < min || number > max)
                throw new FormatException("Expected integer " + min + ".." + max + ".");
            return number;
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

        private static int Separator(string value)
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
                if (!quoted && c == ':') return i;
            }

            return -1;
        }

        private static string Scalar(string value)
        {
            if (value.StartsWith("\"", StringComparison.Ordinal))
            {
                if (value.Length < 2 || !value.EndsWith("\"", StringComparison.Ordinal))
                    throw new FormatException("Unclosed quote.");
                var decoded = new StringBuilder();
                for (int i = 1; i < value.Length - 1; i++)
                {
                    char c = value[i];
                    if (c == '\\')
                    {
                        if (++i >= value.Length - 1 || (value[i] != '\\' && value[i] != '"'))
                            throw new FormatException("Unsupported string escape; use only \\\\ or \\\".");
                        c = value[i];
                    }
                    else if (c == '"') throw new FormatException("Unexpected quote.");

                    decoded.Append(c);
                }

                return decoded.ToString();
            }

            foreach (char c in value)
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != '.')
                    throw new FormatException("Quote strings containing spaces or punctuation using double quotes.");
            return value;
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        internal string Serialize()
        {
            var output = new StringBuilder("haldorSells:\n");
            foreach (CustomTradeSetting row in Sells.Values)
            {
                output.Append("  ").Append(Quote(row.Id)).Append(":\n");
                output.Append("    enabled: ").Append(row.Enabled ? "true" : "false").Append('\n');
                output.Append("    price: ").Append(row.Price.ToString(CultureInfo.InvariantCulture)).Append('\n');
                output.Append("    stack: ").Append(row.Stack.ToString(CultureInfo.InvariantCulture)).Append('\n');
                output.Append("    requiredGlobalKey: ").Append(Quote(row.RequiredKey)).Append('\n');
                output.Append("    category: ").Append(row.Category).Append('\n');
            }

            return output.ToString();
        }
    }
}