using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace HaldorExpansion.Features.Berserker
{
    // Attack.DoMeleeAttack invokes Character.Damage locally before the routed damage RPC.
    // StartAttack marks the exact cloned Attack instance used for the mouse-wheel secondary swing.
    // If Wrath V and enough Blood Debt are present when that swing connects, it becomes Reckoning directly.
    internal static class BerserkerAttackScope
    {
        private static readonly FieldInfo AttackCharacterField = AccessTools.Field(typeof(Attack), "m_character");
        private static readonly FieldInfo AttackWeaponField = AccessTools.Field(typeof(Attack), "m_weapon");

        [ThreadStatic] private static int depth;
        [ThreadStatic] private static Attack activeAttack;
        [ThreadStatic] private static Player activePlayer;
        [ThreadStatic] private static ItemDrop.ItemData activeWeapon;
        [ThreadStatic] private static HashSet<Character> creditedTargets;
        [ThreadStatic] private static bool activeSecondary;
        [ThreadStatic] private static bool reckoningUsedThisAttack;

        [ThreadStatic] private static Player markedSecondaryPlayer;
        [ThreadStatic] private static Attack markedSecondaryAttack;

        internal static void MarkSecondaryAttack(Player player, Attack attack)
        {
            if (player == null || player != Player.m_localPlayer || attack == null ||
                !BerserkerEquipment.Equipped(player))
                return;

            markedSecondaryPlayer = player;
            markedSecondaryAttack = attack;
        }

        internal static void Enter(Attack attack)
        {
            depth++;
            if (depth != 1) return;

            ClearActive();
            if (attack == null || AttackCharacterField == null || AttackWeaponField == null) return;

            Player player = AttackCharacterField.GetValue(attack) as Player;
            ItemDrop.ItemData weapon = AttackWeaponField.GetValue(attack) as ItemDrop.ItemData;
            if (player == null || player != Player.m_localPlayer) return;
            if (!BerserkerEquipment.IsAxe(weapon)) return;

            activeAttack = attack;
            activePlayer = player;
            activeWeapon = weapon;
            creditedTargets = new HashSet<Character>();
            reckoningUsedThisAttack = false;
            activeSecondary = markedSecondaryPlayer == player && ReferenceEquals(markedSecondaryAttack, attack);
        }

        internal static void Exit()
        {
            if (depth <= 0)
            {
                depth = 0;
                ClearActive();
                return;
            }

            depth--;
            if (depth != 0) return;

            Attack finished = activeAttack;
            ClearActive();
            if (ReferenceEquals(markedSecondaryAttack, finished))
            {
                markedSecondaryAttack = null;
                markedSecondaryPlayer = null;
            }
        }

        internal static void TryCredit(Character target, HitData hit)
        {
            Player player = activePlayer;
            ItemDrop.ItemData weapon = activeWeapon;
            if (!ValidEnemyHit(player, weapon, target, hit)) return;

            // One Character only once per swing, even if several colliders are touched.
            if (creditedTargets != null && !creditedTargets.Add(target)) return;
            BerserkerRuntime.NotifyAxeHit(player, 1f);
        }

        internal static bool TryInterceptReckoning(Character target, HitData hit)
        {
            if (!activeSecondary || reckoningUsedThisAttack) return false;
            Player player = activePlayer;
            ItemDrop.ItemData weapon = activeWeapon;
            if (!ValidEnemyHit(player, weapon, target, hit)) return false;
            bool intercepted = BerserkerReckoningNetwork.TryIntercept(player, target, hit);
            if (intercepted) reckoningUsedThisAttack = true;
            return intercepted;
        }

        private static bool ValidEnemyHit(Player player, ItemDrop.ItemData weapon, Character target, HitData hit)
        {
            if (player == null || weapon == null || target == null || hit == null) return false;
            if (player != Player.m_localPlayer || player.IsDead()) return false;
            if (!BerserkerEquipment.IsAxe(weapon)) return false;
            if (target.IsPlayer() || target.IsDead()) return false;
            if (!BaseAI.IsEnemy(player, target)) return false;
            return hit.GetTotalDamage() > 0f;
        }

        private static void ClearActive()
        {
            activeAttack = null;
            activePlayer = null;
            activeWeapon = null;
            creditedTargets = null;
            activeSecondary = false;
            reckoningUsedThisAttack = false;
        }
    }
}