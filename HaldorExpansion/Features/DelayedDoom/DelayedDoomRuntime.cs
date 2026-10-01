using HaldorExpansion.Compatibility;
using HaldorExpansion.Diagnostics;
using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;

namespace HaldorExpansion.Features.DelayedDoom
{
    internal static class DelayedDoomRuntime
    {

        internal const float DelayedDoomTriggerRatio = 0.50f;
        internal const float DelayedDoomImmediateRatio = 0.50f;
        internal const float DelayedDoomDebtRatio = 0.70f;
        internal const float DelayedDoomDuration = 6f;
        internal const int DelayedDoomTicks = 12;

        private static readonly HashSet<int> DelayedDoomInternalDamageVictims = new HashSet<int>();

        private sealed class DelayedDoomDotState
        {
            public Player Player;
            public float RemainingDamage;
            public int TicksLeft;
            public Coroutine Routine;
        }

        private static readonly Dictionary<int, DelayedDoomDotState> ActiveDelayedDoomDots =
            new Dictionary<int, DelayedDoomDotState>();

        internal static bool IsDelayedDoomInternalDamage(Character character)
        {
            return character != null && DelayedDoomInternalDamageVictims.Contains(character.GetInstanceID());
        }

        internal static void AddDelayedDoomDebt(Player player, float delayedDamageTotal)
        {
            if (HaldorExpansionPlugin.Instance == null || player == null || delayedDamageTotal <= 0f)
                return;

            int id = player.GetInstanceID();

            if (!ActiveDelayedDoomDots.TryGetValue(id, out DelayedDoomDotState state))
            {
                state = new DelayedDoomDotState
                {
                    Player = player,
                    RemainingDamage = 0f,
                    TicksLeft = 0,
                    Routine = null
                };

                ActiveDelayedDoomDots[id] = state;
            }

            state.Player = player;
            state.RemainingDamage += delayedDamageTotal;
            state.TicksLeft = DelayedDoomTicks;

            HaldorExpansionPlugin.DebugLog(
                $"[HaldorExpansion] Delayed Doom debt added: " +
                $"added={delayedDamageTotal:0.##}, " +
                $"total={state.RemainingDamage:0.##}, " +
                $"ticksLeft={state.TicksLeft}, " +
                $"{PlayerDiagnostics.GetCestusDebugPlayerTag(player)}");

            if (state.Routine == null)
            {
                HaldorExpansionPlugin.DebugLog(
                    $"[HaldorExpansion] Delayed Doom routine start: " +
                    $"waitPerTick={DelayedDoomDuration / DelayedDoomTicks:0.###}, " +
                    $"{PlayerDiagnostics.GetCestusDebugPlayerTag(player)}");

                state.Routine = HaldorExpansionPlugin.Instance.StartCoroutine(DelayedDoomRoutine(state));
            }
        }

        private static IEnumerator DelayedDoomRoutine(DelayedDoomDotState state)
        {
            if (state == null)
                yield break;

            float waitPerTick = DelayedDoomDuration / DelayedDoomTicks;
            int playerId = state.Player != null ? state.Player.GetInstanceID() : 0;

            try
            {
                while (true)
                {
                    yield return new WaitForSeconds(waitPerTick);

                    Player player = state.Player;
                    if (player == null)
                    {
                        HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Delayed Doom routine stop: player null");
                        yield break;
                    }

                    if (player.IsDead())
                    {
                        HaldorExpansionPlugin.DebugLog(
                            $"[HaldorExpansion] Delayed Doom routine stop: player dead, " +
                            $"{PlayerDiagnostics.GetCestusDebugPlayerTag(player)}");
                        yield break;
                    }

                    if (state.RemainingDamage <= 0f || state.TicksLeft <= 0)
                    {
                        HaldorExpansionPlugin.DebugLog(
                            $"[HaldorExpansion] Delayed Doom routine stop: exhausted, " +
                            $"remaining={state.RemainingDamage:0.##}, " +
                            $"ticksLeft={state.TicksLeft}, " +
                            $"{PlayerDiagnostics.GetCestusDebugPlayerTag(player)}");
                        yield break;
                    }

                    float tickDamage = state.RemainingDamage / state.TicksLeft;
                    tickDamage = Mathf.Max(0f, tickDamage);

                    state.RemainingDamage = Mathf.Max(0f, state.RemainingDamage - tickDamage);
                    state.TicksLeft--;

                    HaldorExpansionPlugin.DebugLog(
                        $"[HaldorExpansion] Delayed Doom pooled tick: " +
                        $"tick={tickDamage:0.##}, " +
                        $"remaining={state.RemainingDamage:0.##}, " +
                        $"ticksLeft={state.TicksLeft}, " +
                        $"{PlayerDiagnostics.GetCestusDebugPlayerTag(player)}");

                    bool survived = ApplyDelayedDoomHealthTick(player, tickDamage);
                    if (!survived)
                        yield break;

                    if (state.RemainingDamage <= 0.001f)
                        yield break;
                }
            }
            finally
            {
                if (state.Player != null)
                {
                    HaldorExpansionPlugin.DebugLog(
                        $"[HaldorExpansion] Delayed Doom routine finally: " +
                        $"remainingBeforeClear={state.RemainingDamage:0.##}, " +
                        $"ticksLeftBeforeClear={state.TicksLeft}, " +
                        $"{PlayerDiagnostics.GetCestusDebugPlayerTag(state.Player)}");
                }

                state.Routine = null;
                state.RemainingDamage = 0f;
                state.TicksLeft = 0;

                if (playerId != 0 &&
                    ActiveDelayedDoomDots.TryGetValue(playerId, out DelayedDoomDotState current) &&
                    ReferenceEquals(current, state))
                {
                    ActiveDelayedDoomDots.Remove(playerId);
                }
            }
        }

        private static bool ApplyDelayedDoomHealthTick(Player player, float amount)
        {
            if (player == null || amount <= 0f)
                return false;

            float currentHealth = CharacterHealthAccess.GetCharacterHealth(player);
            if (currentHealth <= 0f)
                return false;

            float newHealth = currentHealth - amount;

            HaldorExpansionPlugin.DebugLog(
                $"[HaldorExpansion] Delayed Doom tick: " +
                $"current={currentHealth:0.##}, " +
                $"tick={amount:0.##}, " +
                $"new={newHealth:0.##}, " +
                $"{PlayerDiagnostics.GetCestusDebugPlayerTag(player)}");

            if (newHealth > 0f)
            {
                CharacterHealthAccess.SetCharacterHealth(player, newHealth);
                return true;
            }

            ApplyLethalDelayedDoomDamage(player, currentHealth + 999f);
            return false;
        }

        private static void ApplyLethalDelayedDoomDamage(Player player, float amount)
        {
            if (player == null || amount <= 0f)
                return;

            HaldorExpansionPlugin.DebugLog(
                $"[HaldorExpansion] Delayed Doom lethal tick invoke damage: " +
                $"amount={amount:0.##}, " +
                $"{PlayerDiagnostics.GetCestusDebugPlayerTag(player)}");

            HitData delayedHit = new HitData();
            delayedHit.m_damage.m_damage = amount;

            int id = player.GetInstanceID();
            DelayedDoomInternalDamageVictims.Add(id);

            try
            {
                ((Character)player).Damage(delayedHit);
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogError($"[HaldorExpansion] Delayed Doom lethal damage error: {e}");
            }
            finally
            {
                DelayedDoomInternalDamageVictims.Remove(id);
            }
        }
    }
}
