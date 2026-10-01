using HaldorExpansion.Features.HelOath;
using HaldorExpansion.Features.Brisingamen;
using HaldorExpansion.Features.NornThread;
using HaldorExpansion.Features.Cestus;
using HaldorExpansion.Features.DelayedDoom;
using HaldorExpansion.Features.PitKing;
using HaldorExpansion.Features.PeltOfHelheim;
using HaldorExpansion.Features.ShadowCrossbow;
using HaldorExpansion.Features.WoundedBeast;

namespace HaldorExpansion.Integration
{
    internal static class ItemCacheReset
    {

        internal static void ResetItemCaches()
        {
            HelOathItemRegistration.HelOathPrefab = null;
            HelOathItemRegistration.HelOathItemDrop = null;
            BrisingamenItemRegistration.RingPrefab = null;
            BrisingamenItemRegistration.RingItemDrop = null;
            NornThreadItemRegistration.Prefab = null;
            NornThreadItemRegistration.NornThreadItemDrop = null;

            DelayedDoomItemRegistration.DelayedDoomChestPrefab = null;
            DelayedDoomItemRegistration.DelayedDoomChestItemDrop = null;

            PitKingItemRegistration.PitKingChestPrefab = null;
            PitKingItemRegistration.PitKingChestItemDrop = null;

            CestusItemRegistration.GritCestusPrefab = null;
            CestusItemRegistration.GritCestusItemDrop = null;

            ShadowCrossbowItemRegistration.ShadowCrossbowPrefab = null;
            ShadowCrossbowItemRegistration.ShadowCrossbowItemDrop = null;

            WoundedBeastRuntime.ResetWoundedBeastCapeState();
            PeltOfHelheimItemRegistration.ResetCaches();
            PeltOfHelheimStatus.ResetRegistration();

            BrisingamenItemRegistration._ringReadyLogged = false;
            NornThreadItemRegistration._readyLogged = false;
        }
    }
}
