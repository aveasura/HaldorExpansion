using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn;
using Jotunn.Utils;

namespace HaldorExpansion
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency(Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public partial class HaldorExpansionPlugin : BaseUnityPlugin
    {
        private const string ModGuid = "aveasura.haldor.expansion";
        private const string ModName = "Haldor Expansion";
        private const string ModVersion = "1.0.1";

        internal static ManualLogSource Log;
        internal static HaldorExpansionPlugin Instance;

        private static readonly bool DebugLogging = false;

        internal static void DebugLog(string message)
        {
            if (DebugLogging)
            {
                Log?.LogInfo(message);
            }
        }

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            AddLocalizations();
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ModGuid);
            
            if (DebugLogging)
            {
                DumpAdrenalineReflection();
            }
            RegisterCestusInput();
        }
    }
}