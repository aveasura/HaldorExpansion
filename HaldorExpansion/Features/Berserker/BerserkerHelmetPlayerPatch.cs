using HarmonyLib;
using System.Collections.Generic;

namespace HaldorExpansion.Features.Berserker
{
    [HarmonyPatch(typeof(Player), "Update")]
    internal static class BerserkerHelmetPlayerPatch
    {
        private static readonly HashSet<int> ProcessedPlayers = new HashSet<int>();

        private static void Postfix(Player __instance)
        {
            if (__instance == null || ObjectDB.instance == null) return;

            int id = __instance.GetInstanceID();
            if (ProcessedPlayers.Contains(id)) return;

            BerserkerHelmetInventoryRebind.FixItemsInInventory(__instance);
            ProcessedPlayers.Add(id);
        }
    }
}
