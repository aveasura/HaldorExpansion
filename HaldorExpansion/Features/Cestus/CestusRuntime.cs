using HaldorExpansion.Diagnostics;
using UnityEngine;

namespace HaldorExpansion.Features.Cestus
{
    internal static class CestusRuntime
    {

        internal static bool ShouldProcessCestusLocally(Player player)
        {
            return player != null &&
                   Player.m_localPlayer != null &&
                   player == Player.m_localPlayer;
        }

        internal static bool ShouldMirrorCestusToAdrenaline(Player player)
        {
            if (player == null)
                return false;

            CestusStateStore.CestusState state = CestusStateStore.GetOrCreateCestusState(player);

            return CestusEquipment.HasCestusEquipped(player)
                   || state.Charge > 0.001f
                   || state.ShieldRemaining > 0.001f
                   || state.IsCasting;
        }

        internal static bool ShouldForceHideCestusHudBar(Player player)
        {
            if (player == null)
                return false;

            CestusStateStore.CestusState state = CestusStateStore.GetOrCreateCestusState(player);
            return !CestusEquipment.HasCestusEquipped(player)
                   && state.ForceHideHudBar
                   && state.Charge <= 0.001f
                   && state.ShieldRemaining <= 0.001f
                   && !state.IsCasting;
        }

        internal static bool IsCestusCasting(Player player)
        {
            if (player == null)
                return false;

            CestusStateStore.CestusState state = CestusStateStore.GetOrCreateCestusState(player);
            return state.IsCasting;
        }

        internal static bool IsCestusInputLocked(Player player)
        {
            if (player == null)
                return false;

            CestusStateStore.CestusState state = CestusStateStore.GetOrCreateCestusState(player);
            return state.IsCasting && Time.time < state.InputLockedUntil;
        }

        internal static void UpdateCestusLocalEquipState(Player player)
        {
            if (!ShouldProcessCestusLocally(player))
                return;

            CestusStateStore.CestusState state = CestusStateStore.GetOrCreateCestusState(player);
            bool isEquippedNow = CestusEquipment.HasCestusEquipped(player);

            if (isEquippedNow)
            {
                state.ForceHideHudBar = false;
            }
            else if (state.WasEquippedLastFrame)
            {
                ResetCestusState(player, state, "unequipped");
                state.ForceHideHudBar = true;
            }

            state.WasEquippedLastFrame = isEquippedNow;
        }

        private static void ResetCestusState(Player player, CestusStateStore.CestusState state, string reason)
        {
            if (state == null)
                return;

            if (state.ChargeDecayRoutine != null && HaldorExpansionPlugin.Instance != null)
            {
                HaldorExpansionPlugin.Instance.StopCoroutine(state.ChargeDecayRoutine);
            }

            if (state.ShieldRoutine != null && HaldorExpansionPlugin.Instance != null)
            {
                HaldorExpansionPlugin.Instance.StopCoroutine(state.ShieldRoutine);
            }

            if (state.CastRoutine != null && HaldorExpansionPlugin.Instance != null)
            {
                HaldorExpansionPlugin.Instance.StopCoroutine(state.CastRoutine);
            }

            state.ChargeDecayRoutine = null;
            state.ShieldRoutine = null;
            state.CastRoutine = null;

            state.Charge = 0f;
            state.LastChargeGainTime = 0f;

            CestusShieldRuntime.ClearCestusShieldState(player, state, false, true);

            state.IsCasting = false;
            state.InputLockedUntil = 0f;

            if (player != null)
            {
                state.LastObservedHealth = Mathf.Max(0f, player.GetHealth());
                state.HasObservedHealth = true;
            }

            HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] Cestus state reset: reason={reason}, {PlayerDiagnostics.GetCestusDebugPlayerTag(player)}");
        }
    }
}
