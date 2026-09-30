using HaldorExpansion.Features.Cestus;
using HarmonyLib;

namespace HaldorExpansion.Patches
{
    [HarmonyPatch(typeof(Hud), "Update")]
    internal static class CestusShieldHudPatch
    {
        private static void Postfix(Hud __instance)
        {
            if (__instance == null)
                return;

            Player player = Player.m_localPlayer;
            CestusHudView.UpdateCestusShieldOverlay(__instance, player);
        }
    }
}