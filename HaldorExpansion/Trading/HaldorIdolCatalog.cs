using System;
using System.Collections.Generic;

namespace HaldorExpansion.Trading
{
    // The 16 vanilla Forge of Potential idols and our separate Haldor idol.
    // Keep this in the trading layer: idol upgrades/loot are not changed by the shop.
    internal static class HaldorIdolCatalog
    {
        internal const string CustomId = "aveasura_he_idol";
        internal const int BuyPrice = 1000;
        internal const int SellPrice = 300;

        internal sealed class Entry
        {
            internal readonly string Id;
            internal readonly string Name;
            internal readonly string DisplayLabel;

            internal Entry(string id, string englishName, string russianName)
            {
                Id = id;
                Name = englishName;
                DisplayLabel = russianName + " / " + englishName;
            }
        }

        internal static readonly Entry[] Items =
        {
            new Entry("Upgrader0Weapon", "Wooden Battle Idol", "Деревянный идол битвы"),
            new Entry("Upgrader0Armor", "Wooden Protection Idol", "Деревянный идол защиты"),
            new Entry("Upgrader1Weapon", "Bronze Battle Idol", "Бронзовый идол битвы"),
            new Entry("Upgrader1Armor", "Bronze Protection Idol", "Бронзовый идол защиты"),
            new Entry("Upgrader2Weapon", "Iron Battle Idol", "Железный идол битвы"),
            new Entry("Upgrader2Armor", "Iron Protection Idol", "Железный идол защиты"),
            new Entry("Upgrader3Weapon", "Silver Battle Idol", "Серебряный идол битвы"),
            new Entry("Upgrader3Armor", "Silver Protection Idol", "Серебряный идол защиты"),
            new Entry("Upgrader4Weapon", "Black Metal Battle Idol", "Идол битвы из чёрного металла"),
            new Entry("Upgrader4Armor", "Black Metal Protection Idol", "Идол защиты из чёрного металла"),
            new Entry("Upgrader5Weapon", "Black Marble Battle Idol", "Идол битвы из чёрного мрамора"),
            new Entry("Upgrader5Armor", "Black Marble Protection Idol", "Идол защиты из чёрного мрамора"),
            new Entry("Upgrader6Weapon", "Flametal Battle Idol", "Пламенно-металлический идол битвы"),
            new Entry("Upgrader6Armor", "Flametal Protection Idol", "Пламенно-металлический идол защиты"),
            new Entry("Upgrader7Weapon", "Bloodgold Battle Idol", "Идол битвы из кровавого золота"),
            new Entry("Upgrader7Armor", "Bloodgold Protection Idol", "Идол защиты из кровавого золота"),
            new Entry(CustomId, "Haldor Idol", "Идол Хальдора")
        };

        private static readonly HashSet<string> Ids = BuildIds();

        private static HashSet<string> BuildIds()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (Entry item in Items) ids.Add(item.Id);
            return ids;
        }

        internal static bool IsIdol(string prefabName) => prefabName != null && Ids.Contains(prefabName);

        internal static string Label(string prefabName)
        {
            foreach (Entry item in Items)
                if (item.Id == prefabName)
                    return item.DisplayLabel;
            return null;
        }
    }
}