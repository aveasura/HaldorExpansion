using System;
using System.Reflection;
using HaldorExpansion.Compatibility;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Features.Berserker
{
    internal static class BerserkerRagnarokExhaustion
    {
        private const string EffectName = BerserkerIds.ExhaustionStatus;
        private static SE_Stats prototype;
        private static ObjectDB registeredDb;

        internal static void EnsureRegistered()
        {
            if (ObjectDB.instance == null || ObjectDB.instance.m_StatusEffects == null) return;
            if (registeredDb != ObjectDB.instance)
            {
                registeredDb = ObjectDB.instance;
                prototype = null;
            }

            foreach (StatusEffect existing in ObjectDB.instance.m_StatusEffects)
            {
                if (existing != null && existing.name == EffectName)
                {
                    prototype = existing as SE_Stats;
                    Configure(prototype);
                    return;
                }
            }

            prototype = ScriptableObject.CreateInstance<SE_Stats>();
            prototype.name = EffectName;
            Configure(prototype);

            Sprite[] icons = BerserkerItemRegistration.ItemDrop?.m_itemData?.m_shared?.m_icons;
            if (icons != null && icons.Length > 0)
                ReflectionAccess.SetFieldIfExists(prototype, "m_icon", icons[0]);

            ObjectDB.instance.m_StatusEffects.Add(prototype);
        }

        internal static void Apply(Player player)
        {
            if (player == null || player.IsDead()) return;
            EnsureRegistered();
            if (prototype == null) return;
            Configure(prototype);

            object seman = GetSEMan(player);
            if (seman == null) return;

            foreach (MethodInfo method in typeof(SEMan).GetMethods(BindingFlags.Instance | BindingFlags.Public |
                                                                   BindingFlags.NonPublic))
            {
                if (method.Name != "AddStatusEffect") continue;
                ParameterInfo[] ps = method.GetParameters();
                if (ps.Length == 0 || !typeof(StatusEffect).IsAssignableFrom(ps[0].ParameterType)) continue;
                try
                {
                    method.Invoke(seman, BuildArgs(ps, prototype));
                    return;
                }
                catch (TargetInvocationException)
                {
                }
                catch (ArgumentException)
                {
                }
            }

            HaldorExpansionPlugin.Log?.LogWarning(
                "[Berserker] Could not apply Ragnarok Exhaustion with the current SEMan API.");
        }

        private static void Configure(SE_Stats stats)
        {
            if (stats == null) return;
            ReflectionAccess.SetFieldIfExists(stats, "m_name", BerserkerIds.ExhaustionKey);
            ReflectionAccess.SetFieldIfExists(stats, "m_tooltip", BerserkerIds.ExhaustionDescriptionKey);
            ReflectionAccess.SetFieldIfExists(stats, "m_ttl", BerserkerConfiguration.RagnarokExhaustionDurationValue);
            ReflectionAccess.SetFieldIfExists(stats, "m_speedModifier",
                -BerserkerConfiguration.RagnarokExhaustionMovePenaltyFraction);
        }

        private static object GetSEMan(Player player)
        {
            FieldInfo field = AccessTools.Field(typeof(Character), "m_seman") ??
                              AccessTools.Field(typeof(Player), "m_seman");
            if (field != null) return field.GetValue(player);
            MethodInfo getter = AccessTools.Method(typeof(Character), "GetSEMan") ??
                                AccessTools.Method(typeof(Player), "GetSEMan");
            return getter?.Invoke(player, null);
        }

        private static object[] BuildArgs(ParameterInfo[] parameters, object first)
        {
            var args = new object[parameters.Length];
            args[0] = first;
            for (int i = 1; i < parameters.Length; i++)
            {
                ParameterInfo p = parameters[i];
                if (p.HasDefaultValue) args[i] = p.DefaultValue;
                else if (p.ParameterType == typeof(bool)) args[i] = true;
                else if (p.ParameterType == typeof(int)) args[i] = 0;
                else if (p.ParameterType == typeof(float)) args[i] = 0f;
                else if (p.ParameterType.IsValueType) args[i] = Activator.CreateInstance(p.ParameterType);
                else args[i] = null;
            }

            return args;
        }
    }
}