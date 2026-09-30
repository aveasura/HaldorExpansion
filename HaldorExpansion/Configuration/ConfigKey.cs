
namespace HaldorExpansion.Configuration
{
    internal static class ConfigKey
    {

        internal static string SanitizeConfigKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Item";

            return value
                .Replace("=", string.Empty)
                .Replace("\n", " ")
                .Replace("\r", " ")
                .Replace("\t", " ")
                .Replace("\\", string.Empty)
                .Replace("\"", string.Empty)
                .Replace("'", string.Empty)
                .Replace("[", "(")
                .Replace("]", ")")
                .Trim();
        }
    }
}
