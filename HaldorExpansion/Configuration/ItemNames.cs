namespace HaldorExpansion.Configuration
{
    internal static class ItemNames
    {
        internal const string HelOathRu = "Клятва Хель";
        internal const string HelOathEn = "Hel's Oath";
        internal const string BerserkerGreataxeRu = "Секира берсерка";
        internal const string BerserkerGreataxeEn = "Berserker Greataxe";
        internal const string BerserkerCloakRu = "Плащ берсерка";
        internal const string BerserkerCloakEn = "Berserker Cloak";
        internal const string BerserkerHelmetRu = "Шлем берсерка";
        internal const string BerserkerHelmetEn = "Berserker Helmet";
        internal const string BrisingamenRu = "Осквернённый Бризингамен";
        internal const string BrisingamenEn = "Corrupted Brisingamen";
        internal const string NornThreadRu = "Нить Норн";
        internal const string NornThreadEn = "Thread of the Norns";
        internal const string SilentReckoningRu = "Кираса безмолвной расплаты";
        internal const string SilentReckoningEn = "Cuirass of Silent Reckoning";
        internal const string ShadowHuntRu = "Арбалет теневой охоты";
        internal const string ShadowHuntEn = "Crossbow of the Shadow Hunt";
        internal const string BoneCrushersRu = "Костоломы";
        internal const string BoneCrushersEn = "Bone Crushers";
        internal const string PitKingRu = "Накидка хозяина ямы";
        internal const string PitKingEn = "Pit King's Mantle";
        internal const string PeltOfHelheimRu = "Шкура Хельхейма";
        internal const string PeltOfHelheimEn = "Pelt of Helheim";
        internal const string WoundedBeastRu = "Плащ раненого зверя";
        internal const string WoundedBeastEn = "Cloak of the Wounded Beast";
        internal const string BurnedResolveRu = "Плащ выжженной стойкости";
        internal const string BurnedResolveEn = "Cloak of Burned Resolve";

        internal static string ConfigSection(string english, string russian)
            => ConfigKey.SanitizeConfigKey(english + " / " + russian);

        internal static string Label(string legacyName)
        {
            switch (legacyName)
            {
                case "Hel Oath": return HelOathRu + " / " + HelOathEn;
                case "Berserker Greataxe": return BerserkerGreataxeRu + " / " + BerserkerGreataxeEn;
                case "Berserker Cloak": return BerserkerCloakRu + " / " + BerserkerCloakEn;
                case "Berserker Helmet": return BerserkerHelmetRu + " / " + BerserkerHelmetEn;
                case "Corrupted Brisingamen": return BrisingamenRu + " / " + BrisingamenEn;
                case "Thread of the Norns": return NornThreadRu + " / " + NornThreadEn;
                case "Cuirass of Silent Reckoning": return SilentReckoningRu + " / " + SilentReckoningEn;
                case "Crossbow of the Shadow Hunt": return ShadowHuntRu + " / " + ShadowHuntEn;
                case "Grit Cestus": return BoneCrushersRu + " / " + BoneCrushersEn;
                case "Pit King's Mantle": return PitKingRu + " / " + PitKingEn;
                case "Pelt of Helheim": return PeltOfHelheimRu + " / " + PeltOfHelheimEn;
                case "Cloak of the Wounded Beast": return WoundedBeastRu + " / " + WoundedBeastEn;
                case "Fractured Breath Cape": return BurnedResolveRu + " / " + BurnedResolveEn;
                default: return legacyName;
            }
        }
    }
}