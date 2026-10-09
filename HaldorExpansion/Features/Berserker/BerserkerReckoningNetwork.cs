using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Features.Berserker
{
    internal static class BerserkerReckoningNetwork
    {
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

            view.Register<ZPackage>(BerserkerIds.ReckoningDamageRpc,
                (sender, package) => ReceiveDamage(character, view, sender, package));
            if (character is Player player)
            {
                BerserkerEffects.Register(player, view);
                BerserkerWrathEffects.Register(player, view);
                BerserkerHelmetLifeStealNetwork.Register(player, view);
                view.Register<Vector3, float>(BerserkerIds.ReckoningHealRpc, (sender, point, finalDamage) =>
                {
                    if (!view.IsOwner()) return;
                    float actualHeal = BerserkerRuntime.ApplyReckoningHeal(player, finalDamage);
                    if (actualHeal > 0f)
                    {
                        try
                        {
                            BerserkerEffects.BroadcastReckoningHeal(player, point, actualHeal);
                        }
                        catch (Exception e)
                        {
                            HaldorExpansionPlugin.Log?.LogWarning("[Berserker] Reckoning heal VFX failed: " +
                                                                  e.Message);
                        }
                    }
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
                targetView.InvokeRPC(BerserkerIds.ReckoningDamageRpc, package);
                BerserkerRuntime.CommitReckoning(attacker, debt);
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError("[Berserker] Failed to route Reckoning hit: " + e);
                return false;
            }

            // VFX is non-gameplay. A visual RPC failure must never make the already-routed
            // Reckoning fall back to vanilla Character.Damage or refund outgoing Debt.
            try
            {
                BerserkerEffects.BroadcastReckoningImpact(attacker, hit.m_point, debt);
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogWarning("[Berserker] Reckoning impact VFX failed: " + e.Message);
            }

            return true;
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
                if (target.IsDead() || target.GetHealth() <= 0f || target.IsTeleporting() ||
                    target.InCutscene()) return;
                if (target.IsPlayer() || !BaseAI.IsEnemy(attacker, target)) return;

                // Character.ApplyDamage raises m_onDamaged with the final direct damage value
                // after the vanilla resistance/armor pipeline. Unlike HP delta, this value is
                // not truncated by the target's remaining health, so overkill still contributes
                // to Reckoning lifesteal exactly like the displayed damage number.
                float finalDamage = 0f;
                Action<float, Character> captureDamage = (damage, source) =>
                {
                    if (finalDamage > 0f || source != attacker) return;
                    if (float.IsNaN(damage) || float.IsInfinity(damage) || damage <= 0f) return;
                    finalDamage = damage;
                };

                target.m_onDamaged += captureDamage;
                try
                {
                    NativeDamage.Invoke(target, new object[] { sender, hit });
                }
                finally
                {
                    target.m_onDamaged -= captureDamage;
                }

                if (finalDamage > 0f)
                    attackerView.InvokeRPC(BerserkerIds.ReckoningHealRpc, hit.m_point, finalDamage);
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError("[Berserker] Failed to resolve Reckoning hit: " + e);
            }
        }
    }
}