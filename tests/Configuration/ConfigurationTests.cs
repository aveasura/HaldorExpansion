class ConfigurationTests
{
    static int count;

    static void Check(bool ok, string name)
    {
        if (!ok) throw new Exception(name);
        count++;
        Console.WriteLine("PASS: " + name);
    }

    static void Reject(Action action, string name)
    {
        try
        {
            action();
        }
        catch (FormatException)
        {
            Check(true, name);
            return;
        }

        throw new Exception("Accepted invalid input: " + name);
    }

    static void Main()
    {
        var defaults = new TradingDocument();
        defaults.Shop.Add("Iron",
            new TradeSetting { Id = "Iron", Label = "Железо / Iron", Category = "Metals", Price = 225, Stack = 30 });
        defaults.Sell.Add("TrophyDeer", new TradeSetting { Id = "TrophyDeer", Label = "Deer Trophy", Price = 15 });
        string text = defaults.Serialize();
        var parsed = TradingDocument.Parse(text, defaults);
        Check(
            parsed.Shop["Iron"].Price == 225 && parsed.Shop["Iron"].Stack == 30 &&
            parsed.Sell["TrophyDeer"].Price == 15, "trading roundtrip preserves prices and quantities");
        Check(
            TradingDocument.Parse(text.Replace("price: 225", "price: 301"), defaults).Shop["Iron"].Price == 301 &&
            defaults.Shop["Iron"].Price == 225, "parse stages changes without mutating defaults");
        Check(!TradingDocument.Parse(text.Replace("enabled: true", "enabled: false"), defaults).Shop["Iron"].Enabled,
            "shop disable");
        Check(TradingDocument.Parse(text.Replace("price: 15", "price: 0"), defaults).Sell["TrophyDeer"].Price == 0,
            "zero sell disables selling");
        Reject(() => TradingDocument.Parse(text.Replace("price: 225", "price: -1"), defaults),
            "negative price rejected");
        Reject(() => TradingDocument.Parse(text.Replace("price: 225", "price: abc"), defaults),
            "invalid price rejected");
        Reject(() => TradingDocument.Parse(text.Replace("stack: 30", "stack: 10001"), defaults),
            "excessive quantity rejected");
        Reject(() => TradingDocument.Parse(text.Replace("enabled: true", "enabled: yes"), defaults),
            "ambiguous bool rejected");
        Reject(() => TradingDocument.Parse(text.Replace("Iron:", "WrongItem:"), defaults), "unknown prefab rejected");
        Reject(() => TradingDocument.Parse(text.Replace("price: 225", "price: 225\n    price: 226"), defaults),
            "duplicate field rejected");
        Reject(() => TradingDocument.Parse(text.Replace("price: 225", "cost: 225"), defaults),
            "unknown field rejected");
        Reject(() => TradingDocument.Parse(text.Replace("    price: 225", "\tprice: 225"), defaults), "tabs rejected");
        Reject(() => TradingDocument.Parse("shop:\n", defaults), "missing sell rejected");
        Reject(
            () => TradingDocument.Parse(text.Replace("requiredGlobalKey: \"\"", "requiredGlobalKey: *alias"), defaults),
            "YAML alias rejected");
        Reject(
            () => TradingDocument.Parse(text.Replace("requiredGlobalKey: \"\"", "requiredGlobalKey: \"oops"), defaults),
            "unclosed string rejected");
        Check(
            TradingDocument
                .Parse(text.Replace("requiredGlobalKey: \"\"", "requiredGlobalKey: defeated_bonemass # comment"),
                    defaults).Shop["Iron"].RequiredKey == "defeated_bonemass", "global key with inline comment");
        Reject(() => TradingDocument.Parse("shop:\nsell:\n", defaults), "incomplete file cannot reset settings");
        Reject(() => TradingDocument.Parse(text.Substring(0, text.IndexOf("    price: 15")), defaults),
            "truncated last item rejected");
        var expanded = defaults.Copy();
        expanded.Shop.Add("Copper", new TradeSetting
            { Id = "Copper", Label = "Copper", Category = "Metals", Price = 80, Stack = 30 });
        expanded.Sell.Add("TrophyBoar", new TradeSetting { Id = "TrophyBoar", Label = "Boar Trophy", Price = 12 });
        string oldSignature = ConfigSchema.TradingSignature(text, defaults);
        string newSignature = ConfigSchema.TradingSignature(expanded.Serialize(), expanded);
        Check(oldSignature != newSignature, "adding catalog rows changes schema fingerprint");
        var upgraded = TradingDocument.Parse(text.Replace("price: 225", "price: 333"), expanded, false);
        Check(upgraded.Shop["Iron"].Price == 333 && upgraded.Shop["Copper"].Price == 80 &&
              upgraded.Sell["TrophyBoar"].Price == 12, "upgrade keeps customized prices and adds defaults");
        Check(ConfigSchema.TradingSignature(upgraded.Serialize(), expanded) == newSignature,
            "upgraded trading document has current full schema");
        Check(ConfigSchema.TradingSignature(text.Replace("price: 225", "price: 555"), defaults) == oldSignature,
            "editing an existing value does not invalidate format snapshot");
        string truncated = text.Replace("    enabled: true\n", "");
        Reject(() => ConfigSchema.Require(oldSignature,
                ConfigSchema.TradingSignature(truncated, expanded, false), "trading.yml"),
            "truncated old trading document is not mistaken for an upgrade");
        var berserker = defaults.Copy();
        foreach (string id in new[]
                 {
                     BerserkerIds.AxePrefab, BerserkerIds.CloakPrefab,
                     BerserkerIds.HelmetPrefab
                 })
            berserker.Shop.Add(id, new TradeSetting
            {
                Id = id, Label = id, Category = "Custom Items",
                Price = 2500, Stack = 1
            });
        var legacyBerserker = BerserkerTradingMigration.LegacyDefaults(berserker);
        legacyBerserker.Shop["HelmetBerserker"].Price = 1234;
        legacyBerserker.Shop["CapeBerserker"].Enabled = false;
        legacyBerserker.Shop["BattleaxeBerserker"].RequiredKey = "defeated_queen";
        string oldTrading = legacyBerserker.Serialize();
        Check(BerserkerTradingMigration.NeedsUpgrade(oldTrading) &&
              !BerserkerTradingMigration.NeedsUpgrade(berserker.Serialize()),
            "detect pre-release Berserker item IDs only");
        string oldTradingSignature = ConfigSchema.TradingSignature(oldTrading, legacyBerserker);
        ConfigSchema.Require(oldTradingSignature,
            ConfigSchema.TradingSignature(oldTrading, legacyBerserker, false), "trading.yml");
        var converted = BerserkerTradingMigration.Upgrade(
            TradingDocument.Parse(oldTrading, legacyBerserker), berserker);
        Check(converted.Shop[BerserkerIds.HelmetPrefab].Price == 1234 &&
              !converted.Shop[BerserkerIds.CloakPrefab].Enabled &&
              converted.Shop[BerserkerIds.AxePrefab].RequiredKey == "defeated_queen",
            "Berserker ID migration preserves price, availability and progression");
        Check(!BerserkerTradingMigration.NeedsUpgrade(converted.Serialize()) &&
              ConfigSchema.TradingSignature(converted.Serialize(), berserker) ==
              ConfigSchema.TradingSignature(berserker.Serialize(), berserker),
            "migrated file contains only new full-schema IDs");
        Reject(() => ConfigSchema.Require(oldTradingSignature,
                ConfigSchema.TradingSignature(oldTrading.Replace("    enabled: true\n", ""),
                    legacyBerserker, false), "trading.yml"),
            "truncated old ID config is rejected before migration");
        string cfgText = "[UI]\nA = 42\nB = true\n";
        string cfgSignature = ConfigSchema.Signature(ConfigText.Parse(cfgText).Keys);
        Check(cfgSignature == ConfigSchema.Signature(ConfigText.Parse(cfgText.Replace("42", "123")).Keys),
            "config value changes do not change schema signature");
        Reject(() => ConfigSchema.Require(cfgSignature,
                ConfigSchema.Signature(ConfigText.Parse("[UI]\nA = 42\n").Keys), "general.cfg"),
            "truncated cfg document is not mistaken for an upgrade");
        var ini = ConfigText.Parse("# comment\r\n[Cestus]\r\nAbility Key = Mouse4\r\n");
        Check(ini[ConfigText.LegacyKey("Bone Crushers / Костоломы", "Ability Key")] == "Mouse4",
            "legacy renamed input migrates");
        Check(ConfigText.LegacyKey("Compatibility", "Enable long item tooltips") == "UI\nEnable long item tooltips",
            "legacy UI maps to compatibility");
        Reject(() => ConfigText.Parse("[X]\nA=1\nA=2"), "duplicate INI key rejected");
        Reject(() => ConfigText.Parse("[X]\nA"), "truncated INI line rejected");
        string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "settings");
        var manifest = new ConfigSchema
            { General = cfgSignature, Balance = cfgSignature, Trading = oldSignature };
        string manifestPath = Path.Combine(dir, ".format-schema");
        ConfigText.WriteAtomic(manifestPath, manifest.Serialize());
        Check(ConfigSchema.Read(manifestPath).Trading == oldSignature,
            "manifest roundtrip independent of mod version");
        Reject(() =>
            {
                File.WriteAllText(manifestPath, "formatVersion=999\n");
                ConfigSchema.Read(manifestPath);
            },
            "unknown format version rejected");
        ConfigText.WriteAtomic(path, "old");
        ConfigText.WriteAtomic(path, "new");
        Check(File.ReadAllText(path) == "new", "atomic replace");
        bool failed = false;
        try
        {
            ConfigText.WriteAtomic(Path.Combine(dir, "missing", "settings"), "bad");
        }
        catch (IOException)
        {
            failed = true;
        }

        Check(failed && File.ReadAllText(path) == "new", "failed write leaves previous file");
        Directory.Delete(dir, true);
        Console.WriteLine(count + " checks passed");
    }
}