using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace HaldorExpansion.Features.HelOath
{
    // 24 FPS SoulSwap: frame 4 seals the gesture, then the swap commits.
    // Position changes remain authoritative on the local player's Rigidbody.
    internal static class HelOathSoulSwap
    {
        private const string SwapFxRpc = "HE_HelGhostSoulSwap_v2";
        private const string StartRpc = "HE_HelGhostSoulSwapStart_v1";
        private const string EndRpc = "HE_HelGhostSoulSwapEnd_v1";

        private static readonly MethodInfo GetButtonDown = AccessTools.Method(AccessTools.TypeByName("ZInput"),
            "GetButtonDown", new[] { typeof(string) });

        private static readonly MethodInfo TakeInput = AccessTools.Method(typeof(Player), "TakeInput");

        private static readonly ConditionalWeakTable<Player, RemoteCast> RemoteCasts =
            new ConditionalWeakTable<Player, RemoteCast>();

        private sealed class RemoteCast
        {
            internal int Index = -1;
            internal string Token;
            internal float At;
        }

        private static Player castingPlayer;

        internal static bool IsCastLocked(Player player) =>
            player != null && player == Player.m_localPlayer && player == castingPlayer;

        internal static void Register(Player player, ZNetView view)
        {
            if (player == null || view == null || !view.IsValid()) return;
            view.Register<ZPackage>(StartRpc, (sender, packet) =>
            {
                if (!ValidRemote(view, sender) || player == null) return;
                try
                {
                    int index = packet.ReadInt();
                    string token = packet.ReadString();
                    Vector3 before = packet.ReadVector3();
                    if (!ValidBody(index, token) || !Finite(before) ||
                        (before - player.transform.position).sqrMagnitude > 35f * 35f) return;
                    var cast = RemoteCasts.GetValue(player, _ => new RemoteCast());
                    cast.Index = index;
                    cast.Token = token;
                    cast.At = Time.time;
                    HelOathSoulSwapAnimation.BeginRemote(player);
                    HelOathSoulSwapEffects.BeginAura(player);
                    player.GetComponent<HelOathGhostCompanions>()?.BeginSwapAura(index, token);
                    // Match the caster's audio onset (not the later position exchange).
                    HelOathSoulSwapAudio.Play(player, before, HelOathSoulSwapAudio.SoundDelay);
                }
                catch (Exception error)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[Hel Soul Swap] Remote cast start: " + error.Message);
                }
            });
            view.Register<bool>(EndRpc, (sender, ignored) =>
            {
                if (ValidRemote(view, sender)) HelOathSoulSwapAnimation.EndRemote(player);
            });
            view.Register<ZPackage>(SwapFxRpc, (sender, packet) =>
            {
                if (!ValidRemote(view, sender)) return;
                try
                {
                    int index = packet.ReadInt();
                    string token = packet.ReadString();
                    Vector3 oldOwnerPosition = packet.ReadVector3();
                    Vector3 landingPosition = packet.ReadVector3();
                    if (!ValidBody(index, token) || !Finite(oldOwnerPosition) || !Finite(landingPosition) ||
                        (oldOwnerPosition - player.transform.position).sqrMagnitude > 35f * 35f) return;
                    RemoteCast cast;
                    bool prepared = RemoteCasts.TryGetValue(player, out cast) && cast.Index == index &&
                                    cast.Token == token && Time.time - cast.At < 1.5f;
                    if (prepared) cast.Index = -1;
                    player.GetComponent<HelOathGhostCompanions>()?.RemoteSwap(
                        index, token, oldOwnerPosition, prepared);
                    HelOathSoulSwapEffects.PlayVisuals(player, oldOwnerPosition, landingPosition, !prepared);
                    // Compatibility fallback: a client may miss the early cast RPC.
                    if (!prepared) HelOathSoulSwapAudio.Play(player, landingPosition);
                }
                catch (Exception error)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[Hel Soul Swap] Remote visual update: " + error.Message);
                }
            });
        }

        internal static void Update(Player player)
        {
            if (player == null || player != Player.m_localPlayer || IsCastLocked(player) ||
                HelOathConfiguration.SoulSwapButton == null || GetButtonDown == null ||
                TakeInput == null || !HelOathGhostPreview.Eligible(player)) return;
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

            // Keep the existing instant swap if this client cannot load the clip.
            if (HaldorExpansionPlugin.Instance == null || !HelOathSoulSwapAnimation.TryBegin(player))
            {
                Commit(player, companions, selected, token, landing, false);
                return;
            }

            castingPlayer = player;
            HelOathSoulSwapEffects.BeginAura(player);
            companions.BeginSwapAura(selected, token);
            // Local sound starts on button press; the position swap remains on frame 4.
            HelOathSoulSwapAudio.Play(player, player.transform.position, HelOathSoulSwapAudio.SoundDelay);
            BroadcastStart(player, selected, token, player.transform.position);
            HaldorExpansionPlugin.Instance.StartCoroutine(
                AnimatedSwap(player, companions, selected, token));
        }

        private static IEnumerator AnimatedSwap(Player player, HelOathGhostCompanions companions,
            int selected, string token)
        {
            try
            {
                yield return new WaitForSeconds(HelOathSoulSwapAnimation.TeleportDelay);
                Vector3 landing;
                // Ghosts may retire, despawn or move while the hands form the seal.
                // Use the body's CURRENT landing and its original identity.
                if (player != null && player == Player.m_localPlayer && !player.IsDead() &&
                    companions != null && companions.TryResolveSwap(selected, token, out landing))
                    Commit(player, companions, selected, token, landing, true);

                yield return new WaitForSeconds(Mathf.Max(0f,
                    HelOathSoulSwapAnimation.Duration - HelOathSoulSwapAnimation.TeleportDelay));
            }
            finally
            {
                HelOathSoulSwapAnimation.End(player);
                BroadcastEnd(player);
                castingPlayer = null;
            }
        }

        // The only authoritative swap path (both instant fallback and delayed).
        private static void Commit(Player player, HelOathGhostCompanions companions,
            int selected, string token, Vector3 landing, bool prepared)
        {
            Vector3 before = player.transform.position;
            Rigidbody body = player.GetComponent<Rigidbody>();
            HelOathSoulSwapEffects.PlayVisuals(player, before, landing, !prepared);
            // Instant fallback keeps its original sound when the custom clip is unavailable.
            if (!prepared) HelOathSoulSwapAudio.Play(player, landing);
            player.transform.position = landing;
            if (body != null) body.position = landing;
            if (body != null) body.linearVelocity = Vector3.zero;
            companions.CompleteSwap(selected, token, before, prepared);
            var view = HelOathNetwork.View(player);
            if (view == null || !view.IsValid() || !view.IsOwner()) return;
            var packet = new ZPackage();
            HelOathNetwork.Write(packet, selected);
            HelOathNetwork.Write(packet, token);
            HelOathNetwork.Write(packet, before);
            HelOathNetwork.Write(packet, landing);
            view.InvokeRPC(ZNetView.Everybody, SwapFxRpc, packet);
        }

        private static void BroadcastStart(Player player, int index, string token, Vector3 before)
        {
            var view = HelOathNetwork.View(player);
            if (view == null || !view.IsValid() || !view.IsOwner()) return;
            var packet = new ZPackage();
            HelOathNetwork.Write(packet, index);
            HelOathNetwork.Write(packet, token);
            HelOathNetwork.Write(packet, before);
            view.InvokeRPC(ZNetView.Everybody, StartRpc, packet);
        }

        private static void BroadcastEnd(Player player)
        {
            var view = HelOathNetwork.View(player);
            if (view != null && view.IsValid() && view.IsOwner())
                view.InvokeRPC(ZNetView.Everybody, EndRpc, true);
        }

        private static bool ValidRemote(ZNetView view, long sender) =>
            view != null && view.IsValid() && !view.IsOwner() &&
            view.GetZDO() != null && sender == view.GetZDO().GetOwner() &&
            SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;

        private static bool ValidBody(int index, string token)
        {
            Guid parsed;
            return index >= 0 && index < HelOathGhostRules.Limit && token != null &&
                   token.Length == 32 && Guid.TryParseExact(token, "N", out parsed);
        }

        private static bool Finite(Vector3 point) =>
            !float.IsNaN(point.x) && !float.IsNaN(point.y) && !float.IsNaN(point.z)
            && !float.IsInfinity(point.x) && !float.IsInfinity(point.y) && !float.IsInfinity(point.z);
    }
}