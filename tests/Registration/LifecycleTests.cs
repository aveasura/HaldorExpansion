using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HaldorExpansion.Patches;

class LifecycleTests
{
    static int failures;

    static void Check(bool ok, string name)
    {
        Console.WriteLine((ok ? "PASS: " : "FAIL: ") + name);
        if (!ok) failures++;
    }

    static void Main()
    {
        ObjectDB.instance = new ObjectDB();
        var type = typeof(ObjectDB_Awake_Patch);
        var init = type.GetMethod("Initialize", BindingFlags.Static | BindingFlags.NonPublic);
        if (init != null) init.Invoke(null, null);
        Jotunn.Managers.PrefabManager.FireVanilla();
        Check(Probe.Items.Count == 13, "all 13 items are prepared before menu DB copy, without recipes");
        Check(Probe.Recipes == 0, "early item preparation does not require recipes");
        Probe.Items.Clear();
        ObjectDB.instance.m_items.Add(new UnityEngine.GameObject());
        type.GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        Check(Probe.Items.Count == 13, "populated DB initializes synchronously even with zero recipes");
        ObjectDB.instance.m_recipes.Add(new object());
        Jotunn.Managers.ItemManager.FireMenuRegistered();
        Check(Probe.Recipes > 0, "recipe registration runs after menu DB is populated");
        if (failures != 0) Environment.Exit(1);
    }
}

static class Probe
{
    public static HashSet<string> Items = new HashSet<string>();
    public static int Recipes;

    public static bool Item(string n)
    {
        Items.Add(n);
        return true;
    }
}

public class ObjectDB
{
    public static ObjectDB instance;
    public List<UnityEngine.GameObject> m_items = new List<UnityEngine.GameObject>();
    public List<object> m_recipes = new List<object>();
}

namespace UnityEngine
{
    public class GameObject
    {
    }

    public class WaitUntil
    {
        public Func<bool> Predicate;

        public WaitUntil(Func<bool> p)
        {
            Predicate = p;
        }
    }
}

namespace HarmonyLib
{
    public class HarmonyPatch : Attribute
    {
        public HarmonyPatch(Type t, string n)
        {
        }
    }
}

namespace HaldorExpansion
{
    class HaldorExpansionPlugin
    {
        public static HaldorExpansionPlugin Instance = new HaldorExpansionPlugin();
        readonly List<IEnumerator> routines = new List<IEnumerator>();

        public void StartCoroutine(IEnumerator routine)
        {
            routine.MoveNext();
            routines.Add(routine);
        }

        public void Advance()
        {
            foreach (var r in routines)
            {
                var w = r.Current as UnityEngine.WaitUntil;
                if (w == null || w.Predicate()) r.MoveNext();
            }
        }
    }
}

namespace Jotunn.Managers
{
    static class PrefabManager
    {
        public static event Action OnVanillaPrefabsAvailable;

        public static void FireVanilla()
        {
            OnVanillaPrefabsAvailable?.Invoke();
        }
    }

    static class ItemManager
    {
        public static event Action OnItemsRegisteredFejd;
        public static event Action OnItemsRegistered;
        public static void FireWorldRegistered() { OnItemsRegistered?.Invoke(); }

        public static void FireMenuRegistered()
        {
            OnItemsRegisteredFejd?.Invoke();
        }
    }
}

namespace HaldorExpansion.Integration
{
    static class ItemCacheReset
    {
        public static void ResetItemCaches()
        {
        }
    }
}

namespace HaldorExpansion.Trading
{
    static class TrophyValueUpdater
    {
        public static void ApplyTrophyValues(ObjectDB db)
        {
        }
    }
}

namespace HaldorExpansion.Features.Brisingamen
{
    static class BrisingamenItemRegistration
    {
        public static bool EnsureRingReady() => Probe.Item("BrisingamenItemRegistration");
    }
}

namespace HaldorExpansion.Features.NornThread
{
    static class NornThreadItemRegistration
    {
        public static bool EnsureReady() => Probe.Item("NornThreadItemRegistration");
    }
}

namespace HaldorExpansion.Features.DelayedDoom
{
    static class DelayedDoomItemRegistration
    {
        public static bool EnsureDelayedDoomChestReady() => Probe.Item("DelayedDoomItemRegistration");

        public static void RegisterDelayedDoomChestRecipe()
        {
            Probe.Recipes++;
        }
    }
}

namespace HaldorExpansion.Features.PitKing
{
    static class PitKingItemRegistration
    {
        public static bool EnsurePitKingChestReady() => Probe.Item("PitKingItemRegistration");

        public static void RegisterPitKingChestRecipe()
        {
            Probe.Recipes++;
        }
    }
}

namespace HaldorExpansion.Features.Cestus
{
    static class CestusItemRegistration
    {
        public static bool EnsureCestusReady() => Probe.Item("CestusItemRegistration");

        public static void RegisterCestusRecipe()
        {
            Probe.Recipes++;
        }
    }
}

namespace HaldorExpansion.Features.ShadowCrossbow
{
    static class ShadowCrossbowItemRegistration
    {
        public static bool EnsureShadowCrossbowReady() => Probe.Item("ShadowCrossbowItemRegistration");

        public static void RegisterShadowCrossbowRecipe()
        {
            Probe.Recipes++;
        }
    }
}

namespace HaldorExpansion.Features.HelOath
{
    static class HelOathItemRegistration
    {
        public static bool EnsureHelOathReady() => Probe.Item("HelOathItemRegistration");

        public static void RegisterHelOathRecipe()
        {
            Probe.Recipes++;
        }
    }

    static class HelOathTouchStatus
    {
        public static void EnsureRegistered()
        {
        }
    }
}

namespace HaldorExpansion.Features.Berserker
{
    static class BerserkerItemRegistration
    {
        public static bool EnsureReady() => Probe.Item("BerserkerItemRegistration");

        public static void RegisterRecipe()
        {
            Probe.Recipes++;
        }
    }

    static class BerserkerCloakItemRegistration
    {
        public static bool EnsureReady() => Probe.Item("BerserkerCloakItemRegistration");

        public static void RegisterRecipe()
        {
            Probe.Recipes++;
        }
    }

    static class BerserkerHelmetItemRegistration
    {
        public static bool EnsureReady() => Probe.Item("BerserkerHelmetItemRegistration");

        public static void RegisterRecipe()
        {
            Probe.Recipes++;
        }
    }

    static class BerserkerWrathStatus
    {
        public static void EnsureRegistered()
        {
        }
    }

    static class BerserkerRagnarokExhaustion
    {
        public static void EnsureRegistered()
        {
        }
    }
}

namespace HaldorExpansion.Features.WoundedBeast
{
    static class WoundedBeastItemRegistration
    {
        public static bool EnsureWoundedBeastCapeReady() => Probe.Item("WoundedBeastItemRegistration");

        public static void RegisterWoundedBeastCapeRecipe()
        {
            Probe.Recipes++;
        }
    }
}

namespace HaldorExpansion.Features.PeltOfHelheim
{
    static class PeltOfHelheimItemRegistration
    {
        public static bool EnsureReady() => Probe.Item("PeltOfHelheimItemRegistration");

        public static void RegisterRecipe()
        {
            Probe.Recipes++;
        }
    }

    static class PeltOfHelheimStatus
    {
        public static void EnsureRegistered()
        {
        }
    }
}

namespace HaldorExpansion.Features.StaminaCape
{
    static class StaminaCapeItemRegistration
    {
        public static bool EnsureReady() => Probe.Item("StaminaCapeItemRegistration");

        public static void RegisterRecipe()
        {
            Probe.Recipes++;
        }
    }
}
namespace HaldorExpansion.Features.Upgrades { static class IdolRegistration { public static bool EnsureReady() => true; } static class UpgradeRecipes { public static void Register() { Probe.Recipes++; } } }
