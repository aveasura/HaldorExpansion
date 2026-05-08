using System;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace HaldorExpansion
{
    public partial class HaldorExpansionPlugin
    {
        internal const string RingPrefabName = "BrisingamenRing";
        internal const int RingPrice = 1600;

        internal static GameObject RingPrefab;
        internal static ItemDrop RingItemDrop;

        private static bool _ringReadyLogged;

        internal static void ResetItemCaches()
        {
            RingPrefab = null;
            RingItemDrop = null;

            DelayedDoomChestPrefab = null;
            DelayedDoomChestItemDrop = null;

            PitKingChestPrefab = null;
            PitKingChestItemDrop = null;

            GritCestusPrefab = null;
            GritCestusItemDrop = null;

            ShadowCrossbowPrefab = null;
            ShadowCrossbowItemDrop = null;

            ResetWoundedBeastCapeState();

            _ringReadyLogged = false;
        }

        internal static bool EnsureRingReady()
        {
            try
            {
                if (RingItemDrop != null && RingPrefab != null)
                    return true;

                if (ObjectDB.instance == null)
                    return false;

                GameObject existing = ObjectDB.instance.GetItemPrefab(RingPrefabName);
                if (existing != null)
                {
                    RingPrefab = existing;
                    RingItemDrop = existing.GetComponent<ItemDrop>();

                    if (RingItemDrop != null)
                    {
                        ApplyBrisingamenIcon(RingItemDrop.m_itemData.m_shared);
                    }

                    if (!_ringReadyLogged && RingItemDrop != null)
                    {
                        _ringReadyLogged = true;
                        DebugLog($"[HaldorExpansion] Ring already in ObjectDB: {RingPrefabName}");
                    }

                    return RingItemDrop != null;
                }

                GameObject src = FindCarryWeightSource(ObjectDB.instance);
                if (src == null)
                {
                    Log?.LogWarning("[HaldorExpansion] Carry-weight source item not found in ObjectDB (yet)");
                    return false;
                }

                GameObject ringGo = PrefabManager.Instance.CreateClonedPrefab(RingPrefabName, src.name);
                if (ringGo == null)
                {
                    Log?.LogWarning("[HaldorExpansion] CreateClonedPrefab returned null");
                    return false;
                }

                ItemDrop drop = ringGo.GetComponent<ItemDrop>();
                if (drop == null)
                {
                    Log?.LogWarning("[HaldorExpansion] ItemDrop missing on cloned prefab");
                    return false;
                }

                ItemDrop.ItemData.SharedData shared = drop.m_itemData.m_shared;

                shared.m_name = "$item_brisingamen";
                shared.m_description = "$item_brisingamen_desc";

                shared.m_setName = "";
                shared.m_setSize = 0;
                shared.m_setStatusEffect = null;

                SE_Stats baseSe = shared.m_equipStatusEffect as SE_Stats;
                SE_Stats newSe = ScriptableObject.CreateInstance<SE_Stats>();
                newSe.name = "SE_BrisingamenCarryWeight";

                if (baseSe != null)
                {
                    CopyFieldIfExists(baseSe, newSe, "m_icon");
                    CopyFieldIfExists(baseSe, newSe, "m_startMessage");
                    CopyFieldIfExists(baseSe, newSe, "m_stopMessage");
                    CopyFieldIfExists(baseSe, newSe, "m_startMessageType");
                    CopyFieldIfExists(baseSe, newSe, "m_stopMessageType");
                }

                newSe.m_addMaxCarryWeight = 150f;

                SetFieldIfExists(newSe, "m_name", "$item_brisingamen");
                SetFieldIfExists(newSe, "m_tooltip", "$item_brisingamen_desc");

                shared.m_equipStatusEffect = newSe;

                GameObject trinketGo = null;
                if (PrefabManager.Instance != null)
                {
                    trinketGo = PrefabManager.Instance.GetPrefab("TrinketBronzeStamina");
                }

                if (trinketGo != null)
                {
                    ItemDrop tDrop = trinketGo.GetComponent<ItemDrop>();
                    if (tDrop != null)
                    {
                        shared.m_itemType = tDrop.m_itemData.m_shared.m_itemType;
                    }
                }

                ApplyBrisingamenIcon(shared);
                ItemManager.Instance.AddItem(new CustomItem(ringGo, true));

                RingPrefab = ringGo;
                RingItemDrop = drop;

                if (!_ringReadyLogged)
                {
                    _ringReadyLogged = true;
                    DebugLog($"[HaldorExpansion] Ring registered: {RingPrefabName} (source={src.name})");
                }

                return true;
            }
            catch (Exception e)
            {
                Log?.LogError($"[HaldorExpansion] EnsureRingReady error: {e}");
                return false;
            }
        }

        private static GameObject FindCarryWeightSource(ObjectDB db)
        {
            if (db == null || db.m_items == null)
                return null;

            foreach (GameObject go in db.m_items)
            {
                if (go == null)
                    continue;

                ItemDrop d = go.GetComponent<ItemDrop>();
                if (d == null)
                    continue;

                if (d.m_itemData != null &&
                    d.m_itemData.m_shared != null &&
                    d.m_itemData.m_shared.m_name == "$item_megingjord")
                {
                    return go;
                }
            }

            foreach (GameObject go in db.m_items)
            {
                if (go == null)
                    continue;

                ItemDrop d = go.GetComponent<ItemDrop>();
                if (d == null)
                    continue;

                ItemDrop.ItemData.SharedData shared = d.m_itemData != null ? d.m_itemData.m_shared : null;
                SE_Stats se = shared != null ? shared.m_equipStatusEffect as SE_Stats : null;

                if (se != null && se.m_addMaxCarryWeight > 0f)
                    return go;
            }

            return null;
        }

        private static void ApplyBrisingamenIcon(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return;

            if (!TrySetEmbeddedIcon(shared, "brisingamen.png"))
            {
                Log?.LogWarning("[HaldorExpansion] Embedded icon missing for brisingamen: brisingamen.png");
            }
        }
    }
}