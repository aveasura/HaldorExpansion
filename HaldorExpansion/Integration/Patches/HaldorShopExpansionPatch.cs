using HaldorExpansion.Features.HelOath;
using HaldorExpansion.Trading;
using System.Collections.Generic;
using System.Reflection;
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

        static void Prefix(Trader __instance)
        {
            if (!HaldorShopUpdater.IsHaldor(__instance))
                return;

            // Refresh trophy/core sell values from the currently active (server-synced) config
            // before the store UI is opened. SettingChanged also refreshes live changes.
            TrophyValueUpdater.ApplyTrophyValues(ObjectDB.instance);

            if (__instance.m_items == null)
                __instance.m_items = new List<Trader.TradeItem>();

            // добавляет Хальдору обычные предметы из массива ShopItems
            HaldorShopUpdater.TryAddConfiguredItems(__instance);
            
            // Аксессуары
            HaldorShopUpdater.TryAddRing(__instance);
            HaldorShopUpdater.TryAddNornThread(__instance);
            
            // Плащи
            HaldorShopUpdater.TryAddWoundedBeastCape(__instance);
            
            // Доспехи
            HaldorShopUpdater.TryAddDelayedDoomChest(__instance);
            HaldorShopUpdater.TryAddPitKingChest(__instance);
            
            // Оружие
            HaldorShopUpdater.TryAddCestus(__instance);
            HaldorShopUpdater.TryAddShadowCrossbow(__instance);
            HaldorShopUpdater.TryAddHelOath(__instance);
        }
    }
}