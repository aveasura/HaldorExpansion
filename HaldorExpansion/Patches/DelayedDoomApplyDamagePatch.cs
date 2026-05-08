using HarmonyLib;

namespace HaldorExpansion.Patches
{
    [HarmonyPatch(typeof(Character), "ApplyDamage")]
    [HarmonyPriority(Priority.Last)]
    internal static class DelayedDoomApplyDamagePatch
    {
        private sealed class ApplyDamageState
        {
            public Player Player;
            public bool Armed;
            public bool Triggered;
            public float BeforeHealth;
            public float MaxHealth;
            public float IncomingDamage;
            public float ImmediateDamage;
            public float DelayedDamageTotal;
        }

        static void Prefix(Character __instance, HitData __0, ref ApplyDamageState __state)
        {
            __state = null;

            Player player = __instance as Player;
            HitData hit = __0;
            if (player == null || hit == null)
                return;

            if (player != Player.m_localPlayer)
                return;

            if (HaldorExpansionPlugin.IsDelayedDoomInternalDamage(player))
                return;

            if (!HaldorExpansionPlugin.HasDelayedDoomEquipped(player))
                return;

            float beforeHealth = HaldorExpansionPlugin.GetCharacterHealth(player);
            float maxHealth = HaldorExpansionPlugin.GetCharacterMaxHealth(player);
            float incomingDamage = HaldorExpansionPlugin.GetHitTotalDamage(hit);
            float threshold = maxHealth * HaldorExpansionPlugin.DelayedDoomTriggerRatio;

            __state = new ApplyDamageState
            {
                Player = player,
                Armed = true,
                BeforeHealth = beforeHealth,
                MaxHealth = maxHealth,
                IncomingDamage = incomingDamage
            };

            HaldorExpansionPlugin.DebugLog(
                $"[HaldorExpansion] Delayed Doom ApplyDamage prefix: " +
                $"incoming={incomingDamage:0.##}, " +
                $"before={beforeHealth:0.##}, " +
                $"max={maxHealth:0.##}, " +
                $"threshold={threshold:0.##}, " +
                $"{HaldorExpansionPlugin.GetCestusDebugPlayerTag(player)}");

            if (beforeHealth <= 0f || maxHealth <= 0f || incomingDamage <= 0f)
                return;

            if (incomingDamage <= threshold)
                return;

            float immediateDamage = incomingDamage * HaldorExpansionPlugin.DelayedDoomImmediateRatio;
            float delayedDamageTotal = incomingDamage * HaldorExpansionPlugin.DelayedDoomDebtRatio;
            float scale = immediateDamage / incomingDamage;

            hit.m_damage.Modify(scale);

            __state.Triggered = true;
            __state.ImmediateDamage = immediateDamage;
            __state.DelayedDamageTotal = delayedDamageTotal;

            HaldorExpansionPlugin.DebugLog(
                $"[HaldorExpansion] Delayed Doom ApplyDamage modified hit: " +
                $"incoming={incomingDamage:0.##}, " +
                $"immediate={immediateDamage:0.##}, " +
                $"delayed={delayedDamageTotal:0.##}, " +
                $"scale={scale:0.###}, " +
                $"{HaldorExpansionPlugin.GetCestusDebugPlayerTag(player)}");
        }

        static void Postfix(Character __instance, HitData __0, ApplyDamageState __state)
        {
            if (__state == null || !__state.Armed || !__state.Triggered)
                return;

            Player player = __state.Player;
            if (player == null)
                return;

            float afterHealth = HaldorExpansionPlugin.GetCharacterHealth(player);

            HaldorExpansionPlugin.DebugLog(
                $"[HaldorExpansion] Delayed Doom ApplyDamage postfix: " +
                $"incoming={__state.IncomingDamage:0.##}, " +
                $"immediate={__state.ImmediateDamage:0.##}, " +
                $"delayed={__state.DelayedDamageTotal:0.##}, " +
                $"before={__state.BeforeHealth:0.##}, " +
                $"after={afterHealth:0.##}, " +
                $"{HaldorExpansionPlugin.GetCestusDebugPlayerTag(player)}");

            if (player.IsDead() || afterHealth <= 0f)
            {
                HaldorExpansionPlugin.DebugLog(
                    $"[HaldorExpansion] Delayed Doom debt skipped because player died from immediate hit: " +
                    $"incoming={__state.IncomingDamage:0.##}, " +
                    $"immediate={__state.ImmediateDamage:0.##}, " +
                    $"{HaldorExpansionPlugin.GetCestusDebugPlayerTag(player)}");
                return;
            }

            HaldorExpansionPlugin.AddDelayedDoomDebt(player, __state.DelayedDamageTotal);
        }
    }
}
