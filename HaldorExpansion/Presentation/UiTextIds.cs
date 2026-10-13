using HaldorExpansion.Trading;

namespace HaldorExpansion.Presentation
{
    internal static class UiTextIds
    {
        internal const string TooltipScroll = "$he_tooltip_scroll";
        internal const string ShopAll = "$aveasura_he_shop_all";
        internal const string ShopEquipment = "$aveasura_he_shop_equipment";
        internal const string ShopWood = "$aveasura_he_shop_wood";
        internal const string ShopMetals = "$aveasura_he_shop_metals";
        internal const string ShopResources = "$aveasura_he_shop_resources";
        internal const string ShopOther = "$aveasura_he_shop_other";

        internal static string ShopCategoryKey(ShopCategory category)
        {
            switch (category)
            {
                case ShopCategory.Equipment: return ShopEquipment;
                case ShopCategory.Wood: return ShopWood;
                case ShopCategory.Metals: return ShopMetals;
                case ShopCategory.Resources: return ShopResources;
                case ShopCategory.Other: return ShopOther;
                default: return ShopAll;
            }
        }
    }
}