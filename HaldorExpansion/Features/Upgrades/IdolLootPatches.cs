using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Features.Upgrades
{
    [HarmonyPatch(typeof(DropTable), nameof(DropTable.GetDropListItems))]
    internal static class IdolItemLootPatch
    {
        private static void Postfix(DropTable __instance, ref List<ItemDrop.ItemData> __result)
        {
            __result = IdolLoot.AppendItems(__instance, __result);
        }
    }

    [HarmonyPatch(typeof(DropTable), nameof(DropTable.GetDropList), new Type[0])]
    internal static class IdolObjectLootPatch
    {
        private static void Postfix(DropTable __instance, ref List<GameObject> __result)
        {
            __result = IdolLoot.AppendObjects(__instance, __result);
        }
    }

    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
    internal static class IdolCharacterLootPatch
    {
        private static void Postfix(CharacterDrop __instance, ref List<KeyValuePair<GameObject, int>> __result)
        {
            __result = IdolLoot.AppendCharacter(__instance, __result);
        }
    }

    // Default contents are generated once, by the container's owner. Track only that scope;
    // failed player transfers or opening an existing chest must never spawn an extra item.
    [HarmonyPatch(typeof(Container), "AddDefaultItems")]
    internal static class IdolContainerScopePatch
    {
        [ThreadStatic] internal static Container Current;

        private static void Prefix(Container __instance, out Container __state)
        {
            __state = Current;
            Current = __instance;
        }

        private static void Finalizer(Container __state)
        {
            Current = __state;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), new[] { typeof(ItemDrop.ItemData) })]
    internal static class IdolContainerOverflowPatch
    {
        private static void Postfix(Inventory __instance, ItemDrop.ItemData __0, bool __result)
        {
            Container container = IdolContainerScopePatch.Current;
            if (__result || container == null || __instance != container.GetInventory() ||
                __0?.m_dropPrefab != IdolRegistration.Prefab) return;
            ItemDrop.DropItem(__0, __0.m_stack, container.transform.position + Vector3.up, Quaternion.identity);
        }
    }
}