using System;
using System.Collections.Generic;
using System.Reflection;
using HaldorExpansion.Configuration;
using HaldorExpansion.Integration;
using HaldorExpansion.Trading;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HaldorExpansion.Features.Upgrades
{
    internal static class IdolRegistration
    {
        internal const string PrefabName = "aveasura_he_idol";
        internal const string ItemKey = "$item_aveasura_he_idol";
        internal const string DescriptionKey = ItemKey + "_desc";
        internal const string LoreKey = ItemKey + "_lore";
        internal static GameObject Prefab;
        internal static ItemDrop ItemDrop;
        internal static Sprite NativeIcon;

        private static readonly MethodInfo CloneShared = typeof(object).GetMethod("MemberwiseClone",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static bool diagnosticRegistered;

        private static readonly MethodInfo UpdateRegisters = typeof(ObjectDB).GetMethod("UpdateRegisters",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly HashSet<string> NativeIdols = new HashSet<string>(StringComparer.Ordinal);

        internal static bool IsNativeIdol(GameObject prefab)
        {
            return prefab != null && NativeIdols.Contains(prefab.name);
        }

        internal static bool EnsureReady()
        {
            ObjectDB db = ObjectDB.instance;
            if (db?.m_recipes == null || db.m_recipes.Count == 0) return false;

            try
            {
                // Read actual upgrader resources, not guessed prefab names or hardcoded odds.
                NativeIdols.Clear();
                ItemDrop source = null;
                foreach (Recipe recipe in db.m_recipes)
                {
                    if (recipe == null || recipe.m_resources == null) continue;
                    foreach (Piece.Requirement requirement in recipe.m_resources)
                    {
                        ItemDrop resource = requirement?.m_resItem;
                        if (resource == null || !requirement.m_upgraderResource ||
                            resource.gameObject.name == PrefabName)
                            continue;
                        NativeIdols.Add(resource.gameObject.name);
                        if (source == null) source = resource;
                    }
                }

                if (source == null) return false;
                NativeIcon = source.m_itemData.GetIcon();

                GameObject prefab = ItemPrefabLookup.Registered(PrefabName);
                if (prefab == null)
                {
                    prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabName, source.gameObject.name);
                    if (prefab == null) return false;
                    ItemDrop drop = prefab.GetComponent<ItemDrop>();
                    if (drop == null) return false;
                    drop.m_itemData.m_shared =
                        (ItemDrop.ItemData.SharedData)CloneShared.Invoke(source.m_itemData.m_shared, null);
                    Configure(drop, source, prefab);
                    AdamantineIdolAppearance.Apply(prefab, NativeIcon, false);
                    if (!ItemManager.Instance.AddItem(new CustomItem(prefab, fixReference: false))) return false;
                }

                ItemDrop item = prefab.GetComponent<ItemDrop>();
                if (item == null) return false;
                Configure(item, source, prefab);
                if (db.GetItemPrefab(PrefabName) == null)
                {
                    // CopyOtherDB shares m_items, but each DB owns its lookup dictionaries.
                    // Jotunn can have inserted the idol through the menu DB already.
                    if (db.m_items.Exists(candidate => candidate != null && candidate.name == PrefabName))
                        UpdateRegisters.Invoke(db, null);
                    else
                        ItemManager.Instance.RegisterItemInObjectDB(prefab);
                }

                Prefab = prefab;
                ItemDrop = item;
                if (!diagnosticRegistered)
                {
                    CommandManager.Instance.AddConsoleCommand(new LootTestCommand());
                    diagnosticRegistered = true;
                }

                return true;
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError($"[Upgrades] Idol registration failed: {e}");
                return false;
            }
        }

        // Developer diagnostic: samples existing prefab tables, never instantiates containers/items.
        private sealed class LootTestCommand : ConsoleCommand
        {
            public override string Name => "he_idoltest";

            public override string Help =>
                "[HE TEST] he_idoltest [1..5000 rolls per chest] [prefab filter]; no items spawned.";

            public override void Run(string[] args, Terminal terminal)
            {
                int rolls = 1000;
                if (args.Length > 2 || (args.Length > 0 &&
                                        (!int.TryParse(args[0], out rolls) || rolls < 1 || rolls > 5000)))
                {
                    terminal?.AddString(Help);
                    return;
                }

                if (ZNetScene.instance == null || Prefab == null)
                {
                    terminal?.AddString("[HE Idol Test] Enter a world first.");
                    return;
                }

                string filter = args.Length > 1 ? args[1] : "";
                var randomState = Random.state;
                int sources = 0;
                try
                {
                    var visited = new HashSet<GameObject>();
                    foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
                    {
                        if (prefab == null || !visited.Add(prefab) ||
                            prefab.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                        DropTable table = prefab.GetComponent<Container>()?.m_defaultItems;
                        if (table?.m_drops == null || !table.m_drops.Exists(drop => IsNativeIdol(drop.m_item)))
                            continue;
                        sources++;
                        int none = 0, vanillaOnly = 0, customOnly = 0, both = 0;
                        for (int i = 0; i < rolls; i++)
                        {
                            bool vanilla = false, custom = false;
                            // Goes through the real native method AND the installed Harmony loot patch.
                            foreach (ItemDrop.ItemData item in table.GetDropListItems())
                            {
                                if (item == null || item.m_stack <= 0) continue;
                                vanilla |= IsNativeIdol(item.m_dropPrefab);
                                custom |= item.m_dropPrefab == Prefab;
                            }

                            if (vanilla && custom) both++;
                            else if (vanilla) vanillaOnly++;
                            else if (custom) customOnly++;
                            else none++;
                        }

                        string result = $"[HE Idol Test] {prefab.name}: rolls={rolls} none={none} " +
                                        $"vanilla-only={vanillaOnly} custom-only={customOnly} both={both}";
                        terminal?.AddString(result);
                        HaldorExpansionPlugin.Log?.LogInfo(result);
                    }

                    terminal?.AddString($"[HE Idol Test] Sources tested: {sources}. No items spawned. " +
                                        "Counts are random samples, not guaranteed drop rates.");
                }
                catch (Exception error)
                {
                    terminal?.AddString("[HE Idol Test] Failed: " + error.Message);
                    HaldorExpansionPlugin.Log?.LogWarning("[HE Idol Test] " + error);
                }
                finally
                {
                    Random.state = randomState;
                }
            }
        }

        private static void Configure(ItemDrop drop, ItemDrop source, GameObject prefab)
        {
            var shared = drop.m_itemData.m_shared;
            var native = source.m_itemData.m_shared;
            shared.m_name = ItemKey;
            shared.m_description = DescriptionKey;
            shared.m_value =
                SellPriceConfiguration.GetConfiguredHaldorSellPrice(
                    PrefabName, HaldorIdolCatalog.SellPrice);
            shared.m_upgradeChance = native.m_upgradeChance;
            shared.m_breakChance = native.m_breakChance;
            shared.m_breakReturnIngreientsAmount = native.m_breakReturnIngreientsAmount;
            drop.m_itemData.m_dropPrefab = prefab;
        }
    }
}