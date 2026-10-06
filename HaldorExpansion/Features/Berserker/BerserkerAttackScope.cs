using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace HaldorExpansion.Features.Berserker
{
    // Attack.DoMeleeAttack invokes Character.Damage locally before the game's routed damage RPC.
    // Keeping a tiny thread-local scope lets us identify genuine hits from THIS axe without
    // guessing from later damage ticks, environmental damage, DoTs or the attacker's current slot.
    internal static class BerserkerAttackScope
    {
        private static readonly FieldInfo AttackCharacterField = AccessTools.Field(typeof(Attack), "m_character");
        private static readonly FieldInfo AttackWeaponField = AccessTools.Field(typeof(Attack), "m_weapon");

        [ThreadStatic] private static int depth;
        [ThreadStatic] private static Player activePlayer;
        [ThreadStatic] private static ItemDrop.ItemData activeWeapon;
        [ThreadStatic] private static HashSet<Character> creditedTargets;

        internal static void Enter(Attack attack)
        {
            depth++;
            if (depth != 1) return;

            activePlayer = null;
            activeWeapon = null;
            creditedTargets = null;

            if (attack == null || AttackCharacterField == null || AttackWeaponField == null) return;

            Player player = AttackCharacterField.GetValue(attack) as Player;
            ItemDrop.ItemData weapon = AttackWeaponField.GetValue(attack) as ItemDrop.ItemData;
            if (player == null || player != Player.m_localPlayer) return;
            if (!BerserkerEquipment.IsAxe(weapon)) return;

            activePlayer = player;
            activeWeapon = weapon;
            creditedTargets = new HashSet<Character>();
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
            ClearActive();
        }

        internal static void TryCredit(Character target, HitData hit)
        {
            Player player = activePlayer;
            ItemDrop.ItemData weapon = activeWeapon;
            if (player == null || weapon == null || target == null || hit == null) return;
            if (player != Player.m_localPlayer || player.IsDead()) return;
            if (!BerserkerEquipment.IsAxe(weapon)) return;
            if (target.IsPlayer() || target.IsDead()) return;
            if (!BaseAI.IsEnemy(player, target)) return;
            if (hit.GetTotalDamage() <= 0f) return;

            // A single wide swing may touch several colliders belonging to one Character.
            // Count that Character only once for this attack, but count different enemies separately.
            if (creditedTargets != null && !creditedTargets.Add(target)) return;

            BerserkerRuntime.NotifyAxeHit(player, 1f);
        }

        private static void ClearActive()
        {
            activePlayer = null;
            activeWeapon = null;
            creditedTargets = null;
        }
    }
}
