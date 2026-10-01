using HaldorExpansion.Combat;
using HaldorExpansion.Diagnostics;
using HaldorExpansion.Features.DelayedDoom;
using HaldorExpansion.Features.WoundedBeast;
using HarmonyLib;

namespace HaldorExpansion.Patches
{
    [HarmonyPatch(typeof(Character), "ApplyDamage")]
    [HarmonyPriority(Priority.First)]
    internal static class WoundedBeastCapeApplyDamagePatch
    {
        static void Prefix(Character __instance, HitData __0)
        {
            Player player = __instance as Player;
            HitData hit = __0;
            if (player == null || hit == null)
                return;

            if (player != Player.m_localPlayer)
                return;

            if (DelayedDoomRuntime.IsDelayedDoomInternalDamage(player))
                return;

            if (!WoundedBeastRuntime.HasWoundedBeastCapeEquipped(player))
                return;

            float before = HitDamageMath.GetHitTotalDamage(hit);
            if (before <= 0f)
                return;

            hit.m_damage.Modify(WoundedBeastRuntime.WoundedBeastCapeIncomingDamageMultiplier);

            float after = HitDamageMath.GetHitTotalDamage(hit);

            HaldorExpansionPlugin.DebugLog(
                $"[HaldorExpansion] Wounded Beast ApplyDamage penalty: " +
                $"before={before:0.##}, " +
                $"after={after:0.##}, " +
                $"mult={WoundedBeastRuntime.WoundedBeastCapeIncomingDamageMultiplier:0.##}, " +
                $"{PlayerDiagnostics.GetCestusDebugPlayerTag(player)}");
        }
    }
}
