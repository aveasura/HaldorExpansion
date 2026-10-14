using System;
using BepInEx.Logging;
using HarmonyLib;

namespace HaldorExpansion.Compatibility
{
    // Only hide the three observed vanilla HotSpring name-collision warnings.
    // Never change AssetManager's lookup/selection logic or blanket-disable logging.
    internal static class KnownAssetWarningFilter
    {
        private const string SourceName = "Jotunn.Managers.AssetManager";
        private static Harmony harmony;

        internal static void Install()
        {
            if (harmony != null) return;
            try
            {
                var target = AccessTools.Method(typeof(ManualLogSource), nameof(ManualLogSource.Log),
                    new[] { typeof(LogLevel), typeof(object) });
                if (target == null) throw new MissingMethodException("ManualLogSource.Log(LogLevel, object)");
                var owner = new Harmony(HaldorExpansionPlugin.ModGuid + ".knownassetwarnings");
                owner.Patch(target,
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(KnownAssetWarningFilter), nameof(Prefix))));
                harmony = owner;
            }
            catch (Exception error)
            {
                // Optional cosmetic filter: an incompatible logger must not block mod startup.
                HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] Known asset warning filter unavailable: " +
                                                      error);
            }
        }

        internal static void Shutdown()
        {
            if (harmony == null) return;
            harmony.UnpatchSelf();
            harmony = null;
        }

        private static bool Prefix(ManualLogSource __instance, LogLevel __0, object __1)
        {
            return ShouldLog(__instance.SourceName, __0, __1);
        }

        internal static bool ShouldLog(string sourceName, LogLevel level, object data)
        {
            if (level != LogLevel.Warning || sourceName != SourceName || !(data is string text)) return true;
            switch (text)
            {
                case
                    "Ambiguous asset name for path. old: Assets/world/Props/stone/Stone1_huge.prefab, new: Assets/world/Props/DeepNorth/HotSpring/Stone1_huge.prefab, using old path"
                    :
                case
                    "Ambiguous asset name for path. old: Assets/world/Props/Caverocks/caverock_pillar.prefab, new: Assets/world/Props/DeepNorth/HotSpring/caverock_pillar.prefab, using old path"
                    :
                case
                    "Ambiguous asset name for path. old: Assets/world/Props/Rocks/highstone.prefab, new: Assets/world/Props/DeepNorth/HotSpring/highstone.prefab, using old path"
                    :
                    return false;
                default:
                    return true;
            }
        }
    }
}