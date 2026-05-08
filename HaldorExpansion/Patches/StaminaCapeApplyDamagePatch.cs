using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Patches
{
    [HarmonyPatch(typeof(Character), "ApplyDamage")]
    [HarmonyPriority(Priority.First)]
    internal static class StaminaCapeApplyDamagePatch
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

            if (!StaminaCapeFeature.HasEquipped(player))
                return;

            float originalDamage = HaldorExpansionPlugin.GetHitTotalDamage(hit);
            if (originalDamage <= 0f)
                return;

            float redirectedDamageTarget = originalDamage * StaminaCapeFeature.DamageToStaminaRatio;
            float staminaCostTarget = redirectedDamageTarget * StaminaCapeFeature.StaminaCostMultiplier;
            float currentStamina = Mathf.Max(0f, StaminaCapeFeature.GetPlayerStamina(player));

            float paidRatio = staminaCostTarget > 0f
                ? Mathf.Clamp01(currentStamina / staminaCostTarget)
                : 1f;

            float redirectedDamageActual = redirectedDamageTarget * paidRatio;
            float staminaSpent = Mathf.Min(currentStamina, staminaCostTarget);
            float redirectedDamageMissing = Mathf.Max(0f, redirectedDamageTarget - redirectedDamageActual);

            float hpDamageAfterCape;
            if (redirectedDamageMissing > 0.001f)
            {
                hpDamageAfterCape =
                    (originalDamage - redirectedDamageActual) *
                    StaminaCapeFeature.OverflowDamageMultiplier;
            }
            else
            {
                hpDamageAfterCape = originalDamage - redirectedDamageActual;
            }

            hpDamageAfterCape = Mathf.Max(0f, hpDamageAfterCape);

            float scale = originalDamage > 0f
                ? hpDamageAfterCape / originalDamage
                : 1f;

            hit.m_damage.Modify(scale);

            if (staminaSpent > 0.001f)
            {
                StaminaCapeFeature.SpendPlayerStamina(player, staminaSpent);
            }

            HaldorExpansionPlugin.DebugLog(
                $"[HaldorExpansion] Stamina cape ApplyDamage: " +
                $"incoming={originalDamage:0.##}, " +
                $"redirectTarget={redirectedDamageTarget:0.##}, " +
                $"staminaCost={staminaCostTarget:0.##}, " +
                $"staminaBefore={currentStamina:0.##}, " +
                $"staminaSpent={staminaSpent:0.##}, " +
                $"missingRedirect={redirectedDamageMissing:0.##}, " +
                $"hpAfterCape={hpDamageAfterCape:0.##}, " +
                $"overflow={(redirectedDamageMissing > 0.001f ? 1 : 0)}, " +
                $"scale={scale:0.###}, " +
                $"{HaldorExpansionPlugin.GetCestusDebugPlayerTag(player)}");
        }
    }
}
