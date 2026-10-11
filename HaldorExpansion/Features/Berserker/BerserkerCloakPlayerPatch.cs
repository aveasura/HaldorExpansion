using HarmonyLib;
using System.Collections.Generic;

namespace HaldorExpansion.Features.Berserker
{
    // Bonus affects sprinting only; normal jogging/walking, swimming, and mounts are unchanged.
    [HarmonyPatch(typeof(Player), "GetRunSpeedFactor")]
    internal static class BerserkerCloakRunSpeedPatch
    {
        private static void Postfix(Player __instance, ref float __result)
        {
            if (__instance == null || __instance != Player.m_localPlayer) return;
            __result *= 1f + BerserkerCloakRuntime.RunSpeedBonusFraction(__instance);
        }
    }

    [HarmonyPatch(typeof(Player), "Update")]
    internal static class BerserkerCloakPlayerPatch
    {
        private static readonly HashSet<int> ProcessedPlayers = new HashSet<int>();

        private static void Postfix(Player __instance)
        {
            if (__instance == null || ObjectDB.instance == null) return;

            int id = __instance.GetInstanceID();
            if (ProcessedPlayers.Contains(id)) return;

            BerserkerCloakInventoryRebind.FixItemsInInventory(__instance);
            ProcessedPlayers.Add(id);
        }
    }
}
