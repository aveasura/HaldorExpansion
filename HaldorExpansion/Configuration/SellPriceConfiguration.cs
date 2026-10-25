using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using HaldorExpansion.Trading;
using Jotunn.Extensions;

namespace HaldorExpansion.Configuration
{
    internal sealed class SellPriceConfiguration
    {
        private ConfigFile Config;

        private static readonly Dictionary<string, ConfigEntry<int>> HaldorSellPrices =
            new Dictionary<string, ConfigEntry<int>>();

        private static ConfigEntry<int> DefaultTrophySellPriceConfig;

        private const string TrophySellPriceDescription =
            "Sell price in coins when this trophy is sold to Haldor. " +
            "Set to 0 to disable selling it. Negative values are treated as 0.";

        private const string ExtraSellablePriceDescription =
            "Sell price in coins when this item is sold to Haldor. " +
            "Set to 0 to disable selling it. Negative values are treated as 0.";

        internal void RegisterHaldorSellPriceConfig(ConfigFile config)
        {
            Config = config;
            DefaultTrophySellPriceConfig = Config.BindConfig(
                "Sell Prices - General",
                "Unlisted Trophy Price",
                TrophyValueUpdater.DefaultTrophyValue,
                "Fallback sell price for trophy prefabs that are not explicitly listed below, including trophies added by other mods or future game updates. " +
                "Set to 0 to disable selling unlisted trophies. Negative values are treated as 0.",
                synced: true);
            TradingCatalog.Sell("UnlistedTrophy", "Остальные трофеи / Unlisted trophies", "General",
                DefaultTrophySellPriceConfig);
            WatchSellPriceEntry(DefaultTrophySellPriceConfig);

            // Meadows.
            BindTrophySellPrice("Sell Prices - Meadows", "TrophyDeer", "Deer Trophy", 25);
            BindTrophySellPrice("Sell Prices - Meadows", "TrophyBoar", "Boar Trophy", 25);
            BindTrophySellPrice("Sell Prices - Meadows", "TrophyNeck", "Neck Trophy", 25);
            BindTrophySellPrice("Sell Prices - Meadows", "TrophyEikthyr", "Eikthyr Trophy", 350);

            // Black Forest.
            BindTrophySellPrice("Sell Prices - Black Forest", "TrophyGreydwarf", "Greydwarf Trophy", 35);
            BindTrophySellPrice("Sell Prices - Black Forest", "TrophySkeleton", "Skeleton Trophy", 35);
            BindTrophySellPrice("Sell Prices - Black Forest", "TrophyGreydwarfShaman", "Greydwarf Shaman Trophy", 45);
            BindTrophySellPrice("Sell Prices - Black Forest", "TrophyGhost", "Ghost Trophy", 50);
            BindTrophySellPrice("Sell Prices - Black Forest", "TrophyGreydwarfBrute", "Greydwarf Brute Trophy", 50);
            BindTrophySellPrice("Sell Prices - Black Forest", "TrophyBjorn", "Bear Trophy", 80);
            BindTrophySellPrice("Sell Prices - Black Forest", "TrophyFrostTroll", "Troll Trophy", 80);
            BindTrophySellPriceAlias("TrophyForestTroll", "TrophyFrostTroll");
            BindTrophySellPrice("Sell Prices - Black Forest", "TrophySkeletonPoison", "Rancid Remains Trophy", 100);
            BindTrophySellPrice("Sell Prices - Black Forest", "TrophySkeletonHildir", "Brenna Trophy", 100);
            BindTrophySellPrice("Sell Prices - Black Forest", "TrophyTheElder", "The Elder Trophy", 650);

            // Ocean.
            BindTrophySellPrice("Sell Prices - Ocean", "TrophySerpent", "Serpent Trophy", 2500);
            BindTrophySellPrice("Sell Prices - Ocean", "TrophyBonemawSerpent", "Bonemaw Trophy", 2500);

            // Swamp.
            BindTrophySellPrice("Sell Prices - Swamp", "TrophyBlob", "Blob Trophy", 35);
            BindTrophySellPrice("Sell Prices - Swamp", "TrophyDraugr", "Draugr Trophy", 55);
            BindTrophySellPriceAlias("TrophyDraugrFem", "TrophyDraugr");
            BindTrophySellPrice("Sell Prices - Swamp", "TrophyLeech", "Leech Trophy", 55);
            BindTrophySellPrice("Sell Prices - Swamp", "TrophySurtling", "Surtling Trophy", 35);
            BindTrophySellPrice("Sell Prices - Swamp", "TrophyDraugrElite", "Draugr Elite Trophy", 60);
            BindTrophySellPrice("Sell Prices - Swamp", "TrophyWraith", "Wraith Trophy", 70);
            BindTrophySellPrice("Sell Prices - Swamp", "TrophyAbomination", "Abomination Trophy", 120);
            BindTrophySellPrice("Sell Prices - Swamp", "TrophyKvastur", "Kvastur Trophy", 120);
            BindTrophySellPrice("Sell Prices - Swamp", "TrophyBonemass", "Bonemass Trophy", 950);

            // Mountains.
            BindTrophySellPrice("Sell Prices - Mountains", "TrophyWolf", "Wolf Trophy", 35);
            BindTrophySellPrice("Sell Prices - Mountains", "TrophyHatchling", "Drake Trophy", 70);
            BindTrophySellPrice("Sell Prices - Mountains", "TrophyUlv", "Ulv Trophy", 80);
            BindTrophySellPrice("Sell Prices - Mountains", "TrophyFenring", "Fenring Trophy", 80);
            BindTrophySellPrice("Sell Prices - Mountains", "TrophyCultist", "Cultist Trophy", 150);
            BindTrophySellPrice("Sell Prices - Mountains", "TrophySGolem", "Stone Golem Trophy", 250);
            BindTrophySellPrice("Sell Prices - Mountains", "TrophyCultist_Hildir", "Geirrhafa Trophy", 250);
            BindTrophySellPrice("Sell Prices - Mountains", "TrophyDragonQueen", "Moder Trophy", 1200);

            // Plains.
            BindTrophySellPrice("Sell Prices - Plains", "TrophyDeathsquito", "Deathsquito Trophy", 50);
            BindTrophySellPrice("Sell Prices - Plains", "TrophyGoblin", "Fuling Trophy", 50);
            BindTrophySellPrice("Sell Prices - Plains", "TrophyGrowth", "Growth Trophy", 50);
            BindTrophySellPrice("Sell Prices - Plains", "TrophyBjornUndead", "Vile Trophy", 150);
            BindTrophySellPrice("Sell Prices - Plains", "TrophyLox", "Lox Trophy", 120);
            BindTrophySellPrice("Sell Prices - Plains", "TrophyGoblinShaman", "Fuling Shaman Trophy", 80);
            BindTrophySellPrice("Sell Prices - Plains", "TrophyGoblinBrute", "Fuling Berserker Trophy", 150);
            BindTrophySellPrice("Sell Prices - Plains", "TrophyGoblinBruteBrosShaman", "Zil Trophy", 250);
            BindTrophySellPrice("Sell Prices - Plains", "TrophyGoblinBruteBrosBrute", "Thungr Trophy", 250);
            BindTrophySellPrice("Sell Prices - Plains", "TrophyGoblinKing", "Yagluth Trophy", 1500);

            // Mistlands.
            BindTrophySellPrice("Sell Prices - Mistlands", "TrophySeeker", "Seeker Trophy", 65);
            BindTrophySellPrice("Sell Prices - Mistlands", "TrophyTick", "Tick Trophy", 35);
            BindTrophySellPrice("Sell Prices - Mistlands", "TrophyDvergr", "Dvergr Trophy", 65);
            BindTrophySellPrice("Sell Prices - Mistlands", "TrophyHare", "Hare Trophy", 35);
            BindTrophySellPrice("Sell Prices - Mistlands", "TrophyGjall", "Gjall Trophy", 200);
            BindTrophySellPrice("Sell Prices - Mistlands", "TrophySeekerBrute", "Seeker Soldier Trophy", 250);
            BindTrophySellPrice("Sell Prices - Mistlands", "TrophySeekerQueen", "The Queen Trophy", 3000);

            // Ashlands.
            BindTrophySellPrice("Sell Prices - Ashlands", "TrophyCharredArcher", "Marksman Trophy", 80);
            BindTrophySellPrice("Sell Prices - Ashlands", "TrophyVolture", "Volture Trophy", 120);
            BindTrophySellPrice("Sell Prices - Ashlands", "TrophyAsksvin", "Asksvin Trophy", 120);
            BindTrophySellPrice("Sell Prices - Ashlands", "TrophyCharredMage", "Warlock Trophy", 120);
            BindTrophySellPrice("Sell Prices - Ashlands", "TrophyCharredMelee", "Warrior Trophy", 120);
            BindTrophySellPrice("Sell Prices - Ashlands", "TrophyBlob_Lava", "Lava Blob Trophy", 80);
            BindTrophySellPrice("Sell Prices - Ashlands", "TrophyMorgen", "Morgen Trophy", 400);
            BindTrophySellPrice("Sell Prices - Ashlands", "TrophyFallenValkyrie", "Fallen Valkyrie Trophy", 500);
            BindTrophySellPrice("Sell Prices - Ashlands", "TrophyFader", "Fader Trophy", 5000);

            // Deep North.
            BindTrophySellPrice("Sell Prices - Deep North", "TrophyBarka", "Barka Trophy", 800);
            BindTrophySellPrice("Sell Prices - Deep North", "TrophyElaking", "Elaking Trophy", 300);
            BindTrophySellPrice("Sell Prices - Deep North", "TrophyMole", "Eyeless One Trophy", 350);
            BindTrophySellPrice("Sell Prices - Deep North", "TrophyBlob_Frost", "Frost Blob Trophy", 500);
            BindTrophySellPrice("Sell Prices - Deep North", "TrophyJotunWitch", "Hexen Trophy", 800);
            BindTrophySellPrice("Sell Prices - Deep North", "TrophyJotunWarrior", "Krigen Trophy", 500);
            BindTrophySellPrice("Sell Prices - Deep North", "TrophyMoose", "Moose Trophy", 300);
            BindTrophySellPrice("Sell Prices - Deep North", "TrophyBlob_Morkhalla", "Pulp Trophy", 200);
            BindTrophySellPrice("Sell Prices - Deep North", "TrophySeal", "Seal Trophy", 200);
            BindTrophySellPrice("Sell Prices - Deep North", "TrophyWrithan", "Writhan Trophy", 300);
            BindTrophySellPrice("Sell Prices - Deep North", "TrophyDeerWhite", "White Deer Trophy", 400);

            // The three non-trophy items that HaldorExpansion explicitly allows Haldor to buy.
            BindExtraSellPrice("Sell Prices - Cores", "SurtlingCore", "Surtling Core", 70);
            BindExtraSellPrice("Sell Prices - Cores", "BlackCore", "Black Core", 200);
            BindExtraSellPrice("Sell Prices - Cores", "MoltenCore", "Molten Core", 350);
            BindExtraSellPrice("Sell Prices - Cores", "FrostCore", "Frost Core", 550);

            // Each vanilla idol and the normal Haldor idol sell for 300 coins per item.
            // The adamantine Haldor idol has its own 2500-coin default.
            // A value of 0 disables selling the corresponding idol.
            foreach (HaldorIdolCatalog.Entry idol in HaldorIdolCatalog.Items)
                BindExtraSellPrice("Sell Prices - Idols", idol.Id, idol.Name,
                    idol.Id == HaldorIdolCatalog.AdamantineId
                        ? HaldorIdolCatalog.AdamantineSellPrice
                        : HaldorIdolCatalog.SellPrice);
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
                ConfigKey.SanitizeConfigKey(displayName) + " Price",
                defaultPrice,
                description,
                synced: true);

            HaldorSellPrices[prefabName] = entry;
            TradingCatalog.Sell(prefabName, displayName, section, entry);
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
            ConfigurationRuntime.RequestTradeRefresh();
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
                return TrophyValueUpdater.DefaultTrophyValue;

            return DefaultTrophySellPriceConfig.Value < 0 ? 0 : DefaultTrophySellPriceConfig.Value;
        }
    }
}