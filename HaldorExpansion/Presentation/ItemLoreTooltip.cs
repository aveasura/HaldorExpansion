using System;

namespace HaldorExpansion.Presentation
{
    internal static class ItemLoreTooltip
    {
        internal static string MoveToEnd(string tooltip, string descriptionKey, string description, string lore)
        {
            if (string.IsNullOrEmpty(tooltip) || string.IsNullOrEmpty(descriptionKey) ||
                string.IsNullOrEmpty(description) || string.IsNullOrEmpty(lore) ||
                !description.StartsWith(lore, StringComparison.Ordinal)) return tooltip;

            // Vanilla emits the description key before its stats. Also support a
            // preceding patch that has already localized the description.
            int prefixLength;
            if (tooltip.StartsWith(descriptionKey, StringComparison.Ordinal))
                prefixLength = descriptionKey.Length;
            else if (tooltip.StartsWith(description, StringComparison.Ordinal))
                prefixLength = description.Length;
            else return tooltip;

            // Do not touch shared item data or the game's shared StringBuilder.
            // Keep native stat/effect tokens intact for normal UI localization.
            string stats = tooltip.Substring(prefixLength).Trim('\r', '\n');
            string mechanics = description.Substring(lore.Length).Trim('\r', '\n');
            string body = stats;
            if (mechanics.Length > 0)
                body += (body.Length > 0 ? "\n\n" : "") + mechanics;
            return body + (body.Length > 0 ? "\n\n" : "") + lore;
        }
    }
}
