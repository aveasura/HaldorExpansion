using UnityEngine;

namespace HaldorExpansion.Features.Berserker
{
    internal static class BerserkerRuntime
    {
        internal static readonly BerserkerState State = new BerserkerState();
        internal static readonly BerserkerDebtState Debt = new BerserkerDebtState();

        private static Player owner;
        private static int lastSyncedStage = -1;

        internal static int EffectiveWrathStage(Player player)
        {
            if (player == null || player != Player.m_localPlayer || player.IsDead()) return 0;
            if (!BerserkerEquipment.Equipped(player)) return 0;
            return Mathf.Clamp(State.WrathStage, 0, BerserkerState.MaxWrathStage);
        }

        internal static float WrathAttackSpeedMultiplier(Player player)
        {
            int stage = EffectiveWrathStage(player);
            return Mathf.Clamp(1f + stage * BerserkerConfiguration.WrathAttackSpeedFractionPerStage, 0.1f, 3f);
        }

        internal static float WrathStaminaMultiplier(Player player)
        {
            int stage = EffectiveWrathStage(player);
            return Mathf.Clamp(1f - stage * BerserkerConfiguration.WrathStaminaReductionFractionPerStage, 0.1f, 1f);
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
                debtGain = secondary
                    ? BerserkerConfiguration.SecondaryDebtGainValue
                    : BerserkerConfiguration.PrimaryDebtGainValue;

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
                BerserkerConfiguration.DebtPerFullHealthBarValue);
        }

        internal static bool TryGetReckoningPayload(Player player, out double debt, out float bonus)
        {
            debt = 0d;
            bonus = 0f;
            if (player == null || player != Player.m_localPlayer || player.IsDead()) return false;
            EnsureOwner(player);
            if (!State.Frenzy || !BerserkerEquipment.Equipped(player)) return false;

            double currentDebt = Debt.Amount;
            if (currentDebt + 0.0001d < BerserkerConfiguration.ReckoningMinimumDebtValue) return false;

            debt = currentDebt * BerserkerConfiguration.ReckoningSpendFraction;
            if (debt <= 0d) return false;
            bonus = BerserkerReckoningMath.BonusDamage(debt);
            return bonus > 0f;
        }

        internal static void CommitReckoning(Player player, double spentDebt)
        {
            if (player == null || player != Player.m_localPlayer) return;
            Debt.Spend(spentDebt);
        }

        internal static void ApplyReckoningHeal(Player player, float finalDamage)
        {
            if (player == null || player != Player.m_localPlayer || player.IsDead()) return;
            if (float.IsNaN(finalDamage) || float.IsInfinity(finalDamage) || finalDamage <= 0f) return;

            float maxHealth = Mathf.Max(0f, player.GetMaxHealth());
            float current = Mathf.Max(0f, player.GetHealth());
            float heal = BerserkerReckoningMath.HealAmount(
                finalDamage,
                BerserkerConfiguration.ReckoningLifeStealFraction);
            heal = Mathf.Min(heal, Mathf.Max(0f, maxHealth - current));
            if (heal <= 0f) return;

            player.Heal(heal, true);
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
            lastSyncedStage = stage;
        }
    }
}