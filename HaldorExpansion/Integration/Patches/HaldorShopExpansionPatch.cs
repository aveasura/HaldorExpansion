using System.Collections.Generic;
using System.Reflection;
using HaldorExpansion.Trading;
using HarmonyLib;

namespace HaldorExpansion.Patches
{
    [HarmonyPatch]
    internal static class HaldorShopExpansionPatch
    {
        static MethodBase TargetMethod()
        {
            MethodInfo fallback = null;

            MethodInfo[] methods =
                typeof(Trader).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (MethodInfo method in methods)
            {
                if (method.Name != "Interact")
                    continue;

                if (fallback == null)
                    fallback = method;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length >= 1 && typeof(Humanoid).IsAssignableFrom(parameters[0].ParameterType))
                    return method;
            }

            return fallback;
        }

        static void Prefix(Trader __instance) => Refresh(__instance);

        internal static void Refresh(Trader __instance, bool applyTrophyValues = true)
        {
            if (!HaldorShopUpdater.IsHaldor(__instance))
                return;

            // Refresh trophy/core sell values from the currently active (server-synced) config
            // before the store UI is opened. SettingChanged also refreshes live changes.
            // The live configuration path already applied these values before refreshing the shop.
            if (applyTrophyValues)
                TrophyValueUpdater.ApplyTrophyValues(ObjectDB.instance);

            if (__instance.m_items == null)
                __instance.m_items = new List<Trader.TradeItem>();

            HaldorShopUpdater.RemoveDisabledItems(__instance);

            HaldorShopUpdater.TryAddConfiguredItems(__instance);

            // accessories
            HaldorShopUpdater.TryAddRing(__instance);
            HaldorShopUpdater.TryAddNornThread(__instance);

            // cloaks
            HaldorShopUpdater.TryAddStaminaCape(__instance);
            HaldorShopUpdater.TryAddWoundedBeastCape(__instance);
            HaldorShopUpdater.TryAddPeltOfHelheim(__instance);
            HaldorShopUpdater.TryAddBerserkerCloak(__instance);

            // armors
            HaldorShopUpdater.TryAddBerserkerHelmet(__instance);
            HaldorShopUpdater.TryAddDelayedDoomChest(__instance);
            HaldorShopUpdater.TryAddPitKingChest(__instance);

            // weapons
            HaldorShopUpdater.TryAddBerserkerAxe(__instance);
            HaldorShopUpdater.TryAddCestus(__instance);
            HaldorShopUpdater.TryAddShadowCrossbow(__instance);
            HaldorShopUpdater.TryAddHelOath(__instance);

            HaldorShopUpdater.RestoreOrder(__instance);
        }
    }
}