using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HaldorExpansion.Configuration
{
    internal sealed class CustomTradeSetting
    {
        internal string Id, RequiredKey = "", Category = "Auto";
        internal int Price, Amount = 1;
        internal bool Enabled = true;
    }

    internal sealed class CustomBuySetting
    {
        internal string Id;
        internal int Price;
        internal bool Enabled = true;
    }

    internal sealed class CustomTradingDocument
    {
        internal const int MaximumBytes = 1024 * 1024;

        internal readonly Dictionary<string, CustomTradeSetting> Sells =
            new Dictionary<string, CustomTradeSetting>(StringComparer.Ordinal);

        internal readonly Dictionary<string, CustomBuySetting> Buys =
            new Dictionary<string, CustomBuySetting>(StringComparer.Ordinal);

        internal static string DefaultText =>
            @"# ============================================================================
# HALDOR EXPANSION | CUSTOM TRADING
# ============================================================================
# Add your own items to Haldor's shop and choose what Haldor buys from players.
# This file is created automatically on the first launch.
#
# QUICK START
# 1. Find the section you want to change.
#    haldorSells = items players can buy from Haldor.
#    haldorBuys  = items players can sell to Haldor.
# 2. Copy an example item block in that section.
# 3. Replace the item name with its exact prefab ID.
# 4. Set enabled: true and adjust the price and other values.
# 5. Save the file. Changes reload automatically while the game is running.
#
# GOOD TO KNOW
# - Examples are disabled by default. Enable or delete them as you wish.
# - Use the internal prefab ID, not the name shown in the game.
# - Prefab IDs are case-sensitive. Items from other mods are supported if loaded.
# - Use spaces, never tabs. Items need 2 spaces and their settings need 4.
# - Use only the format shown below. Do not use YAML lists or inline objects.
# - Each item ID can appear only once within a section.
# - Prices must be positive whole numbers. Set enabled: false to hide an entry.
# - The same item may appear in both sections with different prices.
# - The game keeps the last valid settings if a file edit contains an error.
#   Check the BepInEx log for the affected line and the reason.
# - In multiplayer, edit this file on the server or host. Server settings win.
# - Enable Custom Trading is a setting in BepInEx Configuration Manager.
#   Open Configuration Manager with F1 (default hotkey).
#   The Configuration Manager plugin is optional.
#   When disabled, all custom buy and sell entries become inactive.
#   Your YAML entries and their individual enabled values are preserved.
# - Built-in HaldorExpansion shop entries are managed by the normal config.
# - Maximum size is 1 MiB of UTF-8 text, including comments.
# - Both trade sections share the limit. Extremely large catalogs may load slower.
#
# The two examples below do not add any items until you enable them.
# ============================================================================


# ----------------------------------------------------------------------------
# 1. ITEMS HALDOR SELLS TO PLAYERS
# ----------------------------------------------------------------------------
# price  = coins paid for ONE purchase, not per individual item.
# amount = number of items received from that purchase.
# Example: ArrowNeedle with price: 25 and amount: 37 gives 37 arrows for
# 25 coins. It does not give 37 full stacks.
#
# amount cannot exceed the item's maximum stack size in the game.
# If you enter more, the amount will be limited to that maximum.
#
# WORLD PROGRESSION
# requiredGlobalKey: """" makes the item available immediately.
# Set a world key to unlock the item only after that key is active.
# Use exact spelling and capitalization. Put the value in quotes.
#
# Boss progression keys:
# defeated_eikthyr     - Eikthyr
# defeated_gdking      - The Elder
# defeated_bonemass    - Bonemass
# defeated_dragon      - Moder
# defeated_goblinking  - Yagluth
# defeated_queen       - The Queen
# defeated_fader       - Fader
# defeated_frozenking  - Kall Fimbulbringer
#
# Other known world keys include:
# KilledTroll          - A Troll has been killed
# killed_surtling      - A Surtling has been killed
# KilledBat            - A Bat has been killed
# defeated_serpent     - A Serpent has been defeated
#
# These are common examples, not a complete list of every world key.
# Modded worlds may have additional keys from installed mods.
# The game console command listkeys shows keys currently active in your world.
# It does not list every key that could become active later.
#
# SHOP CATEGORIES
# category: Auto lets HaldorExpansion choose the shop tab.
# You can also select one of these categories manually:
# Auto       - Automatically choose a category
# Equipment  - Weapons, armor, tools and accessories
# Wood       - Wood and related materials
# Metals     - Ores, metal scraps and refined metals
# Resources  - Other crafting materials and resources
# Other      - Items that do not fit the categories above
# Idols      - Idols, including custom idols from other mods
#
# Auto recognizes idols already known to HaldorExpansion.
# For new idols from other mods, choose category: Idols manually.
#
# To add another item, copy the whole ArrowNeedle block and change its name.
# Keep the same indentation for every setting.

haldorSells:
  ArrowNeedle:
    enabled: false
    price: 25
    amount: 37
    requiredGlobalKey: """"
    category: Auto

  # Example for another item from the game or an installed mod:
#   MyMod_ItemPrefab:
#     enabled: true
#     price: 100
#     amount: 1
#     requiredGlobalKey: """"
#     category: Equipment

  # Example for placing an idol from another mod in the Idols tab:
#   MyMod_AncientIdol:
#     enabled: true
#     price: 1500
#     amount: 1
#     requiredGlobalKey: ""defeated_bonemass""
#     category: Idols


# ----------------------------------------------------------------------------
# 2. ITEMS HALDOR BUYS FROM PLAYERS
# ----------------------------------------------------------------------------
# price is the number of coins paid for ONE item.
# There is no amount setting here.
# Haldor buys the actual quantity in the inventory stack being sold.
#
# Example: Raspberry with price: 10.
# A stack of 18 raspberries sells for 180 coins.
# A stack of 50 raspberries sells for 500 coins.
# Each click on the regular sell coin button sells one eligible stack.
#
# To add another item, copy the Raspberry block and change its prefab ID.
# The buy and sell prices are independent. You control your own economy.

haldorBuys:
  Raspberry:
    enabled: false
    price: 10

  # Example for an item from another mod:
#   MyMod_ItemPrefab:
#     enabled: true
#     price: 5
";

        // Parse a limited YAML mapping subset without aliases, tags or sequences.
        // Build the replacement document before changing any live state.
        internal static CustomTradingDocument Parse(string text)
        {
            if (text == null) throw Error(1, "Document cannot be null.");
            if (Encoding.UTF8.GetByteCount(text) > MaximumBytes)
                throw Error(1, "Document exceeds 1 MiB of UTF-8 data.");
            var result = new CustomTradingDocument();
            var fields = new HashSet<string>(StringComparer.Ordinal);
            var itemLines = new Dictionary<string, int>(StringComparer.Ordinal);
            var prices = new HashSet<string>(StringComparer.Ordinal);
            string section = null;
            CustomTradeSetting sale = null;
            CustomBuySetting purchase = null;
            bool seenSells = false, seenBuys = false;
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
                        if (key != "haldorSells" && key != "haldorBuys")
                            throw new FormatException("Expected haldorSells: or haldorBuys:.");
                        if (value.Length != 0) throw new FormatException(key + " must be a mapping on separate lines.");
                        if ((key == "haldorSells" && seenSells) || (key == "haldorBuys" && seenBuys))
                            throw new FormatException("Duplicate section: " + key + ".");
                        section = key;
                        if (key == "haldorSells") seenSells = true;
                        else seenBuys = true;
                        sale = null;
                        purchase = null;
                    }
                    else if (indent == 2 && section != null)
                    {
                        if (value.Length != 0) throw new FormatException("Item must be a mapping on separate lines.");
                        string id = Scalar(key);
                        if (string.IsNullOrWhiteSpace(id)) throw new FormatException("Prefab ID cannot be empty.");
                        if (section == "haldorSells")
                        {
                            if (result.Sells.ContainsKey(id)) throw new FormatException("Duplicate item: " + id);
                            sale = new CustomTradeSetting { Id = id };
                            purchase = null;
                            result.Sells.Add(id, sale);
                        }
                        else
                        {
                            if (result.Buys.ContainsKey(id)) throw new FormatException("Duplicate item: " + id);
                            purchase = new CustomBuySetting { Id = id };
                            sale = null;
                            result.Buys.Add(id, purchase);
                        }

                        itemLines.Add(section + "\n" + id, lineNumber);
                        fields.Clear();
                    }
                    else if (indent == 4 && section != null && (sale != null || purchase != null))
                    {
                        if (!fields.Add(key)) throw new FormatException("Duplicate field: " + key);
                        if (key == "price")
                        {
                            int price = Number(value, 1, int.MaxValue);
                            if (sale != null) sale.Price = price;
                            else purchase.Price = price;
                            prices.Add(section + "\n" + (sale != null ? sale.Id : purchase.Id));
                        }
                        else if (key == "enabled")
                        {
                            if (value != "true" && value != "false")
                                throw new FormatException("Expected true or false for enabled.");
                            if (sale != null) sale.Enabled = value == "true";
                            else purchase.Enabled = value == "true";
                        }
                        else if (sale != null)
                        {
                            switch (key)
                            {
                                case "amount": sale.Amount = Number(value, 1, 10000); break;
                                case "requiredGlobalKey": sale.RequiredKey = Scalar(value); break;
                                case "category":
                                    string category = Scalar(value);
                                    if (category != "Auto" && category != "Equipment" && category != "Wood" &&
                                        category != "Metals" && category != "Resources" && category != "Other" &&
                                        category != "Idols")
                                        throw new FormatException("Unknown category: " + category + ".");
                                    sale.Category = category;
                                    break;
                                default: throw new FormatException("Unknown field: " + key);
                            }
                        }
                        else throw new FormatException("Unknown field in haldorBuys: " + key);
                    }
                    else
                        throw new FormatException(
                            "Expected indentation of 0, 2 or 4 spaces below a trade section and item ID.");
                }
                catch (FormatException error)
                {
                    throw Error(lineNumber, error.Message);
                }
            }

            foreach (var entry in itemLines)
                if (!prices.Contains(entry.Key))
                    throw Error(entry.Value, "Missing required price for " + entry.Key.Split('\n')[1] + ".");
            // The on-wire representation has more defaults than a short YAML input.
            if (Encoding.UTF8.GetByteCount(result.Serialize()) > MaximumBytes)
                throw Error(1, "Serialized document exceeds 1 MiB of UTF-8 data after expanding defaults.");
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
                output.Append("    amount: ").Append(row.Amount.ToString(CultureInfo.InvariantCulture)).Append('\n');
                output.Append("    requiredGlobalKey: ").Append(Quote(row.RequiredKey)).Append('\n');
                output.Append("    category: ").Append(row.Category).Append('\n');
            }

            output.Append("haldorBuys:\n");
            foreach (CustomBuySetting row in Buys.Values)
            {
                output.Append("  ").Append(Quote(row.Id)).Append(":\n");
                output.Append("    enabled: ").Append(row.Enabled ? "true" : "false").Append('\n');
                output.Append("    price: ").Append(row.Price.ToString(CultureInfo.InvariantCulture)).Append('\n');
            }

            return output.ToString();
        }
    }
}