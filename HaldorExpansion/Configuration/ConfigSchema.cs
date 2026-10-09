using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace HaldorExpansion.Configuration
{
    // Records the last complete set of keys independently of the mod version.
    // A key-set fingerprint rejects truncated files, even when a newer build has more defaults.
    internal sealed class ConfigSchema
    {
        internal const int FormatVersion = 1;
        internal string General, Balance, Trading;

        internal static ConfigSchema Read(string path)
        {
            var lines = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string raw in File.ReadAllLines(path))
            {
                int equals = raw.IndexOf('=');
                if (equals <= 0 || lines.ContainsKey(raw.Substring(0, equals)))
                    throw new FormatException("Invalid or duplicate config schema entry.");
                lines.Add(raw.Substring(0, equals), raw.Substring(equals + 1));
            }

            int version;
            if (lines.Count != 4 || !lines.ContainsKey("formatVersion") ||
                !int.TryParse(lines["formatVersion"], out version) || version != FormatVersion ||
                !IsHash(lines, "general") || !IsHash(lines, "balance") || !IsHash(lines, "trading"))
                throw new FormatException(
                    "Unsupported or invalid config format manifest. Keep existing configs unchanged.");
            return new ConfigSchema
                { General = lines["general"], Balance = lines["balance"], Trading = lines["trading"] };
        }

        private static bool IsHash(Dictionary<string, string> values, string key)
        {
            string hash;
            return values.TryGetValue(key, out hash) && hash.Length == 64 &&
                   hash.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));
        }

        internal string Serialize() => "formatVersion=" + FormatVersion + "\n" +
                                       "general=" + General + "\n" +
                                       "balance=" + Balance + "\n" +
                                       "trading=" + Trading + "\n";

        internal static string Signature(IEnumerable<string> keys)
        {
            string canonical = string.Join("\n", keys.OrderBy(k => k, StringComparer.Ordinal));
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(canonical)))
                    .Replace("-", "").ToLowerInvariant();
        }

        internal static string TradingSignature(string text, TradingDocument defaults, bool complete = true)
        {
            var keys = new HashSet<string>();
            TradingDocument.Parse(text, defaults, complete, keys);
            return Signature(keys);
        }

        internal static void Require(string expected, string actual, string filename)
        {
            if (expected != actual)
                throw new FormatException("Missing or changed entries in " + filename +
                                          ". Restore the complete previous file before upgrading.");
        }
    }
}