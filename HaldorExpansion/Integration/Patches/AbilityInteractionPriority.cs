using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Patches
{
    // Vanilla interactions win when the same physical press would trigger an ability.
    // Capture before Player.Update, since vanilla can consume Use before a postfix runs.
    [HarmonyPatch(typeof(Player), "Update")]
    internal static class AbilityInteractionPriority
    {
        private static readonly FieldInfo HoveringCreature =
            AccessTools.Field(typeof(Player), "m_hoveringCreature");

        private static int sampledFrame = -1;
        private static Player sampledPlayer;
        private static bool interactionPressed;

        [HarmonyPriority(Priority.First)]
        private static void Prefix(Player __instance)
        {
            if (__instance == null || __instance != Player.m_localPlayer) return;

            sampledFrame = Time.frameCount;
            sampledPlayer = __instance;
            interactionPressed = ZInput.instance != null &&
                                 ZInput.GetButtonDown("Use") &&
                                 HasInteractableUnderCrosshair(__instance);
        }

        internal static bool ShouldSuppress(Player player)
        {
            // During dodge or attack vanilla cannot use its hovered target; do not
            // block Soul Swap just because the crosshair happens to face a chest.
            return player != null && player == sampledPlayer && sampledFrame == Time.frameCount &&
                   interactionPressed && !player.InAttack() && !player.InDodge();
        }

        private static bool HasInteractableUnderCrosshair(Player player)
        {
            // Reuse Valheim's own hover target; no raycasts or nearby-object scans.
            if (ContainsInteractable(player.GetHoverObject())) return true;

            // Creatures (e.g. tame animals) use a separate hover field in Valheim.
            Component creature = HoveringCreature != null
                ? HoveringCreature.GetValue(player) as Component
                : null;
            return creature != null && ContainsInteractable(creature.gameObject);
        }

        private static bool ContainsInteractable(GameObject target)
        {
            if (target == null) return false;

            // The hovered collider can be a child of an interactable prefab.
            // Interface components cannot be requested with the generic Component API.
            MonoBehaviour[] components = target.GetComponentsInParent<MonoBehaviour>(true);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null && components[i] is Interactable)
                    return true;
            }

            return false;
        }
    }
}