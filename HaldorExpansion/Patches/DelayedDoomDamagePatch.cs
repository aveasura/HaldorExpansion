﻿using System.Collections.Generic;
using HarmonyLib;

namespace HaldorExpansion.Patches
{
    [HarmonyPatch(typeof(Character), "Damage")]
    internal static class DelayedDoomDamagePatch
    {
        internal sealed class DamageContext
        {
            public Player Player;
            public HitData Hit;
            public bool Enabled;
            public bool ArmorProcessed;

            public bool HasCestus;

            public float BeforeHealth;
            public float MaxHealth;

            public float CestusShieldAbsorbedInApplyArmor;
            public float CestusShieldCorrectedInPostfix;
        }

        private static readonly Stack<DamageContext> ContextStack = new Stack<DamageContext>();

        internal static DamageContext PeekContext()
        {
            return ContextStack.Count > 0 ? ContextStack.Peek() : null;
        }

        static void Prefix(Character __instance, HitData hit)
        {
            Player player = __instance as Player;
            if (player == null || hit == null)
                return;

            DamageContext ctx = new DamageContext
            {
                Player = player,
                Hit = hit
            };

            bool cestusEquipped = HaldorExpansionPlugin.HasCestusEquipped(player);
            bool syncedShieldActive = HaldorExpansionPlugin.GetCestusEffectiveShieldRemaining(player) > 0.001f;
            bool localCestusRuntime =
                HaldorExpansionPlugin.ShouldProcessCestusLocally(player) &&
                (cestusEquipped || syncedShieldActive);

            HaldorExpansionPlugin.DebugLog(
                $"[HaldorExpansion] Damage prefix: " +
                $"{HaldorExpansionPlugin.GetCestusDebugPlayerTag(player)}, " +
                $"CestusDetected={cestusEquipped}, " +
                $"SyncedShieldActive={syncedShieldActive}, " +
                $"CestusLocalRuntime={localCestusRuntime}");

            bool internalDelayedDoomDamage = HaldorExpansionPlugin.IsDelayedDoomInternalDamage(__instance);
            if (internalDelayedDoomDamage)
            {
                HaldorExpansionPlugin.DebugLog(
                    $"[HaldorExpansion] Damage prefix skip internal Delayed Doom damage: " +
                    $"{HaldorExpansionPlugin.GetCestusDebugPlayerTag(player)}");
            }

            if (!internalDelayedDoomDamage && localCestusRuntime)
            {
                float beforeHealth = HaldorExpansionPlugin.GetCharacterHealth(player);
                float maxHealth = HaldorExpansionPlugin.GetCharacterMaxHealth(player);
                HaldorExpansionPlugin.DebugLog(
                    $"[HaldorExpansion] Damage snapshot: " +
                    $"before={beforeHealth:0.##}, " +
                    $"max={maxHealth:0.##}, " +
                    $"shieldActive={(syncedShieldActive ? 1 : 0)}");

                if (beforeHealth > 0f && maxHealth > 0f)
                {
                    ctx.Enabled = true;
                    ctx.BeforeHealth = beforeHealth;
                    ctx.MaxHealth = maxHealth;
                    ctx.HasCestus = localCestusRuntime;
                }
            }

            ContextStack.Push(ctx);
        }

        static void Postfix(Character __instance, HitData hit)
        {
            Player player = __instance as Player;
            if (player == null || hit == null || ContextStack.Count == 0)
                return;

            DamageContext ctx = ContextStack.Pop();
            if (!ctx.Enabled)
                return;

            float afterHealth = HaldorExpansionPlugin.GetCharacterHealth(player);

            if (ctx.HasCestus && ctx.CestusShieldAbsorbedInApplyArmor <= 0.001f)
            {
                ctx.CestusShieldCorrectedInPostfix =
                    HaldorExpansionPlugin.CorrectCestusShieldAfterDamage(player, ctx.BeforeHealth, afterHealth);

                if (ctx.CestusShieldCorrectedInPostfix > 0.001f)
                {
                    afterHealth = HaldorExpansionPlugin.GetCharacterHealth(player);
                }
            }

            if (ctx.HasCestus)
            {
                HaldorExpansionPlugin.DebugLog(
                    $"[HaldorExpansion] Damage postfix after cestus handling: " +
                    $"before={ctx.BeforeHealth:0.##}, " +
                    $"after={afterHealth:0.##}, " +
                    $"cestusPreAbsorb={ctx.CestusShieldAbsorbedInApplyArmor:0.##}, " +
                    $"cestusPostfixCorrect={ctx.CestusShieldCorrectedInPostfix:0.##}, " +
                    $"{HaldorExpansionPlugin.GetCestusDebugPlayerTag(player)}");
            }
        }
    }
}