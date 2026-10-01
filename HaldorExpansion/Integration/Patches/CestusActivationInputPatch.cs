using HaldorExpansion.Features.Cestus;
using System;
using System.Reflection;
using HarmonyLib;

namespace HaldorExpansion.Patches
{
    [HarmonyPatch(typeof(Player), "Update")]
    internal static class CestusActivationInputPatch
    {
        private static readonly Type ZInputType = AccessTools.TypeByName("ZInput");
        private static readonly MethodInfo GetButtonDownMethod =
            ZInputType != null
                ? AccessTools.Method(ZInputType, "GetButtonDown", new[] { typeof(string) })
                : null;

        private static bool IsCestusActivationPressed()
        {
            if (GetButtonDownMethod == null)
                return false;

            if (CestusInputRegistration.CestusAbilityButton == null)
                return false;

            object result = GetButtonDownMethod.Invoke(
                null,
                new object[] { CestusInputRegistration.CestusAbilityButton.Name });

            return result is bool pressed && pressed;
        }

        private static bool ShouldPoll(Player player)
        {
            if (player == null || Player.m_localPlayer == null || player != Player.m_localPlayer)
                return false;

            if (CestusInputRegistration.CestusAbilityButton == null)
                return false;

            if (!CestusEquipment.HasCestusEquipped(player))
                return false;

            if (!CestusChargeRuntime.HasFullCestusCharge(player))
                return false;

            if (CestusRuntime.IsCestusCasting(player))
                return false;

            if (CestusShieldRuntime.IsCestusShieldActive(player))
                return false;

            return true;
        }

        static void Prefix(Player __instance)
        {
            CestusRuntime.UpdateCestusLocalEquipState(__instance);
            CestusChargeRuntime.UpdateCestusLocalDamageCharge(__instance);

            if (!ShouldPoll(__instance))
                return;

            if (!IsCestusActivationPressed())
                return;

            if (CestusAbility.TryActivateCestusShield(__instance))
            {
                CestusAbility.ClearHumanoidBlockState(__instance);
            }
        }
    }
}