using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace HaldorExpansion.Features.HelOath
{
    internal sealed class HelOathShot
    {
        internal Player Owner;
        internal string Epoch;
        internal bool Special;
        internal bool TouchV;
        internal bool Resolved;
        internal float Splash;
        internal float Radius;
        internal Character DirectTarget;
        internal HitData DirectHit;
    }

    internal static class HelOathProjectile
    {
        private static readonly float DirectArrowMultiplier = 3f;
        
        private static readonly ConditionalWeakTable<Projectile, HelOathShot> Shots = new ConditionalWeakTable<Projectile, HelOathShot>();
        [ThreadStatic] internal static HelOathShot Current;

        internal static void Setup(Projectile projectile, Character owner, ItemDrop.ItemData item)
        {
            var player = owner as Player;
            if (player == null || player != Player.m_localPlayer || !HelOathRuntime.IsBow(item)) return;
            HelOathRuntime.Refresh(player);
            bool touchV;
            bool special = HelOathRuntime.State.TryFire(Time.time, out touchV);
            if (special) HelOathEffects.SyncPrepared(player, false);
            float coefficient = touchV ? HelOathConfiguration.TouchVEmbraceCoefficientValue : HelOathConfiguration.NormalEmbraceCoefficient;
            float splash = special ? HelOathDamageMath.CalculateSplash(player.GetMaxStamina(), player.GetMaxHealth(), coefficient) : 0f;
            var shot = new HelOathShot
            {
                Owner = player,
                Special = special,
                TouchV = touchV,
                Epoch = HelOathRuntime.State.Epoch,
                Splash = splash,
                Radius = HelOathConfiguration.ExplosionRadius
            };
            Shots.Remove(projectile);
            Shots.Add(projectile, shot);
            if (special)
            {
                projectile.m_damage.Modify(DirectArrowMultiplier);
                // Vanilla AoE replaces direct damage; the separate burst is resolved below.
                projectile.m_aoe = 0f;
                projectile.m_attackForce = 0f;
            }
        }

        internal static HelOathShot Find(Projectile projectile)
        {
            HelOathShot shot;
            return Shots.TryGetValue(projectile, out shot) && !shot.Resolved ? shot : null;
        }

        internal static void Resolve(HelOathShot shot, Vector3 point)
        {
            if (shot == null || shot.Resolved) return;
            shot.Resolved = true;
            if (!shot.Special || shot.Owner == null) return;
            // One RPC per target contains both components, preserving damage -> control order.
            var targets = new HashSet<Character>();
            foreach (var target in Character.GetAllCharacters())
            {
                if (!CanExplodeOn(shot.Owner, target)) continue;
                float reach = shot.Radius + Mathf.Max(0f, target.GetRadius());
                if ((target.GetCenterPoint() - point).sqrMagnitude <= reach * reach) targets.Add(target);
            }
            if (shot.DirectTarget != null) targets.Add(shot.DirectTarget);
            foreach (var target in targets)
            {
                var hit = target == shot.DirectTarget ? shot.DirectHit : null;
                float splash = CanExplodeOn(shot.Owner, target) ? shot.Splash : 0f;
                HelOathNetwork.Send(target, shot, hit, splash, point);
            }
            HelOathEffects.BroadcastExplosion(shot.Owner, point, shot.Radius, shot.TouchV);
        }

        private static bool CanExplodeOn(Player owner, Character target)
        {
            return target != null && target != owner && !target.IsDead() && !target.IsPlayer()
                && BaseAI.IsEnemy(owner, target);
        }
    }
}
