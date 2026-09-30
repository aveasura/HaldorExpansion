using HaldorExpansion.Combat;
using HaldorExpansion.Diagnostics;
using HaldorExpansion.Features.Cestus;
using System;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Patches
{
    [HarmonyPatch(typeof(HitData), "ApplyArmor", new Type[] { typeof(float) })]
    internal static class DelayedDoomApplyArmorPatch
    {
        static void Postfix(HitData __instance)
        {
            DelayedDoomDamagePatch.DamageContext ctx = DelayedDoomDamagePatch.PeekContext();
            if (ctx == null || !ctx.Enabled || ctx.ArmorProcessed)
                return;

            ctx.ArmorProcessed = true;

            HaldorExpansionPlugin.DebugLog(
                $"[HaldorExpansion] ApplyArmor enter: " +
                $"{PlayerDiagnostics.GetCestusDebugPlayerTag(ctx.Player)}, " +
                $"Cestus={ctx.HasCestus}");

            float postArmorDamage = HitDamageMath.GetHitTotalDamage(__instance);
            if (postArmorDamage <= 0f)
                return;

            HaldorExpansionPlugin.DebugLog(
                $"[HaldorExpansion] ApplyArmor post-processing: " +
                $"hasCestus={ctx.HasCestus}, " +
                $"postArmor={postArmorDamage:0.##}, " +
                $"{PlayerDiagnostics.GetCestusDebugPlayerTag(ctx.Player)}");

            float damageBeforeShield = HitDamageMath.GetHitTotalDamage(__instance);
            if (ctx.HasCestus && ctx.Player != null && damageBeforeShield > 0f)
            {
                float absorbedByShield = CestusShieldRuntime.AbsorbCestusShield(ctx.Player, damageBeforeShield);
                ctx.CestusShieldAbsorbedInApplyArmor = absorbedByShield;

                if (absorbedByShield > 0f)
                {
                    float remainingAfterShield = Mathf.Max(0f, damageBeforeShield - absorbedByShield);
                    float shieldScale = damageBeforeShield > 0f
                        ? remainingAfterShield / damageBeforeShield
                        : 0f;

                    __instance.m_damage.Modify(shieldScale);

                    HaldorExpansionPlugin.DebugLog(
                        $"[HaldorExpansion] Cestus shield pre-armor absorb applied: " +
                        $"incoming={damageBeforeShield:0.##}, " +
                        $"absorbed={absorbedByShield:0.##}, " +
                        $"remaining={remainingAfterShield:0.##}, " +
                        $"scale={shieldScale:0.###}, " +
                        $"{PlayerDiagnostics.GetCestusDebugPlayerTag(ctx.Player)}");
                }
            }
        }
    }
}