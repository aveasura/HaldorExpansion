using System.Reflection;
using UnityEngine;

namespace HaldorExpansion.Features.Cestus
{
    internal static class CestusNetworkState
    {

        private const string CestusShieldZdoRemainingKey = "he_cestus_shield_remaining";
        private const string CestusShieldZdoMaxKey = "he_cestus_shield_max";
        private const string CestusShieldZdoActiveKey = "he_cestus_shield_active";

        internal static ZNetView GetPlayerNView(Player player)
        {
            if (player == null)
                return null;

            FieldInfo field = typeof(Character).GetField(
                "m_nview",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            return field?.GetValue(player) as ZNetView;
        }

        private static bool IsPlayerNViewOwner(Player player)
        {
            ZNetView nview = GetPlayerNView(player);
            return nview != null && nview.IsValid() && nview.IsOwner();
        }

        private static ZDO GetPlayerZdo(Player player)
        {
            ZNetView nview = GetPlayerNView(player);
            return nview != null && nview.IsValid() ? nview.GetZDO() : null;
        }

        internal static bool IsCestusShieldAuthorityRuntime(Player player)
        {
            if (!CestusRuntime.ShouldProcessCestusLocally(player))
                return false;

            ZNetView nview = GetPlayerNView(player);
            return nview == null || !nview.IsValid() || nview.IsOwner();
        }

        internal static float GetSyncedCestusShieldRemaining(Player player)
        {
            ZDO zdo = GetPlayerZdo(player);
            return zdo != null ? Mathf.Max(0f, zdo.GetFloat(CestusShieldZdoRemainingKey, 0f)) : 0f;
        }

        internal static float GetSyncedCestusShieldMax(Player player)
        {
            ZDO zdo = GetPlayerZdo(player);
            return zdo != null ? Mathf.Max(0f, zdo.GetFloat(CestusShieldZdoMaxKey, 0f)) : 0f;
        }

        internal static void SyncCestusShieldState(Player player, CestusStateStore.CestusState state, bool force = false)
        {
            if (player == null || state == null || !IsPlayerNViewOwner(player))
                return;

            float remaining = Mathf.Max(0f, state.ShieldRemaining);
            float max = Mathf.Max(0f, state.ShieldMax);
            bool active = remaining > 0.001f;

            if (!force &&
                Mathf.Abs(state.LastSyncedShieldRemaining - remaining) <= 0.01f &&
                Mathf.Abs(state.LastSyncedShieldMax - max) <= 0.01f &&
                state.LastSyncedShieldActive == active)
            {
                return;
            }

            ZDO zdo = GetPlayerZdo(player);
            if (zdo == null)
                return;

            zdo.Set(CestusShieldZdoRemainingKey, remaining);
            zdo.Set(CestusShieldZdoMaxKey, max);
            zdo.Set(CestusShieldZdoActiveKey, active);

            state.LastSyncedShieldRemaining = remaining;
            state.LastSyncedShieldMax = max;
            state.LastSyncedShieldActive = active;
        }
    }
}
