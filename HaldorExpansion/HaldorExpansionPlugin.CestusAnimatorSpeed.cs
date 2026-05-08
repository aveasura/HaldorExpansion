using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion
{
    public partial class HaldorExpansionPlugin
    {
        private sealed class CestusAnimatorSpeedState
        {
            public bool Applied;
            public float LastBaseSpeed = 1f;
            public float LastAppliedSpeed = 1f;
        }

        private static readonly Dictionary<int, CestusAnimatorSpeedState> CestusAnimatorStates =
            new Dictionary<int, CestusAnimatorSpeedState>();

        [HarmonyPatch(typeof(CharacterAnimEvent), "CustomFixedUpdate")]
        private static class CestusAnimatorSpeedPatch
        {
            private static readonly FieldInfo CurrentAttackField =
                AccessTools.Field(typeof(Humanoid), "m_currentAttack");

            private static void Postfix(Character ___m_character, Animator ___m_animator)
            {
                if (___m_character == null || ___m_animator == null)
                    return;

                Player player = ___m_character as Player;
                if (player == null)
                    return;

                int animatorId = ___m_animator.GetInstanceID();

                CestusAnimatorSpeedState state;
                if (!CestusAnimatorStates.TryGetValue(animatorId, out state))
                {
                    state = new CestusAnimatorSpeedState();
                    CestusAnimatorStates[animatorId] = state;
                }

                float observedSpeed = ___m_animator.speed;
                float baseSpeed = observedSpeed;

                // Если в прошлом тике уже применялся множитель - не даем ему перемножаться бесконечно.
                if (state.Applied && Mathf.Abs(observedSpeed - state.LastAppliedSpeed) < 0.0001f)
                    baseSpeed = state.LastBaseSpeed;

                ItemDrop.ItemData weapon = player.GetCurrentWeapon();
                Attack currentAttack = null;

                if (CurrentAttackField != null)
                    currentAttack = CurrentAttackField.GetValue(player) as Attack;

                bool shouldSlow =
                    player.InAttack() &&
                    IsCestusWeapon(weapon) &&
                    currentAttack != null &&
                    !string.IsNullOrEmpty(currentAttack.m_attackAnimation) &&
                    currentAttack.m_attackAnimation.IndexOf("unarmed", StringComparison.OrdinalIgnoreCase) >= 0;

                if (!shouldSlow)
                {
                    state.Applied = false;
                    state.LastBaseSpeed = observedSpeed;
                    state.LastAppliedSpeed = observedSpeed;
                    return;
                }

                float multiplier = Mathf.Clamp(GritCestusAttackSpeedMultiplier, 0.01f, 10f);
                float newSpeed = baseSpeed * multiplier;

                if (Mathf.Abs(___m_animator.speed - newSpeed) > 0.0001f)
                {
                    ___m_animator.speed = newSpeed;
                    DebugLog("Cestus animator speed applied: " +
                            baseSpeed.ToString("0.###") + " -> " +
                            newSpeed.ToString("0.###"));
                }

                state.Applied = true;
                state.LastBaseSpeed = baseSpeed;
                state.LastAppliedSpeed = newSpeed;
            }
        }
    }
}