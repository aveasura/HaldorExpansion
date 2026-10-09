using System;
using System.IO;
using System.Reflection;
using BepInEx.Configuration;
using HaldorExpansion.Configuration;

class RuntimeTests
{
    static int passed;

    static void Check(bool value, string name)
    {
        if (!value) throw new Exception(name);
        Console.WriteLine("PASS: " + name);
        passed++;
    }

    static void Set(object o, string field, object value) =>
        o.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(o, value);

    static ConfigurationRuntime Create(string dir, bool first, bool expanded = false)
    {
        Directory.CreateDirectory(dir);
        var runtime = new ConfigurationRuntime();
        var general = new ConfigFile(Path.Combine(dir, "general.cfg"), false) { SaveOnConfigSet = false };
        general.Bind("Compatibility", "Enable long item tooltips", true, "");
        var interval = general.Bind("Configuration", "Config check interval (seconds)", 2f,
            new ConfigDescription("Config file polling interval", new AcceptableValueRange<float>(0.5f, float.MaxValue)));
        if (expanded) general.Bind("Compatibility", "New setting", 42, "");
        var balance = new ConfigFile(Path.Combine(dir, "balance.cfg"), false) { SaveOnConfigSet = false };
        balance.Bind("Berserker Greataxe / Секира берсерка", "Hits per Wrath stage", 2f, "");
        if (expanded) balance.Bind("Berserker Greataxe / Секира берсерка", "New ability value", 3f, "");
        var trade = new ConfigFile(Path.Combine(dir, ".memory"), false) { SaveOnConfigSet = false };
        var price = trade.Bind("Shop", "Iron Price", 225, "");
        var key = trade.Bind("Shop", "Iron RequiredGlobalKey", "", "");
        var sell = trade.Bind("Sell", "Trophy Price", 15, "");
        if (first)
        {
            TradingCatalog.Shop("Iron", "Iron", "Metals", price, key);
            TradingCatalog.Sell("Trophy", "Trophy", "Trophy", sell);
        }

        if (expanded)
        {
            var copperPrice = trade.Bind("Shop", "Copper Price", 80, "");
            var copperKey = trade.Bind("Shop", "Copper RequiredGlobalKey", "", "");
            TradingCatalog.Shop("Copper", "Copper", "Metals", copperPrice, copperKey);
        }

        Set(runtime, "<General>k__BackingField", general);
        Set(runtime, "pollInterval", interval);
        Set(runtime, "<Balance>k__BackingField", balance);
        Set(runtime, "<TradeEntries>k__BackingField", trade);
        Set(runtime, "folder", dir);
        Set(runtime, "legacyPath", Path.Combine(dir, "legacy.cfg"));
        Set(runtime, "tradePath", Path.Combine(dir, "trading.yml"));
        Set(runtime, "newGeneral", !File.Exists(general.ConfigFilePath));
        Set(runtime, "newBalance", !File.Exists(balance.ConfigFilePath));
        Set(runtime, "newTrading", !File.Exists(Path.Combine(dir, "trading.yml")));
        return runtime;
    }

    static void Main()
    {
        string dir = Path.Combine(Path.GetTempPath(), "he-runtime-" + Guid.NewGuid().ToString("N"));
        var r = Create(dir, true);
        File.WriteAllText(Path.Combine(dir, "legacy.cfg"),
            "[UI]\nEnable long item tooltips = false\n[Berserker]\nHits per Wrath stage = 4\n[Shop]\nIron Price = -10\nIron RequiredGlobalKey = defeated_bonemass\n[Sell]\nTrophy Price = -2\n");
        r.Finish();
        var interval = (ConfigEntry<float>)r.General["Configuration", "Config check interval (seconds)"];
        Check(interval.Value == 2f, "default config polling interval is two seconds");
        Check(!interval.Description.AcceptableValues.IsValid(0.25f) &&
              interval.Description.AcceptableValues.IsValid(0.5f), "poll interval minimum is half a second");
        string generalPath = r.General.ConfigFilePath;
        string originalGeneral = File.ReadAllText(generalPath);
        DateTime originalStamp = File.GetLastWriteTimeUtc(generalPath);
        Check(originalGeneral.Contains("Config check interval (seconds) = 2"), "polling setting persisted to general.cfg");
        File.WriteAllText(generalPath, originalGeneral.Replace("Config check interval (seconds) = 2",
            "Config check interval (seconds) = 3"));
        File.SetLastWriteTimeUtc(generalPath, originalStamp);
        var poll = typeof(ConfigurationRuntime).GetMethod("Poll", BindingFlags.Instance | BindingFlags.NonPublic);
        poll.Invoke(r, new object[] { generalPath, r.General, false, false });
        Check(interval.Value == 2f, "unchanged metadata skips disk reload");
        poll.Invoke(r, new object[] { generalPath, r.General, false, true });
        Check(interval.Value == 2f, "changed content must be confirmed on a second poll");
        poll.Invoke(r, new object[] { generalPath, r.General, false, true });
        Check(interval.Value == 3f, "periodic content check recovers same-metadata edits");
        interval.Value = 0.5f; // Existing runtime tests intentionally use faster polling to keep their time steps.
        Check(!(bool)r.General["Compatibility", "Enable long item tooltips"].BoxedValue, "legacy UI value preserved");
        Check((float)r.Balance["Berserker Greataxe / Секира берсерка", "Hits per Wrath stage"].BoxedValue == 4,
            "legacy balance preserved");
        Check(TradingCatalog.Current.Shop["Iron"].Price == 0 && TradingCatalog.Current.Sell["Trophy"].Price == 0,
            "legacy negative prices retain zero semantics");
        Check(TradingCatalog.Current.Shop["Iron"].RequiredKey == "defeated_bonemass", "legacy progression preserved");
        Check(
            !File.Exists(Path.Combine(dir, "legacy.cfg")) &&
            Directory.GetFiles(Path.Combine(dir, "backups"), "*.bak").Length == 1, "legacy archived after writes");
        Check(!r.Balance.SaveOnConfigSet, "remote changes cannot auto-save balance");
        Check(!File.Exists(Path.Combine(dir, ".memory")), "no transport config created");
        Check(ConfigSchema.Read(Path.Combine(dir, ".format-schema")).General.Length == 64,
            "separate format manifest created");
        // Simulate a multiplayer session, edit local disk, and emulate Jotunn restoring cached entries.
        ZNet.instance = new ZNet { Server = false };
        r.Update();
        string bp = r.Balance.ConfigFilePath;
        File.WriteAllText(bp, File.ReadAllText(bp).Replace("= 4", "= 7"));
        UnityEngine.Time.unscaledTime = 1;
        r.Update();
        UnityEngine.Time.unscaledTime = 2;
        r.Update();
        ZNet.instance = null; // Unity may clear the singleton before disconnect callbacks finish.
        r.Balance["Berserker Greataxe / Секира берсерка", "Hits per Wrath stage"].BoxedValue = 4f;
        Check(File.ReadAllText(bp).Contains("= 7"), "disconnect restoration cannot overwrite edited client disk");
        ZNet.instance = null;
        UnityEngine.Time.unscaledTime = 3;
        r.Update();
        UnityEngine.Time.unscaledTime = 4;
        r.Update();
        Check((float)r.Balance["Berserker Greataxe / Секира берсерка", "Hits per Wrath stage"].BoxedValue == 7,
            "local balance disk changes apply after disconnect");
        // A valid first setting followed by invalid second one must not partially apply.
        File.WriteAllText(bp, "[Berserker Greataxe / Секира берсерка]\nHits per Wrath stage = 9\nTypo = 5\n");
        UnityEngine.Time.unscaledTime = 5;
        r.Update();
        UnityEngine.Time.unscaledTime = 6;
        r.Update();
        Check((float)r.Balance["Berserker Greataxe / Секира берсерка", "Hits per Wrath stage"].BoxedValue == 7,
            "invalid reload preserves complete prior snapshot");
        // Restore valid file and verify repeat initialization does not import stale old values.
        File.WriteAllText(bp, "[Berserker Greataxe / Секира берсерка]\nHits per Wrath stage = 7\n");
        File.WriteAllText(Path.Combine(dir, "legacy.cfg"), "[Berserker]\nHits per Wrath stage = 99\n");
        var second = Create(dir, false);
        second.Finish();
        Check((float)second.Balance["Berserker Greataxe / Секира берсерка", "Hits per Wrath stage"].BoxedValue == 7,
            "existing new config wins over reintroduced legacy");
        // Simulate an update introducing a new setting in each CFG and an item in trading.yml.
        string tp = Path.Combine(dir, "trading.yml");
        string originalTrade = File.ReadAllText(tp);
        File.WriteAllText(tp, originalTrade.Replace("price: 0", "price: 91"));
        string oldSchema = File.ReadAllText(Path.Combine(dir, ".format-schema"));
        var upgraded = Create(dir, false, expanded: true);
        upgraded.Finish();
        Check(!(bool)upgraded.General["Compatibility", "Enable long item tooltips"].BoxedValue &&
              (int)upgraded.General["Compatibility", "New setting"].BoxedValue == 42,
            "old general values preserved and new settings appended");
        Check((float)upgraded.Balance["Berserker Greataxe / Секира берсерка", "Hits per Wrath stage"].BoxedValue == 7 &&
              (float)upgraded.Balance["Berserker Greataxe / Секира берсерка", "New ability value"].BoxedValue == 3,
            "old balance values preserved and new defaults appended");
        Check(TradingCatalog.Current.Shop["Iron"].Price == 91 &&
              TradingCatalog.Current.Shop["Copper"].Price == 80,
            "old trading price survives while new item receives default");
        Check(File.ReadAllText(tp).Contains("Copper:") &&
              File.ReadAllText(Path.Combine(dir, ".format-schema")) != oldSchema,
            "upgraded catalog and independent schema recorded");
        Check(Directory.GetFiles(Path.Combine(dir, "backups"), "*.bak").Length >= 5,
            "all previous changed configs and schema backed up before conversion");
        string fullyUpgraded = File.ReadAllText(tp);
        File.WriteAllText(tp, fullyUpgraded.Replace("    enabled: true\n", ""));
        bool partialRejected = false;
        try
        {
            Create(dir, false).Finish();
        }
        catch (FormatException)
        {
            partialRejected = true;
        }

        Check(partialRejected, "truncated upgraded trading file rejected on restart");
        File.WriteAllText(tp, fullyUpgraded);
        // Invalid new YAML must leave the source legacy file and contents untouched.
        File.WriteAllText(Path.Combine(dir, "legacy.cfg"), "[Berserker]\nHits per Wrath stage = 8\n");
        File.WriteAllText(Path.Combine(dir, "trading.yml"), "shop:\nsell:\n");
        bool failed = false;
        try
        {
            Create(dir, false).Finish();
        }
        catch (FormatException)
        {
            failed = true;
        }

        Check(failed && File.ReadAllText(Path.Combine(dir, "legacy.cfg")).Contains("= 8"),
            "failed migration retains legacy source");
        Directory.Delete(dir, true);
        Console.WriteLine(passed + " runtime checks passed with real BepInEx configuration and mocked game/network.");
    }
}