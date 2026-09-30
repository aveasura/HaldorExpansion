using HaldorExpansion.Features.Cestus;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Patches
{
    [HarmonyPatch(typeof(Player), "SetControls")]
    internal static class CestusCastInputLockPatch
    {
        static void Prefix(
            Player __instance,
            ref Vector3 movedir,
            ref bool attack,
            ref bool secondaryAttack,
            ref bool block,
            ref bool blockHold,
            ref bool jump,
            ref bool crouch)
        {
            if (__instance == null || __instance != Player.m_localPlayer)
                return;

            if (!CestusRuntime.IsCestusInputLocked(__instance))
                return;

            movedir = Vector3.zero;
            attack = false;
            secondaryAttack = false;
            block = false;
            blockHold = false;
            jump = false;
            crouch = false;
        }
    }
}