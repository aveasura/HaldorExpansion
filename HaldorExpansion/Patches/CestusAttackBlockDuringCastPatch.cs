using HarmonyLib;

namespace HaldorExpansion.Patches
{
    [HarmonyPatch(typeof(Humanoid), "StartAttack")]
    internal static class CestusAttackBlockDuringCastPatch
    {
        static bool Prefix(Humanoid __instance, ref bool __result)
        {
            if (!(__instance is Player player))
                return true;

            if (Player.m_localPlayer == null || player != Player.m_localPlayer)
                return true;

            if (!HaldorExpansionPlugin.IsCestusInputLocked(player))
                return true;

            HaldorExpansionPlugin.ClearHumanoidAttackState(player);
            HaldorExpansionPlugin.ClearHumanoidBlockState(player);

            __result = false;
            return false;
        }
    }
}