using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Configuration
{
    // ConfigurationManager only enumerates plugin.Config (general.cfg). Add the real balance.cfg
    // entries to its settings view without cloning them into general.cfg or adding a hard dependency.
    internal static class ConfigurationManagerIntegration
    {
        private const string ManagerGuid = "com.bepis.bepinex.configurationmanager";
        private static ConfigurationRuntime runtime;
        private static HaldorExpansionPlugin plugin;
        private static FieldInfo allSettingsField;
        private static MethodInfo rebuildFilteredSettings;
        private static ConstructorInfo settingConstructor;
        private static MethodInfo setReadOnly;
        private static readonly List<object> visibleBalanceEntries = new List<object>();
        private static bool installed, unavailable;
        private static bool? lastClientState;
        private static float nextAttempt;

        internal static void Initialize(ConfigurationRuntime configuration, HaldorExpansionPlugin owner)
        {
            runtime = configuration;
            plugin = owner;
        }

        internal static void Update()
        {
            if (runtime == null || unavailable) return;
            if (!installed)
            {
                if (Time.unscaledTime < nextAttempt) return;
                nextAttempt = Time.unscaledTime + 5f;
                TryInstall();
                return;
            }

            // The F1 window may remain open while connecting to or leaving a server.
            // Refresh its permission without rebuilding the whole settings list every frame.
            bool isClient = ZNet.instance != null && !ZNet.instance.IsServer();
            if (lastClientState == isClient) return;
            lastClientState = isClient;
            foreach (object entry in visibleBalanceEntries)
                setReadOnly.Invoke(entry, new object[] { isClient });
        }

        private static void TryInstall()
        {
            PluginInfo info;
            if (!Chainloader.PluginInfos.TryGetValue(ManagerGuid, out info) || info.Instance == null)
                return; // ConfigurationManager is optional and might load after this plugin.

            try
            {
                Type managerType = info.Instance.GetType();
                Type configSettingType = managerType.Assembly.GetType("ConfigurationManager.ConfigSettingEntry");
                Type settingBaseType = managerType.Assembly.GetType("ConfigurationManager.SettingEntryBase");
                MethodInfo buildSettings = AccessTools.Method(managerType, "BuildSettingList");
                allSettingsField = AccessTools.Field(managerType, "_allSettings");
                rebuildFilteredSettings = AccessTools.Method(managerType, "BuildFilteredSettingList");
                settingConstructor = configSettingType?.GetConstructor(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null,
                    new[] { typeof(ConfigEntryBase), typeof(BaseUnityPlugin) }, null);
                setReadOnly = settingBaseType?.GetProperty("ReadOnly",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetSetMethod(true);

                if (buildSettings == null || allSettingsField == null || rebuildFilteredSettings == null ||
                    settingConstructor == null || setReadOnly == null)
                    throw new NotSupportedException("Unsupported ConfigurationManager settings API.");

                new Harmony(HaldorExpansionPlugin.ModGuid + ".configurationmanager").Patch(buildSettings,
                    postfix: new HarmonyMethod(AccessTools.Method(typeof(ConfigurationManagerIntegration),
                        nameof(AddBalanceSettings))));
                installed = true;
                HaldorExpansionPlugin.Log.LogInfo("[Config] ConfigurationManager: balance.cfg settings available in F1.");
            }
            catch (Exception e)
            {
                unavailable = true;
                HaldorExpansionPlugin.Log.LogWarning("[Config] ConfigurationManager integration disabled: " + e.Message);
            }
        }

        private static void AddBalanceSettings(object __instance)
        {
            try
            {
                IList settings = allSettingsField.GetValue(__instance) as IList;
                if (settings == null || runtime?.Balance == null) return;

                visibleBalanceEntries.Clear();
                bool isClient = ZNet.instance != null && !ZNet.instance.IsServer();
                foreach (var setting in runtime.Balance)
                {
                    object view = settingConstructor.Invoke(new object[] { setting.Value, plugin });
                    setReadOnly.Invoke(view, new object[] { isClient });
                    settings.Add(view);
                    visibleBalanceEntries.Add(view);
                }

                lastClientState = isClient;
                // BuildSettingList has already filtered the entries before this postfix.
                rebuildFilteredSettings.Invoke(__instance, null);
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log.LogWarning("[Config] Cannot display balance.cfg in F1: " + e.Message);
            }
        }
    }
}
