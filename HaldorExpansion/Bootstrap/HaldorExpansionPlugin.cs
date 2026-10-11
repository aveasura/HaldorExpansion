using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HaldorExpansion.Configuration;
using HaldorExpansion.Diagnostics;
using HaldorExpansion.Features.Berserker;
using HaldorExpansion.Features.Cestus;
using HaldorExpansion.Features.HelOath;
using HaldorExpansion.Patches;
using HaldorExpansion.Presentation;
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
        private const string ModVersion = "1.3.0";

        internal static ManualLogSource Log;
        internal static HaldorExpansionPlugin Instance;

        private static readonly bool DebugLogging = false;

        // Keep config binding state scoped to this plugin instance.
        private readonly ConfigurationRuntime _configuration = new ConfigurationRuntime();

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

            _configuration.Open(this);
            _shopConfiguration.RegisterShopProgressionConfig(_configuration.TradeEntries);
            _sellPriceConfiguration.RegisterHaldorSellPriceConfig(_configuration.TradeEntries);

            // Bind every configuration entry before Harmony patching. A compatibility issue in one
            // Harmony target must never leave later runtime systems with null ConfigEntry fields.
            CestusInputRegistration.RegisterCestusInput(Config);
            HelOathConfiguration.Register(_configuration.Balance, Config);
            BerserkerConfiguration.Register(_configuration.Balance, Config);
            ItemTooltipController.RegisterConfig(Config);
            TradingUiConfiguration.Register(Config);

            _configuration.Finish();
            ConfigurationManagerIntegration.Initialize(_configuration, this);
            ModLocalization.AddLocalizations();
            ObjectDB_Awake_Patch.Initialize();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ModGuid);

            if (DebugLogging)
            {
                AdrenalineDiagnostics.DumpAdrenalineReflection();
            }
        }

        private void Update()
        {
            _configuration.Update();
            ConfigurationManagerIntegration.Update();
            BerserkerRagnarokWeather.Update();
        }

        private void OnDestroy()
        {
            BerserkerRagnarokWeather.Reset();
            ItemTooltipController.Reset();
            HaldorTabsController.Reset();
            ObjectDB_Awake_Patch.Shutdown();
        }
    }
}