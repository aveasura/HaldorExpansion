using System.Collections.Generic;
using HarmonyLib;

namespace HaldorExpansion
{
    [HarmonyPatch(typeof(Player), "Update")]
    internal static class WoundedBeastCapePlayerUpdatePatch
    {
        private static readonly HashSet<int> ProcessedPlayers = new HashSet<int>();

        static void Postfix(Player __instance)
        {
            if (__instance == null || ObjectDB.instance == null)
                return;

            int id = __instance.GetInstanceID();
            if (!ProcessedPlayers.Contains(id))
            {
                HaldorExpansionPlugin.FixWoundedBeastCapeItemsInInventory(__instance);
                ProcessedPlayers.Add(id);
            }

            HaldorExpansionPlugin.UpdateWoundedBeastCapeRegen(__instance);
        }
    }
}