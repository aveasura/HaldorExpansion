using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace HaldorExpansion.Patches
{
    [HarmonyPatch]
    internal static class PlayerDamageEntryProbePatch
    {
        private static readonly HashSet<string> ExactMethodNames = new HashSet<string>
        {
            "Damage",
            "RPC_Damage",
            "ApplyDamage",
            "ApplyArmor",
            "ApplyModifier",
            "OnDamaged",
            "LowerHealth",
            "SetHealth",
            "Heal",
            "RPC_Heal",
            "UseHealth"
        };

        private static IEnumerable<MethodBase> TargetMethods()
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Type[] targetTypes =
            {
                typeof(Character),
                typeof(Player),
                typeof(HitData)
            };

            List<MethodInfo> methods = targetTypes
                .SelectMany(type => type.GetMethods(flags))
                .Where(method => !method.IsAbstract)
                .Where(method => !method.IsSpecialName)
                .Where(method => !method.ContainsGenericParameters)
                .Where(method => ExactMethodNames.Contains(method.Name))
                .GroupBy(GetMethodKey)
                .Select(group => group.First())
                .OrderBy(method => method.DeclaringType?.Name)
                .ThenBy(method => method.Name)
                .ToList();

            HaldorExpansionPlugin.DebugLog(
                $"[HaldorExpansion] DamageProbe setup: patching {methods.Count} methods");

            foreach (MethodInfo method in methods)
            {
                HaldorExpansionPlugin.DebugLog(
                    $"[HaldorExpansion] DamageProbe target: {GetFriendlyMethodName(method)}");
            }

            return methods;
        }

        private static string GetMethodKey(MethodInfo method)
        {
            string args = string.Join(",", method.GetParameters().Select(parameter => parameter.ParameterType.FullName));
            return $"{method.DeclaringType?.FullName}:{method.Name}({args})";
        }

        private static void Prefix(object __instance, MethodBase __originalMethod, object[] __args)
        {
            LogInvocation("PREFIX", __instance, __originalMethod, __args);
        }

        private static void Postfix(object __instance, MethodBase __originalMethod, object[] __args)
        {
            LogInvocation("POSTFIX", __instance, __originalMethod, __args);
        }

        private static void LogInvocation(string phase, object instance, MethodBase method, object[] args)
        {
            if (!ShouldLog(instance, args))
                return;

            HaldorExpansionPlugin.DebugLog(
                $"[HaldorExpansion] DamageProbe {phase}: " +
                $"method={GetFriendlyMethodName(method)}, " +
                $"target={DescribeInstance(instance)}, " +
                $"args=[{DescribeArgs(args)}]");
        }

        private static bool ShouldLog(object instance, object[] args)
        {
            if (instance is Player)
                return true;

            if (instance is Character character && character is Player)
                return true;

            if (instance is HitData)
                return ContainsInterestingArg(args);

            return ContainsInterestingArg(args);
        }

        private static bool ContainsInterestingArg(object[] args)
        {
            if (args == null)
                return false;

            for (int i = 0; i < args.Length; ++i)
            {
                object arg = args[i];
                if (arg is HitData)
                    return true;

                if (arg is Player)
                    return true;

                if (arg is Character character && character is Player)
                    return true;
            }

            return false;
        }

        private static string DescribeInstance(object instance)
        {
            if (instance == null)
                return "null";

            if (instance is Player player)
                return $"Player({HaldorExpansionPlugin.GetCestusDebugPlayerTag(player)})";

            if (instance is Character character)
                return character.GetType().Name;

            if (instance is HitData hit)
                return $"HitData(total={HaldorExpansionPlugin.GetHitTotalDamage(hit):0.##})";

            return instance.GetType().Name;
        }

        private static string GetFriendlyMethodName(MethodBase method)
        {
            if (method == null)
                return "<null>";

            string parameters = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
            return $"{method.DeclaringType?.Name}.{method.Name}({parameters})";
        }

        private static string DescribeArgs(object[] args)
        {
            if (args == null || args.Length == 0)
                return string.Empty;

            List<string> parts = new List<string>(args.Length);
            for (int i = 0; i < args.Length; ++i)
            {
                parts.Add($"arg{i}={DescribeArg(args[i])}");
            }

            return string.Join("; ", parts);
        }

        private static string DescribeArg(object arg)
        {
            if (arg == null)
                return "null";

            if (arg is HitData hit)
                return $"HitData(total={HaldorExpansionPlugin.GetHitTotalDamage(hit):0.##})";

            if (arg is Player player)
                return $"Player({HaldorExpansionPlugin.GetCestusDebugPlayerTag(player)})";

            if (arg is Character character)
                return character.GetType().Name;

            if (arg is long longValue)
                return $"long({longValue})";

            if (arg is int intValue)
                return $"int({intValue})";

            if (arg is bool boolValue)
                return $"bool({boolValue})";

            if (arg is float floatValue)
                return $"float({floatValue:0.##})";

            if (arg is Enum enumValue)
                return $"{arg.GetType().Name}({enumValue})";

            return $"{arg.GetType().Name}({arg})";
        }
    }
}
