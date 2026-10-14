using HaldorExpansion.Features.Berserker;
using HaldorExpansion.Features.Brisingamen;
using HaldorExpansion.Features.Cestus;
using HaldorExpansion.Features.DelayedDoom;
using HaldorExpansion.Features.HelOath;
using HaldorExpansion.Features.NornThread;
using HaldorExpansion.Features.PeltOfHelheim;
using HaldorExpansion.Features.PitKing;
using HaldorExpansion.Features.ShadowCrossbow;
using HaldorExpansion.Features.StaminaCape;
using HaldorExpansion.Features.Upgrades;
using HaldorExpansion.Features.WoundedBeast;
using HaldorExpansion.Integration;
using HaldorExpansion.Trading;
using HarmonyLib;
using Jotunn.Managers;

namespace HaldorExpansion.Patches
{
    [HarmonyPatch(typeof(ObjectDB), "Awake")]
    internal static class ObjectDB_Awake_Patch
    {
        private static bool initialized;

        internal static void Initialize()
        {
            if (initialized) return;
            initialized = true;
            PrefabManager.OnVanillaPrefabsAvailable += EnsureItemsReady;
            ItemManager.OnItemsRegisteredFejd += CompleteRegistration;
            ItemManager.OnItemsRegistered += CompleteRegistration;
        }

        internal static void Shutdown()
        {
            if (!initialized) return;
            PrefabManager.OnVanillaPrefabsAvailable -= EnsureItemsReady;
            ItemManager.OnItemsRegisteredFejd -= CompleteRegistration;
            ItemManager.OnItemsRegistered -= CompleteRegistration;
            initialized = false;
        }

        private static void Postfix()
        {
            ItemCacheReset.ResetItemCaches();
            // Empty menu DB is populated later by CopyOtherDB. A world DB is
            // already populated here; do not yield a frame before players load.
            CompleteRegistration();
        }

        private static void EnsureItemsReady()
        {
            BrisingamenItemRegistration.EnsureRingReady();
            NornThreadItemRegistration.EnsureReady();
            DelayedDoomItemRegistration.EnsureDelayedDoomChestReady();
            PitKingItemRegistration.EnsurePitKingChestReady();
            CestusItemRegistration.EnsureCestusReady();
            ShadowCrossbowItemRegistration.EnsureShadowCrossbowReady();
            HelOathItemRegistration.EnsureHelOathReady();
            BerserkerItemRegistration.EnsureReady();
            BerserkerCloakItemRegistration.EnsureReady();
            BerserkerHelmetItemRegistration.EnsureReady();
            WoundedBeastItemRegistration.EnsureWoundedBeastCapeReady();
            PeltOfHelheimItemRegistration.EnsureReady();

            StaminaCapeItemRegistration.EnsureReady();
        }

        private static void CompleteRegistration()
        {
            if (ObjectDB.instance?.m_items == null || ObjectDB.instance.m_items.Count == 0)
                return;

            EnsureItemsReady();
            HelOathTouchStatus.EnsureRegistered();
            BerserkerWrathStatus.EnsureRegistered();
            BerserkerRagnarokExhaustion.EnsureRegistered();
            CestusFacebreakerSlow.EnsureRegistered();
            PeltOfHelheimStatus.EnsureRegistered();
            TrophyValueUpdater.ApplyTrophyValues(ObjectDB.instance);

            // Recipes are independent of item visibility. Never block item creation
            // on this condition (the preview only needs prefabs).
            if (ObjectDB.instance.m_recipes == null || ObjectDB.instance.m_recipes.Count == 0)
                return;

            ShadowCrossbowItemRegistration.RegisterShadowCrossbowRecipe();
            HelOathItemRegistration.RegisterHelOathRecipe();
            CestusItemRegistration.RegisterCestusRecipe();
            DelayedDoomItemRegistration.RegisterDelayedDoomChestRecipe();
            PitKingItemRegistration.RegisterPitKingChestRecipe();
            WoundedBeastItemRegistration.RegisterWoundedBeastCapeRecipe();
            PeltOfHelheimItemRegistration.RegisterRecipe();
            BerserkerItemRegistration.RegisterRecipe();
            BerserkerCloakItemRegistration.RegisterRecipe();
            BerserkerHelmetItemRegistration.RegisterRecipe();

            StaminaCapeItemRegistration.RegisterRecipe();

            if (IdolRegistration.EnsureReady())
            {
                UpgradeRecipes.Register();
                AdamantineIdolRegistration.EnsureReady();
            }
        }
    }
}