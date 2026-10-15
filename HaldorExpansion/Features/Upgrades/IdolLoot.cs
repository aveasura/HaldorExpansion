using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaldorExpansion.Features.Upgrades
{
    internal static class IdolLoot
    {
        [ThreadStatic] private static bool sampling;
        private static bool failureLogged;

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

            // The game's pseudo-random counter storage changed in Valheim 1.0.20.
            // Roll a copy of each native idol entry under our OWN prefab name instead of
            // replacing the game's counter dictionary. The native generator handles the
            // actual chance/level/pseudo-random rules and keeps the two item sequences
            // independent (they have distinct prefab names).
            var shadowDrops = new List<CharacterDrop.Drop>(idols.Count);
            foreach (CharacterDrop.Drop drop in idols)
            {
                shadowDrops.Add(new CharacterDrop.Drop
                {
                    m_prefab = IdolRegistration.Prefab,
                    m_amountMin = drop.m_amountMin,
                    m_amountMax = drop.m_amountMax,
                    m_chance = drop.m_chance,
                    m_onePerPlayer = drop.m_onePerPlayer,
                    m_levelMultiplier = drop.m_levelMultiplier,
                    m_dontScale = drop.m_dontScale
                });
            }

            var drops = source.m_drops;
            var result = new List<KeyValuePair<GameObject, int>>(original);
            try
            {
                sampling = true;
                source.m_drops = shadowDrops;
                foreach (var drop in source.GenerateDropList())
                {
                    if (drop.Value <= 0 || drop.Key != IdolRegistration.Prefab) continue;
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