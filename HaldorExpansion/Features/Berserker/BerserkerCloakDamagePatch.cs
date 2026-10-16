using HaldorExpansion.Combat;
using HaldorExpansion.Features.DelayedDoom;
using HarmonyLib;

namespace HaldorExpansion.Features.Berserker
{
    // Same risk model as Cloak of the Wounded Beast: amplify the incoming HitData after
    // Valheim resolves armor/resistances. Blood Debt then observes the real post-defense HP loss.
    [HarmonyPatch(typeof(Character), "ApplyDamage")]
    internal static class BerserkerCloakDamagePatch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(Character __instance, HitData __0)
        {
            Player player = __instance as Player;
            HitData hit = __0;
            if (player == null || hit == null || player != Player.m_localPlayer) return;
            if (DelayedDoomRuntime.IsDelayedDoomInternalDamage(player)) return;
            if (!BerserkerCloakRuntime.HasEquipped(player)) return;

            float before = HitDamageMath.GetHitTotalDamage(hit);
            if (before <= 0f) return;

            hit.m_damage.Modify(BerserkerCloakRuntime.IncomingDamageMultiplier);

            if (HaldorExpansionPlugin.DebugLogging)
            {
                HaldorExpansionPlugin.DebugLog(
                    $"[Berserker] Cloak incoming damage: {before:0.##} -> " +
                    $"{HitDamageMath.GetHitTotalDamage(hit):0.##} " +
                    $"(x{BerserkerCloakRuntime.IncomingDamageMultiplier:0.##})");
            }
        }
    }
}