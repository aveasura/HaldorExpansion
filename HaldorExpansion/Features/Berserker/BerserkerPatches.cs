using System;
using System.Collections.Generic;
using System.Reflection;
using HaldorExpansion.Features.StaminaCape;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Features.Berserker
{
    [HarmonyPatch(typeof(Character), "Awake")]
    internal static class BerserkerReckoningNetworkPatch
    {
        private static void Postfix(Character __instance) => BerserkerReckoningNetwork.Register(__instance);
    }

    [HarmonyPatch(typeof(Player), "Update")]
    internal static class BerserkerPlayerUpdatePatch
    {
        private static void Postfix(Player __instance)
        {
            BerserkerRuntime.Update(__instance);
            BerserkerWrathEffects.UpdatePlayer(__instance);
        }
    }

    // The mouse-wheel alternative attack reaches StartAttack with secondaryAttack=true.
    // Mark the exact cloned Attack instance so that, when Wrath V and enough Debt are present,
    // that same hit becomes Reckoning without a separate prepare/arm button.
    [HarmonyPatch(typeof(Humanoid), "StartAttack", new Type[] { typeof(Character), typeof(bool) })]
    internal static class BerserkerSecondaryAttackIntentPatch
    {
        private static readonly FieldInfo CurrentAttackField =
            AccessTools.Field(typeof(Humanoid), "m_currentAttack");

        private static void Postfix(Humanoid __instance, bool __1, bool __result)
        {
            if (!__result || !__1 || CurrentAttackField == null) return;
            Player player = __instance as Player;
            if (player == null || player != Player.m_localPlayer || !BerserkerEquipment.Equipped(player)) return;

            Attack attack = CurrentAttackField.GetValue(__instance) as Attack;
            if (attack == null) return;

            BerserkerAttackScope.MarkSecondaryAttack(player, attack);
            double debt;
            float bonus;
            if (BerserkerRuntime.TryGetReckoningPayload(player, out debt, out bonus))
                BerserkerEffects.BroadcastReckoningSwing(player, debt);
        }
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
        private static bool Prefix(Character __instance, HitData hit)
        {
            // Credit Wrath first so a hit that reaches Wrath V may immediately qualify
            // that same secondary swing as Reckoning, matching the original behaviour.
            bool credited = BerserkerAttackScope.TryCredit(__instance, hit);

            // A valid secondary hit becomes Reckoning directly when Wrath V + minimum Debt are present.
            // Route it through the owner-side RPC so lifesteal uses final post-defense direct damage.
            bool reckoning = BerserkerAttackScope.TryInterceptReckoning(__instance, hit);

            // Normal primary/secondary hits grant +7/+11 Debt. Reckoning grants no Debt,
            // but still refreshes the outgoing-combat grace timer through FinishDebtCredit.
            BerserkerAttackScope.FinishDebtCredit(credited);
            return !reckoning;
        }
    }

    [HarmonyPatch(typeof(Hud), "Update")]
    internal static class BerserkerDebtHudPatch
    {
        private static void Postfix(Hud __instance)
            => BerserkerDebtHudView.Update(__instance, Player.m_localPlayer);
    }

    [HarmonyPatch(typeof(Player), "UseStamina")]
    internal static class BerserkerWrathStaminaPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Player __instance, ref float __0)
        {
            if (__0 <= 0f || StaminaCapeRuntime.IsForcedStaminaSpend) return;
            __0 *= BerserkerRuntime.WrathStaminaMultiplier(__instance);
        }
    }

    [HarmonyPatch(typeof(CharacterAnimEvent), "CustomFixedUpdate")]
    internal static class BerserkerWrathAttackSpeedPatch
    {
        private sealed class AnimatorState
        {
            internal bool Applied;
            internal float LastBaseSpeed = 1f;
            internal float LastAppliedSpeed = 1f;
        }

        private static readonly FieldInfo CurrentAttackField = AccessTools.Field(typeof(Humanoid), "m_currentAttack");
        private static readonly Dictionary<int, AnimatorState> States = new Dictionary<int, AnimatorState>();

        private static void Postfix(Character ___m_character, Animator ___m_animator)
        {
            Player player = ___m_character as Player;
            if (player == null || player != Player.m_localPlayer || ___m_animator == null) return;

            int id = ___m_animator.GetInstanceID();
            if (!States.TryGetValue(id, out AnimatorState state))
            {
                state = new AnimatorState();
                States[id] = state;
            }

            float observed = ___m_animator.speed;
            float baseSpeed = observed;
            if (state.Applied && Mathf.Abs(observed - state.LastAppliedSpeed) < 0.0001f)
                baseSpeed = state.LastBaseSpeed;

            Attack currentAttack = CurrentAttackField != null ? CurrentAttackField.GetValue(player) as Attack : null;
            int stage = BerserkerRuntime.EffectiveWrathStage(player);
            bool shouldBoost = stage > 0 && player.InAttack() && currentAttack != null &&
                               BerserkerEquipment.Equipped(player);

            if (!shouldBoost)
            {
                if (state.Applied && Mathf.Abs(___m_animator.speed - state.LastAppliedSpeed) < 0.0001f)
                    ___m_animator.speed = state.LastBaseSpeed;
                state.Applied = false;
                state.LastBaseSpeed = ___m_animator.speed;
                state.LastAppliedSpeed = ___m_animator.speed;
                return;
            }

            float newSpeed = baseSpeed * BerserkerRuntime.WrathAttackSpeedMultiplier(player);
            if (Mathf.Abs(___m_animator.speed - newSpeed) > 0.0001f)
                ___m_animator.speed = newSpeed;

            state.Applied = true;
            state.LastBaseSpeed = baseSpeed;
            state.LastAppliedSpeed = newSpeed;
        }
    }

    // Ragnarök uses the same committed-cast philosophy as Cestus: once activation
    // succeeds, normal movement/combat inputs cannot cancel the 1.5 s wind-up.
    [HarmonyPatch(typeof(Player), "SetControls")]
    internal static class BerserkerRagnarokCastInputLockPatch
    {
        private static void Prefix(
            Player __instance,
            ref Vector3 movedir,
            ref bool attack,
            ref bool secondaryAttack,
            ref bool block,
            ref bool blockHold,
            ref bool jump,
            ref bool crouch)
        {
            if (__instance == null || __instance != Player.m_localPlayer) return;
            if (!BerserkerRagnarokRuntime.IsInputLocked(__instance)) return;

            movedir = Vector3.zero;
            attack = false;
            secondaryAttack = false;
            block = false;
            blockHold = false;
            jump = false;
            crouch = false;
        }
    }

    [HarmonyPatch(typeof(Humanoid), "StartAttack")]
    internal static class BerserkerRagnarokAttackBlockDuringCastPatch
    {
        private static bool Prefix(Humanoid __instance, ref bool __result)
        {
            Player player = __instance as Player;
            if (player == null || player != Player.m_localPlayer) return true;
            if (!BerserkerRagnarokRuntime.IsInputLocked(player)) return true;

            BerserkerRagnarokRuntime.ClearHumanoidAttackState(player);
            BerserkerRagnarokRuntime.ClearHumanoidBlockState(player);
            __result = false;
            return false;
        }
    }
}