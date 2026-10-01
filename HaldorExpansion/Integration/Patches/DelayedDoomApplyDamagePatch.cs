using HaldorExpansion.Combat;
using HaldorExpansion.Compatibility;
using HaldorExpansion.Diagnostics;
using HaldorExpansion.Features.DelayedDoom;
using HaldorExpansion.Features.NornThread;
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
            public bool DelayedDoomArmed;
            public bool DelayedDoomTriggered;
            public bool GracePrevented;
            public bool NornThreadTriggered;
            public float BeforeHealth;
            public float MaxHealth;
            public float IncomingDamage;
            public float ImmediateDamage;
            public float DelayedDamageTotal;
            public float FinalIncomingDamage;
        }

        static void Prefix(Character __instance, HitData __0, ref ApplyDamageState __state)
        {
            __state = null;

            Player player = __instance as Player;
            HitData hit = __0;
            if (player == null || hit == null || player != Player.m_localPlayer)
                return;

            float beforeHealth = CharacterHealthAccess.GetCharacterHealth(player);
            float maxHealth = CharacterHealthAccess.GetCharacterMaxHealth(player);
            float incomingDamage = HitDamageMath.GetHitTotalDamage(hit);

            __state = new ApplyDamageState
            {
                Player = player,
                BeforeHealth = beforeHealth,
                MaxHealth = maxHealth,
                IncomingDamage = incomingDamage
            };

            if (beforeHealth <= 0f || incomingDamage <= 0f)
                return;

            // Short post-rescue grace prevents chained DoT / multi-hit damage from killing the
            // player in the same instant. Status effects themselves are not removed.
            if (NornThreadRuntime.IsGraceActive(player))
            {
                hit.m_damage.Modify(0f);
                __state.GracePrevented = true;
                HaldorExpansionPlugin.DebugLog(
                    $"[HaldorExpansion] Norn Thread grace prevented damage: incoming={incomingDamage:0.##}, before={beforeHealth:0.##}");
                return;
            }

            bool internalDelayedDoomDamage = DelayedDoomRuntime.IsDelayedDoomInternalDamage(player);

            // Preserve Delayed Doom's existing post-armor split. Internal delayed ticks must not
            // be split again, but they are still allowed to trigger Norn Thread below.
            if (!internalDelayedDoomDamage && DelayedDoomEquipment.HasDelayedDoomEquipped(player))
            {
                __state.DelayedDoomArmed = true;
                float threshold = maxHealth * DelayedDoomRuntime.DelayedDoomTriggerRatio;

                HaldorExpansionPlugin.DebugLog(
                    $"[HaldorExpansion] Delayed Doom ApplyDamage prefix: " +
                    $"incoming={incomingDamage:0.##}, before={beforeHealth:0.##}, max={maxHealth:0.##}, threshold={threshold:0.##}, " +
                    $"{PlayerDiagnostics.GetCestusDebugPlayerTag(player)}");

                if (maxHealth > 0f && incomingDamage > threshold)
                {
                    float immediateDamage = incomingDamage * DelayedDoomRuntime.DelayedDoomImmediateRatio;
                    float delayedDamageTotal = incomingDamage * DelayedDoomRuntime.DelayedDoomDebtRatio;
                    float scale = immediateDamage / incomingDamage;

                    hit.m_damage.Modify(scale);
                    __state.DelayedDoomTriggered = true;
                    __state.ImmediateDamage = immediateDamage;
                    __state.DelayedDamageTotal = delayedDamageTotal;

                    HaldorExpansionPlugin.DebugLog(
                        $"[HaldorExpansion] Delayed Doom ApplyDamage modified hit: " +
                        $"incoming={incomingDamage:0.##}, immediate={immediateDamage:0.##}, delayed={delayedDamageTotal:0.##}, scale={scale:0.###}, " +
                        $"{PlayerDiagnostics.GetCestusDebugPlayerTag(player)}");
                }
            }

            // This is the final HP damage after armor/resists and all earlier ApplyDamage
            // modifiers (cape penalties/redirects and Delayed Doom). Only now decide lethality.
            float finalIncomingDamage = HitDamageMath.GetHitTotalDamage(hit);
            __state.FinalIncomingDamage = finalIncomingDamage;
            if (finalIncomingDamage <= 0f || finalIncomingDamage < beforeHealth)
                return;

            if (!NornThreadRuntime.TryConsume(player, out ItemDrop.ItemData consumed))
                return;

            // The lethal hit is fully paid for by the destroyed amulet. The health cost is
            // applied in Postfix from the health snapshot taken before the lethal hit.
            hit.m_damage.Modify(0f);
            __state.NornThreadTriggered = true;

            HaldorExpansionPlugin.DebugLog(
                $"[HaldorExpansion] Norn Thread triggered: finalIncoming={finalIncomingDamage:0.##}, before={beforeHealth:0.##}, " +
                $"item={(consumed?.m_shared?.m_name ?? "<unknown>")}");
        }

        static void Postfix(Character __instance, HitData __0, ApplyDamageState __state)
        {
            if (__state == null || __state.Player == null)
                return;

            Player player = __state.Player;

            if (__state.NornThreadTriggered)
            {
                // Do not schedule Delayed Doom debt from the hit that the amulet consumed.
                NornThreadRuntime.CompleteRescue(player, __state.BeforeHealth);
                return;
            }

            if (__state.GracePrevented)
                return;

            if (!__state.DelayedDoomArmed || !__state.DelayedDoomTriggered)
                return;

            float afterHealth = CharacterHealthAccess.GetCharacterHealth(player);

            HaldorExpansionPlugin.DebugLog(
                $"[HaldorExpansion] Delayed Doom ApplyDamage postfix: " +
                $"incoming={__state.IncomingDamage:0.##}, immediate={__state.ImmediateDamage:0.##}, delayed={__state.DelayedDamageTotal:0.##}, " +
                $"before={__state.BeforeHealth:0.##}, after={afterHealth:0.##}, " +
                $"{PlayerDiagnostics.GetCestusDebugPlayerTag(player)}");

            if (player.IsDead() || afterHealth <= 0f)
            {
                HaldorExpansionPlugin.DebugLog(
                    $"[HaldorExpansion] Delayed Doom debt skipped because player died from immediate hit: " +
                    $"incoming={__state.IncomingDamage:0.##}, immediate={__state.ImmediateDamage:0.##}, " +
                    $"{PlayerDiagnostics.GetCestusDebugPlayerTag(player)}");
                return;
            }

            DelayedDoomRuntime.AddDelayedDoomDebt(player, __state.DelayedDamageTotal);
        }
    }
}
