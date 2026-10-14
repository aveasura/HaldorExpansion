using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaldorExpansion.Features.Upgrades
{
    internal static class IdolLoot
    {
        [ThreadStatic] private static bool sampling;
        private static bool failureLogged;

        private static readonly Dictionary<string, Tuple<float, int>> ShadowCounters =
            new Dictionary<string, Tuple<float, int>>();

        private static bool Eligible(DropTable table)
        {
            if (sampling || IdolRegistration.Prefab == null || table?.m_drops == null) return false;
            foreach (DropTable.DropData drop in table.m_drops)
                if (IdolRegistration.IsNativeIdol(drop.m_item))
                    return true;
            return false;
        }

        internal static List<ItemDrop.ItemData> AppendItems(DropTable table, List<ItemDrop.ItemData> original)
        {
            if (!Eligible(table)) return original;
            // Native GetDropListItems reuses a static buffer. Preserve it BEFORE sampling again.
            var result = new List<ItemDrop.ItemData>(original);
            try
            {
                sampling = true;
                foreach (ItemDrop.ItemData item in table.GetDropListItems())
                {
                    if (item.m_stack <= 0 || !IdolRegistration.IsNativeIdol(item.m_dropPrefab)) continue;
                    ItemDrop.ItemData idol = IdolRegistration.ItemDrop.m_itemData.Clone();
                    idol.m_dropPrefab = IdolRegistration.Prefab;
                    idol.m_stack = 1;
                    idol.m_worldLevel = (byte)Game.m_worldLevel;
                    result.Add(idol);
                    break;
                }
            }
            catch (Exception e)
            {
                LogFailure(e);
            }
            finally
            {
                sampling = false;
            }

            AdamantineLoot.AppendItems(table, result);
            return result;
        }

        internal static List<GameObject> AppendObjects(DropTable table, List<GameObject> original)
        {
            if (!Eligible(table)) return original;
            var result = new List<GameObject>(original);
            try
            {
                sampling = true;
                foreach (GameObject item in table.GetDropList())
                {
                    if (!IdolRegistration.IsNativeIdol(item)) continue;
                    result.Add(IdolRegistration.Prefab);
                    break;
                }
            }
            catch (Exception e)
            {
                LogFailure(e);
            }
            finally
            {
                sampling = false;
            }

            return result;
        }

        internal static List<KeyValuePair<GameObject, int>> AppendCharacter(CharacterDrop source,
            List<KeyValuePair<GameObject, int>> original)
        {
            if (sampling || IdolRegistration.Prefab == null || source?.m_drops == null) return original;
            var idols = new List<CharacterDrop.Drop>();
            foreach (CharacterDrop.Drop drop in source.m_drops)
                if (drop != null && IdolRegistration.IsNativeIdol(drop.m_prefab))
                    idols.Add(drop);
            if (idols.Count == 0) return original;

            // Creature loot uses pseudo-drop counters, not DropTable. Keep a separate sequence
            // so the independent draw neither consumes nor observes the vanilla idol counters.
            var drops = source.m_drops;
            var counters = CharacterDrop.s_pseudoCounter;
            var result = new List<KeyValuePair<GameObject, int>>(original);
            try
            {
                sampling = true;
                source.m_drops = idols;
                CharacterDrop.s_pseudoCounter = ShadowCounters;
                foreach (var drop in source.GenerateDropList())
                {
                    if (drop.Value <= 0 || !IdolRegistration.IsNativeIdol(drop.Key)) continue;
                    result.Add(new KeyValuePair<GameObject, int>(IdolRegistration.Prefab, 1));
                    break;
                }
            }
            catch (Exception e)
            {
                LogFailure(e);
            }
            finally
            {
                CharacterDrop.s_pseudoCounter = counters;
                source.m_drops = drops;
                sampling = false;
            }

            return result;
        }

        private static void LogFailure(Exception e)
        {
            if (failureLogged) return;
            failureLogged = true;
            HaldorExpansionPlugin.Log?.LogWarning(
                $"[Upgrades] Independent idol roll failed; original loot retained: {e}");
        }
    }
}