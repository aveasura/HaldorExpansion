using HaldorExpansion.Compatibility;
using HaldorExpansion.Presentation;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Reflection;
using UnityEngine;

namespace HaldorExpansion.Features.NornThread
{
    internal static class NornThreadItemRegistration
    {
        internal const string PrefabName = "NornThreadAmulet";
        internal const string ItemKey = "$item_norn_thread";
        internal const string ItemDescKey = "$item_norn_thread_desc";
        internal const string EffectNameKey = "$se_norn_thread";
        internal const string EffectDescKey = "$se_norn_thread_desc";
        internal const int Price = 6666;

        internal static GameObject Prefab;
        internal static ItemDrop NornThreadItemDrop;
        internal static bool _readyLogged;

        internal static bool EnsureReady()
        {
            try
            {
                if (NornThreadItemDrop != null && Prefab != null)
                    return true;

                if (ObjectDB.instance == null)
                    return false;

                GameObject existing = ObjectDB.instance.GetItemPrefab(PrefabName);
                if (existing != null)
                {
                    Prefab = existing;
                    NornThreadItemDrop = existing.GetComponent<ItemDrop>();
                    if (NornThreadItemDrop != null)
                    {
                        ItemDrop.ItemData.SharedData existingShared = NornThreadItemDrop.m_itemData.m_shared;
                        ClearInheritedAdrenaline(existingShared);
                        existingShared.m_fullAdrenalineSE = null;
                        existingShared.m_attackStatusEffect = null;
                        ReflectionAccess.SetFieldIfExists(existingShared, "m_consumeStatusEffect", null);
                        ApplyIcon(existingShared);
                    }
                    return NornThreadItemDrop != null;
                }

                GameObject source = PrefabManager.Instance?.GetPrefab("TrinketBronzeStamina");
                if (source == null)
                    source = FindUtilitySource(ObjectDB.instance);
                if (source == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] Norn Thread utility source not found in ObjectDB (yet)");
                    return false;
                }

                GameObject go = PrefabManager.Instance.CreateClonedPrefab(PrefabName, source.name);
                if (go == null)
                    return false;

                ItemDrop drop = go.GetComponent<ItemDrop>();
                if (drop == null)
                    return false;

                ItemDrop.ItemData.SharedData shared = drop.m_itemData.m_shared;
                shared.m_name = ItemKey;
                shared.m_description = ItemDescKey;
                shared.m_maxStackSize = 1;
                shared.m_maxQuality = 1;
                shared.m_weight = 0.2f;
                shared.m_value = 0;
                shared.m_questItem = false;
                // The source trinket carries its own gameplay hooks in newer Valheim builds.
                // Norn Thread must inherit only the Utility slot, never the source passive/adrenaline behavior.
                shared.m_setName = string.Empty;
                shared.m_setSize = 0;
                shared.m_setStatusEffect = null;
                shared.m_movementModifier = 0f;
                shared.m_equipStatusEffect = null;
                shared.m_attackStatusEffect = null;
                shared.m_fullAdrenalineSE = null;
                ReflectionAccess.SetFieldIfExists(shared, "m_consumeStatusEffect", null);
                ClearInheritedAdrenaline(shared);

                ItemDrop sourceDrop = source.GetComponent<ItemDrop>();
                if (sourceDrop != null && sourceDrop.m_itemData != null && sourceDrop.m_itemData.m_shared != null)
                    shared.m_itemType = sourceDrop.m_itemData.m_shared.m_itemType;

                // A stat-neutral equip effect gives the amulet a highlighted passive field
                // without granting hidden armor/health/stamina bonuses.
                SE_Stats stats = ScriptableObject.CreateInstance<SE_Stats>();
                stats.name = "SE_NornThread";
                ReflectionAccess.SetFieldIfExists(stats, "m_name", EffectNameKey);
                ReflectionAccess.SetFieldIfExists(stats, "m_tooltip", EffectDescKey);
                shared.m_equipStatusEffect = stats;

                ApplyIcon(shared);
                ItemManager.Instance.AddItem(new CustomItem(go, true));

                Prefab = go;
                NornThreadItemDrop = drop;
                if (!_readyLogged)
                {
                    _readyLogged = true;
                    HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] Norn Thread registered: {PrefabName} (source={source.name})");
                }
                return true;
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError($"[HaldorExpansion] Ensure Norn Thread error: {e}");
                return false;
            }
        }

        private static GameObject FindUtilitySource(ObjectDB db)
        {
            if (db?.m_items == null) return null;
            foreach (GameObject go in db.m_items)
            {
                ItemDrop drop = go?.GetComponent<ItemDrop>();
                if (drop?.m_itemData?.m_shared == null) continue;
                if (drop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility)
                    return go;
            }
            return null;
        }


        private static void ClearInheritedAdrenaline(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null) return;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (FieldInfo field in shared.GetType().GetFields(flags))
            {
                if (field == null || field.IsInitOnly) continue;
                if (field.Name.IndexOf("adren", StringComparison.OrdinalIgnoreCase) < 0) continue;

                try
                {
                    Type type = field.FieldType;
                    if (!type.IsValueType)
                    {
                        field.SetValue(shared, null);
                    }
                    else if (type == typeof(float)) field.SetValue(shared, 0f);
                    else if (type == typeof(double)) field.SetValue(shared, 0d);
                    else if (type == typeof(int)) field.SetValue(shared, 0);
                    else if (type == typeof(uint)) field.SetValue(shared, 0u);
                    else if (type == typeof(long)) field.SetValue(shared, 0L);
                    else if (type == typeof(bool)) field.SetValue(shared, false);
                    else field.SetValue(shared, Activator.CreateInstance(type));
                }
                catch (Exception e)
                {
                    HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] Norn Thread: could not clear inherited adrenaline field {field.Name}: {e.Message}");
                }
            }

            foreach (PropertyInfo property in shared.GetType().GetProperties(flags))
            {
                if (property == null || !property.CanWrite) continue;
                if (property.Name.IndexOf("adren", StringComparison.OrdinalIgnoreCase) < 0) continue;

                try
                {
                    Type type = property.PropertyType;
                    object value = !type.IsValueType ? null : Activator.CreateInstance(type);
                    property.SetValue(shared, value, null);
                }
                catch (Exception e)
                {
                    HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] Norn Thread: could not clear inherited adrenaline property {property.Name}: {e.Message}");
                }
            }
        }

        private static void ApplyIcon(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null) return;
            if (!EmbeddedIconLoader.TrySetEmbeddedIcon(shared, "norn_thread.png"))
                HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] Embedded icon missing for Norn Thread: norn_thread.png");
        }
    }
}
