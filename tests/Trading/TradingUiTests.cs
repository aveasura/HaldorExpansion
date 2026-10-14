using System;
using System.IO;
using System.Reflection;
using BepInEx.Configuration;
using HaldorExpansion.Configuration;
using HaldorExpansion.Trading;

class TradingUiTests
{
    static void Check(bool ok, string name)
    {
        if (!ok) throw new Exception(name);
        Console.WriteLine("PASS: " + name);
    }
    static ConfigurationRuntime Create(string dir, bool first)
    {
        return (ConfigurationRuntime)typeof(RuntimeTests).GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { dir, first, false });
    }
    static void Main()
    {
        string dir = Path.Combine(Path.GetTempPath(), "he-tabs-" + Guid.NewGuid().ToString("N"));
        try
        {
            var old = Create(dir, true);
            old.Finish();
            old.General["Compatibility", "Enable long item tooltips"].BoxedValue = false;
            string original = File.ReadAllText(old.General.ConfigFilePath);
            var upgraded = Create(dir, false);
            TradingUiConfiguration.Register(upgraded.General);
            upgraded.Finish();
            Check(TradingUiConfiguration.Enabled, "new tabs default to true after schema upgrade");
            Check(!(bool)upgraded.General["Compatibility", "Enable long item tooltips"].BoxedValue, "existing user preference preserved");
            Check(!TradingUiConfiguration.HideItems, "category sales ban defaults OFF");
            Check(TradingUiConfiguration.IsCategoryEnabled(ShopCategory.Idols), "idol tab defaults visible");
            foreach (ShopCategory category in Enum.GetValues(typeof(ShopCategory)))
                Check(TradingUiConfiguration.IsCategoryEnabled(category), category + " defaults visible");
            int originalState = TradingUiConfiguration.State;
            upgraded.General["Trading UI", "Show Metals tab"].BoxedValue = false;
            Check(!TradingUiConfiguration.IsCategoryEnabled(ShopCategory.Metals), "metal toggle applies live");
            Check(TradingUiConfiguration.State != originalState, "shop detects changed visibility");
            int metalState = TradingUiConfiguration.State;
            upgraded.General["Trading UI", "Show Idols tab"].BoxedValue = false;
            Check(!TradingUiConfiguration.IsCategoryEnabled(ShopCategory.Idols) &&
                  TradingUiConfiguration.State != metalState, "idol tab live toggle");
            upgraded.General["Trading UI", "Show Idols tab"].BoxedValue = true;
            upgraded.General["Trading UI", "Hide items from disabled tabs"].BoxedValue = true;
            Check(TradingUiConfiguration.HideItems, "sales policy applies live");
            Check(TradingUiConfiguration.IsCategoryEnabled(ShopCategory.All), "ALL cannot be disabled");
            bool backup = false;
            foreach (string path in Directory.GetFiles(Path.Combine(dir, "backups"), "*.bak"))
                if (File.ReadAllText(path) == original) backup = true;
            Check(backup, "original general.cfg backed up before adding tabs setting");
            upgraded.General["Trading UI", "Enable Haldor tabs"].BoxedValue = false;
            Check(!TradingUiConfiguration.Enabled, "configuration-manager change applies immediately");
            var restart = Create(dir, false);
            TradingUiConfiguration.Register(restart.General);
            restart.Finish();
            Check(!TradingUiConfiguration.Enabled, "disabled preference survives restart");
            Check(!TradingUiConfiguration.IsCategoryEnabled(ShopCategory.Metals) && TradingUiConfiguration.HideItems,
                "category and sales policy survive restart");
            restart.General["Trading UI", "Show Metals tab"].BoxedValue = true;
            Check(TradingUiConfiguration.IsCategoryEnabled(ShopCategory.Metals), "category can be restored");
            string pathConfig = restart.General.ConfigFilePath;
            File.WriteAllText(pathConfig, File.ReadAllText(pathConfig).Replace("Enable Haldor tabs = false", "Enable Haldor tabs = true"));
            UnityEngine.Time.unscaledTime = 10;
            restart.Update();
            UnityEngine.Time.unscaledTime = 12;
            restart.Update();
            Check(TradingUiConfiguration.Enabled, "editing general.cfg reloads tabs setting during play");
            Console.WriteLine("Trading UI configuration checks passed (real BepInEx; game/network adapters stubbed)");
        }
        finally { Directory.Delete(dir, true); }
    }
}
