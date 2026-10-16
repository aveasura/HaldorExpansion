using System;
using System.Collections.Generic;
using Jotunn.Entities;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HaldorExpansion.Features.Upgrades
{
    internal static class AdamantineLoot
    {
        internal static bool Eligible(DropTable table)
        {
            if (table?.m_drops == null) return false;
            foreach (DropTable.DropData drop in table.m_drops)
                if (IdolRegistration.IsNativeIdol(drop.m_item))
                    return true;
            return false;
        }

        internal static bool Roll(float chancePercent, float roll)
        {
            return !float.IsNaN(chancePercent) && chancePercent > 0f &&
                   (chancePercent >= 100f || roll < chancePercent / 100f);
        }

        internal static bool RandomizeDefault =>
            (AdamantineIdolRegistration.ChestChance?.Value ?? 1f) == 1f &&
            (AdamantineIdolRegistration.RandomizeDefaultChestChance?.Value ?? true);

        internal static bool TryRoll()
        {
            float chance = AdamantineIdolRegistration.ChestChance?.Value ?? 1f;
            if (float.IsNaN(chance) || chance <= 0f) return false;
            if (RandomizeDefault) chance = Random.Range(1f, 2f);
            return Roll(chance, Random.value);
        }

        internal static void AppendItems(DropTable table, List<ItemDrop.ItemData> result)
        {
            if (AdamantineIdolRegistration.ItemDrop == null || result == null || !Eligible(table) ||
                IdolContainerScopePatch.Current?.m_defaultItems != table) return;
            // Invoked after the ordinary idol's independent sample has ended, exactly once.
            // Vanilla's owner-only default-items guard handles persistence and reopening.
            if (!TryRoll()) return;
            ItemDrop.ItemData item = AdamantineIdolRegistration.ItemDrop.m_itemData.Clone();
            item.m_dropPrefab = AdamantineIdolRegistration.Prefab;
            item.m_stack = IdolLootAmount.Roll(2);
            item.m_worldLevel = (byte)Game.m_worldLevel;
            if (item.m_stack > 0) result.Add(item);
        }
    }

    internal sealed class AdamantineLootTestCommand : ConsoleCommand
    {
        public override string Name => "he_adamantinetest";

        public override string Help =>
            "[HE TEST] he_adamantinetest [1..1000000 rolls] [chest prefab filter]; no items spawned.";

        public override void Run(string[] args, Terminal terminal)
        {
            int rolls = 100000;
            if (args.Length > 2 ||
                (args.Length > 0 && (!int.TryParse(args[0], out rolls) || rolls < 1 || rolls > 1000000)))
            {
                terminal?.AddString(Help);
                return;
            }

            if (ZNetScene.instance == null || AdamantineIdolRegistration.Prefab == null)
            {
                terminal?.AddString("[HE Adamantine Test] Enter a world first.");
                return;
            }

            string filter = args.Length > 1 ? args[1] : "";
            var state = Random.state;
            try
            {
                string chance = AdamantineLoot.RandomizeDefault
                    ? "random 1..2"
                    : (AdamantineIdolRegistration.ChestChance?.Value ?? 1f).ToString();
                int hits = 0, sources = 0;
                for (int i = 0; i < rolls; i++)
                    if (AdamantineLoot.TryRoll())
                        hits++;
                var visited = new HashSet<GameObject>();
                foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
                {
                    if (prefab == null || !visited.Add(prefab) ||
                        prefab.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (!AdamantineLoot.Eligible(prefab.GetComponent<Container>()?.m_defaultItems)) continue;
                    sources++;
                    terminal?.AddString("[HE Adamantine Test] Eligible chest: " + prefab.name);
                }

                terminal?.AddString(
                    $"[HE Adamantine Test] {hits}/{rolls} = {100.0 * hits / rolls:F3}%; configured={chance}%; eligible chests={sources}. No items spawned; random state restored.");
            }
            finally
            {
                Random.state = state;
            }
        }
    }
}