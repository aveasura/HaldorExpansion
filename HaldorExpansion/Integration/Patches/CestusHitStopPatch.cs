using HaldorExpansion.Features.Cestus;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Patches
{
    // Only remove the vanilla melee impact pause for attacks made with Bone Crushers.
    // Do not suppress FreezeFrame itself: it is also used by unrelated game effects.
    [HarmonyPatch(typeof(Character), "FreezeFrame")]
    internal static class CestusHitStopPatch
    {
        private const float VanillaMeleeHitStopSeconds = 0.15f;
        private const float DurationEpsilon = 0.001f;

        [HarmonyPrefix]
        private static void Prefix(Character __instance, ref float duration)
        {
            // Ignore every other freeze duration (for example, a separate gameplay effect).
            if (Mathf.Abs(duration - VanillaMeleeHitStopSeconds) > DurationEpsilon)
                return;

            Player player = __instance as Player;
            if (player == null || !player.InAttack())
                return;

            if (!CestusEquipment.IsCestusWeapon(player.GetCurrentWeapon()))
                return;

            duration = CestusItemRegistration.GritCestusHitStopSeconds;
        }
    }
}
