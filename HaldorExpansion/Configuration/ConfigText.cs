using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HaldorExpansion.Configuration
{
    internal static class ConfigText
    {
        internal static Dictionary<string, string> Parse(string text)
        {
            var values = new Dictionary<string, string>();
            string section = "";
            int lineNo = 0;
            foreach (string raw in text.Replace("\r", "").Split('\n'))
            {
                lineNo++;
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    section = line.Substring(1, line.Length - 2);
                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq < 1 || section.Length == 0) throw new FormatException("Invalid config line " + lineNo);
                string key = section + "\n" + line.Substring(0, eq).Trim();
                if (values.ContainsKey(key)) throw new FormatException("Duplicate config key at line " + lineNo);
                values.Add(key, line.Substring(eq + 1).Trim());
            }

            return values;
        }

        internal static void WriteAtomic(string path, string text)
        {
            string temp = path + ".tmp";
            File.WriteAllText(temp, text, new UTF8Encoding(false));
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
        }

        internal static string LegacyKey(string section, string key)
        {
            if (section == "Compatibility") section = "UI";
            if (section == "Bone Crushers / Костоломы") section = "Cestus";
            if (section == "Berserker Greataxe / Секира берсерка") section = "Berserker";
            if (section == "Hels Oath / Клятва Хель" || section == "Hel's Oath / Клятва Хель") section = "Hel Oath";
            return section + "\n" + key;
        }
    }
}