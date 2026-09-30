using HaldorExpansion.Features.StaminaCape;
using System;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Features.HelOath
{
    [HarmonyPatch(typeof(Character), "Awake")]
    internal static class HelOathNetworkPatch
    {
        private static void Postfix(Character __instance) => HelOathNetwork.Register(__instance);
    }

    [HarmonyPatch(typeof(Player), "Update")]
    internal static class HelOathUpdatePatch
    {
        private static void Prefix(Player __instance) => HelOathRuntime.Update(__instance);
        private static void Postfix(Player __instance)
        {
            HelOathEffects.UpdateWeapon(__instance);
            HelOathTouchEffects.UpdatePlayer(__instance);
        }
    }

    [HarmonyPatch(typeof(Player), "GetTotalFoodValue")]
    internal static class HelOathStatsPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Player __instance, ref float hp, ref float stamina)
        {
            if (!HelOathRuntime.Equipped(__instance)) return;
            float sacrifice = hp * 0.5f;
            hp -= sacrifice;
            stamina += sacrifice;
        }
    }

    [HarmonyPatch(typeof(Player), "UseStamina")]
    internal static class HelOathTouchStaminaPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Player __instance, ref float __0)
        {
            if (__0 <= 0f || StaminaCapeRuntime.IsForcedStaminaSpend) return;
            __0 *= HelOathRuntime.TouchStaminaMultiplier(__instance);
        }
    }

    [HarmonyPatch(typeof(Character), "ApplyDamage")]
    internal static class HelOathTouchDamageResetPatch
    {
        private struct DamageState
        {
            internal float Health;
            internal bool CombatHit;
        }

        private static void Prefix(Character __instance, HitData __0, out DamageState __state)
        {
            __state = new DamageState { Health = -1f, CombatHit = false };
            var player = __instance as Player;
            if (player == null || player != Player.m_localPlayer) return;
            __state.Health = player.GetHealth();
            Character attacker = __0 != null ? __0.GetAttacker() : null;
            // Falling and other environmental health loss have no attacker and must not punish Touch.
            __state.CombatHit = attacker != null && attacker != player;
        }

        private static void Postfix(Character __instance, DamageState __state)
        {
            var player = __instance as Player;
            if (player == null || player != Player.m_localPlayer || __state.Health < 0f || !__state.CombatHit) return;
            if (player.GetHealth() + 0.001f < __state.Health) HelOathRuntime.NotifyCombatHit(player);
        }
    }

    [HarmonyPatch(typeof(Player), "GetAdrenaline")]
    internal static class HelOathChargeHudPatch
    {
        // Cestus can hide the vanilla bar after being unequipped; run after its postfix.
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Player __instance, ref float __result)
        {
            if (__instance == Player.m_localPlayer && HelOathRuntime.Equipped(__instance))
                __result = HelOathRuntime.State.Prepared ? 100f : HelOathRuntime.State.Charge;
        }
    }

    [HarmonyPatch(typeof(Player), "GetMaxAdrenaline")]
    internal static class HelOathMaxChargeHudPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Player __instance, ref float __result)
        {
            if (__instance == Player.m_localPlayer && HelOathRuntime.Equipped(__instance)) __result = 100f;
        }
    }

    [HarmonyPatch(typeof(Projectile), "Setup")]
    internal static class HelOathProjectileSetupPatch
    {
        private static void Postfix(Projectile __instance, Character owner, ItemDrop.ItemData item)
            => HelOathProjectile.Setup(__instance, owner, item);
    }

    [HarmonyPatch(typeof(Projectile), "OnHit")]
    internal static class HelOathProjectileHitPatch
    {
        private static void Prefix(Projectile __instance, out HelOathShot __state)
        {
            __state = HelOathProjectile.Current;
            HelOathProjectile.Current = HelOathProjectile.Find(__instance);
        }
        private static void Postfix(Projectile __instance, Vector3 hitPoint, bool ___m_didHit)
        {
            if (___m_didHit) HelOathProjectile.Resolve(HelOathProjectile.Find(__instance), hitPoint);
        }
        private static Exception Finalizer(Exception __exception, HelOathShot __state)
        {
            HelOathProjectile.Current = __state;
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Character), "Damage")]
    internal static class HelOathDamagePatch
    {
        private static bool Prefix(Character __instance, HitData hit) => !HelOathNetwork.Intercept(__instance, hit);
    }
}
