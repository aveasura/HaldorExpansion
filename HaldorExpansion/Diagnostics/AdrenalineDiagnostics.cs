using System.Linq;
using System.Reflection;
using System;

namespace HaldorExpansion.Diagnostics
{
    internal static class AdrenalineDiagnostics
    {
        private static bool _adrenalineReflectionDumped;

        internal static void DumpAdrenalineReflection()
        {
            if (_adrenalineReflectionDumped)
                return;

            _adrenalineReflectionDumped = true;

            try
            {
                Assembly asm = typeof(Player).Assembly;

                HaldorExpansionPlugin.DebugLog("[HaldorExpansion] ===== ADRENALINE / TRINKET REFLECTION DUMP START =====");

                string[] needles =
                {
                    "adren",
                    "trinket"
                };

                Type[] interestingTypes = asm.GetTypes()
                    .Where(t =>
                        needles.Any(n =>
                            (t.FullName ?? string.Empty).IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            t.Name.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0))
                    .OrderBy(t => t.FullName)
                    .ToArray();

                foreach (Type t in interestingTypes)
                {
                    HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] [Reflect] TYPE: {t.FullName}");

                    foreach (FieldInfo f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                    {
                        HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] [Reflect]   FIELD: {f.FieldType.Name} {f.Name}");
                    }

                    foreach (PropertyInfo p in t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                    {
                        HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] [Reflect]   PROP: {p.PropertyType.Name} {p.Name}");
                    }

                    foreach (MethodInfo m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                    {
                        if (needles.Any(n => m.Name.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            string parameters = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                            HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] [Reflect]   METHOD: {m.ReturnType.Name} {m.Name}({parameters})");
                        }
                    }
                }

                DumpRelevantMembers(typeof(Player), "Player");
                DumpRelevantMembers(typeof(Hud), "Hud");
                DumpRelevantMembers(typeof(Humanoid), "Humanoid");
                DumpRelevantMembers(typeof(InventoryGui), "InventoryGui");

                HaldorExpansionPlugin.DebugLog("[HaldorExpansion] ===== ADRENALINE / TRINKET REFLECTION DUMP END =====");
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError($"[HaldorExpansion] Adrenaline reflection dump failed: {e}");
            }
        }

        private static void DumpRelevantMembers(Type t, string label)
        {
            try
            {
                string[] needles =
                {
                    "adren",
                    "trinket"
                };

                HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] [Reflect] ===== {label} relevant members =====");

                foreach (FieldInfo f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    if (needles.Any(n => f.Name.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] [Reflect]   FIELD: {f.FieldType.Name} {f.Name}");
                    }
                }

                foreach (PropertyInfo p in t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    if (needles.Any(n => p.Name.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] [Reflect]   PROP: {p.PropertyType.Name} {p.Name}");
                    }
                }

                foreach (MethodInfo m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    if (needles.Any(n => m.Name.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        string parameters = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                        HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] [Reflect]   METHOD: {m.ReturnType.Name} {m.Name}({parameters})");
                    }
                }
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError($"[HaldorExpansion] DumpRelevantMembers failed for {label}: {e}");
            }
        }
    }
}
