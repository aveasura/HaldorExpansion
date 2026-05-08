using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HaldorExpansion
{
    public partial class HaldorExpansionPlugin
    {
        private static readonly HashSet<string> DelayedDoomItemKeys = new HashSet<string>
        {
            "$item_chest_delayeddoom"
        };

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

        internal static bool HasDelayedDoomEquipped(Player player)
        {
            if (player == null)
                return false;

            Inventory inventory = player.GetInventory();
            if (inventory == null)
                return false;

            List<ItemDrop.ItemData> items = inventory.GetAllItems();
            if (items == null)
                return false;

            foreach (ItemDrop.ItemData item in items)
            {
                if (item == null || !item.m_equipped || item.m_shared == null)
                    continue;

                string itemKey = item.m_shared.m_name;

                if (!string.IsNullOrEmpty(itemKey) && DelayedDoomItemKeys.Contains(itemKey))
                    return true;
            }

            return false;
        }

        internal static bool IsDelayedDoomInternalDamage(Character character)
        {
            return character != null && DelayedDoomInternalDamageVictims.Contains(character.GetInstanceID());
        }

        internal static void AddDelayedDoomDebt(Player player, float delayedDamageTotal)
        {
            if (Instance == null || player == null || delayedDamageTotal <= 0f)
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

            DebugLog(
                $"[HaldorExpansion] Delayed Doom debt added: " +
                $"added={delayedDamageTotal:0.##}, " +
                $"total={state.RemainingDamage:0.##}, " +
                $"ticksLeft={state.TicksLeft}, " +
                $"{GetCestusDebugPlayerTag(player)}");

            if (state.Routine == null)
            {
                DebugLog(
                    $"[HaldorExpansion] Delayed Doom routine start: " +
                    $"waitPerTick={DelayedDoomDuration / DelayedDoomTicks:0.###}, " +
                    $"{GetCestusDebugPlayerTag(player)}");

                state.Routine = Instance.StartCoroutine(DelayedDoomRoutine(state));
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
                        DebugLog("[HaldorExpansion] Delayed Doom routine stop: player null");
                        yield break;
                    }

                    if (player.IsDead())
                    {
                        DebugLog(
                            $"[HaldorExpansion] Delayed Doom routine stop: player dead, " +
                            $"{GetCestusDebugPlayerTag(player)}");
                        yield break;
                    }

                    if (state.RemainingDamage <= 0f || state.TicksLeft <= 0)
                    {
                        DebugLog(
                            $"[HaldorExpansion] Delayed Doom routine stop: exhausted, " +
                            $"remaining={state.RemainingDamage:0.##}, " +
                            $"ticksLeft={state.TicksLeft}, " +
                            $"{GetCestusDebugPlayerTag(player)}");
                        yield break;
                    }

                    float tickDamage = state.RemainingDamage / state.TicksLeft;
                    tickDamage = Mathf.Max(0f, tickDamage);

                    state.RemainingDamage = Mathf.Max(0f, state.RemainingDamage - tickDamage);
                    state.TicksLeft--;

                    DebugLog(
                        $"[HaldorExpansion] Delayed Doom pooled tick: " +
                        $"tick={tickDamage:0.##}, " +
                        $"remaining={state.RemainingDamage:0.##}, " +
                        $"ticksLeft={state.TicksLeft}, " +
                        $"{GetCestusDebugPlayerTag(player)}");

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
                    DebugLog(
                        $"[HaldorExpansion] Delayed Doom routine finally: " +
                        $"remainingBeforeClear={state.RemainingDamage:0.##}, " +
                        $"ticksLeftBeforeClear={state.TicksLeft}, " +
                        $"{GetCestusDebugPlayerTag(state.Player)}");
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

        internal static float GetHitTotalDamage(HitData hit)
        {
            if (hit == null)
                return 0f;

            HitData.DamageTypes damage = hit.m_damage;

            return Mathf.Max(0f,
                damage.m_damage +
                damage.m_blunt +
                damage.m_slash +
                damage.m_pierce +
                damage.m_fire +
                damage.m_frost +
                damage.m_lightning +
                damage.m_poison +
                damage.m_spirit
            );
        }

        private static bool ApplyDelayedDoomHealthTick(Player player, float amount)
        {
            if (player == null || amount <= 0f)
                return false;

            float currentHealth = GetCharacterHealth(player);
            if (currentHealth <= 0f)
                return false;

            float newHealth = currentHealth - amount;

            DebugLog(
                $"[HaldorExpansion] Delayed Doom tick: " +
                $"current={currentHealth:0.##}, " +
                $"tick={amount:0.##}, " +
                $"new={newHealth:0.##}, " +
                $"{GetCestusDebugPlayerTag(player)}");

            if (newHealth > 0f)
            {
                SetCharacterHealth(player, newHealth);
                return true;
            }

            ApplyLethalDelayedDoomDamage(player, currentHealth + 999f);
            return false;
        }

        private static void ApplyLethalDelayedDoomDamage(Player player, float amount)
        {
            if (player == null || amount <= 0f)
                return;

            DebugLog(
                $"[HaldorExpansion] Delayed Doom lethal tick invoke damage: " +
                $"amount={amount:0.##}, " +
                $"{GetCestusDebugPlayerTag(player)}");

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
                Log?.LogError($"[HaldorExpansion] Delayed Doom lethal damage error: {e}");
            }
            finally
            {
                DelayedDoomInternalDamageVictims.Remove(id);
            }
        }

        internal static float GetPlayerBaseFoodRegen(Player player)
        {
            if (player == null)
                return 0f;

            List<Player.Food> foods = player.GetFoods();
            if (foods == null)
                return 0f;

            float total = 0f;

            foreach (Player.Food food in foods)
            {
                if (food?.m_item?.m_shared == null)
                    continue;

                total += Mathf.Max(0f, food.m_item.m_shared.m_foodRegen);
            }

            return total;
        }
    }
}