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

            if (HaldorExpansionPlugin.IsDelayedDoomInternalDamage(player))
                return;

            if (!HaldorExpansionPlugin.HasWoundedBeastCapeEquipped(player))
                return;

            float before = HaldorExpansionPlugin.GetHitTotalDamage(hit);
            if (before <= 0f)
                return;

            hit.m_damage.Modify(HaldorExpansionPlugin.WoundedBeastCapeIncomingDamageMultiplier);

            float after = HaldorExpansionPlugin.GetHitTotalDamage(hit);

            HaldorExpansionPlugin.DebugLog(
                $"[HaldorExpansion] Wounded Beast ApplyDamage penalty: " +
                $"before={before:0.##}, " +
                $"after={after:0.##}, " +
                $"mult={HaldorExpansionPlugin.WoundedBeastCapeIncomingDamageMultiplier:0.##}, " +
                $"{HaldorExpansionPlugin.GetCestusDebugPlayerTag(player)}");
        }
    }
}
