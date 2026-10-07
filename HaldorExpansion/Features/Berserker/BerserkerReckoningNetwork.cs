using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Features.Berserker
{
    internal static class BerserkerReckoningNetwork
    {
        private const string DamageRpc = "HE_BerserkerReckoningDamage_v1";
        private const string HealRpc = "HE_BerserkerReckoningHeal_v1";

        private static readonly FieldInfo CharacterView = AccessTools.Field(typeof(Character), "m_nview");
        private static readonly MethodInfo WeakSpot = AccessTools.Method(typeof(Character), "FindWeakSpotIndex");
        private static readonly FieldInfo HitCollider = AccessTools.Field(typeof(HitData), "m_hitCollider");
        private static readonly MethodInfo NativeDamage = AccessTools.Method(typeof(Character), "RPC_Damage");

        private static ZNetView View(Character character)
            => character == null || CharacterView == null ? null : CharacterView.GetValue(character) as ZNetView;

        internal static void Register(Character character)
        {
            ZNetView view = View(character);
            if (view == null || !view.IsValid()) return;

            view.Register<ZPackage>(DamageRpc, (sender, package) => ReceiveDamage(character, view, sender, package));
            if (character is Player player)
            {
                BerserkerEffects.Register(player, view);
                view.Register<float>(HealRpc, (sender, actualDamage) =>
                {
                    if (view.IsOwner()) BerserkerRuntime.ApplyReckoningHeal(player, actualDamage);
                });
            }
        }

        internal static bool TryIntercept(Player attacker, Character target, HitData hit)
        {
            if (attacker == null || target == null || hit == null) return false;
            ZNetView targetView = View(target);
            if (targetView == null || !targetView.IsValid()) return false;

            double debt;
            float bonus;
            if (!BerserkerRuntime.TryGetReckoningPayload(attacker, out debt, out bonus)) return false;
            if (bonus <= 0f) return false;

            // Character.Damage normally selects weak spots before routing RPC_Damage.
            // Preserve that step because this empowered hit routes through our own RPC.
            try
            {
                if (WeakSpot != null && HitCollider != null)
                    hit.m_weakSpot = (short)WeakSpot.Invoke(target, new[] { HitCollider.GetValue(hit) });
            }
            catch (Exception)
            {
                // Weak-spot lookup is optional; continue with the normal hit if unavailable.
            }

            HitData.DamageTypes damage = hit.m_damage;
            damage.m_slash += bonus;
            hit.m_damage = damage;
            hit.SetAttacker(attacker);

            try
            {
                ZPackage package = new ZPackage();
                hit.Serialize(ref package);
                targetView.InvokeRPC(DamageRpc, package);
                BerserkerRuntime.CommitReckoning(attacker);
                BerserkerEffects.BroadcastReckoningImpact(attacker, hit.m_point, debt);
                return true;
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError("[Berserker] Failed to route Reckoning hit: " + e);
                return false;
            }
        }

        private static void ReceiveDamage(Character target, ZNetView view, long sender, ZPackage package)
        {
            if (target == null || view == null || !view.IsOwner() || package == null || NativeDamage == null) return;

            try
            {
                HitData hit = new HitData();
                hit.Deserialize(ref package);
                Player attacker = hit.GetAttacker() as Player;
                if (attacker == null) return;

                ZNetView attackerView = View(attacker);
                if (attackerView == null || !attackerView.IsValid() || attackerView.GetZDO() == null ||
                    attackerView.GetZDO().GetOwner() != sender) return;
                if (target.IsDead() || target.GetHealth() <= 0f || target.IsTeleporting() || target.InCutscene()) return;
                if (target.IsPlayer() || !BaseAI.IsEnemy(attacker, target)) return;

                float before = target.GetHealth();
                NativeDamage.Invoke(target, new object[] { sender, hit });
                float actualDamage = Mathf.Max(0f, before - Mathf.Max(0f, target.GetHealth()));
                if (actualDamage > 0f)
                    attackerView.InvokeRPC(HealRpc, actualDamage);
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError("[Berserker] Failed to resolve Reckoning hit: " + e);
            }
        }
    }
}
