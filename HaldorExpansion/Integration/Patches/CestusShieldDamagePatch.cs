using HaldorExpansion.Combat;
using HaldorExpansion.Features.Cestus;
using HaldorExpansion.Features.DelayedDoom;
using HaldorExpansion.Features.NornThread;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Patches
{
    // RPC hits and poison/burning ticks converge here. Damage/ApplyArmor alone
    // miss status ticks and remote RPCs; restoring HP afterwards cannot stop death.
    [HarmonyPatch(typeof(Character), "ApplyDamage")]
    internal static class CestusShieldDamagePatch
    {
        // Keep the shield ahead of cape redirects/penalties, debt splitting and
        // lethal rescue. Each actual damage packet spends shield only once.
        [HarmonyPriority(Priority.First + 1)]
        private static void Prefix(Character __instance, HitData __0)
        {
            Player player = __instance as Player;
            if (player == null || player != Player.m_localPlayer || __0 == null ||
                player.IsDead() || player.GetHealth() <= 0f || player.IsTeleporting() ||
                player.IsDebugFlying() || player.InCutscene()) return;

            // Internal debt was already incurred; the shield must not repay it.
            if (DelayedDoomRuntime.IsDelayedDoomInternalDamage(player) ||
                NornThreadRuntime.IsGraceActive(player)) return;

            float incoming = HitDamageMath.GetHitTotalDamage(__0);
            if (incoming <= 0f) return;
            float absorbed = CestusShieldRuntime.AbsorbCestusShield(player, incoming);
            if (absorbed > 0f)
                __0.m_damage.Modify(Mathf.Clamp01((incoming - absorbed) / incoming));
        }
    }
}