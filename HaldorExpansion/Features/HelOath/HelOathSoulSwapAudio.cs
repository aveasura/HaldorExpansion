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
    // Play the approved teleport sample at the actual instant of the position exchange.
    // This is presentation-only: audio errors cannot affect movement or ghost state.
    internal static class HelOathSoulSwapAudio
    {
        private const float MaxDistance = 48f;
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

        internal static void Play(Player player, Vector3 destination)
        {
            if (player == null || Application.isBatchMode ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;

            bool local = player == Player.m_localPlayer;
            if (!local && Player.m_localPlayer != null &&
                (destination - Player.m_localPlayer.transform.position).sqrMagnitude > MaxDistance * MaxDistance)
                return;

            try
            {
                if (clip == null) clip = CreateClip();
                var playback = PlaybackByPlayer.GetValue(player, _ => new Playback());
                // Rapid swaps should restart the sound, not stack several 1.7s tails.
                if (playback.Root != null) Object.Destroy(playback.Root);

                var root = new GameObject("HelOath_SoulSwapSound");
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
                source.Play();
                Object.Destroy(root, clip.length + 0.25f);
            }
            catch (Exception error)
            {
                if (loggedFailure) return;
                loggedFailure = true;
                HaldorExpansionPlugin.Log?.LogWarning("[Hel Soul Swap Audio] " + error);
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
            // Reflection resolves Unity's float[] overload rather than ReadOnlySpan<float>.
            if (SetClipData == null || !(bool)SetClipData.Invoke(result, new object[] { samples, 0 }))
                throw new InvalidOperationException("Unable to upload embedded Soul Swap audio samples");
            return result;
        }
    }
}