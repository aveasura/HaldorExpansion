using HaldorExpansion.Trading;

namespace HaldorExpansion.Presentation
{
    internal static class HaldorTabsLayout
    {
        // Presentation order is independent of persisted category identities.
        internal static readonly ShopCategory[] CategoryOrder =
        {
            ShopCategory.All, ShopCategory.Equipment, ShopCategory.Wood, ShopCategory.Metals,
            ShopCategory.Resources, ShopCategory.Idols, ShopCategory.Other
        };

        internal static float CalculateStep(int count, float availableHeight)
        {
            if (count <= 0 || availableHeight <= 0f) return 0f;

            // Include the final gap before the native coin frame.
            // The view keeps native dimensions; only vertical spacing is calculated.
            return availableHeight / count;
        }
    }
}