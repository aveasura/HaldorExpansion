using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Patches
{
    [HarmonyPatch]
    internal static class CestusVanillaAdrenalineBlockPatch
    {
        private static readonly FieldInfo AdrenalineField =
            AccessTools.Field(typeof(Player), "m_adrenaline");

        private static readonly FieldInfo AdrenalineDegenTimerField =
            AccessTools.Field(typeof(Player), "m_adrenalineDegenTimer");

        private static readonly FieldInfo AdrenalineGuardianPowerField =
            AccessTools.Field(typeof(Player), "m_adrenalineGuardianPower");

        private static readonly FieldInfo HudAdrenalineHideTimerField =
            AccessTools.Field(typeof(Hud), "m_adrenalineHideTimer");

        private static readonly FieldInfo HudAdrenalineBarRootField =
            AccessTools.Field(typeof(Hud), "m_adrenalineBarRoot");

        private static bool WasMirrorActiveLastFrame;

        [HarmonyPatch(typeof(Player), "AddAdrenaline")]
        [HarmonyPrefix]
        private static bool AddAdrenalinePrefix(Player __instance)
        {
            if (__instance == null || __instance != Player.m_localPlayer)
                return true;

            bool blockVanillaAdrenaline = HaldorExpansionPlugin.ShouldMirrorCestusToAdrenaline(__instance);
            if (blockVanillaAdrenaline)
            {
                HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Blocked vanilla adrenaline gain for local Cestus player");
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(Player), "Update")]
        [HarmonyPostfix]
        private static void UpdatePostfix(Player __instance)
        {
            if (__instance == null || __instance != Player.m_localPlayer)
                return;

            bool mirrorActive = HaldorExpansionPlugin.ShouldMirrorCestusToAdrenaline(__instance);
            if (!mirrorActive && !WasMirrorActiveLastFrame)
                return;

            bool hideBarImmediately = !mirrorActive && WasMirrorActiveLastFrame;
            ClearVanillaAdrenaline(__instance, hideBarImmediately);

            WasMirrorActiveLastFrame = mirrorActive;
        }

        [HarmonyPatch(typeof(Hud), "UpdateAdrenaline")]
        [HarmonyPostfix]
        private static void HudUpdateAdrenalinePostfix(Hud __instance)
        {
            Player localPlayer = Player.m_localPlayer;
            if (__instance == null || localPlayer == null)
                return;

            if (!HaldorExpansionPlugin.ShouldForceHideCestusHudBar(localPlayer))
                return;

            HudAdrenalineHideTimerField?.SetValue(__instance, 0f);

            RectTransform adrenalineBarRoot = HudAdrenalineBarRootField?.GetValue(__instance) as RectTransform;
            if (adrenalineBarRoot != null)
            {
                adrenalineBarRoot.gameObject.SetActive(false);
            }
        }

        private static void ClearVanillaAdrenaline(Player player, bool hideBarImmediately)
        {
            if (player == null)
                return;

            AdrenalineField?.SetValue(player, 0f);
            AdrenalineDegenTimerField?.SetValue(player, 0f);
            AdrenalineGuardianPowerField?.SetValue(player, 0f);

            Hud hud = Hud.instance;
            if (hud == null)
                return;

            HudAdrenalineHideTimerField?.SetValue(hud, 0f);

            if (!hideBarImmediately)
                return;

            RectTransform adrenalineBarRoot = HudAdrenalineBarRootField?.GetValue(hud) as RectTransform;
            if (adrenalineBarRoot != null)
            {
                adrenalineBarRoot.gameObject.SetActive(false);
            }
        }
    }
}
