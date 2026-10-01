using HaldorExpansion.Compatibility;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace HaldorExpansion.Features.NornThread
{
    internal static class NornThreadRuntime
    {
        internal const float SavedHealthRatio = 0.50f;
        internal const float GraceDuration = 1.50f;

        private static readonly Dictionary<int, float> GraceUntil = new Dictionary<int, float>();
        private static readonly MethodInfo UnequipItemMethod = AccessTools.Method(typeof(Humanoid), "UnequipItem", new[] { typeof(ItemDrop.ItemData), typeof(bool) });
        private static readonly MethodInfo RemoveItemMethod = AccessTools.Method(typeof(Inventory), "RemoveItem", new[] { typeof(ItemDrop.ItemData) });
        private static readonly MethodInfo RemoveItemAmountMethod = AccessTools.Method(typeof(Inventory), "RemoveItem", new[] { typeof(ItemDrop.ItemData), typeof(int) });

        internal static bool IsGraceActive(Player player)
        {
            if (player == null) return false;
            int id = player.GetInstanceID();
            if (!GraceUntil.TryGetValue(id, out float until)) return false;
            if (Time.time < until) return true;
            GraceUntil.Remove(id);
            return false;
        }

        internal static float StartGrace(Player player)
        {
            if (player == null) return 0f;
            float until = Time.time + GraceDuration;
            GraceUntil[player.GetInstanceID()] = until;
            return until;
        }

        internal static ItemDrop.ItemData FindEquipped(Player player)
        {
            Inventory inventory = player?.GetInventory();
            List<ItemDrop.ItemData> items = inventory?.GetAllItems();
            if (items == null) return null;

            foreach (ItemDrop.ItemData item in items)
            {
                if (item == null || !item.m_equipped || item.m_shared == null) continue;
                if (item.m_dropPrefab != null && item.m_dropPrefab.name == NornThreadItemRegistration.PrefabName)
                    return item;
                if (item.m_shared.m_name == NornThreadItemRegistration.ItemKey)
                    return item;
            }
            return null;
        }

        internal static bool TryConsume(Player player, out ItemDrop.ItemData consumed)
        {
            consumed = FindEquipped(player);
            if (consumed == null) return false;

            Inventory inventory = player.GetInventory();
            if (inventory == null) return false;

            if (RemoveItemMethod == null && RemoveItemAmountMethod == null)
            {
                HaldorExpansionPlugin.Log?.LogError("[HaldorExpansion] Norn Thread could not find an Inventory.RemoveItem(ItemData...) overload; amulet was not consumed.");
                return false;
            }

            try
            {
                if (UnequipItemMethod != null)
                    UnequipItemMethod.Invoke(player, new object[] { consumed, false });
                else
                    consumed.m_equipped = false;

                if (RemoveItemMethod != null)
                    RemoveItemMethod.Invoke(inventory, new object[] { consumed });
                else
                    RemoveItemAmountMethod.Invoke(inventory, new object[] { consumed, 1 });

                return true;
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError($"[HaldorExpansion] Norn Thread consume error: {e}");
                return false;
            }
        }

        internal static float SavedHealth(float beforeHealth)
        {
            if (float.IsNaN(beforeHealth) || float.IsInfinity(beforeHealth) || beforeHealth <= 0f)
                return 1f;
            return Mathf.Max(1f, beforeHealth * SavedHealthRatio);
        }

        internal static void CompleteRescue(Player player, float beforeHealth)
        {
            if (player == null) return;
            CharacterHealthAccess.SetCharacterHealth(player, SavedHealth(beforeHealth));
            StartGrace(player);
            player.Message(MessageHud.MessageType.Center, "$msg_norn_thread_broken");
            NornThreadEffects.Broadcast(player);
        }

        internal static void Reset(Player player)
        {
            if (player != null) GraceUntil.Remove(player.GetInstanceID());
        }
    }
}
