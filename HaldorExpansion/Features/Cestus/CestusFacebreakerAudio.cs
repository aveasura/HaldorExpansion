using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HaldorExpansion.Features.Cestus
{
    /// <summary>
    /// Lossless in-code audio for Facebreaker. Both approved WAV tracks are
    /// embedded as compressed stereo PCM, without external asset files.
    /// Play both tracks sample-synchronously from cast start, initially with
    /// WithStagger muted. Only a confirmed two-sided grab crossfades to it,
    /// before its impact layer starts at ~0.95s after the cast.
    /// Audio never drives animation, physics, cooldown, or stagger decisions.
    /// </summary>
    internal static class CestusFacebreakerAudio
    {
        private const string SoundRpc = "aveasura_he_cestus_facebreaker_sound_v2";
        private const int StartCue = 1;
        private const int StaggerCue = 2;
        private const float SoundMaxDistance = 48f;
        private const float SwitchFadeSeconds = 0.035f;

        // Sound alone: change the delay without altering pull (0.54s) or stagger timing.
        // The 0.1s of DIGITAL SILENCE at the start of the approved WAV was removed.
        internal const float SoundStartDelaySeconds = 0.1f;
        internal const float SoundVolume = 1.0f;

        private static readonly FieldInfo CharacterView = AccessTools.Field(typeof(Character), "m_nview");

        // Reflection prevents Unity's ReadOnlySpan<float> overload from breaking net48 builds.
        private static readonly MethodInfo SetClipData = AccessTools.Method(
            typeof(AudioClip), "SetData", new[] { typeof(float[]), typeof(int) });

        private static readonly ConditionalWeakTable<ZNetView, object> RegisteredViews =
            new ConditionalWeakTable<ZNetView, object>();

        private static readonly ConditionalWeakTable<Player, Playback> PlaybackByPlayer =
            new ConditionalWeakTable<Player, Playback>();

        private sealed class Playback
        {
            internal GameObject Root;
            internal AudioSource Normal;
            internal AudioSource Stagger;
            internal float Volume;
            internal Coroutine Transition;
            internal bool StaggerSelected;
        }

        private static AudioClip normalClip;
        private static AudioClip staggerClip;

        private static ZNetView GetView(Player player)
            => player == null || CharacterView == null ? null : CharacterView.GetValue(player) as ZNetView;

        internal static void Register(Character character, ZNetView view = null)
        {
            Player player = character as Player;
            if (player == null) return;
            if (view == null) view = GetView(player);
            if (view == null || !view.IsValid() || RegisteredViews.TryGetValue(view, out _)) return;

            view.Register<int>(SoundRpc, (sender, cue) =>
            {
                if (cue != StartCue && cue != StaggerCue) return;
                if (view == null || !view.IsValid() || view.GetZDO() == null ||
                    view.GetZDO().GetOwner() != sender || player == Player.m_localPlayer)
                    return;
                if (cue == StartCue)
                    StartSound(player, false);
                else
                    SwitchToStagger(player);
            });
            RegisteredViews.Add(view, new object());
        }

        // Called on confirmed RELEASE / cast begin, not during RMB targeting.
        internal static void StartCast(Player caster)
        {
            if (caster == null || caster != Player.m_localPlayer || caster.IsDead()) return;
            StartSound(caster, true);
            Broadcast(caster, StartCue);
        }

        // The existing ResolvePull computes this condition at the hand-closing frame.
        internal static void SelectStagger(Player caster)
        {
            if (caster == null || caster != Player.m_localPlayer || caster.IsDead()) return;
            SwitchToStagger(caster);
            Broadcast(caster, StaggerCue);
        }

        private static void Broadcast(Player caster, int cue)
        {
            ZNetView view = GetView(caster);
            if (view != null && view.IsValid())
                view.InvokeRPC(ZNetView.Everybody, SoundRpc, cue);
        }

        private static void StartSound(Player player, bool local)
        {
            if (player == null || Application.isBatchMode) return;
            if (!local && Player.m_localPlayer != null &&
                Vector3.Distance(Player.m_localPlayer.transform.position, player.transform.position) > SoundMaxDistance)
                return;

            try
            {
                if (normalClip == null)
                    normalClip = MakeClip(
                        "HE_Facebreaker_NoStagger_ExtraClean", CestusFacebreakerAudioData.NoStaggerGzip,
                        CestusFacebreakerAudioData.NoStaggerFrames);
                if (staggerClip == null)
                    staggerClip = MakeClip(
                        "HE_Facebreaker_WithStagger_ExtraClean", CestusFacebreakerAudioData.WithStaggerGzip,
                        CestusFacebreakerAudioData.WithStaggerFrames);

                Playback playback = PlaybackByPlayer.GetValue(player, _ => new Playback());
                if (playback.Transition != null && HaldorExpansionPlugin.Instance != null)
                    HaldorExpansionPlugin.Instance.StopCoroutine(playback.Transition);
                playback.Transition = null;
                if (playback.Root != null) Object.Destroy(playback.Root);

                GameObject root = new GameObject("HE_Facebreaker_EmbeddedAudio");
                root.transform.SetParent(player.transform, true);
                root.transform.position = player.GetCenterPoint();
                float volume = SoundVolume * AudioMan.GetSFXVolume();
                playback.Root = root;
                playback.Volume = volume;
                playback.StaggerSelected = false;
                playback.Normal = MakeSource(root, normalClip, local, volume);
                playback.Stagger = MakeSource(root, staggerClip, local, 0f);

                // DSP scheduling keeps both versions synchronized at the sample level.
                // An adjustable delay REPLACES, not adds to, the WAV's leading silence.
                double playAt = AudioSettings.dspTime + Math.Max(0.02f, SoundStartDelaySeconds);
                playback.Normal.PlayScheduled(playAt);
                playback.Stagger.PlayScheduled(playAt);

                // Longest track is 1.97s before the silent 0.1s trim.
                Object.Destroy(root, SoundStartDelaySeconds + staggerClip.length + 0.5f);
            }
            catch (Exception ex)
            {
                HaldorExpansionPlugin.Log?.LogWarning("[Facebreaker Audio] " + ex.Message);
            }
        }

        private static void SwitchToStagger(Player player)
        {
            if (player == null || Application.isBatchMode) return;
            Playback playback;
            if (!PlaybackByPlayer.TryGetValue(player, out playback) || playback.Root == null ||
                playback.StaggerSelected || playback.Normal == null || playback.Stagger == null)
                return;

            playback.StaggerSelected = true;
            if (HaldorExpansionPlugin.Instance != null)
                playback.Transition = HaldorExpansionPlugin.Instance.StartCoroutine(FadeToStagger(playback));
            else
            {
                playback.Normal.volume = 0f;
                playback.Stagger.volume = playback.Volume;
            }
        }

        private static IEnumerator FadeToStagger(Playback playback)
        {
            float elapsed = 0f;
            while (elapsed < SwitchFadeSeconds && playback.Root != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float fraction = Mathf.Clamp01(elapsed / SwitchFadeSeconds);
                // The two original WAVs have essentially identical early samples;
                // the 35ms blend avoids a level discontinuity between their masters.
                playback.Normal.volume = playback.Volume * (1f - fraction);
                playback.Stagger.volume = playback.Volume * fraction;
                yield return null;
            }

            if (playback.Root != null)
            {
                playback.Normal.volume = 0f;
                playback.Stagger.volume = playback.Volume;
            }

            playback.Transition = null;
        }

        private static AudioSource MakeSource(GameObject root, AudioClip clip, bool local, float volume)
        {
            AudioSource source = root.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.clip = clip;
            source.loop = false;
            source.pitch = 1f;
            // Full original stereo playback for the caster. 3D positional audio for others.
            source.spatialBlend = local ? 0f : 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 3.5f;
            source.maxDistance = SoundMaxDistance;
            source.dopplerLevel = 0f;
            source.volume = volume;
            return source;
        }

        private static AudioClip MakeClip(string name, string[] gzipChunks, int frameCount)
        {
            int byteCount = frameCount * CestusFacebreakerAudioData.Channels * sizeof(short);
            byte[] pcm = new byte[byteCount];
            byte[] gzipBytes = Convert.FromBase64String(string.Concat(gzipChunks));
            using (MemoryStream input = new MemoryStream(gzipBytes, false))
            using (GZipStream decompressor = new GZipStream(input, CompressionMode.Decompress))
            {
                int read = 0;
                while (read < byteCount)
                {
                    int result = decompressor.Read(pcm, read, byteCount - read);
                    if (result == 0) throw new InvalidDataException("Truncated embedded Facebreaker audio: " + name);
                    read += result;
                }

                if (decompressor.ReadByte() != -1)
                    throw new InvalidDataException("Oversized embedded Facebreaker audio: " + name);
            }

            // PCM16 -> float is exact for every source sample (2^-15 scaling).
            float[] samples = new float[byteCount / 2];
            for (int i = 0, offset = 0; i < samples.Length; i++, offset += 2)
            {
                short pcm16 = (short)(pcm[offset] | (pcm[offset + 1] << 8));
                samples[i] = pcm16 / 32768f;
            }

            AudioClip clip = AudioClip.Create(name, frameCount, CestusFacebreakerAudioData.Channels,
                CestusFacebreakerAudioData.SampleRate, false);
            if (SetClipData == null)
                throw new MissingMethodException("AudioClip.SetData(float[], int) was not found");
            if (!(bool)SetClipData.Invoke(clip, new object[] { samples, 0 }))
                throw new InvalidOperationException("AudioClip.SetData failed for " + name);
            return clip;
        }
    }
}