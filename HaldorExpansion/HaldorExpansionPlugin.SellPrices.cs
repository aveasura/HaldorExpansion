using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using Jotunn.Extensions;

namespace HaldorExpansion
{
    public partial class HaldorExpansionPlugin
    {
        private static readonly Dictionary<string, ConfigEntry<int>> HaldorSellPrices =
            new Dictionary<string, ConfigEntry<int>>();

        private static ConfigEntry<int> DefaultTrophySellPriceConfig;

        private const string TrophySellPriceDescription =
            "Sell price in coins when this trophy is sold to Haldor. " +
            "Set to 0 to disable selling it. Negative values are treated as 0.";

        private const string ExtraSellablePriceDescription =
            "Sell price in coins when this item is sold to Haldor. " +
            "Set to 0 to disable selling it. Negative values are treated as 0.";

        private void RegisterHaldorSellPriceConfig()
        {
            DefaultTrophySellPriceConfig = Config.BindConfig(
                "Sell Prices - General",
                "Unlisted Trophy Price",
                DefaultTrophyValue,
                "Fallback sell price for trophy prefabs that are not explicitly listed below, including trophies added by other mods or future game updates. " +
                "Set to 0 to disable selling unlisted trophies. Negative values are treated as 0.",
                synced: true);
            WatchSellPriceEntry(DefaultTrophySellPriceConfig);

            // Meadows.
            BindTrophySellPrice("Sell Prices - Meadows", "TrophyDeer", "Deer Trophy", 15);
            BindTrophySellPrice("Sell Prices - Meadows", "TrophyBoar", "Boar Trophy", 15);
            BindTrophySellPrice("Sell Prices - Meadows", "TrophyNeck", "Neck Trophy", 25);
            BindTrophySellPrice("Sell Prices - Meadows", "TrophyEikthyr", "Eikthyr Trophy", 70);

            // Black Forest.
            BindTrophySellPrice("Sell Prices - Black Forest", "TrophyGreydwarf", "Greydwarf Trophy", 15);
            BindTrophySellPrice("Sell Prices - Black Forest", "TrophySkeleton", "Skeleton Trophy", 15);
            BindTrophySellPrice("Sell Prices - Black Forest", "TrophyGreydwarfShaman", "Greydwarf Shaman Trophy", 20);
            BindTrophySellPrice("Sell Prices - Black Forest", "TrophyGhost", "Ghost Trophy", 25);
            BindTrophySellPrice("Sell Prices - Black Forest", "TrophyGreydwarfBrute", "Greydwarf Brute Trophy", 20);
            BindTrophySellPrice("Sell Prices - Black Forest", "TrophyBjorn", "Bear Trophy", 50);
            BindTrophySellPrice("Sell Prices - Black Forest", "TrophyFrostTroll", "Troll Trophy", 50);
            BindTrophySellPriceAlias("TrophyForestTroll", "TrophyFrostTroll");
            BindTrophySellPrice("Sell Prices - Black Forest", "TrophySkeletonPoison", "Rancid Remains Trophy", 50);
            BindTrophySellPrice("Sell Prices - Black Forest", "TrophySkeletonHildir", "Brenna Trophy", 80);
            BindTrophySellPrice("Sell Prices - Black Forest", "TrophyTheElder", "The Elder Trophy", 100);

            // Ocean.
            BindTrophySellPrice("Sell Prices - Ocean", "TrophySerpent", "Serpent Trophy", 500);
            BindTrophySellPrice("Sell Prices - Ocean", "TrophyBonemawSerpent", "Bonemaw Trophy", 500);

            // Swamp.
            BindTrophySellPrice("Sell Prices - Swamp", "TrophyBlob", "Blob Trophy", 15);
            BindTrophySellPrice("Sell Prices - Swamp", "TrophyDraugr", "Draugr Trophy", 15);
            BindTrophySellPriceAlias("TrophyDraugrFem", "TrophyDraugr");
            BindTrophySellPrice("Sell Prices - Swamp", "TrophyLeech", "Leech Trophy", 15);
            BindTrophySellPrice("Sell Prices - Swamp", "TrophySurtling", "Surtling Trophy", 15);
            BindTrophySellPrice("Sell Prices - Swamp", "TrophyDraugrElite", "Draugr Elite Trophy", 30);
            BindTrophySellPrice("Sell Prices - Swamp", "TrophyWraith", "Wraith Trophy", 35);
            BindTrophySellPrice("Sell Prices - Swamp", "TrophyAbomination", "Abomination Trophy", 75);
            BindTrophySellPrice("Sell Prices - Swamp", "TrophyKvastur", "Kvastur Trophy", 80);
            BindTrophySellPrice("Sell Prices - Swamp", "TrophyBonemass", "Bonemass Trophy", 150);

            // Mountains.
            BindTrophySellPrice("Sell Prices - Mountains", "TrophyWolf", "Wolf Trophy", 15);
            BindTrophySellPrice("Sell Prices - Mountains", "TrophyHatchling", "Drake Trophy", 15);
            BindTrophySellPrice("Sell Prices - Mountains", "TrophyUlv", "Ulv Trophy", 20);
            BindTrophySellPrice("Sell Prices - Mountains", "TrophyFenring", "Fenring Trophy", 25);
            BindTrophySellPrice("Sell Prices - Mountains", "TrophyCultist", "Cultist Trophy", 40);
            BindTrophySellPrice("Sell Prices - Mountains", "TrophySGolem", "Stone Golem Trophy", 70);
            BindTrophySellPrice("Sell Prices - Mountains", "TrophyCultist_Hildir", "Geirrhafa Trophy", 80);
            BindTrophySellPrice("Sell Prices - Mountains", "TrophyDragonQueen", "Moder Trophy", 200);

            // Plains.
            BindTrophySellPrice("Sell Prices - Plains", "TrophyDeathsquito", "Deathsquito Trophy", 15);
            BindTrophySellPrice("Sell Prices - Plains", "TrophyGoblin", "Fuling Trophy", 15);
            BindTrophySellPrice("Sell Prices - Plains", "TrophyGrowth", "Growth Trophy", 15);
            BindTrophySellPrice("Sell Prices - Plains", "TrophyBjornUndead", "Vile Trophy", 50);
            BindTrophySellPrice("Sell Prices - Plains", "TrophyLox", "Lox Trophy", 35);
            BindTrophySellPrice("Sell Prices - Plains", "TrophyGoblinShaman", "Fuling Shaman Trophy", 30);
            BindTrophySellPrice("Sell Prices - Plains", "TrophyGoblinBrute", "Fuling Berserker Trophy", 70);
            BindTrophySellPrice("Sell Prices - Plains", "TrophyGoblinBruteBrosShaman", "Zil Trophy", 80);
            BindTrophySellPrice("Sell Prices - Plains", "TrophyGoblinBruteBrosBrute", "Thungr Trophy", 80);
            BindTrophySellPrice("Sell Prices - Plains", "TrophyGoblinKing", "Yagluth Trophy", 350);

            // Mistlands.
            BindTrophySellPrice("Sell Prices - Mistlands", "TrophySeeker", "Seeker Trophy", 20);
            BindTrophySellPrice("Sell Prices - Mistlands", "TrophyTick", "Tick Trophy", 20);
            BindTrophySellPrice("Sell Prices - Mistlands", "TrophyDvergr", "Dvergr Trophy", 20);
            BindTrophySellPrice("Sell Prices - Mistlands", "TrophyHare", "Hare Trophy", 20);
            BindTrophySellPrice("Sell Prices - Mistlands", "TrophyGjall", "Gjall Trophy", 70);
            BindTrophySellPrice("Sell Prices - Mistlands", "TrophySeekerBrute", "Seeker Soldier Trophy", 80);
            BindTrophySellPrice("Sell Prices - Mistlands", "TrophySeekerQueen", "The Queen Trophy", 500);

            // Ashlands.
            BindTrophySellPrice("Sell Prices - Ashlands", "TrophyCharredArcher", "Marksman Trophy", 20);
            BindTrophySellPrice("Sell Prices - Ashlands", "TrophyVolture", "Volture Trophy", 20);
            BindTrophySellPrice("Sell Prices - Ashlands", "TrophyAsksvin", "Asksvin Trophy", 20);
            BindTrophySellPrice("Sell Prices - Ashlands", "TrophyCharredMage", "Warlock Trophy", 25);
            BindTrophySellPrice("Sell Prices - Ashlands", "TrophyCharredMelee", "Warrior Trophy", 25);
            BindTrophySellPrice("Sell Prices - Ashlands", "TrophyBlob_Lava", "Lava Blob Trophy", 30);
            BindTrophySellPrice("Sell Prices - Ashlands", "TrophyMorgen", "Morgen Trophy", 100);
            BindTrophySellPrice("Sell Prices - Ashlands", "TrophyFallenValkyrie", "Fallen Valkyrie Trophy", 120);
            BindTrophySellPrice("Sell Prices - Ashlands", "TrophyFader", "Fader Trophy", 1000);

            // Deep North.
            BindTrophySellPrice("Sell Prices - Deep North", "TrophyBarka", "Barka Trophy", 50);
            BindTrophySellPrice("Sell Prices - Deep North", "TrophyElaking", "Elaking Trophy", 50);
            BindTrophySellPrice("Sell Prices - Deep North", "TrophyMole", "Eyeless One Trophy", 50);
            BindTrophySellPrice("Sell Prices - Deep North", "TrophyBlob_Frost", "Frost Blob Trophy", 50);
            BindTrophySellPrice("Sell Prices - Deep North", "TrophyJotunWitch", "Hexen Trophy", 50);
            BindTrophySellPrice("Sell Prices - Deep North", "TrophyJotunWarrior", "Krigen Trophy", 50);
            BindTrophySellPrice("Sell Prices - Deep North", "TrophyMoose", "Moose Trophy", 50);
            BindTrophySellPrice("Sell Prices - Deep North", "TrophyBlob_Morkhalla", "Pulp Trophy", 50);
            BindTrophySellPrice("Sell Prices - Deep North", "TrophySeal", "Seal Trophy", 50);
            BindTrophySellPrice("Sell Prices - Deep North", "TrophyWrithan", "Writhan Trophy", 50);
            BindTrophySellPrice("Sell Prices - Deep North", "TrophyDeerWhite", "White Deer Trophy", 50);

            // The three non-trophy items that HaldorExpansion explicitly allows Haldor to buy.
            BindExtraSellPrice("Sell Prices - Cores", "SurtlingCore", "Surtling Core", 50);
            BindExtraSellPrice("Sell Prices - Cores", "BlackCore", "Black Core", 100);
            BindExtraSellPrice("Sell Prices - Cores", "MoltenCore", "Molten Core", 150);
        }

        private void BindTrophySellPrice(string section, string prefabName, string displayName, int defaultPrice)
        {
            BindHaldorSellPrice(section, prefabName, displayName, defaultPrice, TrophySellPriceDescription);
        }

        private void BindExtraSellPrice(string section, string prefabName, string displayName, int defaultPrice)
        {
            BindHaldorSellPrice(section, prefabName, displayName, defaultPrice, ExtraSellablePriceDescription);
        }

        private void BindHaldorSellPrice(
            string section,
            string prefabName,
            string displayName,
            int defaultPrice,
            string description)
        {
            ConfigEntry<int> entry = Config.BindConfig(
                section,
                SanitizeConfigKey(displayName) + " Price",
                defaultPrice,
                description,
                synced: true);

            HaldorSellPrices[prefabName] = entry;
            WatchSellPriceEntry(entry);
        }

        private static void BindTrophySellPriceAlias(string aliasPrefabName, string sourcePrefabName)
        {
            ConfigEntry<int> source;
            if (HaldorSellPrices.TryGetValue(sourcePrefabName, out source) && source != null)
            {
                HaldorSellPrices[aliasPrefabName] = source;
            }
        }

        private static void WatchSellPriceEntry(ConfigEntry<int> entry)
        {
            if (entry == null)
                return;

            entry.SettingChanged += OnHaldorSellPriceChanged;
        }

        private static void OnHaldorSellPriceChanged(object sender, EventArgs e)
        {
            if (ObjectDB.instance != null)
            {
                ApplyTrophyValues(ObjectDB.instance);
            }
        }

        internal static int GetConfiguredHaldorSellPrice(string prefabName, int fallbackPrice)
        {
            ConfigEntry<int> entry;
            if (!string.IsNullOrEmpty(prefabName) &&
                HaldorSellPrices.TryGetValue(prefabName, out entry) &&
                entry != null)
            {
                return entry.Value < 0 ? 0 : entry.Value;
            }

            return fallbackPrice < 0 ? 0 : fallbackPrice;
        }

        internal static int GetDefaultTrophySellPrice()
        {
            if (DefaultTrophySellPriceConfig == null)
                return DefaultTrophyValue;

            return DefaultTrophySellPriceConfig.Value < 0 ? 0 : DefaultTrophySellPriceConfig.Value;
        }
    }
}
