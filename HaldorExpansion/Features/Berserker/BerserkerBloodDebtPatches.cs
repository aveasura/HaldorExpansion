using HaldorExpansion.Features.DelayedDoom;
using HaldorExpansion.Features.NornThread;
using HarmonyLib;

namespace HaldorExpansion.Features.Berserker
{
    // Observe the actual HP delta after armor/resistances/shields, not the raw hit.
    // Never credit environment, self-drain, other players, or Norn's rescue.
    [HarmonyPatch(typeof(Character), "ApplyDamage")]
    internal static class BerserkerBloodDebtDamagePatch
    {
        private struct Observation
        {
            internal Player Player;
            internal float BeforeHp;
            internal bool Eligible;
        }

        [HarmonyPriority(Priority.First)]
        private static void Prefix(Character __instance, HitData __0, out Observation __state)
        {
            __state = default(Observation);
            Player player = __instance as Player;
            if (player == null || player != Player.m_localPlayer || player.IsDead() ||
                __0 == null || !BerserkerEquipment.Equipped(player)) return;

            // Snapshot even rejected events for limited diagnostics in game.
            __state.Player = player;
            __state.BeforeHp = player.GetHealth();

            if (DelayedDoomRuntime.IsDelayedDoomInternalDamage(player))
            {
                return;
            }

            if (NornThreadRuntime.IsGraceActive(player))
            {
                return;
            }

            Character attacker = __0.GetAttacker();
            if (attacker == null)
            {
                return;
            }

            if (attacker == player || attacker.IsPlayer())
            {
                return;
            }

            if (!BaseAI.IsEnemy(player, attacker))
            {
                return;
            }

            __state.Eligible = __state.BeforeHp > 0f;
        }

        // Harmony Postfix priorities are executed high-to-low. Last ensures Norn's
        // Postfix can finish restoring HP *before* we measure the real loss.
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Observation __state)
        {
            Player player = __state.Player;
            if (player == null) return;

            float after = player.GetHealth();
            float realLoss = __state.BeforeHp - after;
            if (realLoss <= 0.001f) return;

            if (!__state.Eligible || player.IsDead() || after <= 0f ||
                NornThreadRuntime.IsGraceActive(player))
                return;

            BerserkerRuntime.NotifyEnemyDamage(player, realLoss);
        }
    }
}