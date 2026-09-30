using HaldorExpansion.Features.Cestus;
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

            if (!CestusRuntime.IsCestusInputLocked(player))
                return true;

            CestusAbility.ClearHumanoidAttackState(player);
            CestusAbility.ClearHumanoidBlockState(player);

            __result = false;
            return false;
        }
    }
}