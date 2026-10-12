using HaldorExpansion.Features.Cestus;
using HarmonyLib;

namespace HaldorExpansion.Patches
{
    [HarmonyPatch(typeof(Character), "Awake")]
    internal static class CestusFacebreakerNetworkPatch
    {
        private static void Postfix(Character __instance)
        {
            CestusFacebreakerNetwork.Register(__instance);
            CestusFacebreakerAnimation.Register(__instance);
        }
    }

    // A creature's ZNetView may initialize after Character.Awake (or vice versa).
    // Register when the view is ready, without registering the same RPC twice.
    [HarmonyPatch(typeof(ZNetView), "Awake")]
    internal static class CestusFacebreakerViewReadyPatch
    {
        private static void Postfix(ZNetView __instance)
        {
            if (__instance == null)
                return;
            Character character = __instance.GetComponent<Character>();
            if (character != null)
            {
                CestusFacebreakerNetwork.Register(character, __instance);
                CestusFacebreakerAnimation.Register(character, __instance);
            }
        }
    }

    // Character movement may overwrite velocity assigned from a coroutine.
    // Running after vanilla's physics update keeps Facebreaker authoritative while active.
    [HarmonyPatch(typeof(Character), "CustomFixedUpdate")]
    internal static class CestusFacebreakerPullMovementPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Character __instance)
        {
            CestusFacebreakerNetwork.AfterCharacterFixedUpdate(__instance);
            // Keep the body side-on after vanilla physics rotation too.
            CestusFacebreaker.MaintainCastFacing(__instance as Player);
        }
    }
}