using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Features.HelOath
{
    internal static class HelOathNetwork
    {
        private const string DamageRpc = "HE_HelOathDamage_v1";
        private const string CreditRpc = "HE_HelOathCredit_v1";
        private const string TouchDamageRpc = "HE_HelOathTouchDamage_v1";
        private static readonly FieldInfo CharacterView = AccessTools.Field(typeof(Character), "m_nview");
        internal static ZNetView View(Character character) => character == null ? null : (ZNetView)CharacterView.GetValue(character);
        private static readonly MethodInfo WeakSpot = AccessTools.Method(typeof(Character), "FindWeakSpotIndex");

        internal static void CreditSource(string source, float damage)
        {
            if (damage <= 0f) return;
            int split = source.IndexOf(':');
            long id;
            if (split < 1 || !long.TryParse(source.Substring(0, split), out id)) return;
            var player = Player.GetPlayer(id);
            var view = View(player);
            if (view != null && view.IsValid()) view.InvokeRPC(CreditRpc, source.Substring(split + 1), damage);
        }

        internal static void Register(Character character)
        {
            var view = View(character);
            if (view == null || !view.IsValid()) return;
            view.Register<ZPackage>(DamageRpc, (sender, package) => Receive(character, view, sender, package));
            if (character is Player visualPlayer)
            {
                HelOathEffects.Register(visualPlayer, view);
                HelOathTouchEffects.Register(visualPlayer, view);
            }
            if (character is Player player)
            {
                view.Register<string, float>(CreditRpc, (sender, epoch, damage) =>
                {
                    if (view.IsOwner()) HelOathRuntime.Credit(player, epoch, damage);
                });
                view.Register<float>(TouchDamageRpc, (sender, damage) =>
                {
                    if (view.IsOwner() && damage > 0f) HelOathRuntime.NotifyBowDamage(player);
                });
            }
        }

        internal static bool Intercept(Character target, HitData hit)
        {
            var shot = HelOathProjectile.Current;
            if (shot == null || shot.Owner == null || hit.GetAttacker() != shot.Owner) return false;
            var view = View(target);
            if (view == null || !view.IsValid()) return false;
            // Preserve the weakspot selection normally done by Character.Damage.
            hit.m_weakSpot = (short)WeakSpot.Invoke(target, new object[] { AccessTools.Field(typeof(HitData), "m_hitCollider").GetValue(hit) });
            if (shot.Special)
            {
                shot.DirectTarget = target;
                shot.DirectHit = hit;
            }
            else Send(target, shot, hit, 0f, hit.m_point);
            return true;
        }

        internal static void Send(Character target, HelOathShot shot, HitData direct, float frost, Vector3 origin)
        {
            var view = View(target);
            if (view == null || !view.IsValid()) return;
            var package = new ZPackage();
            Write(package, shot.Special);
            Write(package, shot.Epoch);
            Write(package, frost);
            Write(package, origin);
            Write(package, direct != null);
            var hit = direct ?? new HitData();
            hit.SetAttacker(shot.Owner);
            hit.Serialize(ref package);
            view.InvokeRPC(DamageRpc, package);
        }

        // Select the exact overload: the game also exposes Write overloads from assembly_utils.
        private static class PackageWriter<T>
        {
            internal static readonly MethodInfo Method = AccessTools.Method(typeof(ZPackage), "Write", new[] { typeof(T) });
        }

        private static void Write<T>(ZPackage package, T value) => PackageWriter<T>.Method.Invoke(package, new object[] { value });

        private static void Receive(Character target, ZNetView view, long sender, ZPackage package)
        {
            if (!view.IsOwner()) return;
            try
            {
                bool special = package.ReadBool();
                string epoch = package.ReadString();
                float frost = package.ReadSingle();
                Vector3 origin = package.ReadVector3();
                bool hasDirect = package.ReadBool();
                var hit = new HitData();
                hit.Deserialize(ref package);
                var attacker = hit.GetAttacker() as Player;
                if (attacker == null) return;
                var attackerView = View(attacker);
                if (attackerView == null || !attackerView.IsValid() || attackerView.GetZDO().GetOwner() != sender) return;
                if (float.IsNaN(frost) || float.IsInfinity(frost) || frost < 0f) return;
                if (target.IsDead() || target.GetHealth() <= 0f || target.IsTeleporting() || target.InCutscene()) return;

                float health = target.GetHealth();
                Vector3 direction = target.GetCenterPoint() - origin;
                if (direction.sqrMagnitude < 0.001f) direction = hit.m_dir.sqrMagnitude > 0f ? hit.m_dir : attacker.transform.forward;
                direction.Normalize();
                if (hasDirect)
                {
                    if (special)
                    {
                        hit.m_pushForce = 0f;
                        hit.m_staggerMultiplier = 0f;
                    }
                    // Run the real, Harmony-patched game damage pipeline on the target owner.
                    HelOathDotTracking.Apply(target, sender, hit, !special && !target.IsPlayer() && BaseAI.IsEnemy(attacker, target) ? attacker.GetPlayerID() + ":" + epoch : null);
                }
                if (special && frost > 0f && !target.IsDead() && target.GetHealth() > 0f)
                {
                    var burst = new HitData { m_point = target.GetCenterPoint(), m_dir = direction,
                        m_pushForce = 0f, m_staggerMultiplier = 0f, m_backstabBonus = 1f,
                        m_dodgeable = false, m_blockable = false, m_ranged = true,
                        m_skill = Skills.SkillType.Bows, m_hitType = HitData.HitType.PlayerHit };
                    burst.m_damage.m_frost = frost;
                    burst.SetAttacker(attacker);
                    HelOathDotTracking.Apply(target, sender, burst, null);
                }
                float actualDamage = Mathf.Max(0f, health - Mathf.Max(0f, target.GetHealth()));
                if (special)
                {
                    // Embrace never recharges itself, but real special-shot damage may awaken a
                    // fresh Hel's Touch cycle after activation consumed the previous stacks.
                    if (actualDamage > 0f && !target.IsPlayer() && BaseAI.IsEnemy(attacker, target))
                        attackerView.InvokeRPC(TouchDamageRpc, actualDamage);
                    if (!target.IsPlayer() && !target.IsDead() && target.GetHealth() > 0f)
                    {
                        target.ApplyPushback(direction, 20f);
                        target.Stagger(direction);
                    }
                    return;
                }
                if (actualDamage > 0f && !target.IsPlayer() && BaseAI.IsEnemy(attacker, target))
                    attackerView.InvokeRPC(CreditRpc, epoch, actualDamage);
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError("[Hel Oath] Failed to process hit: " + e);
            }
        }
    }
}
