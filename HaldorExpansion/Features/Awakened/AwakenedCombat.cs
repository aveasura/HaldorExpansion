using System;
using System.Collections.Generic;
using System.Reflection;
using HaldorExpansion.Features.Berserker;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Features.Awakened
{
    // Only this local Attack scope creates receipts. Skill/ranged flags alone cannot
    // distinguish Bone Crushers' strikes from Nova or an equipped axe from a meteor.
    internal static class AwakenedCombat
    {
        private const string DamageRpc = "HaldorAwakenedDirectDamage";
        private const string ReceiptRpc = "HaldorAwakenedDirectReceipt";
        private const float ReceiptLifetime = 30f;
        private const float ReplayRetention = 60f;
        private static readonly FieldInfo CharacterView = AccessTools.Field(typeof(Character), "m_nview");
        private static readonly FieldInfo AttackPlayer = AccessTools.Field(typeof(Attack), "m_character");
        private static readonly FieldInfo AttackWeapon = AccessTools.Field(typeof(Attack), "m_weapon");
        private static readonly FieldInfo HitCollider = AccessTools.Field(typeof(HitData), "m_hitCollider");
        private static readonly MethodInfo WeakSpot = AccessTools.Method(typeof(Character), "FindWeakSpotIndex");
        private static readonly MethodInfo NativeDamage = AccessTools.Method(typeof(Character), "RPC_Damage");

        private sealed class Pending
        {
            internal Player Player;
            internal ItemDrop.ItemData Weapon, Helmet;
            internal long Owner;
            internal float Expires;
            internal bool Axe, Feast, Bone;
            internal bool Received, Killed;
            internal float Loss;
        }

        private sealed class Context
        {
            internal Character Target;
            internal HitData Hit;
            internal float Before, Loss;
            internal bool Observed, Killed, NestedDamage;
        }

        private static readonly Dictionary<long, Pending> PendingHits = new Dictionary<long, Pending>();
        private static readonly Dictionary<string, float> Resolved = new Dictionary<string, float>();
        private static readonly List<long> Remove = new List<long>();
        private static readonly List<string> StaleReplays = new List<string>();
        private static long nextToken;
        [ThreadStatic] private static int depth;
        [ThreadStatic] private static Player player;
        [ThreadStatic] private static ItemDrop.ItemData weapon;
        [ThreadStatic] private static HashSet<Character> targets;
        [ThreadStatic] private static Attack markedAttack;
        [ThreadStatic] private static bool markedSecondary, secondary;
        [ThreadStatic] private static Context context;

        private static ZNetView View(Character c) => c == null ? null : CharacterView?.GetValue(c) as ZNetView;

        internal static void MarkAttack(Player p, Attack attack, bool isSecondary)
        {
            if (p != null && p == Player.m_localPlayer)
            {
                markedAttack = attack;
                markedSecondary = isSecondary;
            }
        }

        internal static void Enter(Attack attack)
        {
            if (++depth != 1) return;
            player = AttackPlayer?.GetValue(attack) as Player;
            weapon = AttackWeapon?.GetValue(attack) as ItemDrop.ItemData;
            secondary = ReferenceEquals(markedAttack, attack) && markedSecondary;
            targets?.Clear();
        }

        internal static void Exit()
        {
            if (--depth > 0) return;
            depth = 0;
            player = null;
            weapon = null;
            targets?.Clear();
        }

        internal static long CreateToken(Character target, HitData hit)
        {
            if (depth != 1 || player == null || player != Player.m_localPlayer || player.IsDead() ||
                weapon == null || !ReferenceEquals(player.GetCurrentWeapon(), weapon) || target == null ||
                target.IsPlayer() || target.IsDead() || hit == null || hit.m_ranged ||
                hit.GetAttacker() != player || hit.GetTotalDamage() <= 0f || !BaseAI.IsEnemy(player, target)) return 0;
            Artifact kind = AwakenedEquipment.Identify(weapon);
            bool axe = kind == Artifact.BerserkerAxe;
            bool bone = kind == Artifact.BoneCrushers && !secondary && AwakenedEquipment.IsAwakened(weapon);
            bool harvest = axe && AwakenedEquipment.IsAwakened(weapon);
            bool feast = axe && AwakenedEquipment.Has(player, Artifact.BerserkerHelmet) &&
                         BerserkerRuntime.EffectiveWrathStage(player) >= 1;
            if (!harvest && !feast && !bone) return 0;
            ZNetView view = View(target);
            if (view == null || !view.IsValid() || view.GetZDO() == null) return 0;
            if (targets == null) targets = new HashSet<Character>();
            if (!targets.Add(target)) return 0;
            ItemDrop.ItemData helmet = null;
            if (feast)
                foreach (ItemDrop.ItemData item in player.GetInventory().GetAllItems())
                    if (item != null && item.m_equipped && AwakenedEquipment.IsAwakened(item) &&
                        AwakenedEquipment.Identify(item) == Artifact.BerserkerHelmet)
                    {
                        helmet = item;
                        break;
                    }

            long token = ++nextToken;
            PendingHits[token] = new Pending
            {
                Player = player, Weapon = weapon, Owner = view.GetZDO().GetOwner(),
                Expires = Time.time + ReceiptLifetime, Axe = harvest, Feast = feast, Bone = bone, Helmet = helmet
            };
            return token;
        }

        internal static bool TryRoute(Character target, HitData hit)
        {
            long token = CreateToken(target, hit);
            if (token == 0) return false;
            ZPackage package;
            try
            {
                if (WeakSpot != null && HitCollider != null)
                    hit.m_weakSpot = (short)WeakSpot.Invoke(target, new[] { HitCollider.GetValue(hit) });
                package = new ZPackage();
                hit.Serialize(ref package);
                package.Write(token);
            }
            catch (Exception e)
            {
                PendingHits.Remove(token);
                HaldorExpansionPlugin.Log?.LogWarning("[Awakened] Could not route direct hit: " + e.Message);
                return false;
            }

            // Once dispatch is attempted an exception may be from a local receiver after
            // damage was applied. Never fall back to vanilla and damage the target twice.
            try
            {
                View(target).InvokeRPC(DamageRpc, package);
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogWarning("[Awakened] Direct hit dispatch failed: " + e.Message);
            }

            return true;
        }

        internal static void Register(Character character, ZNetView view)
        {
            view.Register<ZPackage>(DamageRpc, (sender, package) =>
            {
                try
                {
                    if (!view.IsOwner() || package == null || NativeDamage == null) return;
                    HitData hit = new HitData();
                    hit.Deserialize(ref package);
                    long token = package.ReadLong();
                    Player attacker = hit.GetAttacker() as Player;
                    ZNetView av = View(attacker);
                    if (av == null || !av.IsValid() || av.GetZDO() == null || av.GetZDO().GetOwner() != sender) return;
                    if (character.IsPlayer() || character.IsDead() || !BaseAI.IsEnemy(attacker, character)) return;
                    Resolve(character, attacker, hit, token,
                        () => NativeDamage.Invoke(character, new object[] { sender, hit }));
                }
                catch (Exception e)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[Awakened] Direct hit rejected: " + e.Message);
                }
            });
            Player p = character as Player;
            if (p == null) return;
            view.Register<ZPackage>(ReceiptRpc, (sender, package) =>
            {
                try
                {
                    if (!view.IsOwner() || p != Player.m_localPlayer || package == null) return;
                    long token = package.ReadLong();
                    float loss = package.ReadSingle();
                    bool killed = package.ReadBool();
                    Pending pending;
                    if (!PendingHits.TryGetValue(token, out pending) || pending.Player != p || pending.Received ||
                        pending.Owner != sender || pending.Expires < Time.time || float.IsNaN(loss) ||
                        float.IsInfinity(loss) || loss <= 0f) return;
                    pending.Received = true;
                    pending.Loss = loss;
                    pending.Killed = killed;
                }
                catch (Exception e)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[Awakened] Receipt rejected: " + e.Message);
                }
            });
        }

        // Reckoning calls this around its existing native RPC, never routes a second hit.
        internal static void Resolve(Character target, Player attacker, HitData hit, long token, Action native)
        {
            if (token == 0)
            {
                native();
                return;
            }

            ZNetView tv = View(target), av = View(attacker);
            if (tv == null || !tv.IsOwner() || av == null || !av.IsValid()) return;
            string key = av.GetZDO().m_uid.ToString() + ":" + token;
            if (Resolved.ContainsKey(key)) return;
            // Bound replay memory to receipt lifetime; late receipts cannot reward the attacker.
            StaleReplays.Clear();
            foreach (var entry in Resolved)
                if (entry.Value < Time.time)
                    StaleReplays.Add(entry.Key);
            foreach (string entry in StaleReplays) Resolved.Remove(entry);
            // Release keys before invoking damage, which may reenter Resolve.
            StaleReplays.Clear();
            Resolved[key] = Time.time + ReplayRetention;
            Context previous = context;
            if (previous != null && previous.Target == target) previous.NestedDamage = true;
            Context current = new Context { Target = target, Hit = hit };
            context = current;
            try
            {
                native();
            }
            finally
            {
                context = previous;
            }

            if (!current.Observed || current.NestedDamage || current.Loss <= 0f) return;
            ZPackage receipt = new ZPackage();
            receipt.Write(token);
            receipt.Write(current.Loss);
            receipt.Write(current.Killed);
            av.InvokeRPC(ReceiptRpc, receipt);
        }

        internal static object Observe(Character target, HitData hit)
        {
            Context c = context;
            if (c != null && c.Target == target && c.Observed) c.NestedDamage = true;
            if (c == null || c.Observed || c.Target != target || !ReferenceEquals(c.Hit, hit) ||
                target.IsPlayer() || target.IsDead()) return null;
            c.Before = target.GetHealth();
            if (float.IsNaN(c.Before) || float.IsInfinity(c.Before) || c.Before <= 0f) return null;
            c.Observed = true;
            return c;
        }

        internal static void Complete(Character target, object observation)
        {
            Context c = observation as Context;
            if (c == null || target == null) return;
            c.Loss = Mathf.Clamp(c.Before - Mathf.Max(0f, target.GetHealth()), 0f, c.Before);
            c.Killed = c.Loss > 0f && (target.IsDead() || target.GetHealth() <= 0f);
        }

        internal static void Update(Player p)
        {
            if (p == null || p != Player.m_localPlayer) return;
            Remove.Clear();
            foreach (var entry in PendingHits)
            {
                Pending pending = entry.Value;
                if (pending.Player != p || p.IsDead() || pending.Expires < Time.time ||
                    !ReferenceEquals(p.GetCurrentWeapon(), pending.Weapon))
                {
                    Remove.Add(entry.Key);
                    continue;
                }

                if (!pending.Received) continue;
                Remove.Add(entry.Key);
                if (pending.Killed && pending.Axe && AwakenedEquipment.IsAwakened(pending.Weapon))
                    BerserkerRuntime.GrantAwakenedKillDebt(p);
                float heal = 0f;
                if (pending.Bone && AwakenedEquipment.IsAwakened(pending.Weapon))
                    heal += pending.Loss * AwakenedBalance.BoneCrushers.LifeSteal;
                if (pending.Killed && pending.Feast && pending.Helmet != null && pending.Helmet.m_equipped &&
                    p.GetInventory().GetAllItems().Contains(pending.Helmet) &&
                    AwakenedEquipment.IsAwakened(pending.Helmet) &&
                    BerserkerRuntime.EffectiveWrathStage(p) >= 1)
                    heal += Mathf.Max(0f, p.GetMaxHealth() - p.GetHealth()) *
                            AwakenedBalance.BerserkerHelmet.MissingHealthOnKill;
                if (heal > 0f) p.Heal(Mathf.Min(heal, Mathf.Max(0f, p.GetMaxHealth() - p.GetHealth())), true);
            }

            foreach (long token in Remove) PendingHits.Remove(token);
        }
    }
}