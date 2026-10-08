using Jotunn.Managers;
using UnityEngine;

namespace HaldorExpansion.Integration
{
    internal static class ItemPrefabLookup
    {
        internal static GameObject Registered(string name)
        {
            // During the menu's CopyOtherDB prefix the active DB is still empty.
            // Reuse Jotunn's persistent custom prefab instead of cloning it again.
            return ObjectDB.instance?.GetItemPrefab(name)
                   ?? ItemManager.Instance.GetItem(name)?.ItemPrefab;
        }

        internal static GameObject Source(string name)
        {
            // OnVanillaPrefabsAvailable guarantees the vanilla prefab cache is ready,
            // but the current ObjectDB has not yet received its item/recipe lists.
            return ObjectDB.instance?.GetItemPrefab(name)
                   ?? PrefabManager.Instance.GetPrefab(name);
        }
    }
}