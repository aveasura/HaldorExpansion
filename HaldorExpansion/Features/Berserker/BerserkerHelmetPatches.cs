using HaldorExpansion.Combat;
using HaldorExpansion.Features.DelayedDoom;
using HarmonyLib;

namespace HaldorExpansion.Features.Berserker
{
    // Apply the Wrath armor bonus to the player's final body-armor value. Player.GetBodyArmor is
    // already a proven patch point in this project (Cestus uses it), and the character armor UI
    // reads the same total. This avoids depending on ItemData.GetArmor overloads that changed in 1.0.17.
    [HarmonyPatch(typeof(Player), "GetBodyArmor")]
    internal static class BerserkerHelmetDynamicArmorPatch
    {
        private static void Postfix(Player __instance, ref float __result)
        {
            if (__instance == null || __instance.IsDead()) return;
            if (!BerserkerHelmetRuntime.HasEquipped(__instance)) return;

            __result += BerserkerHelmetRuntime.ArmorBonus(__instance);
        }
    }

    // Wrath grants a non-linear reduction up to 5% at stage V while the helmet is equipped.
    // This intentionally composes multiplicatively with the Berserker Cloak's +30% damage taken.
    [HarmonyPatch(typeof(Character), "ApplyDamage")]
    [HarmonyPriority(Priority.First)]
    internal static class BerserkerHelmetDamagePatch
    {
        private static void Prefix(Character __instance, HitData __0)
        {
            Player player = __instance as Player;
            HitData hit = __0;
            if (player == null || hit == null || player != Player.m_localPlayer) return;
            if (DelayedDoomRuntime.IsDelayedDoomInternalDamage(player)) return;

            int stage = BerserkerHelmetRuntime.WrathStage(player);
            if (stage <= 0) return;

            float before = HitDamageMath.GetHitTotalDamage(hit);
            if (before <= 0f) return;

            float multiplier = BerserkerHelmetRuntime.IncomingDamageMultiplier(player);
            hit.m_damage.Modify(multiplier);

            if (HaldorExpansionPlugin.DebugLogging)
            {
                HaldorExpansionPlugin.DebugLog(
                    $"[Berserker] Helmet damage reduction Wrath {stage}: {before:0.##} -> " +
                    $"{HitDamageMath.GetHitTotalDamage(hit):0.##} (x{multiplier:0.###})");
            }
        }
    }

    // Valheim funnels stagger buildup through Character.AddStaggerDamage. Reducing the incoming
    // stagger amount here preserves the native stagger threshold/recovery logic while giving
    // non-linear resistance up to 75% at Wrath V.
    [HarmonyPatch(typeof(Character), "AddStaggerDamage")]
    internal static class BerserkerHelmetStaggerPatch
    {
        private static void Prefix(Character __instance, ref float __0)
        {
            Player player = __instance as Player;
            if (player == null || player != Player.m_localPlayer || __0 <= 0f) return;

            int stage = BerserkerHelmetRuntime.WrathStage(player);
            if (stage <= 0) return;

            float multiplier = BerserkerHelmetRuntime.IncomingStaggerMultiplier(player);
            __0 *= multiplier;
        }
    }
}