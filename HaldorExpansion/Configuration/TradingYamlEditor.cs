using System;

namespace HaldorExpansion.Configuration
{
    internal static class TradingYamlEditor
    {
        // Update a single field instead of serializing the whole file, preserving user comments and layout.
        internal static string Replace(string yaml, string group, string id, string field, string value)
        {
            string[] lines = yaml.Split('\n');
            string section = null, item = null;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd('\r');
                int commentAt = CommentIndex(line);
                string content = (commentAt < 0 ? line : line.Substring(0, commentAt)).TrimEnd();
                if (string.IsNullOrWhiteSpace(content)) continue;
                int indent = content.Length - content.TrimStart().Length;
                string token = content.Trim();
                int colon = token.IndexOf(':');
                if (colon < 1) continue; // The caller parses and validates the complete file first.
                string key = token.Substring(0, colon).Trim();
                if (indent == 0)
                {
                    section = key;
                    item = null;
                }
                else if (indent == 2) item = key;
                else if (indent == 4 && section == group && item == id && key == field)
                {
                    int separator = line.IndexOf(':');
                    string comment = commentAt >= 0 ? " " + line.Substring(commentAt) : "";
                    string ending = lines[i].EndsWith("\r") ? "\r" : "";
                    lines[i] = line.Substring(0, separator + 1) + " " + value + comment + ending;
                    return string.Join("\n", lines);
                }
            }

            throw new FormatException("Missing trading.yml setting: " + group + "/" + id + "/" + field);
        }

        private static int CommentIndex(string line)
        {
            bool quoted = false, escaped = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
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
                else if (c == '#' && !quoted) return i;
            }

            return -1;
        }
    }
}