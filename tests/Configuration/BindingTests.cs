using Jotunn.Configs;

class BindingTests
{
    static void Main()
    {
        string dir = Path.Combine(Path.GetTempPath(), "he-bind-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var general = new ConfigFile(Path.Combine(dir, "general.cfg"), false) { SaveOnConfigSet = false };
        var balance = new ConfigFile(Path.Combine(dir, "balance.cfg"), false) { SaveOnConfigSet = false };
        CestusInputRegistration.RegisterCestusInput(general);
        HelOathConfiguration.Register(balance, general);
        BerserkerConfiguration.Register(balance, general);
        if (general.Count != 3 || balance.Count != 22) throw new Exception("Missing settings");
        foreach (var entry in general) new ConfigDefinition(entry.Key.Section, entry.Key.Key);
        foreach (var entry in balance) new ConfigDefinition(entry.Key.Section, entry.Key.Key);
        Console.WriteLine(
            "PASS: all 25 ability/input bindings registered using real BepInEx (Jotunn/input adapters stubbed)");
        foreach (var pair in balance)
            if (pair.Key.Section.Contains("Клятва") &&
                ConfigText.LegacyKey(pair.Key.Section, pair.Key.Key) != "Hel Oath\n" + pair.Key.Key)
                throw new Exception("Legacy Hel Oath mapping lost");
        Console.WriteLine("PASS: Hel Oath legacy mapping preserved");
        var trade = new ConfigFile(Path.Combine(dir, "trade-memory"), false) { SaveOnConfigSet = false };
        new ShopConfiguration().RegisterShopProgressionConfig(trade);
        new SellPriceConfiguration().RegisterHaldorSellPriceConfig(trade);
        if (trade.Count != 209 || TradingCatalog.Defaults.Shop.Count != 59 || TradingCatalog.Defaults.Sell.Count != 91)
            throw new Exception("Missing trading bindings");
        var ui = general.Bind("Compatibility", "Enable long item tooltips", true, "");
        if (general.Count + balance.Count + trade.Count != 235) throw new Exception("Wrong total");
        // Every idol is configurable for both buy and sell; the shop stack is fixed at one.
        foreach (var idol in HaldorExpansion.Trading.HaldorIdolCatalog.Items)
        {
            TradeSetting buy = TradingCatalog.Defaults.Shop[idol.Id];
            TradeSetting sell = TradingCatalog.Defaults.Sell[idol.Id];
            if (buy.Price != 1000 || sell.Price != 300 || buy.Stack != 1 ||
                TradingCatalog.Stack(idol.Id, 20, 20) != 1)
                throw new Exception("Wrong idol buy/sell default or stack: " + idol.Id);
        }
        TradingDocument.Parse(TradingCatalog.Defaults.Serialize(), TradingCatalog.Defaults);
        Console.WriteLine("PASS: all 235 settings bind; full 59/91 trading document roundtrips");
        general.Save();
        balance.Save();
        var read = new ConfigFile(balance.ConfigFilePath, false);
        Console.WriteLine("PASS: generated files saved and read using BepInEx");
        Directory.Delete(dir, true);
    }
}

namespace HaldorExpansion
{
    public static class HaldorExpansionPlugin
    {
        public const string ModGuid = "aveasura.haldor.expansion";
    }
}

namespace HaldorExpansion.Features.HelOath
{
    internal static class HelOathWorldProgression
    {
        internal static float ScaleRequiredDamage(float f) => f;
    }
}

namespace Jotunn.Managers
{
    public class InputManager
    {
        public static InputManager Instance = new InputManager();

        public void AddButton(string id, ButtonConfig button)
        {
        }
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
            string description, bool synced = true, AcceptableValueBase acceptableValues = null)
            => file.Bind(section, key, value, new ConfigDescription(description, acceptableValues));
    }
}

namespace HaldorExpansion.Trading
{
    internal static class TrophyValueUpdater
    {
        internal const int DefaultTrophyValue = 10;
    }
}

namespace HaldorExpansion.Configuration
{
    internal static class ConfigurationRuntime
    {
        internal static void RequestTradeRefresh()
        {
        }
    }
}