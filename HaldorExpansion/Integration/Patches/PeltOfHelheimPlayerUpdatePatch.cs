using System.Collections.Generic;
using HaldorExpansion.Features.PeltOfHelheim;
using HarmonyLib;

namespace HaldorExpansion.Patches
{
    [HarmonyPatch(typeof(Player), "Update")]
    internal static class PeltOfHelheimPlayerUpdatePatch
    {
        private static readonly HashSet<int> ProcessedPlayers = new HashSet<int>();

        private static void Postfix(Player __instance)
        {
            if (__instance == null || ObjectDB.instance == null) return;

            int id = __instance.GetInstanceID();
            if (!ProcessedPlayers.Contains(id))
            {
                PeltOfHelheimInventoryRebind.FixItemsInInventory(__instance);
                ProcessedPlayers.Add(id);
            }

            PeltOfHelheimRuntime.Update(__instance);
        }
    }
}
