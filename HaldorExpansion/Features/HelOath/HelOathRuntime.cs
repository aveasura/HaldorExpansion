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
        private static int lastSyncedTouch = -1;
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

        private static bool TouchEligible(Player player)
            => player != null && player == Player.m_localPlayer && !player.IsDead() && !player.IsTeleporting()
               && Equipped(player) && HelOathEquipment.HasFullFenrisSet(player);

        internal static int EffectiveTouchStacks(Player player)
        {
            if (!TouchEligible(player)) return 0;
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
                lastSyncedTouch = -1;
            }
            var current = !player.IsDead() && !player.IsTeleporting() && Equipped(player) ? player.GetCurrentWeapon() : null;
            if (!ReferenceEquals(lastWeapon, current))
            {
                // Store progress on the concrete bow before it leaves the active weapon slot.
                if (lastWeapon != null) HelOathChargeStore.Write(lastWeapon, State.Charge);

                // A different Hel's Oath instance owns its own charge. Touch remains player state and
                // becomes inactive/decaying while the required loadout is unavailable.
                State.Equip(current, current != null ? HelOathChargeStore.Read(current) : 0f);
                lastWeapon = current;

                // Recompute limits without ticking food, healing or refilling stamina.
                object[] values = { 0f, 0f, 0f };
                FoodValue.Invoke(player, values);
                player.SetMaxHealth((float)values[0], false);
                MaxStamina.Invoke(player, new object[] { (float)values[1], false });
            }

            // Death is still a hard reset. Ordinary weapon/armor swaps use the grace + decay path.
            if (player.IsDead()) State.ResetTouch();

            HelOathEffects.SyncPrepared(player, State.Prepared);
            SyncTouch(player);
        }

        internal static void Update(Player player)
        {
            if (player != Player.m_localPlayer) return;
            Refresh(player);

            State.UpdateTouch(Time.time, TouchEligible(player), HelOathConfiguration.TouchStackIntervalValue);
            SyncTouch(player);

            if (!Equipped(player) || State.Charge < 100f || State.Prepared) return;
            if (!(bool)TakeInput.Invoke(player, null) || player.InAttack() || player.InDodge()) return;
            if (!(bool)GetButtonDown.Invoke(null, new object[] { HelOathConfiguration.Button.Name })) return;
            if (State.Activate(Time.time, 0f, TouchEligible(player)))
            {
                PersistCharge();
                State.Tick(Time.time);
                SyncTouch(player);
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
                TouchEligible(player),
                HelOathConfiguration.TouchStackIntervalValue);
            State.AddDamage(damage, HelOathConfiguration.RequiredDamage, Time.time, epoch);
            PersistCharge();
        }

        // Special Embrace damage does not charge Embrace again, but it is still Hel's Oath damage
        // and therefore may awaken a fresh Touch cycle after the previous stacks were consumed.
        internal static void NotifyBowDamage(Player player)
        {
            if (player == null || player != Player.m_localPlayer) return;
            Refresh(player);
            State.RegisterBowDamage(Time.time,
                TouchEligible(player),
                HelOathConfiguration.TouchStackIntervalValue);
        }

        internal static void NotifyCombatHit(Player player)
        {
            if (player == null || player != Player.m_localPlayer) return;
            if (owner != player) Refresh(player);
            int oldTouch = EffectiveTouchStacks(player);
            bool canRestart = TouchEligible(player);
            State.RegisterCombatHit(Time.time, canRestart, HelOathConfiguration.TouchStackIntervalValue);
            int newTouch = EffectiveTouchStacks(player);
            if (oldTouch == HelOathState.MaxTouchStacks && newTouch < HelOathState.MaxTouchStacks)
                HelOathTouchEffects.BroadcastBreak(player);
            SyncTouch(player);
        }

        private static void SyncTouch(Player player)
        {
            int activeTouch = EffectiveTouchStacks(player);
            if (activeTouch == lastSyncedTouch) return;
            lastSyncedTouch = activeTouch;
            HelOathTouchEffects.Sync(player, activeTouch);
            HelOathTouchStatus.Sync(player, activeTouch);
        }

        private static void PersistCharge()
        {
            if (lastWeapon != null) HelOathChargeStore.Write(lastWeapon, State.Charge);
        }
    }
}
