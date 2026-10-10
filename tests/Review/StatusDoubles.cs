// Boundary doubles only: these tests do not run Unity, a HUD, or real Valheim.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEngine
{
    public class Sprite { }
    public class ScriptableObject
    {
        public string name;
        public static T CreateInstance<T>() where T : new() => new T();
    }
    public static class Mathf { public static int Clamp(int value, int min, int max) => Math.Clamp(value, min, max); }
}
namespace HarmonyLib
{
    public static class AccessTools
    {
        public static FieldInfo Field(Type type, string name) => type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        public static MethodInfo Method(Type type, string name) => type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    }
}
public class StatusEffect : UnityEngine.ScriptableObject
{
    public string m_name, m_tooltip;
    public float m_ttl;
    public UnityEngine.Sprite m_icon;
    public StatusEffect Clone() => (StatusEffect)MemberwiseClone();
    public int NameHash() => StableHash(name);
    public static int StableHash(string text)
    {
        unchecked
        {
            int first = 5381, second = 5381;
            for (int i = 0; i < text.Length && text[i] != '\0'; i += 2)
            {
                first = ((first << 5) + first) ^ text[i];
                if (i == text.Length - 1 || text[i + 1] == '\0') break;
                second = ((second << 5) + second) ^ text[i + 1];
            }
            return first + second * 1566083941;
        }
    }
}
public class SE_Stats : StatusEffect
{
    public float m_speedModifier;
    public List<HitData.DamageModPair> m_mods = new List<HitData.DamageModPair>();
}
public class HitData
{
    public enum DamageType { Pierce }
    public enum DamageModifier { VeryResistant }
    public struct DamageModPair { public DamageType m_type; public DamageModifier m_modifier; }
}
public class SEMan
{
    public readonly List<StatusEffect> Effects = new List<StatusEffect>();
    public bool FailAdd, ThrowOnAdd;
    public int Adds, Removals;
    public StatusEffect GetStatusEffect(int hash) => Effects.Find(effect => effect.NameHash() == hash);
    public StatusEffect AddStatusEffect(StatusEffect effect, bool resetTime = false, int itemLevel = 0, float skillLevel = 0f, short variant = -1)
    {
        Adds++;
        if (ThrowOnAdd) throw new InvalidOperationException("Simulated API failure");
        if (FailAdd || GetStatusEffect(effect.NameHash()) != null) return null;
        var clone = effect.Clone();
        Effects.Add(clone);
        return clone;
    }
    public bool RemoveStatusEffect(int hash, bool quiet = false)
    {
        Removals++;
        var active = GetStatusEffect(hash);
        return active != null && Effects.Remove(active); // Vanilla does not destroy the cached object.
    }
    public bool RemoveStatusEffect(StatusEffect effect, bool quiet = false) => RemoveStatusEffect(effect.NameHash(), quiet);
}
public class Character
{
    public SEMan m_seman = new SEMan();
    public SEMan GetSEMan() => m_seman;
}
public class Player : Character
{
    public static Player m_localPlayer;
    public float Health = 123f, Stamina = 87f;
}
public class CountingStatusList : IEnumerable<StatusEffect>
{
    private readonly List<StatusEffect> values = new List<StatusEffect>();
    public int Enumerations;
    public int Count => values.Count;
    public StatusEffect this[int index] => values[index];
    public void Add(StatusEffect effect) => values.Add(effect);
    public IEnumerator<StatusEffect> GetEnumerator() { Enumerations++; return values.GetEnumerator(); }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
public class ObjectDB
{
    public static ObjectDB instance;
    public CountingStatusList m_StatusEffects = new CountingStatusList();
}
public class ItemDrop
{
    public ItemData m_itemData = new ItemData();
    public class ItemData { public SharedData m_shared = new SharedData(); }
    public class SharedData { public UnityEngine.Sprite[] m_icons; }
}
namespace HaldorExpansion
{
    internal static class HaldorExpansionPlugin
    {
        internal static readonly Logger Log = new Logger();
        internal class Logger { public void LogWarning(string text) { } }
    }
}
namespace HaldorExpansion.Features.HelOath
{
    internal static class HelOathItemRegistration { internal static ItemDrop HelOathItemDrop; }
}
namespace HaldorExpansion.Features.PeltOfHelheim
{
    internal static class PeltOfHelheimItemRegistration
    {
        internal static ItemDrop CapeItemDrop;
        internal const string EffectNameKey = "$se_peltofhelheim";
        internal const string EffectDescKey = "$se_peltofhelheim_desc";
    }
}
namespace HaldorExpansion.Features.DelayedDoom
{
    internal static class DelayedDoomItemRegistration { internal static ItemDrop DelayedDoomChestItemDrop; }
}
