using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace HaldorExpansion.Features.Cestus
{
    /// <summary>
    /// Optional Facebreaker cast visual. This never controls target selection, stamina,
    /// cooldowns or pull physics. If a game's Animator cannot use the imported clip,
    /// the old instantaneous Facebreaker path remains available.
    /// </summary>
    internal static class CestusFacebreakerAnimation
    {
        private const string BundleResource = "HaldorExpansion.Assets.haldor_facebreaker";
        private const string AnimationName = "Facebreaker";
        private const string EmoteName = "point";
        private const string CastVisualRpc = "aveasura_he_cestus_facebreaker_anim_v1";

        private const string StopVisualRpc = "aveasura_he_cestus_facebreaker_anim_end_v1";

        // Changing runtimeAnimatorController after every cast is a likely reason
        // the Valheim Animator restarts in its multi-second "Standing Up" state.
        // Keep the same override controller installed and restore only the point
        // clip bindings when the emote finishes. Input lock remains unaffected.
        private const float ExitWaitTimeoutSeconds = 2.0f;
        private const int ExitStableFrames = 2;
        private static readonly FieldInfo AnimatorField = AccessTools.Field(typeof(Character), "m_animator");
        private static readonly FieldInfo CharacterView = AccessTools.Field(typeof(Character), "m_nview");
        private static readonly MethodInfo StopEmote = AccessTools.Method(typeof(Player), "StopEmote", Type.EmptyTypes);

        private static readonly ConditionalWeakTable<ZNetView, object> Registered =
            new ConditionalWeakTable<ZNetView, object>();

        private static readonly ConditionalWeakTable<Player, VisualState> Visuals =
            new ConditionalWeakTable<Player, VisualState>();

        private sealed class VisualState
        {
            internal Animator Animator;
            internal RuntimeAnimatorController Original;
            internal AnimatorOverrideController Override;
            internal Coroutine RemoteTimeout;
            internal Coroutine PendingRestore;
            internal int Revision;
            internal List<KeyValuePair<AnimationClip, AnimationClip>> BaselineOverrides;
            internal bool ClipInjected;
            internal AnimationClip ActiveClip;
            internal bool SoulSwapActive;
        }

        private static bool attemptedLoad;
        private static AnimationClip castClip;
        private static AssetBundle bundle;

        private static bool CanRender => SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;

        private static ZNetView View(Player player)
            => player == null || CharacterView == null ? null : CharacterView.GetValue(player) as ZNetView;

        internal static void Register(Character character, ZNetView view = null)
        {
            Player player = character as Player;
            if (player == null) return;
            if (view == null) view = View(player);
            if (view == null || !view.IsValid() || Registered.TryGetValue(view, out _)) return;

            view.Register<float>(CastVisualRpc, (sender, seconds) =>
            {
                if (!ValidSender(view, sender) || player == Player.m_localPlayer ||
                    float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0.3f || seconds > 2f)
                    return;
                if (InstallOverride(player) && HaldorExpansionPlugin.Instance != null)
                {
                    VisualState state = Visuals.GetValue(player, _ => new VisualState());
                    if (state.RemoteTimeout != null)
                        HaldorExpansionPlugin.Instance.StopCoroutine(state.RemoteTimeout);
                    state.RemoteTimeout = HaldorExpansionPlugin.Instance.StartCoroutine(
                        RemoteExpiry(player, seconds + 0.30f));
                }
            });
            view.Register<bool>(StopVisualRpc, (sender, ignored) =>
            {
                if (ValidSender(view, sender) && player != Player.m_localPlayer &&
                    !IsSoulSwapActive(player))
                    RestoreAfterEmote(player);
            });
            Registered.Add(view, new object());
        }

        private static bool ValidSender(ZNetView view, long sender)
        {
            return view != null && view.IsValid() && view.GetZDO() != null &&
                   view.GetZDO().GetOwner() == sender;
        }

        internal static bool TryBegin(Player player)
        {
            if (player == null || player != Player.m_localPlayer ||
                !InstallOverride(player)) return false;

            try
            {
                // Replicate the point emote through Valheim, but temporarily
                // replace only its clip; never reset the Animator after the cast.
                Broadcast(player, CastVisualRpc, CestusFacebreaker.CastAnimationDurationSeconds);
                player.StartEmote(EmoteName, true);
                return true;
            }
            catch (Exception ex)
            {
                HaldorExpansionPlugin.Log?.LogWarning("[Facebreaker Animation] Emote start failed: " + ex.Message);
                RestorePointClip(player);
                Broadcast(player, StopVisualRpc, true);
                return false;
            }
        }

        // Soul Swap uses the SAME persistent point-emote override as Facebreaker.
        // Never stack a second AnimatorOverrideController on the player.
        internal static bool TryBeginSoulSwap(Player player, AnimationClip clip)
        {
            if (player == null || player != Player.m_localPlayer || clip == null ||
                !InstallOverride(player, clip, true)) return false;
            try
            {
                player.StartEmote(EmoteName, true);
                return true;
            }
            catch (Exception ex)
            {
                HaldorExpansionPlugin.Log?.LogWarning("[Soul Swap Animation] Start failed: " + ex.Message);
                RestorePointClip(player);
                return false;
            }
        }

        internal static void BeginRemoteSoulSwap(Player player, AnimationClip clip, float duration)
        {
            if (player == null || player == Player.m_localPlayer || clip == null ||
                !InstallOverride(player, clip, true) || HaldorExpansionPlugin.Instance == null) return;
            VisualState state = Visuals.GetValue(player, _ => new VisualState());
            if (state.RemoteTimeout != null)
                HaldorExpansionPlugin.Instance.StopCoroutine(state.RemoteTimeout);
            state.RemoteTimeout = HaldorExpansionPlugin.Instance.StartCoroutine(
                RemoteExpiry(player, duration + 0.30f));
        }

        internal static bool IsSoulSwapActive(Player player) =>
            player != null && Visuals.TryGetValue(player, out VisualState state) &&
            state.ClipInjected && state.SoulSwapActive;

        internal static void EndSoulSwap(Player player)
        {
            if (IsSoulSwapActive(player)) EndCustomLocal(player, false);
        }

        internal static void EndRemoteSoulSwap(Player player)
        {
            if (player != null && player != Player.m_localPlayer && IsSoulSwapActive(player))
                RestoreAfterEmote(player);
        }

        internal static void EndLocal(Player player)
        {
            if (!IsSoulSwapActive(player)) EndCustomLocal(player, true);
        }

        private static void EndCustomLocal(Player player, bool facebreaker)
        {
            if (player == null || !Visuals.TryGetValue(player, out VisualState state) ||
                state.Override == null || !state.ClipInjected)
                return;

            // The ability is allowed to release controls on time, even if the
            // Animator takes additional frames to blend out of the emote.
            // StopEmote is still Valheim's normal way of leaving the emote.
            // We only restore the original point CLIP, not the whole controller.
            if (player == Player.m_localPlayer && player.InEmote() && StopEmote != null)
            {
                try
                {
                    StopEmote.Invoke(player, null);
                }
                catch (Exception ex)
                {
                    HaldorExpansionPlugin.Log?.LogWarning("[Facebreaker Animation] Emote stop failed: " + ex.Message);
                }
            }

            RestoreAfterEmote(player);
            if (facebreaker && player == Player.m_localPlayer)
                Broadcast(player, StopVisualRpc, true);
        }

        private static void Broadcast<T>(Player player, string rpc, T arg)
        {
            ZNetView view = View(player);
            if (view != null && view.IsValid())
                view.InvokeRPC(ZNetView.Everybody, rpc, arg);
        }

        private static IEnumerator RemoteExpiry(Player player, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (player != null && Visuals.TryGetValue(player, out VisualState state))
                state.RemoteTimeout = null;
            RestoreAfterEmote(player);
        }

        // Separate visual cleanup from Facebreaker's actual cast coroutine. The
        // pull, stamina, cooldown and input lock do NOT wait for this coroutine.
        private static void RestoreAfterEmote(Player player)
        {
            if (player == null || !Visuals.TryGetValue(player, out VisualState state) ||
                state.Override == null || !state.ClipInjected)
                return;
            CancelPendingRestore(state);
            // A normal Stop RPC supersedes the remote fail-safe timer; otherwise
            // that timer could restart the cleanup while the emote is exiting.
            if (state.RemoteTimeout != null && HaldorExpansionPlugin.Instance != null)
                HaldorExpansionPlugin.Instance.StopCoroutine(state.RemoteTimeout);
            state.RemoteTimeout = null;
            if (HaldorExpansionPlugin.Instance == null)
            {
                RestorePointClip(player);
                return;
            }

            int revision = state.Revision;
            state.PendingRestore = HaldorExpansionPlugin.Instance.StartCoroutine(
                WaitForEmoteExit(player, state, revision));
        }

        private static IEnumerator WaitForEmoteExit(Player player, VisualState state, int revision)
        {
            float deadline = Time.unscaledTime + ExitWaitTimeoutSeconds;
            int stableFrames = 0;
            // Always give Valheim one Animator update to process StopEmote.
            yield return null;
            // Never switch runtimeAnimatorController here: restoring the original
            // controller after the cast appears to restart Valheim's spawn/get-up
            // animation, even when a locomotion CrossFade was requested earlier.
            while (player != null && state.Revision == revision && state.Override != null &&
                   state.Animator != null && state.Animator.runtimeAnimatorController == state.Override)
            {
                bool emoteActive = player.InEmote();
                bool clipActive = IsCustomClipPlaying(state.Animator, state.ActiveClip);
                bool transitioning = IsAnimatorTransitioning(state.Animator);
                if (!emoteActive && !clipActive && !transitioning)
                {
                    if (++stableFrames >= ExitStableFrames)
                        break;
                }
                else
                {
                    stableFrames = 0;
                }

                if (Time.unscaledTime >= deadline)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        "[Facebreaker Animation] Timed out waiting for the emote to exit; " +
                        "restoring original point clip (inEmote=" + emoteActive +
                        ", clipActive=" + clipActive + ", transition=" + transitioning + ").");
                    break;
                }

                yield return null;
            }

            if (state.Revision == revision)
            {
                state.PendingRestore = null;
                RestorePointClip(player);
            }
        }

        private static bool IsAnimatorTransitioning(Animator animator)
        {
            if (animator == null || !animator.isActiveAndEnabled) return false;
            for (int layer = 0; layer < animator.layerCount; layer++)
                if (animator.IsInTransition(layer))
                    return true;
            return false;
        }

        private static bool IsCustomClipPlaying(Animator animator, AnimationClip activeClip)
        {
            if (animator == null || !animator.isActiveAndEnabled || activeClip == null) return false;
            for (int layer = 0; layer < animator.layerCount; layer++)
            {
                AnimatorClipInfo[] current = animator.GetCurrentAnimatorClipInfo(layer);
                for (int i = 0; i < current.Length; i++)
                    if (current[i].clip == activeClip && current[i].weight > 0.001f)
                        return true;
                if (!animator.IsInTransition(layer)) continue;
                AnimatorClipInfo[] next = animator.GetNextAnimatorClipInfo(layer);
                for (int i = 0; i < next.Length; i++)
                    if (next[i].clip == activeClip && next[i].weight > 0.001f)
                        return true;
            }

            return false;
        }

        private static void CancelPendingRestore(VisualState state)
        {
            if (state == null) return;
            state.Revision++;
            if (state.PendingRestore != null && HaldorExpansionPlugin.Instance != null)
                HaldorExpansionPlugin.Instance.StopCoroutine(state.PendingRestore);
            state.PendingRestore = null;
        }

        // Reapply the cast clip only while this Facebreaker is running. During
        // ordinary play (including Nova) the original point animation is kept.
        private static bool ApplyClip(VisualState state, AnimationClip clip, bool soulSwap)
        {
            if (state == null || state.Override == null || state.BaselineOverrides == null || clip == null)
                return false;

            var replacements = new List<KeyValuePair<AnimationClip, AnimationClip>>(state.BaselineOverrides);
            int count = 0;
            for (int i = 0; i < replacements.Count; i++)
            {
                AnimationClip key = replacements[i].Key;
                if (key == null || key.name.IndexOf(EmoteName, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                replacements[i] = new KeyValuePair<AnimationClip, AnimationClip>(key, clip);
                count++;
            }

            if (count == 0) return false;
            state.Override.ApplyOverrides(replacements);
            state.ClipInjected = true;
            state.ActiveClip = clip;
            state.SoulSwapActive = soulSwap;
            return true;
        }

        private static void RestorePointClip(Player player)
        {
            if (player == null || !Visuals.TryGetValue(player, out VisualState state) ||
                state.Override == null || !state.ClipInjected) return;
            try
            {
                if (state.Animator != null &&
                    state.Animator.runtimeAnimatorController == state.Override &&
                    state.BaselineOverrides != null)
                {
                    // ApplyOverrides changes clip bindings without switching or
                    // restarting the player's Animator state machine.
                    state.Override.ApplyOverrides(state.BaselineOverrides);
                }
            }
            catch (Exception ex)
            {
                HaldorExpansionPlugin.Log?.LogWarning(
                    "[Facebreaker Animation] Could not restore point clip: " + ex.Message);
            }
            finally
            {
                state.ClipInjected = false;
                state.ActiveClip = null;
                state.SoulSwapActive = false;
            }
        }

        private static bool InstallOverride(Player player)
        {
            if (!LoadClip()) return false;
            return InstallOverride(player, castClip, false);
        }

        private static bool InstallOverride(Player player, AnimationClip clip, bool soulSwap)
        {
            if (!CanRender || clip == null) return false;
            Animator animator = AnimatorField?.GetValue(player) as Animator;
            if (animator == null || animator.runtimeAnimatorController == null)
                return false;
            VisualState state = Visuals.GetValue(player, _ => new VisualState());
            // Do not steal the point-emote from the other ability mid-cast.
            if (state.ClipInjected && state.SoulSwapActive != soulSwap && player.InEmote())
                return false;
            if (state.Override != null && state.Animator == animator &&
                animator.runtimeAnimatorController == state.Override)
            {
                // Reuse the controller without a state-machine reset. A new cast
                // also cancels old recovery timers, including on remote players.
                CancelPendingRestore(state);
                if (state.RemoteTimeout != null && HaldorExpansionPlugin.Instance != null)
                    HaldorExpansionPlugin.Instance.StopCoroutine(state.RemoteTimeout);
                state.RemoteTimeout = null;
                try
                {
                    return ApplyClip(state, clip, soulSwap);
                }
                catch (Exception ex)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        "[Facebreaker Animation] Clip override update failed: " + ex.Message);
                    return false;
                }
            }

            DiscardStaleOverride(player);

            RuntimeAnimatorController original = animator.runtimeAnimatorController;
            AnimatorOverrideController copy = null;
            try
            {
                copy = new AnimatorOverrideController(original);
                var baseline = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                copy.GetOverrides(baseline);
                state.Animator = animator;
                state.Original = original;
                state.Override = copy;
                state.BaselineOverrides = baseline;
                if (!ApplyClip(state, clip, soulSwap))
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        "[Facebreaker Animation] Could not find a " + EmoteName +
                        " clip; the original instant cast is still available.");
                    DiscardStaleOverride(player);
                    return false;
                }

                animator.runtimeAnimatorController = copy;
                return true;
            }
            catch (Exception ex)
            {
                HaldorExpansionPlugin.Log?.LogWarning("[Facebreaker Animation] Override error: " + ex.Message);
                DiscardStaleOverride(player);
                return false;
            }
        }

        // Only needed if another mod changes/replaces the animator or player.
        // Routine Facebreaker cleanup must NOT call this: that was the likely
        // source of the multi-second "Standing Up" recovery animation.
        private static void DiscardStaleOverride(Player player)
        {
            if (player == null || !Visuals.TryGetValue(player, out VisualState state)) return;
            CancelPendingRestore(state);
            if (state.RemoteTimeout != null && HaldorExpansionPlugin.Instance != null)
                HaldorExpansionPlugin.Instance.StopCoroutine(state.RemoteTimeout);
            state.RemoteTimeout = null;
            if (state.Override != null)
            {
                if (state.Animator != null &&
                    state.Animator.runtimeAnimatorController == state.Override)
                    state.Animator.runtimeAnimatorController = state.Original;
                Object.Destroy(state.Override);
            }

            state.Override = null;
            state.Original = null;
            state.Animator = null;
            state.BaselineOverrides = null;
            state.ClipInjected = false;
            state.ActiveClip = null;
            state.SoulSwapActive = false;
        }

        private static bool LoadClip()
        {
            if (attemptedLoad) return castClip != null;
            attemptedLoad = true;
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(BundleResource))
                {
                    if (stream == null) throw new FileNotFoundException(BundleResource);
                    using (var bytes = new MemoryStream())
                    {
                        stream.CopyTo(bytes);
                        bundle = AssetBundle.LoadFromMemory(bytes.ToArray());
                    }
                }

                if (bundle == null) throw new InvalidOperationException("Unity could not open the AssetBundle");
                AnimationClip[] clips = bundle.LoadAllAssets<AnimationClip>();
                if (clips == null || clips.Length == 0)
                    clips = bundle.LoadAssetWithSubAssets<AnimationClip>("assets/facebreaker.fbx");
                if (clips != null)
                    for (int i = 0; i < clips.Length; i++)
                        if (clips[i] != null && clips[i].name == AnimationName)
                        {
                            castClip = clips[i];
                            break;
                        }

                if (castClip == null) throw new InvalidOperationException("Facebreaker AnimationClip was not found");
                return true;
            }
            catch (Exception ex)
            {
                HaldorExpansionPlugin.Log?.LogWarning(
                    "[Facebreaker Animation] Loading failed; using the original instant cast: " + ex.Message);
                return false;
            }
        }
    }
}