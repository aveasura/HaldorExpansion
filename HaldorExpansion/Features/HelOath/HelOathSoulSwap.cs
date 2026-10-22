using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace HaldorExpansion.Features.HelOath
{
    // Instant position swap with a materialized ghost; its body and FIFO remain intact.
    internal static class HelOathSoulSwap
    {
        private const string SwapFxRpc = "HE_HelGhostSoulSwap_v2";

        private static readonly MethodInfo GetButtonDown = AccessTools.Method(AccessTools.TypeByName("ZInput"),
            "GetButtonDown", new[] { typeof(string) });

        private static readonly MethodInfo TakeInput = AccessTools.Method(typeof(Player), "TakeInput");

        internal static void Register(Player player, ZNetView view)
        {
            if (player == null || view == null || !view.IsValid()) return;
            view.Register<ZPackage>(SwapFxRpc, (sender, packet) =>
            {
                if (!view.IsValid() || view.IsOwner() || sender != view.GetZDO().GetOwner()
                    || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
                try
                {
                    int index = packet.ReadInt();
                    string token = packet.ReadString();
                    Vector3 oldOwnerPosition = packet.ReadVector3();
                    Vector3 landingPosition = packet.ReadVector3();
                    Guid parsed;
                    if (index < 0 || index >= HelOathGhostRules.Limit || token == null
                        || token.Length != 32 || !Guid.TryParseExact(token, "N", out parsed)
                        || !Finite(oldOwnerPosition) || !Finite(landingPosition)
                        || (oldOwnerPosition - player.transform.position).sqrMagnitude > 35f * 35f) return;
                    player.GetComponent<HelOathGhostCompanions>()?.RemoteSwap(index, token, oldOwnerPosition);
                    HelOathSoulSwapEffects.PlayVisuals(player, oldOwnerPosition, landingPosition);
                    HelOathSoulSwapAudio.Play(player, landingPosition);
                }
                catch (Exception error)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[Hel Soul Swap] Remote visual update: " + error.Message);
                }
            });
        }

        internal static void Update(Player player)
        {
            if (player == null || player != Player.m_localPlayer
                               || HelOathConfiguration.SoulSwapButton == null || GetButtonDown == null ||
                               TakeInput == null
                               || !HelOathGhostPreview.Eligible(player)) return;
            // Only the existing UI/input gate is retained. No grounded, rolling,
            // jumping, attacking, stamina, cooldown or enemy-distance veto.
            if (!(bool)TakeInput.Invoke(player, null)) return;
            if (!(bool)GetButtonDown.Invoke(null, new object[] { HelOathConfiguration.SoulSwapButton.Name })) return;
            var companions = player.GetComponent<HelOathGhostCompanions>();
            int selected;
            string token;
            Vector3 landing;
            if (companions == null || !companions.TrySelectSwap(out selected, out token, out landing))
            {
                player.Message(MessageHud.MessageType.Center, HelOathIds.SoulSwapUnavailableKey);
                return;
            }

            // This is not Valheim's portal-style TeleportTo (which triggers loading).
            // The character owns its Rigidbody; setting both immediately prevents a physics snap-back.
            Vector3 before = player.transform.position;
            Rigidbody body = player.GetComponent<Rigidbody>();
            HelOathSoulSwapEffects.PlayVisuals(player, before, landing);
            HelOathSoulSwapAudio.Play(player, landing);
            player.transform.position = landing;
            if (body != null) body.position = landing;
            if (body != null) body.linearVelocity = Vector3.zero;
            companions.CompleteSwap(selected, token, before);
            var view = HelOathNetwork.View(player);
            if (view == null || !view.IsValid() || !view.IsOwner()) return;
            var packet = new ZPackage();
            HelOathNetwork.Write(packet, selected);
            HelOathNetwork.Write(packet, token);
            HelOathNetwork.Write(packet, before);
            HelOathNetwork.Write(packet, landing);
            view.InvokeRPC(ZNetView.Everybody, SwapFxRpc, packet);
        }

        private static bool Finite(Vector3 point) =>
            !float.IsNaN(point.x) && !float.IsNaN(point.y) && !float.IsNaN(point.z)
            && !float.IsInfinity(point.x) && !float.IsInfinity(point.y) && !float.IsInfinity(point.z);
    }
}