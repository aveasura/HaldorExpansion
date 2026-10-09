using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HaldorExpansion.Presentation;
using HarmonyLib;

namespace HaldorExpansion.Patches
{
    [HarmonyPatch]
    internal static class HaldorTabsListPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(StoreGui), "FillList");
            yield return AccessTools.Method(typeof(StoreGui), "SelectItem");
            yield return AccessTools.Method(typeof(StoreGui), "GetSelectedItemIndex");
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var original = AccessTools.Method(typeof(Trader), nameof(Trader.GetAvailableItems));
            var replacement =
                AccessTools.Method(typeof(HaldorTabsController), nameof(HaldorTabsController.GetVisibleItems));
            int replaced = 0;
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(original))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = replacement;
                    replaced++;
                }

                yield return instruction;
            }

            if (replaced != 1)
                throw new InvalidOperationException("Unexpected StoreGui item-list contract for Haldor tabs.");
        }
    }

    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.Show))]
    internal static class HaldorTabsShowPatch
    {
        private static void Postfix(StoreGui __instance) => HaldorTabsController.Sync(__instance);
    }

    [HarmonyPatch(typeof(StoreGui), "Update")]
    internal static class HaldorTabsUpdatePatch
    {
        private static void Prefix(StoreGui __instance) => HaldorTabsController.Tick(__instance);
    }

    [HarmonyPatch(typeof(StoreGui), "SelectItem")]
    internal static class HaldorTabsSelectionPatch
    {
        private static void Prefix(StoreGui __instance, ref int __0) =>
            HaldorTabsController.ClampSelection(__instance, ref __0);
    }

    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.Hide))]
    internal static class HaldorTabsHidePatch
    {
        private static void Postfix() => HaldorTabsController.Reset();
    }

    [HarmonyPatch(typeof(StoreGui), "OnDestroy")]
    internal static class HaldorTabsDestroyPatch
    {
        private static void Prefix() => HaldorTabsController.Reset();
    }
}