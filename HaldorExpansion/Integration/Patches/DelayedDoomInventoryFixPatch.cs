using HaldorExpansion.Features.Cestus;
using HaldorExpansion.Features.DelayedDoom;
using System.Collections.Generic;
using HarmonyLib;

namespace HaldorExpansion.Patches
{
    [HarmonyPatch(typeof(Player), "Update")]
    internal static class DelayedDoomInventoryFixPatch
    {
        private static readonly HashSet<int> ProcessedPlayers = new HashSet<int>();

        static void Postfix(Player __instance)
        {
            if (__instance == null || ObjectDB.instance == null)
                return;

            int id = __instance.GetInstanceID();
            if (ProcessedPlayers.Contains(id))
                return;

            // временный лог рецептов для всех предметов в инвентаре
            foreach (var item in __instance.GetInventory().GetAllItems())
            {
                if (item == null || item.m_shared == null)
                    continue;

                Recipe r = ObjectDB.instance.GetRecipe(item);

                HaldorExpansionPlugin.DebugLog(
                    $"[RepairDebug] item={item.m_shared.m_name}, " +
                    $"recipe={(r != null ? r.name : "null")}, " +
                    $"craft={(r != null ? r.m_craftingStation?.name : "null")}, " +
                    $"repair={(r != null ? r.m_repairStation?.name : "null")}, " +
                    $"durability={item.m_durability:0.##}/{item.GetMaxDurability():0.##}");
            }

            bool done = DelayedDoomInventoryRebind.FixDelayedDoomItemsInInventory(__instance);
            bool fixedCestus = CestusInventoryRebind.FixCestusItemsInInventory(__instance);

            // чтобы лог не спамился каждый кадр
            ProcessedPlayers.Add(id);
        }
    }
}