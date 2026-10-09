using System;
using System.Collections;
using System.Collections.Generic;
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
        private bool localizationDirty, applyingConfig, balanceSavePending;
        private float nextPoll;
        private readonly Dictionary<string, string> accepted = new Dictionary<string, string>();
        private readonly Dictionary<string, string> pending = new Dictionary<string, string>();
        private CustomRPC tradingRpc;
        private TradingDocument localTrading;
        private static bool IsClient => ZNet.instance != null && !ZNet.instance.IsServer();

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
            if (migrated)
            {
                var old = ConfigText.Parse(File.ReadAllText(legacyPath));
                if (newGeneral) Import(General, old);
                if (newBalance) Import(Balance, old);
                if (newTrading) Import(TradeEntries, old);
            }

            localTrading = newTrading
                ? TradingCatalog.FromEntries()
                : TradingDocument.Parse(File.ReadAllText(tradePath), TradingCatalog.Defaults);
            TradingDocument.Parse(localTrading.Serialize(), TradingCatalog.Defaults);
            Validate(General, Values(General));
            Validate(Balance, Values(Balance));
            if (!newGeneral)
                Validate(General, ConfigText.Parse(File.ReadAllText(General.ConfigFilePath)), complete: true);
            if (!newBalance)
                Validate(Balance, ConfigText.Parse(File.ReadAllText(Balance.ConfigFilePath)), complete: true);
            if (newGeneral) General.Save();
            if (newBalance) Balance.Save();
            if (newTrading) ConfigText.WriteAtomic(tradePath, localTrading.Serialize());
            Validate(General, ConfigText.Parse(File.ReadAllText(General.ConfigFilePath)), complete: true);
            Validate(Balance, ConfigText.Parse(File.ReadAllText(Balance.ConfigFilePath)), complete: true);
            TradingDocument.Parse(File.ReadAllText(tradePath), TradingCatalog.Defaults);
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
            General.SaveOnConfigSet = true;
            Balance.SaveOnConfigSet = false;
            foreach (string path in new[] { General.ConfigFilePath, Balance.ConfigFilePath, tradePath })
                accepted[path] = File.ReadAllText(path);
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
            if (Time.unscaledTime >= nextPoll)
            {
                nextPoll = Time.unscaledTime + 0.5f;
                Poll(General.ConfigFilePath, General, false);
                Poll(Balance.ConfigFilePath, Balance, client);
                Poll(tradePath, null, client);
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
                        HaldorShopExpansionPatch.Refresh(trader);
                        AccessTools.Method(typeof(StoreGui), "FillList").Invoke(StoreGui.instance, null);
                    }
                }
            }
        }

        private void Poll(string path, ConfigFile file, bool remoteAuthority)
        {
            try
            {
                if (!File.Exists(path)) return;
                string text = File.ReadAllText(path);
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