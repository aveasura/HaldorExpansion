// Boundary adapters only. Translation text, IDs, constants and config calculations
// come from production sources; BepInEx ConfigFile/ConfigEntry are real assemblies.
using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;

public class Localization
{
    public static Localization instance;
    public string Language;
    public readonly Dictionary<string, string> Words = new Dictionary<string, string>();
    public string GetSelectedLanguage() => Language;
    // Mirrors native Localize/AddWord: changing a word does not invalidate cached text.
    private readonly TranslationCache m_cache = new TranslationCache();
    private void AddWord(string key, string value) => Words[key] = value;
    public string Localize(string text)
    {
        if (m_cache.Text.TryGetValue(text, out string translated)) return translated;
        translated = System.Text.RegularExpressions.Regex.Replace(text, @"\$[A-Za-z0-9_]+", match =>
            Words.TryGetValue(match.Value.Substring(1), out string word) ? word : match.Value);
        m_cache.Text[text] = translated;
        return translated;
    }
    private class TranslationCache
    {
        public readonly Dictionary<string, string> Text = new Dictionary<string, string>();
        public void EvictAll() => Text.Clear();
    }
}
namespace HarmonyLib
{
    public static class AccessTools
    {
        public static FieldInfo Field(Type type, string name) =>
            type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        public static MethodInfo Method(Type type, string name) =>
            type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    }
}
namespace Jotunn.Entities
{
    public class CustomLocalization
    {
        public readonly Dictionary<string, Dictionary<string, string>> Languages =
            new Dictionary<string, Dictionary<string, string>>();
        public void AddTranslation(string language, Dictionary<string, string> words) => Languages[language] = words;
    }
}
namespace Jotunn.Managers
{
    public class LocalizationManager
    {
        public static readonly LocalizationManager Instance = new LocalizationManager();
        private readonly Jotunn.Entities.CustomLocalization localization = new Jotunn.Entities.CustomLocalization();
        public Jotunn.Entities.CustomLocalization GetLocalization() => localization;
    }
    public class InputManager
    {
        public static readonly InputManager Instance = new InputManager();
        public void AddButton(string id, Jotunn.Configs.ButtonConfig config) { }
    }
}
namespace Jotunn.Configs
{
    public class ButtonConfig
    {
        public string Name, HintToken;
        public ConfigEntryBase Config;
        public bool BlockOtherInputs;
    }
}
namespace Jotunn.Extensions
{
    public static class ConfigFileExtensions
    {
        public static ConfigEntry<T> BindConfig<T>(this ConfigFile file, string section, string key, T value,
            string description, bool synced = true, AcceptableValueBase acceptableValues = null) =>
            file.Bind(section, key, value, new ConfigDescription(description, acceptableValues));
    }
}
namespace UnityEngine
{
    public static class Mathf
    {
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));
        public static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
    }
}
namespace HaldorExpansion
{
    public static class HaldorExpansionPlugin { public const string ModGuid = "localization-review"; }
}
namespace HaldorExpansion.Trading
{

}
