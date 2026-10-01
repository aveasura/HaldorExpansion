using HaldorExpansion.Features.Cestus;
using HarmonyLib;

namespace HaldorExpansion.Patches
{
    [HarmonyPatch(typeof(Player), "GetAdrenaline")]
    internal static class CestusAdrenalineValuePatch
    {
        static void Postfix(Player __instance, ref float __result)
        {
            if (__instance == null || __instance != Player.m_localPlayer)
                return;

            // Когда игрок убирает Кастет, бонус +30 HP может на мгновение
            // подпитывать внутренний путь адреналина Вальхейма через потерю здоровья.
            // Принудительно обнулить текущий и максимальный уровень адреналина во время этого перехода.
            if (CestusRuntime.ShouldForceHideCestusHudBar(__instance))
            {
                __result = 0f;
                return;
            }

            if (!CestusRuntime.ShouldMirrorCestusToAdrenaline(__instance))
                return;

            __result = CestusChargeRuntime.GetCestusCharge(__instance);
        }
    }

    [HarmonyPatch(typeof(Player), "GetMaxAdrenaline")]
    internal static class CestusAdrenalineMaxPatch
    {
        static void Postfix(Player __instance, ref float __result)
        {
            if (__instance == null || __instance != Player.m_localPlayer)
                return;

            if (CestusRuntime.ShouldForceHideCestusHudBar(__instance))
            {
                __result = 0f;
                return;
            }

            if (!CestusRuntime.ShouldMirrorCestusToAdrenaline(__instance))
                return;

            __result = CestusChargeRuntime.CestusChargeMax;
        }
    }
}