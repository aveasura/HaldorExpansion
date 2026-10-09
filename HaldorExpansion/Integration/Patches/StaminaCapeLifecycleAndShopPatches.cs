using System.Collections.Generic;
using HaldorExpansion.Features.StaminaCape;
using HarmonyLib;

namespace HaldorExpansion
{
    [HarmonyPatch(typeof(Player), "Update")]
    internal static class StaminaCapeInventoryFixPatch
    {
        private static readonly HashSet<int> ProcessedPlayers = new HashSet<int>();

        static void Postfix(Player __instance)
        {
            if (__instance == null || ObjectDB.instance == null)
                return;

            int id = __instance.GetInstanceID();
            if (ProcessedPlayers.Contains(id))
                return;

            StaminaCapeInventoryRebind.FixItemsInInventory(__instance);
            ProcessedPlayers.Add(id);
        }
    }
}