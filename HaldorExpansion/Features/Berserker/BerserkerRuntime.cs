using UnityEngine;

namespace HaldorExpansion.Features.Berserker
{
    internal static class BerserkerRuntime
    {
        internal static readonly BerserkerState State = new BerserkerState();
        internal static readonly BerserkerDebtState Debt = new BerserkerDebtState();

        private static Player owner;
        private static int lastSyncedStage = -1;
        internal const int ReckoningMinimumWrathStage = 2;

        internal static int EffectiveWrathStage(Player player)
        {
            if (player == null || player != Player.m_localPlayer || player.IsDead()) return 0;
            if (!BerserkerEquipment.Equipped(player)) return 0;
            return Mathf.Clamp(State.WrathStage, 0, BerserkerState.MaxWrathStage);
        }

        internal static float WrathAttackSpeedMultiplier(Player player)
        {
            int stage = EffectiveWrathStage(player);
            float wrathBonus = BerserkerConfiguration.WrathAttackSpeedBonusFraction(stage);
            float exhaustionPenalty = BerserkerRagnarokExhaustion.IsActive(player)
                ? BerserkerConfiguration.RagnarokExhaustionAttackPenaltyFraction
                : 0f;
            // Wrath and Exhaustion combine additively, not as separate multipliers.
            return Mathf.Max(0.1f, 1f + wrathBonus - exhaustionPenalty);
        }

        internal static float WrathStaminaMultiplier(Player player)
        {
            int stage = EffectiveWrathStage(player);
            return 1f - BerserkerConfiguration.WrathStaminaReductionFraction(stage);
        }

        internal static void Update(Player player)
        {
            if (player == null || player != Player.m_localPlayer) return;
            EnsureOwner(player);

            if (player.IsDead())
            {
                Reset(player);
                return;
            }

            // Poll Ragnarök before decay. If the player presses the ability key while
            // Wrath V / Debt requirements are currently valid, the cast commits first
            // and the short cast window cannot immediately decay itself out of validity.
            BerserkerRagnarokRuntime.Update(player);

            if (!BerserkerRagnarokRuntime.IsInputLocked(player))
            {
                int before = State.WrathStage;
                State.Tick(Time.time, BerserkerConfiguration.DecayIntervalValue);
                if (State.WrathStage != before) SyncStatus(player);

                Debt.Tick(
                    Time.time,
                    BerserkerConfiguration.DebtDecayRateValue,
                    BerserkerConfiguration.DebtIncomingGraceValue,
                    BerserkerConfiguration.DebtOutgoingGraceValue);
            }

            // Wrath V / Frenzy no longer self-damages the player. Risk comes from
            // taking real enemy damage to build Blood Debt; future equipment can amplify it.
        }

        internal static void NotifyAxeHit(Player player, float credit)
        {
            if (player == null || player != Player.m_localPlayer || player.IsDead()) return;
            EnsureOwner(player);

            int before = State.WrathStage;
            State.RegisterHit(
                Time.time,
                credit,
                BerserkerConfiguration.HitsPerStageValue,
                BerserkerConfiguration.DecayIntervalValue);

            if (State.WrathStage != before) SyncStatus(player);
        }

        internal static void NotifyAxeDebtHit(Player player, bool secondary, bool grantsDebt)
        {
            if (player == null || player != Player.m_localPlayer || player.IsDead()) return;
            EnsureOwner(player);

            float debtGain = 0f;
            if (grantsDebt)
            {
                debtGain = secondary
                    ? BerserkerConfiguration.SecondaryDebtGainValue
                    : BerserkerConfiguration.PrimaryDebtGainValue;
                debtGain *= BerserkerCloakRuntime.DebtMultiplier(player);
            }

            // Reckoning passes grantsDebt=false: it still refreshes outgoing combat activity
            // so Debt decay does not begin while the player is actively fighting.
            Debt.RegisterAxeHit(Time.time, debtGain);
        }

        internal static void NotifyEnemyDamage(Player player, float actualHpLost)
        {
            if (player == null || player != Player.m_localPlayer || player.IsDead()) return;
            EnsureOwner(player);

            float maxHealth = Mathf.Max(0f, player.GetMaxHealth());
            if (maxHealth <= 0f) return;
            Debt.AddEnemyDamage(
                Time.time,
                actualHpLost,
                maxHealth,
                BerserkerConfiguration.DebtPerFullHealthBarValue * BerserkerCloakRuntime.DebtMultiplier(player));
        }

        internal static bool TryGetReckoningPayload(Player player, out double debt, out float bonus)
        {
            debt = 0d;
            bonus = 0f;
            if (player == null || player != Player.m_localPlayer || player.IsDead()) return false;
            EnsureOwner(player);
            if (State.WrathStage < ReckoningMinimumWrathStage || !BerserkerEquipment.Equipped(player)) return false;

            double currentDebt = Debt.Amount;
            if (currentDebt + 0.0001d < BerserkerConfiguration.ReckoningMinimumDebtValue) return false;

            debt = currentDebt * BerserkerConfiguration.ReckoningSpendFraction;
            if (debt <= 0d) return false;
            bonus = BerserkerReckoningMath.BonusDamage(debt);
            return bonus > 0f;
        }

        // Temporary test harness: preserve owner initialization, stage decay
        // bookkeeping and ZDO/status synchronization just like real axe hits.
        internal static void GrantTestWrathV(Player player)
        {
            if (player == null || player != Player.m_localPlayer || player.IsDead()) return;
            EnsureOwner(player);
            State.RegisterHit(Time.time,
                BerserkerConfiguration.HitsPerStageValue * BerserkerState.MaxWrathStage,
                BerserkerConfiguration.HitsPerStageValue,
                BerserkerConfiguration.DecayIntervalValue);
            SyncStatus(player);
        }

        // x5 is an additive +500 points, not a reset of any existing Debt.
        // Reuse its normal outgoing-activity clock so it does not immediately
        // decay before the player can initiate Ragnarök.
        internal static void GrantTestDebt(Player player, double points)
        {
            if (player == null || player != Player.m_localPlayer || player.IsDead() ||
                double.IsNaN(points) || double.IsInfinity(points) || points <= 0d) return;
            EnsureOwner(player);
            Debt.RegisterAxeHit(Time.time, (float)points);
        }

        // Ragnarök burns all Wrath stacks only when the committed cast actually
        // releases its meteors. Do not reset Debt here; the cast consumes that
        // separately after validating its meteor prefab and success conditions.
        internal static void ConsumeWrathForRagnarok(Player player)
        {
            if (player == null || player != Player.m_localPlayer || player.IsDead()) return;
            EnsureOwner(player);
            State.Reset();
            SyncStatus(player);
        }

        internal static void CommitReckoning(Player player, double spentDebt)
        {
            if (player == null || player != Player.m_localPlayer) return;
            Debt.Spend(spentDebt);
        }

        internal static float ApplyReckoningHeal(Player player, float finalDamage)
        {
            if (player == null || player != Player.m_localPlayer || player.IsDead()) return 0f;
            if (float.IsNaN(finalDamage) || float.IsInfinity(finalDamage) || finalDamage <= 0f) return 0f;

            float maxHealth = Mathf.Max(0f, player.GetMaxHealth());
            float current = Mathf.Max(0f, player.GetHealth());
            float heal = BerserkerReckoningMath.HealAmount(
                finalDamage,
                BerserkerConfiguration.ReckoningLifeStealFraction);
            heal = Mathf.Min(heal, Mathf.Max(0f, maxHealth - current));
            if (heal <= 0f) return 0f;

            player.Heal(heal, true);
            return heal;
        }

        internal static void Reset(Player player)
        {
            BerserkerRagnarokRuntime.Reset(player);
            State.Reset();
            Debt.Reset();
            if (player == Player.m_localPlayer)
            {
                lastSyncedStage = -1;
                BerserkerWrathStatus.Sync(player, 0);
                BerserkerWrathEffects.Sync(player, 0);
                lastSyncedStage = 0;
            }
        }

        private static void EnsureOwner(Player player)
        {
            if (owner == player) return;
            owner = player;
            BerserkerRagnarokRuntime.Reset(player);
            State.Reset();
            Debt.Reset();
            lastSyncedStage = -1;
            SyncStatus(player);
        }

        private static void SyncStatus(Player player)
        {
            int stage = State.WrathStage;
            if (stage == lastSyncedStage) return;
            BerserkerWrathStatus.Sync(player, stage);
            BerserkerWrathEffects.Sync(player, stage);
            lastSyncedStage = stage;
        }
    }
}