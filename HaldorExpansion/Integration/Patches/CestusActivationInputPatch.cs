using System;
using System.Reflection;
using HaldorExpansion.Features.Cestus;
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

        private static bool ShouldPoll(Player player, bool cestusEquipped)
        {
            if (player == null || Player.m_localPlayer == null || player != Player.m_localPlayer)
                return false;

            if (CestusInputRegistration.CestusAbilityButton == null)
                return false;

            if (!cestusEquipped)
                return false;

            if (!CestusChargeRuntime.HasFullCestusCharge(player))
                return false;

            if (CestusRuntime.IsCestusCasting(player) || CestusFacebreaker.IsCastLocked(player))
                return false;

            if (CestusShieldRuntime.IsCestusShieldActive(player))
                return false;

            return true;
        }

        static void Postfix(Player __instance)
        {
            // Facebreaker's lateral arm motion must stay aligned with its
            // captured strip axis even if vanilla Update tries to turn us.
            CestusFacebreaker.MaintainCastFacing(__instance);
        }

        static void Prefix(Player __instance)
        {
            // Invocation-local only: standalone/event callers still inspect live equipment.
            bool cestusEquipped = CestusRuntime.ShouldProcessCestusLocally(__instance) &&
                                  CestusEquipment.HasCestusEquipped(__instance);
            CestusRuntime.UpdateCestusLocalEquipState(__instance, cestusEquipped);
            CestusChargeRuntime.UpdateCestusLocalDamageCharge(__instance, cestusEquipped);
            CestusFacebreaker.UpdateLocal(__instance, cestusEquipped);

            if (!ShouldPoll(__instance, cestusEquipped))
                return;

            if (AbilityInteractionPriority.ShouldSuppress(__instance) || !IsCestusActivationPressed())
                return;

            if (CestusAbility.TryActivateCestusShield(__instance))
            {
                CestusAbility.ClearHumanoidBlockState(__instance);
            }
        }
    }
}