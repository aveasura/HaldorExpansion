using HaldorExpansion.Features.Cestus;
using HaldorExpansion.Features.DelayedDoom;
using HarmonyLib;

namespace HaldorExpansion.Features.Awakened
{
    [HarmonyPatch(typeof(Player), "UpdateStats", new[] { typeof(float) })]
    internal static class AwakenedDelayedDoomRegenPatch
    {
        private static void Postfix(Player __instance, float __0)
        {
            // Never rescue from a lethal event: this runs outside the damage pipeline,
            // only on the live player's simulation owner and while real debt remains.
            if (__instance == null || __instance != Player.m_localPlayer || __instance.IsDead() ||
                __instance.IsTeleporting() || __instance.InIntro() ||
                DelayedDoomRuntime.IsDelayedDoomInternalDamage(__instance) ||
                !AwakenedEquipment.Has(__instance, Artifact.DelayedDoomChest)) return;
            float heal = AwakenedAbilityMath.DebtRegeneration(__instance.GetHealth(),
                __instance.GetMaxHealth(), __0, DelayedDoomRuntime.HasActiveDebt(__instance));
            if (heal > 0f) __instance.Heal(heal, false);
        }
    }
}

namespace HaldorExpansion.Features.Awakened
{
    [HarmonyPatch(typeof(Humanoid), "SetupEquipment")]
    internal static class AwakenedShieldEquipmentPatch
    {
        private static void Postfix(Humanoid __instance)
        {
            Player player = __instance as Player;
            if (player == null || player != Player.m_localPlayer) return;
            CestusStateStore.CestusState state;
            if (CestusStateStore.ActiveCestusStates.TryGetValue(
                    CestusStateStore.GetCestusStateKey(player), out state))
                CestusShieldRuntime.RefreshAwakenedShield(player, state);
        }
    }
}