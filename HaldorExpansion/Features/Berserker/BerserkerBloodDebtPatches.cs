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
            internal string SkipReason;
        }

        // Only a finite number of messages in this prototype: useful to diagnose
        // a missing source/HP delta without flooding BepInEx logs each frame.
        private static int diagnosticsLeft = 12;
        private static void Trace(string message)
        {
            if (diagnosticsLeft <= 0) return;
            diagnosticsLeft--;
            HaldorExpansionPlugin.Log?.LogInfo("[Berserker Debt] " + message);
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
                __state.SkipReason = "Delayed Doom internal damage";
                return;
            }
            if (NornThreadRuntime.IsGraceActive(player))
            {
                __state.SkipReason = "Norn Thread rescue grace";
                return;
            }

            Character attacker = __0.GetAttacker();
            if (attacker == null)
            {
                __state.SkipReason = "attacker unresolved/environment";
                return;
            }
            if (attacker == player || attacker.IsPlayer())
            {
                __state.SkipReason = "self/player hit";
                return;
            }
            if (!BaseAI.IsEnemy(player, attacker))
            {
                __state.SkipReason = "non-hostile creature";
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
            {
                Trace("skipped: " + (__state.SkipReason ?? "dead/Norn protection") +
                      ", actualHP=" + realLoss.ToString("0.##"));
                return;
            }

            BerserkerRuntime.NotifyEnemyDamage(player, realLoss);
            Trace("credited HP=" + realLoss.ToString("0.##") +
                  " => Debt=" + BerserkerRuntime.Debt.Amount.ToString("0.##"));
        }

    }
}
