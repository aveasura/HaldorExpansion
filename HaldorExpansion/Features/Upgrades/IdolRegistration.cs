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
                    CommandManager.Instance.AddConsoleCommand(new MobLootTestCommand());
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

        // Temporary developer diagnostic for Valheim 1.0.20 CharacterDrop compatibility.
        // Uses the real native generator and our Harmony postfix on an existing, registered
        // creature prefab. No enemies/items are spawned and no saved world data is touched.
        private sealed class MobLootTestCommand : ConsoleCommand
        {
            public override string Name => "he_idolmobtest";

            public override string Help =>
                "[HE TEST] he_idolmobtest; check CharacterDrop idol compatibility (no world drops).";

            public override void Run(string[] args, Terminal terminal)
            {
                if (args.Length != 0)
                {
                    terminal?.AddString(Help);
                    return;
                }

                if (ZNetScene.instance?.m_prefabs == null || ObjectDB.instance?.m_recipes == null ||
                    Prefab == null)
                {
                    terminal?.AddString("[HE Idol Mob Test] Enter a world first.");
                    return;
                }

                // Find an actual vanilla upgrader resource from the game's recipes,
                // rather than assuming a particular native idol prefab name.
                GameObject native = null;
                foreach (Recipe recipe in ObjectDB.instance.m_recipes)
                {
                    if (recipe?.m_resources == null) continue;
                    foreach (Piece.Requirement requirement in recipe.m_resources)
                    {
                        ItemDrop resource = requirement?.m_resItem;
                        if (requirement != null && requirement.m_upgraderResource &&
                            resource != null && IsNativeIdol(resource.gameObject))
                        {
                            native = resource.gameObject;
                            break;
                        }
                    }

                    if (native != null) break;
                }

                if (native == null)
                {
                    terminal?.AddString("[HE Idol Mob Test] FAIL: native idol resource not found.");
                    return;
                }

                // Prefer a simple common creature, but work with any registered creature
                // with a CharacterDrop if the usual ones are absent or renamed by mods.
                CharacterDrop source = null;
                string sourceName = null;
                foreach (string preferred in new[] { "Greydwarf", "Boar", "Neck" })
                {
                    foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
                    {
                        if (prefab == null || prefab.name != preferred) continue;
                        CharacterDrop candidate = prefab.GetComponent<CharacterDrop>();
                        if (candidate?.m_drops == null || candidate.m_drops.Count == 0) continue;
                        source = candidate;
                        sourceName = prefab.name;
                        break;
                    }

                    if (source != null) break;
                }

                if (source == null)
                {
                    foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
                    {
                        if (prefab == null) continue;
                        CharacterDrop candidate = prefab.GetComponent<CharacterDrop>();
                        if (candidate?.m_drops == null || candidate.m_drops.Count == 0) continue;
                        source = candidate;
                        sourceName = prefab.name;
                        break;
                    }
                }

                if (source == null)
                {
                    terminal?.AddString("[HE Idol Mob Test] FAIL: no creature CharacterDrop prefab found.");
                    return;
                }

                // 100% removes randomness from the assertion and keeps the test outside
                // the game's rare-drop pseudo-counter branch. Do not modify the original
                // entries: replace only the list reference for one synchronous call.
                var originalDrops = source.m_drops;
                var testDrops = new List<CharacterDrop.Drop>
                {
                    new CharacterDrop.Drop
                    {
                        m_prefab = native,
                        m_amountMin = 1,
                        m_amountMax = 1,
                        m_chance = 1f,
                        m_onePerPlayer = false,
                        m_levelMultiplier = false,
                        m_dontScale = true
                    }
                };
                var randomState = Random.state;
                List<KeyValuePair<GameObject, int>> result = null;
                Exception failure = null;
                try
                {
                    source.m_drops = testDrops;
                    // Calls Valheim's actual method, which runs our Harmony postfix.
                    result = source.GenerateDropList();
                }
                catch (Exception error)
                {
                    failure = error;
                }
                finally
                {
                    source.m_drops = originalDrops;
                    Random.state = randomState;
                }

                if (failure != null)
                {
                    string message = "[HE Idol Mob Test] FAIL: " + sourceName +
                                     " GenerateDropList threw " + failure.GetType().Name +
                                     ": " + failure.Message + "; original table restored.";
                    terminal?.AddString(message);
                    HaldorExpansionPlugin.Log?.LogWarning(message + " " + failure);
                    return;
                }

                int vanillaCount = 0;
                int customCount = 0;
                if (result != null)
                {
                    foreach (KeyValuePair<GameObject, int> entry in result)
                    {
                        if (entry.Key == native) vanillaCount += entry.Value;
                        if (entry.Key == Prefab) customCount += entry.Value;
                    }
                }

                bool restored = ReferenceEquals(originalDrops, source.m_drops);
                bool passed = vanillaCount == 1 && customCount == 1 && restored;
                string report = "[HE Idol Mob Test] " + (passed ? "PASS" : "FAIL") +
                                ": creature=" + sourceName + " vanilla=" + vanillaCount +
                                " custom=" + customCount + " restored=" + restored +
                                ". No world items spawned.";
                terminal?.AddString(report);
                if (passed) HaldorExpansionPlugin.Log?.LogInfo(report);
                else HaldorExpansionPlugin.Log?.LogWarning(report);
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