using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace HaldorExpansion.Features.Awakened
{
    internal static class AwakenedStatRefresh
    {
        private sealed class AppliedState
        {
            internal int Signature;
        }

        private static readonly ConditionalWeakTable<Player, AppliedState> Applied =
            new ConditionalWeakTable<Player, AppliedState>();

        private static readonly MethodInfo FoodValue = AccessTools.Method(typeof(Player), "GetTotalFoodValue");

        internal static void Refresh(Player player)
        {
            if (player == null || player != Player.m_localPlayer) return;
            int signature = AwakenedEquipment.StatRefreshSignature(player);
            AppliedState state = Applied.GetOrCreateValue(player);
            if (state.Signature == signature) return;

            // Re-read all native food/stat contributions (including Hel Oath's conversion).
            // Never tick food, heal, refill stamina, or update unrelated players' maxima.
            object[] values = { 0f, 0f, 0f };
            FoodValue.Invoke(player, values);
            player.SetMaxHealth((float)values[0], false);
            player.SetMaxStamina((float)values[1], false);
            state.Signature = signature;
        }
    }

    [HarmonyPatch(typeof(Humanoid), "SetupEquipment")]
    internal static class AwakenedEquipmentStatsRefreshPatch
    {
        // Vanilla invokes this after changing both slot references and m_equipped.
        private static void Postfix(Humanoid __instance) => AwakenedStatRefresh.Refresh(__instance as Player);
    }

    [HarmonyPatch(typeof(Player), "Update")]
    internal static class AwakenedQualityStatsRefreshPatch
    {
        // Upgrades or threshold config changes can alter awakening without changing the equipped slot.
        private static void Prefix(Player __instance) => AwakenedStatRefresh.Refresh(__instance);
    }
}