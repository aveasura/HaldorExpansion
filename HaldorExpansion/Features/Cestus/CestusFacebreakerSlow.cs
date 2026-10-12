using System;
using System.Reflection;
using HaldorExpansion.Compatibility;
using UnityEngine;

namespace HaldorExpansion.Features.Cestus
{
    /// <summary>Movement slow for any successfully pulled Facebreaker target, applied on its owner.</summary>
    internal static class CestusFacebreakerSlow
    {
        private const string EffectId = "aveasura_he_cestus_facebreaker_clash";
        internal const string NameKey = "$se_he_facebreaker_clash";
        internal const string DescriptionKey = "$se_he_facebreaker_clash_desc";
        private static ObjectDB registeredDb;
        private static SE_Stats prototype;
        private static bool warned;
        private static readonly MethodInfo[] AddMethods = typeof(SEMan).GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        internal static void EnsureRegistered()
        {
            if (ObjectDB.instance == null || ObjectDB.instance.m_StatusEffects == null)
                return;
            if (registeredDb != ObjectDB.instance)
            {
                registeredDb = ObjectDB.instance;
                prototype = null;
            }

            foreach (StatusEffect item in ObjectDB.instance.m_StatusEffects)
                if (item != null && item.name == EffectId)
                {
                    prototype = item as SE_Stats;
                    return;
                }

            prototype = ScriptableObject.CreateInstance<SE_Stats>();
            prototype.name = EffectId;
            ReflectionAccess.SetFieldIfExists(prototype, "m_name", NameKey);
            ReflectionAccess.SetFieldIfExists(prototype, "m_tooltip", DescriptionKey);
            ReflectionAccess.SetFieldIfExists(prototype, "m_ttl", CestusFacebreaker.SlowDurationSeconds);
            ReflectionAccess.SetFieldIfExists(prototype, "m_speedModifier", -CestusFacebreaker.SlowFraction);
            ObjectDB.instance.m_StatusEffects.Add(prototype);
        }

        internal static void Apply(Character target)
        {
            if (target == null || target.IsDead())
                return;
            EnsureRegistered();
            if (prototype == null)
                return;
            SEMan seman = target.GetSEMan();
            if (seman == null)
                return;

            StatusEffect active = seman.GetStatusEffect(prototype.NameHash());
            if (active != null)
            {
                // Refresh rather than stacking identical -90% modifiers.
                active.ResetTime();
                return;
            }

            foreach (MethodInfo method in AddMethods)
            {
                if (method.Name != "AddStatusEffect") continue;
                ParameterInfo[] ps = method.GetParameters();
                if (ps.Length == 0 || !typeof(StatusEffect).IsAssignableFrom(ps[0].ParameterType)) continue;
                object[] args = new object[ps.Length];
                args[0] = prototype;
                for (int i = 1; i < ps.Length; i++)
                {
                    if (ps[i].HasDefaultValue) args[i] = ps[i].DefaultValue;
                    else if (ps[i].ParameterType == typeof(bool)) args[i] = true;
                    else if (ps[i].ParameterType == typeof(float)) args[i] = 0f;
                    else if (ps[i].ParameterType == typeof(int)) args[i] = 0;
                    else if (ps[i].ParameterType.IsValueType) args[i] = Activator.CreateInstance(ps[i].ParameterType);
                    else args[i] = null;
                }
                try
                {
                    method.Invoke(seman, args);
                    return;
                }
                catch (TargetInvocationException) { }
                catch (ArgumentException) { }
            }

            if (!warned)
            {
                warned = true;
                HaldorExpansionPlugin.Log?.LogWarning("[Cestus Facebreaker] Slow status could not be applied: unsupported SEMan signature.");
            }
        }
    }
}
