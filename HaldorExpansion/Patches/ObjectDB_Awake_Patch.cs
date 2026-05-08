using System.Collections;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Patches
{
    [HarmonyPatch(typeof(ObjectDB), "Awake")]
    internal static class ObjectDB_Awake_Patch
    {
        private static void Postfix()
        {
            HaldorExpansionPlugin.ResetItemCaches();

            if (HaldorExpansionPlugin.Instance != null)
            {
                HaldorExpansionPlugin.Instance.StartCoroutine(DelayedEnsure());
            }
        }

        private static IEnumerator DelayedEnsure()
        {
            yield return null;

            yield return new WaitUntil(() =>
                ObjectDB.instance != null &&
                ObjectDB.instance.m_items != null &&
                ObjectDB.instance.m_items.Count > 0 &&
                ObjectDB.instance.m_recipes != null &&
                ObjectDB.instance.m_recipes.Count > 0);

            HaldorExpansionPlugin.EnsureRingReady();
            HaldorExpansionPlugin.EnsureDelayedDoomChestReady();
            HaldorExpansionPlugin.EnsurePitKingChestReady();
            HaldorExpansionPlugin.EnsureCestusReady();
            HaldorExpansionPlugin.EnsureShadowCrossbowReady();
            HaldorExpansionPlugin.EnsureWoundedBeastCapeReady();

            HaldorExpansionPlugin.RegisterShadowCrossbowRecipe();
            HaldorExpansionPlugin.RegisterCestusRecipe();
            HaldorExpansionPlugin.RegisterDelayedDoomChestRecipe();
            HaldorExpansionPlugin.RegisterPitKingChestRecipe();
            HaldorExpansionPlugin.RegisterWoundedBeastCapeRecipe();

            HaldorExpansionPlugin.ApplyTrophyValues(ObjectDB.instance);
        }
    }
}