using HaldorExpansion.Features.WoundedBeast;
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
                WoundedBeastInventoryRebind.FixWoundedBeastCapeItemsInInventory(__instance);
                ProcessedPlayers.Add(id);
            }

            WoundedBeastRuntime.UpdateWoundedBeastCapeRegen(__instance);
        }
    }
}