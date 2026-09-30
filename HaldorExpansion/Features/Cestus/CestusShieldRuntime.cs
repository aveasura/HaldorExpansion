using HaldorExpansion.Compatibility;
using HaldorExpansion.Diagnostics;
using System.Collections;
using UnityEngine;

namespace HaldorExpansion.Features.Cestus
{
    internal static class CestusShieldRuntime
    {

        internal const float CestusShieldMaxHpRatio = 0.40f; // 40% от максимального хп станивится временным щитом
        internal const float CestusShieldMinFlat = 20f; // минимально возможный щит == 20 единиц

        internal const float CestusShieldDuration = 6f;
        internal const int CestusShieldTicks = 40;

        internal static float GetCestusEffectiveShieldRemaining(Player player)
        {
            if (player == null)
                return 0f;

            CestusStateStore.CestusState state = CestusStateStore.GetOrCreateCestusState(player);
            return Mathf.Max(Mathf.Max(0f, state.ShieldRemaining), CestusNetworkState.GetSyncedCestusShieldRemaining(player));
        }

        internal static void ClearCestusShieldState(Player player, CestusStateStore.CestusState state, bool stopRoutine, bool syncNow)
        {
            if (state == null)
                return;

            if (stopRoutine && state.ShieldRoutine != null && HaldorExpansionPlugin.Instance != null)
            {
                HaldorExpansionPlugin.Instance.StopCoroutine(state.ShieldRoutine);
                state.ShieldRoutine = null;
            }

            state.ShieldRemaining = 0f;
            state.ShieldMax = 0f;
            state.ShieldDecayPerTick = 0f;
            state.ShieldTicksLeft = 0;

            if (syncNow)
            {
                CestusNetworkState.SyncCestusShieldState(player, state, true);
            }
            else
            {
                state.LastSyncedShieldRemaining = -1f;
                state.LastSyncedShieldMax = -1f;
                state.LastSyncedShieldActive = false;
            }
        }

        internal static bool IsCestusShieldActive(Player player)
        {
            return GetCestusEffectiveShieldRemaining(player) > 0.001f;
        }

        internal static float AbsorbCestusShield(Player player, float incomingDamage)
        {
            if (player == null || incomingDamage <= 0f)
                return 0f;

            if (!CestusNetworkState.IsCestusShieldAuthorityRuntime(player))
                return 0f;

            CestusStateStore.CestusState state = CestusStateStore.GetOrCreateCestusState(player);
            if (state.ShieldRemaining <= 0.001f)
                return 0f;

            float absorbed = Mathf.Min(state.ShieldRemaining, incomingDamage);
            state.ShieldRemaining = Mathf.Max(0f, state.ShieldRemaining - absorbed);

            if (state.ShieldRemaining <= 0.001f)
            {
                ClearCestusShieldState(player, state, false, true);
            }
            else
            {
                CestusNetworkState.SyncCestusShieldState(player, state);
            }

            HaldorExpansionPlugin.DebugLog(
                $"[HaldorExpansion] Cestus shield absorbed: " +
                $"incoming={incomingDamage:0.##}, " +
                $"absorbed={absorbed:0.##}, " +
                $"remaining={state.ShieldRemaining:0.##}, " +
                $"syncedRemaining={CestusNetworkState.GetSyncedCestusShieldRemaining(player):0.##}, " +
                $"{PlayerDiagnostics.GetCestusDebugPlayerTag(player)}");

            return absorbed;
        }

        internal static float CorrectCestusShieldAfterDamage(Player player, float beforeHealth, float afterHealth)
        {
            if (player == null)
                return 0f;

            if (!CestusNetworkState.IsCestusShieldAuthorityRuntime(player))
                return 0f;

            if (player.IsDead())
                return 0f;

            float healthLoss = beforeHealth - afterHealth;
            if (healthLoss <= 0.001f)
                return 0f;

            CestusStateStore.CestusState state = CestusStateStore.GetOrCreateCestusState(player);
            if (state.ShieldRemaining <= 0.001f)
                return 0f;

            float absorbed = Mathf.Min(state.ShieldRemaining, healthLoss);
            if (absorbed <= 0.001f)
                return 0f;

            state.ShieldRemaining = Mathf.Max(0f, state.ShieldRemaining - absorbed);

            float correctedHealth = Mathf.Min(CharacterHealthAccess.GetCharacterMaxHealth(player), Mathf.Max(0f, afterHealth + absorbed));
            CharacterHealthAccess.SetCharacterHealth(player, correctedHealth);

            if (state.ShieldRemaining <= 0.001f)
            {
                ClearCestusShieldState(player, state, false, true);
            }
            else
            {
                CestusNetworkState.SyncCestusShieldState(player, state);
            }

            HaldorExpansionPlugin.DebugLog(
                $"[HaldorExpansion] Cestus shield corrected post-damage: " +
                $"before={beforeHealth:0.##}, " +
                $"after={afterHealth:0.##}, " +
                $"absorbed={absorbed:0.##}, " +
                $"newHealth={correctedHealth:0.##}, " +
                $"remaining={state.ShieldRemaining:0.##}, " +
                $"{PlayerDiagnostics.GetCestusDebugPlayerTag(player)}");

            return absorbed;
        }

        internal static IEnumerator CestusShieldRoutine(CestusStateStore.CestusState state)
        {
            if (state == null)
                yield break;

            float waitPerTick = CestusShieldDuration / CestusShieldTicks;
            long playerId = state.Player != null ? CestusStateStore.GetCestusStateKey(state.Player) : 0L;

            try
            {
                while (true)
                {
                    yield return new WaitForSeconds(waitPerTick);

                    Player player = state.Player;
                    if (player == null || player.IsDead())
                        yield break;

                    if (state.ShieldRemaining <= 0.001f || state.ShieldTicksLeft <= 0)
                        yield break;

                    float decay = Mathf.Min(state.ShieldRemaining, state.ShieldDecayPerTick);

                    state.ShieldRemaining = Mathf.Max(0f, state.ShieldRemaining - decay);
                    state.ShieldTicksLeft--;

                    if (state.ShieldRemaining <= 0.001f)
                    {
                        ClearCestusShieldState(player, state, false, true);
                    }
                    else
                    {
                        CestusNetworkState.SyncCestusShieldState(player, state);
                    }

                    HaldorExpansionPlugin.DebugLog(
                        $"[HaldorExpansion] Cestus shield decay: " +
                        $"decay={decay:0.##}, " +
                        $"remaining={state.ShieldRemaining:0.##}, " +
                        $"ticksLeft={state.ShieldTicksLeft}");

                    if (state.ShieldRemaining <= 0.001f)
                        yield break;
                }
            }
            finally
            {
                state.ShieldRoutine = null;
                ClearCestusShieldState(state.Player, state, false, true);

                if (playerId != 0 &&
                    CestusStateStore.ActiveCestusStates.TryGetValue(playerId, out CestusStateStore.CestusState current) &&
                    ReferenceEquals(current, state))
                {
                    current.ShieldRoutine = null;
                    ClearCestusShieldState(current.Player, current, false, true);
                }
            }
        }

        internal static float GetCestusShieldNormalized(Player player)
        {
            if (player == null)
                return 0f;

            CestusStateStore.CestusState state = CestusStateStore.GetOrCreateCestusState(player);

            if (state.ShieldRemaining <= 0.001f || state.ShieldMax <= 0.001f)
                return 0f;

            return Mathf.Clamp01(state.ShieldRemaining / state.ShieldMax);
        }

        internal static float GetCestusShieldBarNormalized(Player player)
        {
            if (player == null)
                return 0f;

            CestusStateStore.CestusState state = CestusStateStore.GetOrCreateCestusState(player);
            if (state.ShieldRemaining <= 0.001f)
                return 0f;

            float maxHealth = CharacterHealthAccess.GetCharacterMaxHealth(player);
            if (maxHealth <= 0f)
                return 0f;

            return Mathf.Clamp01(state.ShieldRemaining / maxHealth);
        }

        internal static float GetCestusHealthNormalized(Player player)
        {
            if (player == null)
                return 0f;

            float maxHealth = CharacterHealthAccess.GetCharacterMaxHealth(player);
            if (maxHealth <= 0f)
                return 0f;

            return Mathf.Clamp01(player.GetHealth() / maxHealth);
        }
    }
}
