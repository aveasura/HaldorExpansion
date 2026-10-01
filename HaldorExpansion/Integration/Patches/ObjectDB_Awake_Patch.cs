using HaldorExpansion.Features.HelOath;
using HaldorExpansion.Features.Brisingamen;
using HaldorExpansion.Features.NornThread;
using HaldorExpansion.Features.Cestus;
using HaldorExpansion.Features.DelayedDoom;
using HaldorExpansion.Features.PitKing;
using HaldorExpansion.Features.PeltOfHelheim;
using HaldorExpansion.Features.ShadowCrossbow;
using HaldorExpansion.Features.WoundedBeast;
using HaldorExpansion.Integration;
using HaldorExpansion.Trading;
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
            ItemCacheReset.ResetItemCaches();

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

            BrisingamenItemRegistration.EnsureRingReady();
            NornThreadItemRegistration.EnsureReady();
            DelayedDoomItemRegistration.EnsureDelayedDoomChestReady();
            PitKingItemRegistration.EnsurePitKingChestReady();
            CestusItemRegistration.EnsureCestusReady();
            ShadowCrossbowItemRegistration.EnsureShadowCrossbowReady();
            HelOathItemRegistration.EnsureHelOathReady();
            HelOathTouchStatus.EnsureRegistered();
            WoundedBeastItemRegistration.EnsureWoundedBeastCapeReady();
            PeltOfHelheimItemRegistration.EnsureReady();
            PeltOfHelheimStatus.EnsureRegistered();

            ShadowCrossbowItemRegistration.RegisterShadowCrossbowRecipe();
            HelOathItemRegistration.RegisterHelOathRecipe();
            CestusItemRegistration.RegisterCestusRecipe();
            DelayedDoomItemRegistration.RegisterDelayedDoomChestRecipe();
            PitKingItemRegistration.RegisterPitKingChestRecipe();
            WoundedBeastItemRegistration.RegisterWoundedBeastCapeRecipe();
            PeltOfHelheimItemRegistration.RegisterRecipe();

            TrophyValueUpdater.ApplyTrophyValues(ObjectDB.instance);
        }
    }
}