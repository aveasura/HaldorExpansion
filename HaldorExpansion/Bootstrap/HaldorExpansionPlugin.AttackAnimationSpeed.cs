using System;
using System.Reflection;
using HaldorExpansion.Combat;
using HaldorExpansion.Features.Berserker;
using HaldorExpansion.Features.Cestus;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion
{
    public partial class HaldorExpansionPlugin
    {
        // A single postfix owns the combined attack speed instead of two postfixes
        // each observing and re-multiplying the Animator.speed written by the other.
        [HarmonyPatch(typeof(CharacterAnimEvent), "CustomFixedUpdate")]
        [HarmonyPriority(Priority.Last)]
        private static class AttackAnimationSpeedPatch
        {
            private static readonly FieldInfo CurrentAttackField =
                AccessTools.Field(typeof(Humanoid), "m_currentAttack");

            private static void Postfix(Character ___m_character, Animator ___m_animator)
            {
                Player player = ___m_character as Player;
                if (player == null || ___m_animator == null)
                    return;

                // Avoid reflection, equipment checks and config reads while idle.
                Attack currentAttack = player.InAttack() && CurrentAttackField != null
                    ? CurrentAttackField.GetValue(player) as Attack
                    : null;

                float multiplier = 1f;
                if (currentAttack != null)
                {
                    // Preserve the previous behaviour: Wrath / Exhaustion are
                    // calculated only for the locally controlled player.
                    if (player == Player.m_localPlayer)
                        multiplier = BerserkerRuntime.WrathAttackSpeedMultiplier(player);

                    // Preserve Cestus animations for local and remote players.
                    string animation = currentAttack.m_attackAnimation;
                    if (!string.IsNullOrEmpty(animation) &&
                        animation.IndexOf("unarmed", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        CestusEquipment.IsCestusWeapon(player.GetCurrentWeapon()))
                    {
                        // Exhaustion still affects Cestus: 1.9 * 0.75 = 1.425.
                        multiplier *= CestusItemRegistration.GritCestusAttackSpeedMultiplier;
                    }
                }

                AttackAnimationSpeedRuntime.Apply(___m_animator, multiplier);
            }
        }
    }
}
