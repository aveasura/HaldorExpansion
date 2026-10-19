using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using HaldorExpansion.Compatibility;
using HaldorExpansion.Features.DelayedDoom;
using UnityEngine;
using UnityEngine.Rendering;

namespace HaldorExpansion.Features.HelOath
{
    internal static class HelOathTribute
    {
        internal const string CooldownDataKey = "he_hel_tribute_expiry_utc_v1";
        internal const string PassiveNameKey = "$he_hel_tribute_name";
        internal const string RecoveryNameKey = "$he_hel_tribute_recovery_name";
        internal const string AcceptedMessageKey = "$he_hel_tribute_accepted";
        private const string RescueRpc = "HE_HelTributeFx_v1";

        private sealed class State
        {
            internal float GraceUntil;
            internal bool Pending;
            internal float NextHudUpdate;
            internal bool HudWarningLogged;
            internal ZNetView RegisteredView;
        }

        private static readonly ConditionalWeakTable<Player, State> States = new ConditionalWeakTable<Player, State>();
        private static StatusEffect prototype;

        internal static void Register(Player player, ZNetView view)
        {
            if (player == null || view == null || !view.IsValid()) return;
            State state = States.GetValue(player, ignored => new State());
            if (state.RegisteredView == view) return;
            state.RegisteredView = view;
            view.Register<Vector3>(RescueRpc, (sender, point) =>
            {
                if (!view.IsValid() || sender != view.GetZDO().GetOwner()) return;
                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
                // Cosmetic only: no character cloning, hit objects or combat effects.
                HelOathTributeFx.Play(point, player == Player.m_localPlayer);
            });
        }

        internal static bool IsGraceActive(Player player)
        {
            State state;
            return player != null && States.TryGetValue(player, out state) && Time.time < state.GraceUntil;
        }

        internal static bool TryRescue(Player player, float beforeHealth)
        {
            if (player == null || player != Player.m_localPlayer || beforeHealth <= 0f ||
                float.IsNaN(beforeHealth) || float.IsInfinity(beforeHealth)) return false;
            State state = States.GetValue(player, ignored => new State());
            if (state.Pending || Remaining(player) > 0f) return false;
            if (!HelOathGhostPreview.TryConsumeForTribute(player)) return false;
            player.m_customData[CooldownDataKey] = HelOathTributeMath.Expiry(DateTime.UtcNow.Ticks)
                .ToString(CultureInfo.InvariantCulture);
            state.Pending = true;
            state.GraceUntil = Time.time + HelOathTributeMath.GraceSeconds;
            return true;
        }

        internal static void CompleteRescue(Player player, float beforeHealth)
        {
            State state;
            if (player == null || !States.TryGetValue(player, out state) || !state.Pending) return;
            state.Pending = false;
            DelayedDoomRuntime.ClearDelayedDoomDebt(player);
            CharacterHealthAccess.SetCharacterHealth(player, HelOathTributeMath.SavedHealth(beforeHealth));
            player.Message(MessageHud.MessageType.Center, AcceptedMessageKey);
            state.NextHudUpdate = 0f;
            Update(player);
            ZNetView view = state.RegisteredView;
            if (view != null && view.IsValid() && view.IsOwner())
                view.InvokeRPC(ZNetView.Everybody, RescueRpc, player.GetCenterPoint());
            else if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                HelOathTributeFx.Play(player.GetCenterPoint(), player == Player.m_localPlayer);
        }

        private static float Remaining(Player player)
        {
            string raw;
            return player.m_customData.TryGetValue(CooldownDataKey, out raw)
                ? HelOathTributeMath.Remaining(raw, DateTime.UtcNow.Ticks)
                : 0f;
        }

        internal static void Update(Player player)
        {
            if (player == null || player != Player.m_localPlayer) return;
            State state = States.GetValue(player, ignored => new State());
            if (Time.time < state.NextHudUpdate) return;
            state.NextHudUpdate = Time.time + 0.2f;
            float remaining = Remaining(player);
            // Expired data can be discarded; death/equipment/status changes never clear it.
            if (remaining <= 0f) player.m_customData.Remove(CooldownDataKey);
            try
            {
                SEMan manager = player.GetSEMan();
                if (manager == null) return;
                if (prototype == null)
                {
                    prototype = ScriptableObject.CreateInstance<StatusEffect>();
                    prototype.name = "HE_HelTributeRecovery";
                    prototype.m_name = RecoveryNameKey;
                    prototype.m_tooltip = "$he_hel_tribute_recovery_description";
                }

                if (remaining <= 0f)
                {
                    if (manager.GetStatusEffect(prototype.NameHash()) != null)
                        manager.RemoveStatusEffect(prototype.NameHash());
                    return;
                }

                Sprite[] icons = HelOathItemRegistration.HelOathItemDrop?.m_itemData?.m_shared?.m_icons;
                if (icons != null && icons.Length > 0) prototype.m_icon = icons[0];
                prototype.m_ttl = remaining;
                StatusEffect active =
                    manager.GetStatusEffect(prototype.NameHash()) ?? manager.AddStatusEffect(prototype);
                if (active == null) return;
                active.m_icon = prototype.m_icon;
                active.m_ttl = remaining;
                ReflectionAccess.SetFieldIfExists(active, "m_time", 0f);
            }
            catch (Exception error)
            {
                if (!state.HudWarningLogged)
                {
                    state.HudWarningLogged = true;
                    HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] Tribute recovery HUD unavailable: " +
                                                          error.Message);
                }
            }
        }

        internal static void Reset(Player player)
        {
            if (player != null) States.Remove(player);
        }
    }
}