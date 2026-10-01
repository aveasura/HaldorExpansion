using System;
using System.Collections.Generic;
using System.Reflection;
using HaldorExpansion.Compatibility;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Features.PeltOfHelheim
{
    internal static class PeltOfHelheimStatus
    {
        private const string EffectObjectName = "SE_PeltOfHelheimWard";
        private static SE_Stats prototype;
        private static StatusEffect active;
        private static ObjectDB registeredDb;

        internal static void ResetRegistration()
        {
            prototype = null;
            active = null;
            registeredDb = null;
        }

        internal static void EnsureRegistered()
        {
            if (ObjectDB.instance == null || ObjectDB.instance.m_StatusEffects == null) return;
            if (registeredDb != ObjectDB.instance)
            {
                registeredDb = ObjectDB.instance;
                prototype = null;
                active = null;
            }

            foreach (StatusEffect existing in ObjectDB.instance.m_StatusEffects)
            {
                if (existing != null && existing.name == EffectObjectName)
                {
                    prototype = existing as SE_Stats;
                    Configure(prototype);
                    return;
                }
            }

            var se = ScriptableObject.CreateInstance<SE_Stats>();
            se.name = EffectObjectName;
            Configure(se);

            Sprite icon = GetCapeIcon();
            if (icon != null) ReflectionAccess.SetFieldIfExists(se, "m_icon", icon);

            ObjectDB.instance.m_StatusEffects.Add(se);
            prototype = se;
        }

        internal static void Sync(Player player, bool enabled)
        {
            if (player == null || player != Player.m_localPlayer) return;
            EnsureRegistered();
            if (prototype == null) return;

            if (enabled)
            {
                if (active != null) return;
                active = Add(player, prototype);
                if (active is SE_Stats stats) Configure(stats);
                return;
            }

            Remove(player);
        }

        private static void Configure(SE_Stats stats)
        {
            if (stats == null) return;
            ReflectionAccess.SetFieldIfExists(stats, "m_name", PeltOfHelheimItemRegistration.EffectNameKey);
            ReflectionAccess.SetFieldIfExists(stats, "m_tooltip", PeltOfHelheimItemRegistration.EffectDescKey);
            ReflectionAccess.SetFieldIfExists(stats, "m_ttl", 0f);
            stats.m_mods = new List<HitData.DamageModPair>
            {
                new HitData.DamageModPair
                {
                    m_type = HitData.DamageType.Pierce,
                    m_modifier = HitData.DamageModifier.VeryResistant
                }
            };
        }

        private static Sprite GetCapeIcon()
        {
            Sprite[] icons = PeltOfHelheimItemRegistration.CapeItemDrop?.m_itemData?.m_shared?.m_icons;
            return icons != null && icons.Length > 0 ? icons[0] : null;
        }

        private static StatusEffect Add(Player player, StatusEffect effect)
        {
            object seman = GetSEMan(player);
            if (seman == null || effect == null) return null;

            MethodInfo[] methods = typeof(SEMan).GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            for (int pass = 0; pass < 2; pass++)
            {
                foreach (MethodInfo method in methods)
                {
                    if (method.Name != "AddStatusEffect") continue;
                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length == 0) continue;

                    bool statusOverload = typeof(StatusEffect).IsAssignableFrom(parameters[0].ParameterType);
                    if ((pass == 0) != statusOverload) continue;

                    object first;
                    if (statusOverload) first = effect;
                    else if (parameters[0].ParameterType == typeof(int)) first = StableHash(effect.name);
                    else continue;

                    try
                    {
                        object result = method.Invoke(seman, BuildArgs(parameters, first));
                        if (result is StatusEffect status) return status;
                        StatusEffect found = FindActive(seman);
                        if (found != null) return found;
                    }
                    catch (TargetInvocationException) { }
                    catch (ArgumentException) { }
                }
            }

            HaldorExpansionPlugin.Log?.LogWarning(
                "[HaldorExpansion] Could not apply Pelt of Helheim status effect with the current SEMan API.");
            return null;
        }

        private static void Remove(Player player)
        {
            if (active == null) return;
            object seman = GetSEMan(player);
            if (seman != null)
            {
                foreach (MethodInfo method in typeof(SEMan).GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (method.Name != "RemoveStatusEffect") continue;
                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length == 0) continue;

                    object first;
                    if (parameters[0].ParameterType == typeof(int)) first = StableHash(EffectObjectName);
                    else if (typeof(StatusEffect).IsAssignableFrom(parameters[0].ParameterType)) first = active;
                    else continue;

                    try
                    {
                        method.Invoke(seman, BuildArgs(parameters, first));
                        break;
                    }
                    catch (TargetInvocationException) { }
                    catch (ArgumentException) { }
                }
            }
            active = null;
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

        private static StatusEffect FindActive(object seman)
        {
            if (seman == null) return null;
            foreach (FieldInfo field in typeof(SEMan).GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!(field.GetValue(seman) is IEnumerable<StatusEffect> effects)) continue;
                foreach (StatusEffect effect in effects)
                {
                    if (effect != null && effect.name != null &&
                        effect.name.StartsWith(EffectObjectName, StringComparison.Ordinal))
                        return effect;
                }
            }
            return null;
        }

        private static object[] BuildArgs(ParameterInfo[] parameters, object first)
        {
            var args = new object[parameters.Length];
            args[0] = first;
            for (int i = 1; i < parameters.Length; i++)
            {
                ParameterInfo p = parameters[i];
                if (p.HasDefaultValue) args[i] = p.DefaultValue;
                else if (p.ParameterType == typeof(bool)) args[i] = false;
                else if (p.ParameterType == typeof(int)) args[i] = 0;
                else if (p.ParameterType == typeof(float)) args[i] = 0f;
                else if (p.ParameterType.IsValueType) args[i] = Activator.CreateInstance(p.ParameterType);
                else args[i] = null;
            }
            return args;
        }

        private static int StableHash(string text)
        {
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;
                for (int i = 0; i < text.Length && text[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ text[i];
                    if (i == text.Length - 1 || text[i + 1] == '\0') break;
                    hash2 = ((hash2 << 5) + hash2) ^ text[i + 1];
                }
                return hash1 + hash2 * 1566083941;
            }
        }
    }
}
