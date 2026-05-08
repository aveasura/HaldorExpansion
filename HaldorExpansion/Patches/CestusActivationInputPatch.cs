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

            if (HaldorExpansionPlugin.CestusAbilityButton == null)
                return false;

            object result = GetButtonDownMethod.Invoke(
                null,
                new object[] { HaldorExpansionPlugin.CestusAbilityButton.Name });

            return result is bool pressed && pressed;
        }

        private static bool ShouldPoll(Player player)
        {
            if (player == null || Player.m_localPlayer == null || player != Player.m_localPlayer)
                return false;

            if (HaldorExpansionPlugin.CestusAbilityButton == null)
                return false;

            if (!HaldorExpansionPlugin.HasCestusEquipped(player))
                return false;

            if (!HaldorExpansionPlugin.HasFullCestusCharge(player))
                return false;

            if (HaldorExpansionPlugin.IsCestusCasting(player))
                return false;

            if (HaldorExpansionPlugin.IsCestusShieldActive(player))
                return false;

            return true;
        }

        static void Prefix(Player __instance)
        {
            HaldorExpansionPlugin.UpdateCestusLocalEquipState(__instance);
            HaldorExpansionPlugin.UpdateCestusLocalDamageCharge(__instance);

            if (!ShouldPoll(__instance))
                return;

            if (!IsCestusActivationPressed())
                return;

            if (HaldorExpansionPlugin.TryActivateCestusShield(__instance))
            {
                HaldorExpansionPlugin.ClearHumanoidBlockState(__instance);
            }
        }
    }
}