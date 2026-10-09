using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Features.Berserker
{
    // Damage is measured by the victim's owner, but health is restored by the attacker owner.
    // This also supports a dedicated server and ordinary multiplayer world ownership.
    internal static class BerserkerHelmetLifeStealNetwork
    {
        private static readonly FieldInfo CharacterView = AccessTools.Field(typeof(Character), "m_nview");

        internal static void Register(Player player, ZNetView view)
        {
            if (player == null || view == null || !view.IsValid()) return;
            view.Register<float>(BerserkerIds.HelmetLifeStealRpc, (sender, actualEnemyHealthLost) =>
            {
                if (!view.IsOwner()) return;
                BerserkerHelmetRuntime.ApplyLifeSteal(player, actualEnemyHealthLost);
            });
        }

        internal static void NotifyDamage(Player attacker, float actualEnemyHealthLost)
        {
            if (attacker == null || actualEnemyHealthLost <= 0f ||
                float.IsNaN(actualEnemyHealthLost) || float.IsInfinity(actualEnemyHealthLost)) return;

            ZNetView view = CharacterView?.GetValue(attacker) as ZNetView;
            if (view == null || !view.IsValid()) return;
            view.InvokeRPC(BerserkerIds.HelmetLifeStealRpc, actualEnemyHealthLost);
        }
    }

    // Character.ApplyDamage runs on the victim's network owner, after Valheim resolves
    // armor/resistances and receives both vanilla melee hits and routed Reckoning hits.
    // This intentionally counts actual missing enemy HP; a killing blow cannot heal off overkill.
    [HarmonyPatch(typeof(Character), "ApplyDamage")]
    internal static class BerserkerHelmetLifeStealDamagePatch
    {
        private struct Observation
        {
            internal Player Attacker;
            internal float BeforeHp;
        }

        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Character __instance, HitData __0, out Observation __state)
        {
            __state = default(Observation);
            if (__instance == null || __0 == null || __instance.IsPlayer() || __instance.IsDead()) return;

            // Weapon-only direct axe melee hits. Projectiles, including all Ragnarök
            // meteors (Projectile.DoAOE marks them ranged), never trigger helmet healing.
            if (__0.m_ranged || __0.m_skill != Skills.SkillType.Axes) return;

            Player attacker = __0.GetAttacker() as Player;
            if (attacker == null || attacker.IsDead() || !BaseAI.IsEnemy(attacker, __instance)) return;

            float health = __instance.GetHealth();
            if (health <= 0f || float.IsNaN(health) || float.IsInfinity(health)) return;
            __state.Attacker = attacker;
            __state.BeforeHp = health;
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Character __instance, Observation __state)
        {
            if (__state.Attacker == null || __instance == null) return;
            float actualEnemyHealthLost = __state.BeforeHp - Mathf.Max(0f, __instance.GetHealth());
            if (actualEnemyHealthLost <= 0f) return;
            BerserkerHelmetLifeStealNetwork.NotifyDamage(__state.Attacker, actualEnemyHealthLost);
        }
    }
}