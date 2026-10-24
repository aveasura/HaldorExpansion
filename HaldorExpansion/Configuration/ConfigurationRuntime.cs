using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HaldorExpansion.Patches;
using HaldorExpansion.Presentation;
using HaldorExpansion.Trading;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace HaldorExpansion.Configuration
{
    internal sealed class ConfigurationRuntime
    {
        internal ConfigFile General { get; private set; }
        internal ConfigFile Balance { get; private set; }
        internal ConfigFile TradeEntries { get; private set; }
        private string folder, legacyPath, tradePath;
        private bool newGeneral, newBalance, newTrading, initialized, wasClient;

        private static bool tradeDirty;

        // Retired configuration flag. Never bind it again or change any Facebreaker key binding.
        private const string RemovedFacebreakerFlag =
            "Bone Crushers / Костоломы\nFacebreaker RMB Default Migrated";

        private bool localizationDirty, applyingConfig, balanceSavePending;
        private float nextPoll, nextFullRead;
        private ConfigEntry<float> pollInterval;
        private readonly Dictionary<string, FileStamp> fileStamps = new Dictionary<string, FileStamp>();
        private readonly Dictionary<string, string> accepted = new Dictionary<string, string>();
        private readonly Dictionary<string, string> pending = new Dictionary<string, string>();
        private CustomRPC tradingRpc;
        private TradingDocument localTrading;
        private TradingEditorSettings tradingEditor;

        internal IEnumerable<ConfigEntryBase> TradingUiEntries => tradingEditor == null
            ? Enumerable.Empty<ConfigEntryBase>()
            : tradingEditor.Entries;

        internal string TradingDisplayName(ConfigEntryBase entry, bool russian)
            => tradingEditor?.GetDisplayName(entry, russian);

        private static bool IsClient => ZNet.instance != null && !ZNet.instance.IsServer();

        private float PollInterval => pollInterval != null && pollInterval.Value >= 0.5f &&
                                      !float.IsInfinity(pollInterval.Value)
            ? pollInterval.Value
            : 2f;

        private struct FileStamp
        {
            internal readonly DateTime LastWriteUtc;
            internal readonly long Length;

            internal FileStamp(DateTime lastWriteUtc, long length)
            {
                LastWriteUtc = lastWriteUtc;
                Length = length;
            }

            internal bool SameAs(FileStamp other) => LastWriteUtc == other.LastWriteUtc && Length == other.Length;
        }

        private static bool TryGetStamp(string path, out FileStamp stamp)
        {
            var info = new FileInfo(path);
            if (!info.Exists)
            {
                stamp = default(FileStamp);
                return false;
            }

            stamp = new FileStamp(info.LastWriteTimeUtc, info.Length);
            return true;
        }

        internal static void RequestTradeRefresh()
        {
            tradeDirty = true;
        }

        internal void Open(HaldorExpansionPlugin plugin)
        {
            legacyPath = plugin.Config.ConfigFilePath;
            plugin.Config.SaveOnConfigSet = false;
            folder = Path.Combine(Paths.ConfigPath, "aveasura-HaldorExpansion");
            Directory.CreateDirectory(folder);
            string generalPath = Path.Combine(folder, "general.cfg"), balancePath = Path.Combine(folder, "balance.cfg");
            tradePath = Path.Combine(folder, "trading.yml");
            newGeneral = !File.Exists(generalPath);
            newBalance = !File.Exists(balancePath);
            newTrading = !File.Exists(tradePath);
            if (!newGeneral) ConfigText.Parse(File.ReadAllText(generalPath));
            if (!newBalance) ConfigText.Parse(File.ReadAllText(balancePath));
            General = new ConfigFile(generalPath, false, plugin.Info.Metadata) { SaveOnConfigSet = false };
            pollInterval = General.Bind("Configuration", "Config check interval (seconds)", 3f,
                new ConfigDescription(
                    "How often to check general.cfg, balance.cfg and trading.yml for external changes. " +
                    "Checks file timestamps and sizes before reading; modified files are verified twice " +
                    "before applying. Minimum 0.5 seconds, by default 3. Local setting, applies while playing. " +
                    "Интервал проверки изменений конфигов в секундах. По умолчанию 3, минимум 0.5.",
                    new AcceptableValueRange<float>(0.5f, float.MaxValue)));
            Balance = new ConfigFile(balancePath, false, plugin.Info.Metadata) { SaveOnConfigSet = false };
            TradeEntries = new ConfigFile(Path.Combine(folder, ".trading-memory"), false) { SaveOnConfigSet = false };
            var field = AccessTools.Field(typeof(BaseUnityPlugin), "<Config>k__BackingField");
            if (field == null)
                throw new NotSupportedException("BepInEx Config backing field unavailable; old config preserved.");
            field.SetValue(plugin, General);
            SynchronizationManager.Instance.RegisterCustomConfig(General);
            SynchronizationManager.Instance.RegisterCustomConfig(Balance);
        }

        internal void Finish()
        {
            bool migrated = File.Exists(legacyPath);
            string schemaPath = Path.Combine(folder, ".format-schema");
            ConfigSchema previous = File.Exists(schemaPath) ? ConfigSchema.Read(schemaPath) : null;
            if (previous != null && (newGeneral || newBalance || newTrading))
                throw new FormatException("A configuration file is missing. Restore it before upgrading.");

            Dictionary<string, string> generalDisk =
                newGeneral ? null : ConfigText.Parse(File.ReadAllText(General.ConfigFilePath));
            Dictionary<string, string> balanceDisk =
                newBalance ? null : ConfigText.Parse(File.ReadAllText(Balance.ConfigFilePath));
            string tradingDisk = newTrading ? null : File.ReadAllText(tradePath);
            bool oldBerserkerIds = !newTrading && BerserkerTradingMigration.NeedsUpgrade(tradingDisk);
            bool oldFlametalId = !newTrading && FlametalTradingMigration.NeedsUpgrade(tradingDisk);
            TradingDocument parseDefaults = TradingCatalog.Defaults;
            if (oldFlametalId) parseDefaults = FlametalTradingMigration.LegacyDefaults(parseDefaults);
            if (oldBerserkerIds) parseDefaults = BerserkerTradingMigration.LegacyDefaults(parseDefaults);

            if (previous != null)
            {
                // Only the retired Facebreaker flag may differ from an older key-set manifest.
                // All other config entries still require an exact match.
                string generalSignature = ConfigSchema.Signature(generalDisk.Keys);
                if (generalSignature != previous.General)
                {
                    bool hasRemovedFlag = generalDisk.ContainsKey(RemovedFacebreakerFlag);
                    IEnumerable<string> compatibleKeys = hasRemovedFlag
                        ? generalDisk.Keys.Where(key => key != RemovedFacebreakerFlag)
                        : generalDisk.Keys.Concat(new[] { RemovedFacebreakerFlag });
                    ConfigSchema.Require(previous.General, ConfigSchema.Signature(compatibleKeys), "general.cfg");
                }

                ConfigSchema.Require(previous.Balance, ConfigSchema.Signature(balanceDisk.Keys), "balance.cfg");
                ConfigSchema.Require(previous.Trading,
                    ConfigSchema.TradingSignature(tradingDisk, parseDefaults, false), "trading.yml");
            }

            // The old flag has no effect and must not fail strict config validation.
            // Remove it only after validating the previous file/schema (above).
            bool removeFacebreakerFlag = generalDisk != null && generalDisk.Remove(RemovedFacebreakerFlag);

            if (migrated)
            {
                var old = ConfigText.Parse(File.ReadAllText(legacyPath));
                if (newGeneral) Import(General, old);
                if (newBalance) Import(Balance, old);
                if (newTrading) Import(TradeEntries, old);
            }

            localTrading = newTrading
                ? TradingCatalog.FromEntries()
                : TradingDocument.Parse(tradingDisk, parseDefaults, complete: previous == null);
            if (oldFlametalId)
            {
                // If both legacy ID schemes are present, convert Flametal first while
                // retaining the old Berserker IDs, then migrate those separately.
                TradingDocument target = oldBerserkerIds
                    ? BerserkerTradingMigration.LegacyDefaults(TradingCatalog.Defaults)
                    : TradingCatalog.Defaults;
                localTrading = FlametalTradingMigration.Upgrade(localTrading, target);
            }

            if (oldBerserkerIds)
                localTrading = BerserkerTradingMigration.Upgrade(localTrading, TradingCatalog.Defaults);
            TradingDocument.Parse(localTrading.Serialize(), TradingCatalog.Defaults);

            Validate(General, Values(General));
            Validate(Balance, Values(Balance));
            if (!newGeneral) Validate(General, generalDisk, complete: previous == null);
            if (!newBalance) Validate(Balance, balanceDisk, complete: previous == null);

            var current = new ConfigSchema
            {
                General = ConfigSchema.Signature(Values(General).Keys),
                Balance = ConfigSchema.Signature(Values(Balance).Keys),
                Trading = ConfigSchema.TradingSignature(TradingCatalog.Defaults.Serialize(), TradingCatalog.Defaults)
            };
            bool updateGeneral = newGeneral || removeFacebreakerFlag ||
                                 (previous != null && previous.General != current.General);
            bool updateBalance = newBalance || (previous != null && previous.Balance != current.Balance);
            bool updateTrading = newTrading || oldBerserkerIds || oldFlametalId ||
                                 (previous != null && previous.Trading != current.Trading);
            if (updateGeneral || updateBalance || updateTrading)
            {
                string backups = Path.Combine(folder, "backups");
                Directory.CreateDirectory(backups);
                if (!newGeneral && updateGeneral) Backup(General.ConfigFilePath, backups);
                if (!newBalance && updateBalance) Backup(Balance.ConfigFilePath, backups);
                if (!newTrading && updateTrading) Backup(tradePath, backups);
                if (previous != null) Backup(schemaPath, backups);
            }

            if (updateGeneral)
            {
                if (removeFacebreakerFlag)
                {
                    // BepInEx preserves unbound (orphaned) entries even when Save() is called.
                    // Strip this one retired key from the backed-up file and reload the config
                    // so later user edits cannot resurrect it from the in-memory orphan cache.
                    string cleaned = ConfigText.WithoutSetting(File.ReadAllText(General.ConfigFilePath),
                        RemovedFacebreakerFlag);
                    ConfigText.WriteAtomic(General.ConfigFilePath, cleaned);
                    General.Reload();
                }

                General.Save();
            }

            if (updateBalance) Balance.Save();
            if (updateTrading) ConfigText.WriteAtomic(tradePath, localTrading.Serialize());
            Validate(General, ConfigText.Parse(File.ReadAllText(General.ConfigFilePath)), complete: true);
            Validate(Balance, ConfigText.Parse(File.ReadAllText(Balance.ConfigFilePath)), complete: true);
            TradingDocument.Parse(File.ReadAllText(tradePath), TradingCatalog.Defaults);
            if (previous == null || updateGeneral || updateBalance || updateTrading)
                ConfigText.WriteAtomic(schemaPath, current.Serialize());
            if (migrated)
            {
                string backups = Path.Combine(folder, "backups");
                Directory.CreateDirectory(backups);
                string backup = Path.Combine(backups,
                    "legacy-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N") +
                    ".cfg.bak");
                File.Move(legacyPath, backup);
                File.WriteAllText(
                    Path.Combine(Path.GetDirectoryName(legacyPath), "aveasura.haldor.expansion.CONFIG_MOVED.txt"),
                    "Settings moved / Настройки перенесены:\n" + "config/aveasura-HaldorExpansion/\n" +
                    "general.cfg: keys, UI.\nbalance.cfg: abilities.\n trading.yml: trading.\nOld config is a backup only in backups/.\n");
            }

            TradingCatalog.Apply(localTrading);
            tradingEditor = new TradingEditorSettings(this, TradeEntries, localTrading);
            General.SaveOnConfigSet = true;
            Balance.SaveOnConfigSet = false;
            foreach (string path in new[] { General.ConfigFilePath, Balance.ConfigFilePath, tradePath })
            {
                accepted[path] = File.ReadAllText(path);
                FileStamp stamp;
                if (TryGetStamp(path, out stamp)) fileStamps[path] = stamp;
            }

            nextFullRead = Time.unscaledTime + 60f;
            tradingRpc = NetworkManager.Instance.AddRPC("TradingConfigV1", RejectClientUpdate, ReceiveTrading);
            SynchronizationManager.Instance.AddInitialSynchronization(tradingRpc, MakePackage);
            Balance.SettingChanged += (sender, args) =>
            {
                localizationDirty = true;
                if (!IsClient && !wasClient && !applyingConfig) balanceSavePending = true;
            };
            General.SettingChanged += (sender, args) => localizationDirty = true;
            initialized = true;
        }

        private static void Backup(string path, string backups)
        {
            string backup = Path.Combine(backups, Path.GetFileName(path) + "-" +
                                                  DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" +
                                                  Guid.NewGuid().ToString("N") + ".bak");
            File.Copy(path, backup);
        }

        private static Dictionary<string, string> Values(ConfigFile file)
        {
            var values = new Dictionary<string, string>();
            foreach (var entry in file)
                values.Add(entry.Key.Section + "\n" + entry.Key.Key, entry.Value.GetSerializedValue());
            return values;
        }

        private static void Import(ConfigFile file, Dictionary<string, string> old)
        {
            var staged = new Dictionary<string, string>();
            foreach (var pair in file)
            {
                string value;
                if (old.TryGetValue(ConfigText.LegacyKey(pair.Key.Section, pair.Key.Key), out value))
                    staged.Add(pair.Key.Section + "\n" + pair.Key.Key, value);
            }

            Validate(file, staged, true);
            foreach (var pair in file)
            {
                string value;
                if (staged.TryGetValue(pair.Key.Section + "\n" + pair.Key.Key, out value))
                    pair.Value.SetSerializedValue(value);
            }
        }

        private static void Validate(ConfigFile file, Dictionary<string, string> values, bool legacy = false,
            bool complete = false)
        {
            var known = file.ToDictionary(p => p.Key.Section + "\n" + p.Key.Key, p => p.Value);
            if (complete && values.Count != known.Count)
                throw new FormatException("Incomplete config: keep all settings in the file.");
            foreach (var pair in values)
            {
                ConfigEntryBase entry;
                if (!known.TryGetValue(pair.Key, out entry))
                    throw new FormatException("Unknown setting: " + pair.Key.Replace('\n', '/'));
                object value = TomlTypeConverter.ConvertToValue(pair.Value, entry.SettingType);
                if (value is float && (float.IsNaN((float)value) || float.IsInfinity((float)value)))
                    throw new FormatException("Non-finite value: " + pair.Key);
                if (!legacy && entry.Description.AcceptableValues != null &&
                    !entry.Description.AcceptableValues.IsValid(value))
                    throw new FormatException("Out of range: " + pair.Key);
            }
        }

        internal void Update()
        {
            if (!initialized) return;
            bool client = IsClient;
            if (wasClient && !client)
            {
                TradingCatalog.Apply(localTrading);
                // Re-read local balance after Jotunn has released server authority.
                accepted[Balance.ConfigFilePath] = "";
            }

            wasClient = client;
            tradingEditor?.Refresh(TradingCatalog.Current);
            if (Time.unscaledTime >= nextPoll)
            {
                nextPoll = Time.unscaledTime + PollInterval;
                bool fullRead = Time.unscaledTime >= nextFullRead;
                if (fullRead) nextFullRead = Time.unscaledTime + 60f;
                Poll(General.ConfigFilePath, General, false, fullRead);
                Poll(Balance.ConfigFilePath, Balance, client, fullRead);
                Poll(tradePath, null, client, fullRead);
            }

            if (balanceSavePending)
            {
                balanceSavePending = false;
                if (!client)
                {
                    try
                    {
                        Balance.Save();
                        accepted[Balance.ConfigFilePath] = File.ReadAllText(Balance.ConfigFilePath);
                        FileStamp stamp;
                        if (TryGetStamp(Balance.ConfigFilePath, out stamp)) fileStamps[Balance.ConfigFilePath] = stamp;
                        pending.Remove(Balance.ConfigFilePath);
                    }
                    catch (Exception e)
                    {
                        HaldorExpansionPlugin.Log.LogError("[Config] Cannot save balance: " + e.Message);
                    }
                }
            }

            if (localizationDirty)
            {
                localizationDirty = false;
                ModLocalization.AddLocalizations();
            }

            if (tradeDirty)
            {
                tradeDirty = false;
                TrophyValueUpdater.ApplyTrophyValues(ObjectDB.instance);
                if (StoreGui.instance != null && StoreGui.IsVisible())
                {
                    var trader = AccessTools.Field(typeof(StoreGui), "m_trader").GetValue(StoreGui.instance) as Trader;
                    if (HaldorShopUpdater.IsHaldor(trader))
                    {
                        HaldorShopExpansionPatch.Refresh(trader, applyTrophyValues: false);
                        AccessTools.Method(typeof(StoreGui), "FillList").Invoke(StoreGui.instance, null);
                    }
                }
            }
        }

        private void Poll(string path, ConfigFile file, bool remoteAuthority, bool fullRead)
        {
            try
            {
                FileStamp stamp;
                if (!TryGetStamp(path, out stamp))
                {
                    fileStamps.Remove(path);
                    pending.Remove(path);
                    return;
                }

                FileStamp previousStamp;
                if (!fullRead && !pending.ContainsKey(path) && accepted[path].Length != 0 &&
                    fileStamps.TryGetValue(path, out previousStamp) && previousStamp.SameAs(stamp))
                    return;

                string text = File.ReadAllText(path);
                FileStamp afterRead;
                if (!TryGetStamp(path, out afterRead) || !afterRead.SameAs(stamp))
                    return; // File changed while being read; retry on the next poll.
                fileStamps[path] = stamp;
                if (accepted[path] == text)
                {
                    pending.Remove(path);
                    return;
                }

                string previous;
                if (!pending.TryGetValue(path, out previous) || previous != text)
                {
                    pending[path] = text;
                    return;
                }

                accepted[path] = text;
                pending.Remove(path);
                if (file != null) Validate(file, ConfigText.Parse(text), complete: true);
                else
                {
                    var parsed = TradingDocument.Parse(text, TradingCatalog.Defaults);
                    localTrading = parsed;
                    if (!remoteAuthority)
                    {
                        TradingCatalog.Apply(parsed);
                        if (ZNet.instance != null && ZNet.instance.IsServer())
                            tradingRpc.SendPackage(ZNet.instance.GetPeers(), MakePackage());
                    }
                }

                if (file != null && !remoteAuthority)
                {
                    bool save = file.SaveOnConfigSet;
                    file.SaveOnConfigSet = false;
                    applyingConfig = true;
                    try
                    {
                        // Apply the validated snapshot, not a second disk read that may differ.
                        var values = ConfigText.Parse(text);
                        foreach (var pair in file)
                        {
                            string value;
                            if (values.TryGetValue(pair.Key.Section + "\n" + pair.Key.Key, out value))
                                pair.Value.SetSerializedValue(value);
                        }

                        AccessTools.Method(typeof(ConfigFile), "OnConfigReloaded").Invoke(file, null);
                    }
                    finally
                    {
                        file.SaveOnConfigSet = save;
                        applyingConfig = false;
                    }
                }

                HaldorExpansionPlugin.Log.LogInfo("[Config] " + Path.GetFileName(path) +
                                                  (remoteAuthority
                                                      ? ": local file changed; server settings remain active."
                                                      : ": reloaded."));
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log.LogWarning("[Config] " + path + ": " + e.Message +
                                                     " Last valid settings retained.");
            }
        }

        // Only the server (or single-player host) may persist trading edits made in F1.
        // Never overwrite pending changes from an external YAML editor.
        internal bool TryEditTrading(string group, string id, string field, object rawValue)
        {
            if (!initialized || IsClient) return false;
            try
            {
                string text = File.ReadAllText(tradePath);
                if (text != accepted[tradePath])
                    throw new InvalidOperationException(
                        "trading.yml was edited externally; wait for its reload first.");

                var oldDocument = TradingDocument.Parse(text, TradingCatalog.Defaults);
                TradeSetting row = group == "shop" ? oldDocument.Shop[id] : oldDocument.Sell[id];
                string serialized;
                switch (field)
                {
                    case "price":
                        row.Price = (int)rawValue;
                        serialized = row.Price.ToString(CultureInfo.InvariantCulture);
                        break;
                    case "enabled":
                        row.Enabled = (bool)rawValue;
                        serialized = row.Enabled ? "true" : "false";
                        break;
                    case "stack":
                        row.Stack = (int)rawValue;
                        serialized = row.Stack.ToString(CultureInfo.InvariantCulture);
                        break;
                    case "requiredGlobalKey":
                        row.RequiredKey = (string)rawValue ?? "";
                        serialized = "\"" + row.RequiredKey.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
                        break;
                    default: throw new ArgumentException("Unknown trading field: " + field);
                }

                string updated = TradingYamlEditor.Replace(text, group, id, field, serialized);
                // Verify the precise resulting YAML before writing or changing live settings.
                TradingDocument verified = TradingDocument.Parse(updated, TradingCatalog.Defaults);
                ConfigText.WriteAtomic(tradePath, updated);
                accepted[tradePath] = updated;
                FileStamp stamp;
                if (TryGetStamp(tradePath, out stamp)) fileStamps[tradePath] = stamp;
                pending.Remove(tradePath);
                localTrading = verified;
                TradingCatalog.Apply(verified);
                tradingEditor?.Refresh(verified);
                if (ZNet.instance != null && ZNet.instance.IsServer())
                    tradingRpc.SendPackage(ZNet.instance.GetPeers(), MakePackage());
                return true;
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log.LogWarning("[Config] Cannot edit trading.yml from F1: " + e.Message);
                return false;
            }
        }

        private ZPackage MakePackage()
        {
            var p = new ZPackage();
            p.Write(localTrading.Serialize());
            return p;
        }

        private IEnumerator RejectClientUpdate(long sender, ZPackage package)
        {
            yield break;
        }

        private IEnumerator ReceiveTrading(long sender, ZPackage package)
        {
            if (ZNet.instance == null || ZNet.instance.IsServer() || ZNet.instance.GetServerPeer() == null ||
                sender != ZNet.instance.GetServerPeer().m_uid) yield break;
            try
            {
                TradingCatalog.Apply(TradingDocument.Parse(package.ReadString(), TradingCatalog.Defaults));
                wasClient = true;
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log.LogError("[Config] Invalid server trading settings: " + e.Message);
            }

            yield break;
        }
    }
}