using System;
using System.Reflection;
using BepInEx.Configuration;
using HaldorExpansion.Configuration;
using HaldorExpansion.Integration;
using HaldorExpansion.Trading;
using Jotunn.Entities;
using Jotunn.Extensions;
using Jotunn.Managers;
using UnityEngine;

namespace HaldorExpansion.Features.Upgrades
{
    internal static class AdamantineIdolRegistration
    {
        internal const string PrefabName = HaldorIdolCatalog.AdamantineId;
        internal const string ItemKey = "$item_aveasura_he_adamantine_idol";
        internal const string DescriptionKey = ItemKey + "_desc";
        internal const string LoreKey = ItemKey + "_lore";
        internal const string ForgeLabelKey = "$he_adamantine_forge_label";
        internal const string ForgeWarningKey = "$he_adamantine_forge_warning";
        internal static GameObject Prefab;
        internal static ItemDrop ItemDrop;
        internal static ConfigEntry<float> ChestChance;
        private static bool commandRegistered;

        private static readonly MethodInfo CloneShared = typeof(object).GetMethod("MemberwiseClone",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo UpdateRegisters = typeof(ObjectDB).GetMethod("UpdateRegisters",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        internal static void RegisterConfig(ConfigFile config)
        {
            ChestChance = config.BindConfig("Forge of Possibilities", "Adamantine Idol chest drop chance", 1f,
                "Independent chance (%) per eligible chest, 0 disables drops. / Независимый шанс (%) на подходящий сундук; 0 отключает выпадение.",
                synced: true, acceptableValues: new AcceptableValueRange<float>(0f, 100f));
        }

        internal static bool EnsureReady()
        {
            ObjectDB db = ObjectDB.instance;
            ItemDrop source = IdolRegistration.ItemDrop;
            if (db == null || source == null) return false;
            try
            {
                GameObject prefab = ItemPrefabLookup.Registered(PrefabName);
                if (prefab == null)
                {
                    prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabName, IdolRegistration.PrefabName);
                    if (prefab == null) return false;
                    ItemDrop drop = prefab.GetComponent<ItemDrop>();
                    if (drop == null) return false;
                    // Explicitly detach SharedData, including when a prefab provider uses shallow copies.
                    drop.m_itemData.m_shared =
                        (ItemDrop.ItemData.SharedData)CloneShared.Invoke(source.m_itemData.m_shared, null);
                    Configure(drop, source, prefab);
                    AdamantineIdolAppearance.Apply(prefab, IdolRegistration.NativeIcon ?? source.m_itemData.GetIcon());
                    if (!ItemManager.Instance.AddItem(new CustomItem(prefab, fixReference: false))) return false;
                }

                ItemDrop item = prefab.GetComponent<ItemDrop>();
                if (item == null) return false;
                Configure(item, source, prefab);
                if (db.GetItemPrefab(PrefabName) == null)
                {
                    if (db.m_items.Exists(candidate => candidate != null && candidate.name == PrefabName))
                        UpdateRegisters.Invoke(db, null);
                    else ItemManager.Instance.RegisterItemInObjectDB(prefab);
                }

                Prefab = prefab;
                ItemDrop = item;
                if (!commandRegistered)
                {
                    CommandManager.Instance.AddConsoleCommand(new AdamantineLootTestCommand());
                    commandRegistered = true;
                }

                return true;
            }
            catch (Exception error)
            {
                HaldorExpansionPlugin.Log?.LogError("[Adamantine Idol] Registration failed: " + error);
                return false;
            }
        }

        private static void Configure(ItemDrop drop, ItemDrop source, GameObject prefab)
        {
            var shared = drop.m_itemData.m_shared;
            shared.m_name = ItemKey;
            shared.m_description = DescriptionKey;
            shared.m_value =
                SellPriceConfiguration.GetConfiguredHaldorSellPrice(PrefabName, HaldorIdolCatalog.AdamantineSellPrice);
            shared.m_upgradeChance = source.m_itemData.m_shared.m_upgradeChance;
            // Negative sentinel also excludes vanilla's inclusive break comparison at roll == 1.
            shared.m_breakChance = -1f;
            shared.m_breakReturnIngreientsAmount = 0f;
            drop.m_itemData.m_dropPrefab = prefab;
        }
    }
}