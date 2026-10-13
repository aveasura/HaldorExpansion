using System;
using System.Reflection;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;

namespace HaldorExpansion.Features.Upgrades
{
    // Developer-only shortcut for testing Forge of Potential quality thresholds.
    // Operates on one existing local inventory item; never spawns or replaces items.
    internal sealed class QualityTestCommand : ConsoleCommand
    {
        private const int MaxTestQuality = 1000;

        private static readonly MethodInfo InventoryChanged =
            AccessTools.Method(typeof(Inventory), "Changed", new[] { typeof(bool), typeof(bool) });

        public override string Name => "he_setquality";

        public override string Help =>
            "[HE TEST] he_setquality <1..1000> [unique prefab/item name]; sets one Forge-supported inventory item's quality.";

        public override bool IsCheat => true;

        internal static void Register()
        {
            CommandManager.Instance.AddConsoleCommand(new QualityTestCommand());
        }

        public override void Run(string[] args, Terminal terminal)
        {
            if (args == null || args.Length < 1 ||
                !int.TryParse(args[0], out int quality) || quality < 1 || quality > MaxTestQuality)
            {
                terminal?.AddString(Help);
                return;
            }

            Player player = Player.m_localPlayer;
            if (player == null || player.IsDead())
            {
                terminal?.AddString("[HE Test] Enter a world with a living character first.");
                return;
            }

            Inventory inventory = player.GetInventory();
            if (inventory == null || InventoryChanged == null)
            {
                terminal?.AddString("[HE Test] Inventory update is unavailable; no item was changed.");
                return;
            }

            string filter = args.Length > 1 ? string.Join(" ", args, 1, args.Length - 1).Trim() : null;
            ItemDrop.ItemData target = null;
            int matches = 0;
            var items = inventory.GetAllItems();
            if (items == null) return;

            if (!string.IsNullOrEmpty(filter))
            {
                // Prefer an exact match over partial matches; do not guess between duplicates.
                foreach (ItemDrop.ItemData item in items)
                {
                    if (!UpgradeRecipes.IsSupported(item) || !Matches(item, filter, true)) continue;
                    target = item;
                    matches++;
                }

                if (matches == 0)
                {
                    foreach (ItemDrop.ItemData item in items)
                    {
                        if (!UpgradeRecipes.IsSupported(item) || !Matches(item, filter, false)) continue;
                        target = item;
                        matches++;
                    }
                }
            }
            else
            {
                // F5 console does not retain an inventory hover/selection. A held
                // supported weapon is unambiguous; otherwise use a sole equipped item.
                ItemDrop.ItemData weapon = player.GetCurrentWeapon();
                if (inventory.ContainsItem(weapon) && UpgradeRecipes.IsSupported(weapon))
                {
                    target = weapon;
                    matches = 1;
                }
                else
                {
                    foreach (ItemDrop.ItemData item in items)
                    {
                        if (!UpgradeRecipes.IsSupported(item) || !item.m_equipped) continue;
                        target = item;
                        matches++;
                    }

                    if (matches == 0)
                    {
                        foreach (ItemDrop.ItemData item in items)
                        {
                            if (!UpgradeRecipes.IsSupported(item)) continue;
                            target = item;
                            matches++;
                        }
                    }
                }
            }

            if (matches != 1 || target == null)
            {
                terminal?.AddString(matches == 0
                    ? "[HE Test] No matching Forge-supported artifact in your inventory."
                    : "[HE Test] Multiple artifacts match; specify a unique prefab or item name.");
                terminal?.AddString("[HE Test] Example: he_setquality 10 BrisingamenRing");
                return;
            }

            int oldQuality = target.m_quality;
            if (oldQuality == quality)
            {
                terminal?.AddString("[HE Test] " + DisplayName(target) + " is already quality " + quality + ".");
                return;
            }

            try
            {
                target.m_quality = quality;
                // Use Valheim's own notification so inventory UI/save listeners see the change.
                InventoryChanged.Invoke(inventory, new object[] { false, false });
                terminal?.AddString("[HE Test] " + DisplayName(target) + ": quality " + oldQuality + " -> " + quality +
                                    ".");
            }
            catch (Exception error)
            {
                target.m_quality = oldQuality;
                terminal?.AddString("[HE Test] Could not update quality; original value restored.");
                HaldorExpansionPlugin.Log?.LogWarning("[HE Test] he_setquality: " + error);
            }
        }

        private static bool Matches(ItemDrop.ItemData item, string filter, bool exact)
        {
            string prefab = item.m_dropPrefab != null ? item.m_dropPrefab.name : "";
            string key = item.m_shared != null ? item.m_shared.m_name : "";
            string name = DisplayName(item);
            if (exact)
                return string.Equals(prefab, filter, StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(key, filter, StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(name, filter, StringComparison.OrdinalIgnoreCase);
            return prefab.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   key.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string DisplayName(ItemDrop.ItemData item)
        {
            string key = item.m_shared != null ? item.m_shared.m_name : "";
            return Localization.instance != null ? Localization.instance.Localize(key) : key;
        }
    }
}