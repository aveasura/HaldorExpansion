using HaldorExpansion.Features.HelOath;
using HaldorExpansion.Configuration;
using HaldorExpansion.Diagnostics;
using HaldorExpansion.Features.Cestus;
using HaldorExpansion.Presentation;
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
        internal const string ModGuid = "aveasura.haldor.expansion";
        private const string ModName = "Haldor Expansion";
        private const string ModVersion = "1.3.14";

        internal static ManualLogSource Log;
        internal static HaldorExpansionPlugin Instance;

        private static readonly bool DebugLogging = false;

        // Keep config binding state scoped to this plugin instance.
        private readonly ShopConfiguration _shopConfiguration = new ShopConfiguration();
        private readonly SellPriceConfiguration _sellPriceConfiguration = new SellPriceConfiguration();

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

            _shopConfiguration.RegisterShopProgressionConfig(Config);
            _sellPriceConfiguration.RegisterHaldorSellPriceConfig(Config);
            ModLocalization.AddLocalizations();
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ModGuid);
            
            if (DebugLogging)
            {
                AdrenalineDiagnostics.DumpAdrenalineReflection();
            }
            CestusInputRegistration.RegisterCestusInput(Config);
            HelOathConfiguration.Register(Config);
        }
    }
}