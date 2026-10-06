using System;
using HarmonyLib;

namespace HaldorExpansion.Features.Berserker
{
    [HarmonyPatch(typeof(Player), "Update")]
    internal static class BerserkerPlayerUpdatePatch
    {
        private static void Postfix(Player __instance) => BerserkerRuntime.Update(__instance);
    }

    [HarmonyPatch(typeof(Attack), "DoMeleeAttack")]
    internal static class BerserkerMeleeAttackScopePatch
    {
        private static void Prefix(Attack __instance) => BerserkerAttackScope.Enter(__instance);

        private static Exception Finalizer(Exception __exception)
        {
            BerserkerAttackScope.Exit();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Attack), "DoAreaAttack")]
    internal static class BerserkerAreaAttackScopePatch
    {
        private static void Prefix(Attack __instance) => BerserkerAttackScope.Enter(__instance);

        private static Exception Finalizer(Exception __exception)
        {
            BerserkerAttackScope.Exit();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Character), "Damage")]
    internal static class BerserkerAxeHitPatch
    {
        private static void Prefix(Character __instance, HitData hit)
            => BerserkerAttackScope.TryCredit(__instance, hit);
    }
}

namespace HaldorExpansion.Features.Berserker
{
    [HarmonyPatch(typeof(Hud), "Update")]
    internal static class BerserkerDebtHudPatch
    {
        private static void Postfix(Hud __instance)
            => BerserkerDebtHudView.Update(__instance, Player.m_localPlayer);
    }
}
