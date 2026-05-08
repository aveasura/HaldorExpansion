using System.Collections.Generic;
using UnityEngine;

namespace HaldorExpansion
{
    public partial class HaldorExpansionPlugin
    {
        private const int DefaultTrophyValue = 10;
        
        // Отдельно: не трофеи, но Хальдор может купить
        private static readonly Dictionary<string, int> ExtraSellableItems = new Dictionary<string, int>
        {
            { "SurtlingCore", 30 },
            { "BlackCore", 80 },
            { "MoltenCore", 130 }
        };

        // Только трофеи
        private static readonly Dictionary<string, int> TrophyValueOverrides = new Dictionary<string, int>
        {
            // Луга
            { "TrophyDeer", 15 },
            { "TrophyBoar", 15 },
            { "TrophyNeck", 25 },
            { "TrophyEikthyr", 70 },

            // Черный лес
            { "TrophyGreydwarf", 15 },
            { "TrophySkeleton", 15 },
            { "TrophyGreydwarfShaman", 20 },
            { "TrophyGhost", 25 },
            { "TrophyGreydwarfBrute", 20 },
            { "TrophyBjorn", 50 },
            { "TrophyFrostTroll", 50 },
            { "TrophySkeletonPoison", 50 },
            { "TrophySkeletonHildir", 80 },
            { "TrophyTheElder", 100 },

            // Океан
            { "TrophySerpent", 500 },
            { "TrophyBonemawSerpent", 500 },

            // Болото
            { "TrophyBlob", 15 },
            { "TrophyDraugr", 15 },
            { "TrophyLeech", 15 },
            { "TrophySurtling", 15 },
            { "TrophyDraugrElite", 30 },
            { "TrophyWraith", 35 },
            { "TrophyAbomination", 75 },
            { "TrophyKvastur", 80 },
            { "TrophyBonemass", 150 },

            // Гора
            { "TrophyWolf", 15 },
            { "TrophyHatchling", 15 },
            { "TrophyUlv", 20 },
            { "TrophyFenring", 25 },
            { "TrophyCultist", 40 },
            { "TrophySGolem", 70 },
            { "TrophyCultist_Hildir", 80 },
            { "TrophyDragonQueen", 200 },

            // Равнины
            { "TrophyDeathsquito", 15 },
            { "TrophyGoblin", 15 },
            { "TrophyGrowth", 15 },
            { "TrophyBjornUndead", 50 },
            { "TrophyLox", 35 },
            { "TrophyGoblinShaman", 30 },
            { "TrophyGoblinBrute", 70 },
            { "TrophyGoblinBruteBrosShaman", 80 },
            { "TrophyGoblinBruteBrosBrute", 80 },
            { "TrophyGoblinKing", 350 },

            // Туманные земли
            { "TrophySeeker", 20 },
            { "TrophyTick", 20 },
            { "TrophyDvergr", 20 },
            { "TrophyHare", 20 },
            { "TrophyGjall", 70 },
            { "TrophySeekerBrute", 80 },
            { "TrophySeekerQueen", 500 },

            // Пепельные земли
            { "TrophyCharredArcher", 20 },
            { "TrophyVolture", 20 },
            { "TrophyAsksvin", 20 },
            { "TrophyCharredMage", 25 },
            { "TrophyCharredMelee", 25 },
            { "TrophyMorgen", 100 },
            { "TrophyFallenValkyrie", 120 },
            { "TrophyFader", 1000 }
        };

        internal static void ApplyTrophyValues(ObjectDB db)
        {
            if (db == null || db.m_items == null)
                return;

            foreach (GameObject go in db.m_items)
            {
                if (go == null)
                    continue;

                ItemDrop drop = go.GetComponent<ItemDrop>();
                if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null)
                    continue;

                ItemDrop.ItemData.SharedData shared = drop.m_itemData.m_shared;

                // Сначала - отдельные нетрофейные предметы
                if (ExtraSellableItems.TryGetValue(go.name, out int extraValue))
                {
                    shared.m_value = extraValue;
                    DebugLog(
                        $"[HaldorExpansion] Extra sellable item value set: prefab={go.name}, key={shared.m_name}, value={extraValue}");
                    continue;
                }

                // Дальше работаем только с трофеями
                if (shared.m_itemType != ItemDrop.ItemData.ItemType.Trophy)
                    continue;

                int value = DefaultTrophyValue;

                if (TrophyValueOverrides.TryGetValue(go.name, out int overrideValue))
                {
                    value = overrideValue;
                }

                shared.m_value = value;

                DebugLog(
                    $"[HaldorExpansion] Trophy value set: prefab={go.name}, key={shared.m_name}, value={value}");
            }
        }

        internal static bool IsSellableToHaldor(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null)
                return false;

            string prefabName = item.m_dropPrefab != null ? item.m_dropPrefab.name : null;
            if (string.IsNullOrEmpty(prefabName))
                return false;

            if (ExtraSellableItems.ContainsKey(prefabName))
                return true;

            return item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy
                   && item.m_shared.m_value > 0;
        }
    }
}