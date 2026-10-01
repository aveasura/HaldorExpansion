using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Features.HelOath
{
    internal static class HelOathDotTracking
    {
        internal sealed class Ownership
        {
            internal readonly HelOathDotLedger Fire = new HelOathDotLedger();
            internal readonly HelOathDotLedger Spirit = new HelOathDotLedger();
            internal readonly HelOathDotLedger Poison = new HelOathDotLedger();
        }
        internal sealed class Tick
        {
            internal Character Target;
            internal Ownership Ledger;
            internal float Fire, Spirit, Poison;
        }
        internal sealed class DamageObservation
        {
            internal float Health, Total;
            internal Dictionary<string, float> Shares;
        }
        private static readonly ConditionalWeakTable<StatusEffect, Ownership> Owners = new ConditionalWeakTable<StatusEffect, Ownership>();
        private static readonly FieldInfo EffectCharacter = AccessTools.Field(typeof(StatusEffect), "m_character");
        private static readonly MethodInfo NativeDamage = AccessTools.Method(typeof(Character), "RPC_Damage");
        private static readonly FieldInfo FireLeft = AccessTools.Field(typeof(SE_Burning), "m_fireDamageLeft");
        private static readonly FieldInfo SpiritLeft = AccessTools.Field(typeof(SE_Burning), "m_spiritDamageLeft");
        private static readonly FieldInfo PoisonLeft = AccessTools.Field(typeof(SE_Poison), "m_damageLeft");
        [ThreadStatic] private static Character sourceTarget;
        [ThreadStatic] private static string source;
        [ThreadStatic] internal static Tick CurrentTick;

        internal static void Apply(Character target, long sender, HitData hit, string creditSource)
        {
            var previousTarget = sourceTarget;
            string previousSource = source;
            sourceTarget = target;
            source = creditSource;
            try { NativeDamage.Invoke(target, new object[] { sender, hit }); }
            finally { sourceTarget = previousTarget; source = previousSource; }
        }
        internal static void Added(StatusEffect effect, float damage, string channel, bool replace)
        {
            bool attributed = source != null && (Character)EffectCharacter.GetValue(effect) == sourceTarget;
            Ownership ledger;
            if (!Owners.TryGetValue(effect, out ledger))
            {
                if (!attributed) return;
                ledger = Owners.GetValue(effect, ignored => new Ownership());
            }
            var pool = channel == "fire" ? ledger.Fire : channel == "spirit" ? ledger.Spirit : ledger.Poison;
            if (replace) pool.Clear();
            if (attributed) pool.Add(source, damage);
        }
        internal static Tick Begin(StatusEffect effect)
        {
            Ownership ledger;
            if (!Owners.TryGetValue(effect, out ledger)) return null;
            var tick = new Tick { Target = (Character)EffectCharacter.GetValue(effect), Ledger = ledger };
            if (effect is SE_Burning)
            {
                tick.Fire = (float)FireLeft.GetValue(effect);
                tick.Spirit = (float)SpiritLeft.GetValue(effect);
            }
            else if (effect is SE_Poison)
                tick.Poison = (float)PoisonLeft.GetValue(effect);
            return tick;
        }
        internal static DamageObservation Before(Character target, HitData hit)
        {
            var tick = CurrentTick;
            if (tick == null || tick.Target != target || target.IsDead()) return null;
            if (hit.m_hitType != HitData.HitType.Burning && hit.m_hitType != HitData.HitType.Poisoned) return null;
            var shares = tick.Ledger.Fire.Consume(tick.Fire, hit.m_damage.m_fire);
            Merge(shares, tick.Ledger.Spirit.Consume(tick.Spirit, hit.m_damage.m_spirit));
            Merge(shares, tick.Ledger.Poison.Consume(tick.Poison, hit.m_damage.m_poison));
            return new DamageObservation { Health = target.GetHealth(), Total = hit.GetTotalDamage(), Shares = shares };
        }
        private static void Merge(Dictionary<string, float> target, Dictionary<string, float> values)
        {
            foreach (var pair in values)
            {
                float previous;
                target.TryGetValue(pair.Key, out previous);
                target[pair.Key] = previous + pair.Value;
            }
        }
        internal static void After(Character target, DamageObservation observed)
        {
            if (observed == null || observed.Total <= 0f) return;
            float actual = Mathf.Max(0f, observed.Health - Mathf.Max(0f, target.GetHealth()));
            foreach (var pair in observed.Shares)
                HelOathNetwork.CreditSource(pair.Key, actual * pair.Value / observed.Total);
        }
    }

    [HarmonyPatch(typeof(SE_Burning), "AddFireDamage")]
    internal static class HelOathFireOwnershipPatch
    {
        private static void Postfix(SE_Burning __instance, float damage, bool __result)
        { if (__result) HelOathDotTracking.Added(__instance, damage, "fire", false); }
    }
    [HarmonyPatch(typeof(SE_Burning), "AddSpiritDamage")]
    internal static class HelOathSpiritOwnershipPatch
    {
        private static void Postfix(SE_Burning __instance, float damage, bool __result)
        { if (__result) HelOathDotTracking.Added(__instance, damage, "spirit", false); }
    }
    [HarmonyPatch(typeof(SE_Poison), "AddDamage")]
    internal static class HelOathPoisonOwnershipPatch
    {
        private static void Prefix(float damage, float ___m_damageLeft, out bool __state) => __state = damage >= ___m_damageLeft;
        private static void Postfix(SE_Poison __instance, float damage, bool __state)
        { if (__state) HelOathDotTracking.Added(__instance, damage, "poison", true); }
    }
    [HarmonyPatch]
    internal static class HelOathDotTickPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(SE_Burning), "UpdateStatusEffect");
            yield return AccessTools.Method(typeof(SE_Poison), "UpdateStatusEffect");
        }
        private static void Prefix(StatusEffect __instance, out HelOathDotTracking.Tick __state)
        {
            __state = HelOathDotTracking.CurrentTick;
            HelOathDotTracking.CurrentTick = HelOathDotTracking.Begin(__instance);
        }
        private static Exception Finalizer(Exception __exception, HelOathDotTracking.Tick __state)
        {
            HelOathDotTracking.CurrentTick = __state;
            return __exception;
        }
    }
    [HarmonyPatch(typeof(Character), "ApplyDamage")]
    internal static class HelOathDotDamagePatch
    {
        private static void Prefix(Character __instance, HitData hit, out HelOathDotTracking.DamageObservation __state)
            => __state = HelOathDotTracking.Before(__instance, hit);
        private static void Postfix(Character __instance, HelOathDotTracking.DamageObservation __state)
            => HelOathDotTracking.After(__instance, __state);
    }
}
