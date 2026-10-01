using HarmonyLib;

namespace HaldorExpansion
{
    [HarmonyPatch(typeof(Character), "Damage")]
    internal static class WoundedBeastCapeDamagePatch
    {
        static void Prefix(Character __instance, HitData hit)
        {
            Player player = __instance as Player;
            if (player == null || hit == null)
                return;

            HaldorExpansionPlugin.ApplyWoundedBeastCapeIncomingDamagePenalty(player, hit);
        }
    }
}