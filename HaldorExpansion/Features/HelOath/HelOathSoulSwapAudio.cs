using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace HaldorExpansion.Features.HelOath
{
    // Start the approved teleport sample during the seal gesture, ahead of the position exchange.
    // This is presentation-only: audio errors cannot affect movement or ghost state.
    internal static class HelOathSoulSwapAudio
    {
        private const float MaxDistance = 48f;

        // Sound timing relative to pressing Soul Swap (seconds):
        //  0f = start immediately; positive = delay; negative = start immediately
        //        but skip that many seconds at the beginning of the sample.
        // For example -0.03f moves the sound's strong attack ~30ms earlier,
        // without changing the teleport at animation frame 4 (0.125s).
        internal const float SoundDelay = 0.00f;
        internal const float LocalVolume = 0.95f;
        internal const float RemoteVolume = 0.78f;

        private static readonly MethodInfo SetClipData = AccessTools.Method(
            typeof(AudioClip), "SetData", new[] { typeof(float[]), typeof(int) });

        private static readonly ConditionalWeakTable<Player, Playback> PlaybackByPlayer =
            new ConditionalWeakTable<Player, Playback>();

        private sealed class Playback
        {
            internal GameObject Root;
        }

        private static AudioClip clip;
        private static bool loggedFailure;
        private static bool attemptedLoad;

        internal static void Prewarm() => EnsureClip();

        private static bool EnsureClip()
        {
            if (clip != null) return true;
            if (attemptedLoad || Application.isBatchMode ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return false;
            attemptedLoad = true;
            try
            {
                clip = CreateClip();
            }
            catch (Exception error)
            {
                LogFailure(error);
            }

            return clip != null;
        }

        private static void LogFailure(Exception error)
        {
            if (loggedFailure) return;
            loggedFailure = true;
            HaldorExpansionPlugin.Log?.LogWarning("[Hel Soul Swap Audio] " + error);
        }

        internal static void Play(Player player, Vector3 destination, float delay = 0f)
        {
            if (player == null || Application.isBatchMode ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;

            bool local = player == Player.m_localPlayer;
            if (!local && Player.m_localPlayer != null &&
                (destination - Player.m_localPlayer.transform.position).sqrMagnitude > MaxDistance * MaxDistance)
                return;

            if (!EnsureClip()) return;
            GameObject root = null;
            try
            {
                var playback = PlaybackByPlayer.GetValue(player, _ => new Playback());
                // Rapid swaps should restart the sound, not stack several 1.7s tails.
                if (playback.Root != null) Object.Destroy(playback.Root);

                root = new GameObject("HelOath_SoulSwapSound");
                root.transform.position = destination + Vector3.up * 1f;
                playback.Root = root;

                var source = root.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.clip = clip;
                source.spatialBlend = local ? 0f : 1f;
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                source.minDistance = 3f;
                source.maxDistance = MaxDistance;
                source.dopplerLevel = 0f;
                source.volume = (local ? LocalVolume : RemoteVolume) * AudioMan.GetSFXVolume();
                // Schedule against Unity audio time rather than delaying the swap coroutine.
                // This keeps the actual teleport fixed at animation frame 4.
                float startDelay = Mathf.Max(0f, delay);
                int skippedSamples = delay < 0f
                    ? Mathf.Clamp(Mathf.RoundToInt(-delay * clip.frequency), 0, clip.samples - 1)
                    : 0;
                if (skippedSamples > 0) source.timeSamples = skippedSamples;
                if (startDelay > 0f) source.PlayDelayed(startDelay);
                else source.Play();
                float remaining = clip.length - skippedSamples / (float)clip.frequency;
                Object.Destroy(root, remaining + startDelay + 0.25f);
            }
            catch (Exception error)
            {
                if (root != null) Object.Destroy(root);
                LogFailure(error);
            }
        }

        private static AudioClip CreateClip()
        {
            int expected = HelOathSoulSwapAudioData.Frames * HelOathSoulSwapAudioData.Channels * sizeof(short);
            byte[] pcm = new byte[expected];
            byte[] gzipBytes = Convert.FromBase64String(string.Concat(HelOathSoulSwapAudioData.GzipChunks));
            using (var buffer = new MemoryStream(gzipBytes, false))
            using (var gzip = new GZipStream(buffer, CompressionMode.Decompress))
            {
                int offset = 0;
                while (offset < pcm.Length)
                {
                    int bytesRead = gzip.Read(pcm, offset, pcm.Length - offset);
                    if (bytesRead == 0) throw new InvalidDataException("Truncated embedded Soul Swap sound");
                    offset += bytesRead;
                }

                if (gzip.ReadByte() != -1)
                    throw new InvalidDataException("Oversized embedded Soul Swap sound");
            }

            float[] samples = new float[expected / sizeof(short)];
            for (int i = 0, offset = 0; i < samples.Length; i++, offset += 2)
            {
                short value = (short)(pcm[offset] | (pcm[offset + 1] << 8));
                samples[i] = value / 32768f;
            }

            AudioClip result = AudioClip.Create("HelOath_SoulSwap_Teleport", HelOathSoulSwapAudioData.Frames,
                HelOathSoulSwapAudioData.Channels, HelOathSoulSwapAudioData.SampleRate, false);
            try
            {
                // Select float[] explicitly: current Unity also has a span overload.
                if (SetClipData == null || !(bool)SetClipData.Invoke(result, new object[] { samples, 0 }))
                    throw new InvalidOperationException("Unable to upload embedded Soul Swap audio samples");
                return result;
            }
            catch
            {
                if (result != null) Object.Destroy(result);
                throw;
            }
        }
    }
}