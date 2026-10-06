using HaldorExpansion.Compatibility;
using UnityEngine;

namespace HaldorExpansion.Features.Berserker
{
    internal static class BerserkerRuntime
    {
        internal static readonly BerserkerState State = new BerserkerState();
        internal static readonly BerserkerDebtState Debt = new BerserkerDebtState();

        private static Player owner;
        private static int lastSyncedStage = -1;
        private static float frenzyDrainAccumulator;

        internal static void Update(Player player)
        {
            if (player == null || player != Player.m_localPlayer) return;
            EnsureOwner(player);

            if (player.IsDead())
            {
                Reset(player);
                return;
            }

            int before = State.WrathStage;
            State.Tick(Time.time, BerserkerConfiguration.DecayIntervalValue);
            if (State.WrathStage != before) SyncStatus(player);
            Debt.Tick(Time.time, BerserkerConfiguration.DebtDecayRateValue);

            // Wrath belongs to the player and survives a short weapon swap through normal decay,
            // but Frenzy itself is active only while the prototype greataxe is actually wielded.
            if (State.Frenzy && BerserkerEquipment.Equipped(player) &&
                !player.IsTeleporting() && !player.InCutscene())
            {
                frenzyDrainAccumulator += Mathf.Max(0f, Time.deltaTime);
                if (frenzyDrainAccumulator >= 0.25f)
                {
                    float elapsed = frenzyDrainAccumulator;
                    frenzyDrainAccumulator = 0f;
                    ApplyFrenzyDrain(player, elapsed);
                }
            }
            else
            {
                frenzyDrainAccumulator = 0f;
            }
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

        internal static void NotifyEnemyDamage(Player player, float actualHpLost)
        {
            if (player == null || player != Player.m_localPlayer || player.IsDead()) return;
            EnsureOwner(player);
            Debt.AddEnemyDamage(Time.time, actualHpLost,
                BerserkerConfiguration.EnemyDebtMultiplierValue,
                BerserkerConfiguration.DebtDecayDelayValue);
        }

        internal static void Reset(Player player)
        {
            State.Reset();
            Debt.Reset();
            frenzyDrainAccumulator = 0f;
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
            State.Reset();
            Debt.Reset();
            frenzyDrainAccumulator = 0f;
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

        private static void ApplyFrenzyDrain(Player player, float deltaTime)
        {
            if (deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;

            float maxHealth = Mathf.Max(0f, player.GetMaxHealth());
            float health = Mathf.Max(0f, player.GetHealth());
            if (maxHealth <= 0f || health <= 0f) return;

            float floor = Mathf.Max(1f, maxHealth * BerserkerConfiguration.FrenzyMinimumHealthFraction);
            if (health <= floor) return;

            float drain = maxHealth * BerserkerConfiguration.FrenzyDrainFractionPerSecond * deltaTime;
            if (drain <= 0f || float.IsNaN(drain) || float.IsInfinity(drain)) return;

            float after = Mathf.Max(floor, health - drain);
            CharacterHealthAccess.SetCharacterHealth(player, after);
            float actualLoss = Mathf.Max(0f, health - Mathf.Max(0f, player.GetHealth()));
            if (actualLoss > 0f)
                Debt.AddFrenzyLoss(Time.time, actualLoss,
                    BerserkerConfiguration.FrenzyDebtMultiplierValue,
                    BerserkerConfiguration.DebtDecayDelayValue);
        }
    }
}
