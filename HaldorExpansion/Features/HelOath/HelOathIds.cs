namespace HaldorExpansion.Features.HelOath
{
    internal static class HelOathIds
    {
        internal const string AbilityKey = "$he_hel_ability";
        internal const string PreparedKey = "$he_hel_prepared";
        internal const string TouchStatus = "SE_HelOathTouch";
        internal const string TouchKeyPrefix = "$se_hel_touch_";

        internal static string TouchNameKey(int stage) => TouchKeyPrefix + stage;
        internal static string TouchDescriptionKey(int stage) => TouchNameKey(stage) + "_desc";
    }
}