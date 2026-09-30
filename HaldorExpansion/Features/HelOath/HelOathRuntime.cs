using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Features.HelOath
{
    internal static class HelOathRuntime
    {
        internal static HelOathState State = new HelOathState();
        private static Player owner;
        private static ItemDrop.ItemData lastWeapon;
        private static readonly MethodInfo GetButtonDown = AccessTools.Method(AccessTools.TypeByName("ZInput"), "GetButtonDown", new[] { typeof(string) });
        private static readonly MethodInfo TakeInput = AccessTools.Method(typeof(Player), "TakeInput");
        private static readonly MethodInfo FoodValue = AccessTools.Method(typeof(Player), "GetTotalFoodValue");
        private static readonly MethodInfo MaxStamina = AccessTools.Method(typeof(Player), "SetMaxStamina");

        internal static bool IsBow(ItemDrop.ItemData item)
        {
            return item != null && item.m_shared != null &&
                ((item.m_dropPrefab != null && item.m_dropPrefab.name == HelOathItemRegistration.HelOathPrefabName)
                 || item.m_shared.m_name == HelOathItemRegistration.HelOathItemKey);
        }

        internal static bool Equipped(Player player) => player != null && IsBow(player.GetCurrentWeapon());

        internal static int EffectiveTouchStacks(Player player)
        {
            if (player == null || player != Player.m_localPlayer || !Equipped(player)) return 0;
            return State.TouchStacks;
        }

        internal static float TouchStaminaMultiplier(Player player) => 1f - EffectiveTouchStacks(player) * 0.04f;

        internal static void Refresh(Player player)
        {
            if (player == null || player != Player.m_localPlayer) return;
            if (owner != player)
            {
                owner = player;
                State = new HelOathState();
                lastWeapon = null;
            }
            var current = !player.IsDead() && !player.IsTeleporting() && Equipped(player) ? player.GetCurrentWeapon() : null;
            int oldTouch = State.TouchStacks;
            State.Equip(current);
            HelOathEffects.SyncPrepared(player, State.Prepared);
            if (oldTouch != State.TouchStacks)
            {
                HelOathTouchEffects.Sync(player, State.TouchStacks);
                HelOathTouchStatus.Sync(player, State.TouchStacks);
            }
            if (!ReferenceEquals(lastWeapon, current))
            {
                lastWeapon = current;
                // Recompute limits without ticking food, healing or refilling stamina.
                object[] values = { 0f, 0f, 0f };
                FoodValue.Invoke(player, values);
                player.SetMaxHealth((float)values[0], false);
                MaxStamina.Invoke(player, new object[] { (float)values[1], false });
            }
        }

        internal static void Update(Player player)
        {
            if (player != Player.m_localPlayer) return;
            Refresh(player);

            int oldTouch = State.TouchStacks;
            State.UpdateTouch(Time.time, Equipped(player) && HelOathEquipment.HasFullFenrisSet(player), HelOathConfiguration.TouchStackIntervalValue);
            if (oldTouch != State.TouchStacks)
            {
                HelOathTouchEffects.Sync(player, State.TouchStacks);
                HelOathTouchStatus.Sync(player, State.TouchStacks);
            }

            if (!Equipped(player) || State.Charge < 100f || State.Prepared) return;
            if (!(bool)TakeInput.Invoke(player, null) || player.InAttack() || player.InDodge()) return;
            if (!(bool)GetButtonDown.Invoke(null, new object[] { HelOathConfiguration.Button.Name })) return;
            if (State.Activate(Time.time, 0f))
            {
                State.Tick(Time.time);
                HelOathTouchEffects.Sync(player, 0);
                HelOathTouchStatus.Sync(player, 0);
                HelOathEffects.SyncPrepared(player, true);
                player.Message(MessageHud.MessageType.Center, "$he_hel_prepared");
            }
        }

        internal static void Credit(Player player, string epoch, float damage)
        {
            if (player != Player.m_localPlayer || damage <= 0f) return;
            Refresh(player);
            // Any real, attributed damage from a normal Hel's Oath shot can awaken Touch.
            State.RegisterBowDamage(Time.time,
                Equipped(player) && HelOathEquipment.HasFullFenrisSet(player),
                HelOathConfiguration.TouchStackIntervalValue);
            State.AddDamage(damage, HelOathConfiguration.RequiredDamage, Time.time, epoch);
        }

        // Special Embrace damage does not charge Embrace again, but it is still Hel's Oath damage
        // and therefore may awaken a fresh Touch cycle after the previous stacks were consumed.
        internal static void NotifyBowDamage(Player player)
        {
            if (player == null || player != Player.m_localPlayer) return;
            Refresh(player);
            State.RegisterBowDamage(Time.time,
                Equipped(player) && HelOathEquipment.HasFullFenrisSet(player),
                HelOathConfiguration.TouchStackIntervalValue);
        }

        internal static void NotifyCombatHit(Player player)
        {
            if (player == null || player != Player.m_localPlayer) return;
            if (owner != player) Refresh(player);
            int oldTouch = State.TouchStacks;
            bool canRestart = !player.IsDead() && !player.IsTeleporting() && Equipped(player) && HelOathEquipment.HasFullFenrisSet(player);
            State.RegisterCombatHit(Time.time, canRestart, HelOathConfiguration.TouchStackIntervalValue);
            if (oldTouch == HelOathState.MaxTouchStacks && State.TouchStacks < HelOathState.MaxTouchStacks)
                HelOathTouchEffects.BroadcastBreak(player);
            if (oldTouch != State.TouchStacks)
            {
                HelOathTouchEffects.Sync(player, State.TouchStacks);
                HelOathTouchStatus.Sync(player, State.TouchStacks);
            }
        }
    }
}
