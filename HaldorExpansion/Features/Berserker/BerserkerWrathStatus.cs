using System;
using System.Collections.Generic;
using System.Reflection;
using HaldorExpansion.Compatibility;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Features.Berserker
{
    internal static class BerserkerWrathStatus
    {
        private static SE_Stats prototype;
        private static StatusEffect active;
        private static int activeStage;
        private static ObjectDB registeredDb;

        internal static void EnsureRegistered()
        {
            if (ObjectDB.instance == null || ObjectDB.instance.m_StatusEffects == null) return;
            if (registeredDb != ObjectDB.instance)
            {
                registeredDb = ObjectDB.instance;
                prototype = null;
                active = null;
                activeStage = 0;
            }

            foreach (StatusEffect existing in ObjectDB.instance.m_StatusEffects)
            {
                if (existing != null && existing.name == BerserkerIds.WrathStatus)
                {
                    prototype = existing as SE_Stats;
                    return;
                }
            }

            var se = ScriptableObject.CreateInstance<SE_Stats>();
            se.name = BerserkerIds.WrathStatus;
            Configure(se, 1);

            Sprite icon = GetAxeIcon();
            if (icon != null) ReflectionAccess.SetFieldIfExists(se, "m_icon", icon);

            ObjectDB.instance.m_StatusEffects.Add(se);
            prototype = se;
        }

        internal static void Sync(Player player, int stage)
        {
            if (player == null || player != Player.m_localPlayer) return;
            stage = Mathf.Clamp(stage, 0, BerserkerState.MaxWrathStage);
            if (stage == activeStage && ((stage == 0 && active == null) || (stage > 0 && active != null))) return;

            EnsureRegistered();
            if (prototype == null) return;

            RemoveCurrent(player);
            activeStage = 0;
            if (stage <= 0) return;

            Configure(prototype, stage);
            StatusEffect applied = Add(player, prototype);
            if (applied == null) return;
            active = applied;
            activeStage = stage;
            Configure(active, stage);
        }

        private static void Configure(StatusEffect effect, int stage)
        {
            if (effect == null) return;
            stage = Mathf.Clamp(stage, 1, BerserkerState.MaxWrathStage);
            ReflectionAccess.SetFieldIfExists(effect, "m_name", BerserkerIds.WrathKeyPrefix + stage);
            ReflectionAccess.SetFieldIfExists(effect, "m_tooltip", BerserkerIds.WrathKeyPrefix + stage + "_desc");
            ReflectionAccess.SetFieldIfExists(effect, "m_ttl", 0f);
        }

        private static Sprite GetAxeIcon()
        {
            ItemDrop drop = BerserkerItemRegistration.ItemDrop;
            Sprite[] icons = drop?.m_itemData?.m_shared?.m_icons;
            return icons != null && icons.Length > 0 ? icons[0] : null;
        }

        private static StatusEffect Add(Player player, StatusEffect effect)
        {
            object seman = GetSEMan(player);
            if (seman == null || effect == null) return null;

            MethodInfo[] methods =
                typeof(SEMan).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int pass = 0; pass < 2; pass++)
            {
                foreach (MethodInfo method in methods)
                {
                    if (method.Name != "AddStatusEffect") continue;
                    ParameterInfo[] ps = method.GetParameters();
                    if (ps.Length == 0) continue;

                    bool statusOverload = typeof(StatusEffect).IsAssignableFrom(ps[0].ParameterType);
                    if ((pass == 0) != statusOverload) continue;

                    object first;
                    if (statusOverload) first = effect;
                    else if (ps[0].ParameterType == typeof(int)) first = StableHash(effect.name);
                    else continue;

                    try
                    {
                        object result = method.Invoke(seman, BuildArgs(ps, first));
                        if (result is StatusEffect status) return status;
                        StatusEffect found = FindActive(seman, effect.name);
                        if (found != null) return found;
                    }
                    catch (TargetInvocationException)
                    {
                    }
                    catch (ArgumentException)
                    {
                    }
                }
            }

            HaldorExpansionPlugin.Log?.LogWarning(
                "[Berserker] Could not apply Wrath status effect with the current SEMan API.");
            return null;
        }

        private static void RemoveCurrent(Player player)
        {
            if (active == null && activeStage == 0) return;
            object seman = GetSEMan(player);
            if (seman != null)
            {
                foreach (MethodInfo method in typeof(SEMan).GetMethods(BindingFlags.Instance | BindingFlags.Public |
                                                                       BindingFlags.NonPublic))
                {
                    if (method.Name != "RemoveStatusEffect") continue;
                    ParameterInfo[] ps = method.GetParameters();
                    if (ps.Length == 0) continue;

                    object first;
                    if (ps[0].ParameterType == typeof(int)) first = StableHash(BerserkerIds.WrathStatus);
                    else if (typeof(StatusEffect).IsAssignableFrom(ps[0].ParameterType)) first = active ?? prototype;
                    else continue;

                    try
                    {
                        method.Invoke(seman, BuildArgs(ps, first));
                        break;
                    }
                    catch (TargetInvocationException)
                    {
                    }
                    catch (ArgumentException)
                    {
                    }
                }
            }

            active = null;
            activeStage = 0;
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

        private static StatusEffect FindActive(object seman, string objectName)
        {
            if (seman == null) return null;
            foreach (FieldInfo field in typeof(SEMan).GetFields(BindingFlags.Instance | BindingFlags.Public |
                                                                BindingFlags.NonPublic))
            {
                object value = field.GetValue(seman);
                if (!(value is IEnumerable<StatusEffect> effects)) continue;
                foreach (StatusEffect effect in effects)
                    if (effect != null && effect.name != null &&
                        effect.name.StartsWith(objectName, StringComparison.Ordinal))
                        return effect;
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