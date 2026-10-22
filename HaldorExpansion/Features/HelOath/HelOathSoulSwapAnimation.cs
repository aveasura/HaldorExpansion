using System;
using System.IO;
using System.Reflection;
using HaldorExpansion.Features.Cestus;
using UnityEngine;
using UnityEngine.Rendering;

namespace HaldorExpansion.Features.HelOath
{
    // Loads the Unity-baked clip, while the persistent point-emote controller is
    // owned by Facebreaker's shared animation bridge. No stacked overrides.
    internal static class HelOathSoulSwapAnimation
    {
        private const string Resource = "HaldorExpansion.Assets.haldor_soulswap";
        internal const float TeleportDelay = 3f / 24f; // Frame 4, with frame 1 at t=0.
        internal const float Duration = 8f / 24f; // Frame 9 (0.3333s).
        private static bool attempted;
        private static bool warned;
        private static AssetBundle bundle;
        private static AnimationClip clip;

        internal static bool TryBegin(Player player)
        {
            return LoadClip() && CestusFacebreakerAnimation.TryBeginSoulSwap(player, clip);
        }

        internal static void BeginRemote(Player player)
        {
            if (LoadClip())
                CestusFacebreakerAnimation.BeginRemoteSoulSwap(player, clip, Duration);
        }

        internal static void End(Player player) => CestusFacebreakerAnimation.EndSoulSwap(player);
        internal static void EndRemote(Player player) => CestusFacebreakerAnimation.EndRemoteSoulSwap(player);

        private static bool LoadClip()
        {
            if (attempted) return clip != null;
            attempted = true;
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null || Application.isBatchMode)
                return false;
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(Resource))
                {
                    if (stream == null) throw new FileNotFoundException(Resource);
                    using (var buffer = new MemoryStream())
                    {
                        stream.CopyTo(buffer);
                        bundle = AssetBundle.LoadFromMemory(buffer.ToArray());
                    }
                }

                if (bundle == null) throw new InvalidOperationException("Cannot open Soul Swap AssetBundle");
                var clips = bundle.LoadAllAssets<AnimationClip>();
                if (clips == null || clips.Length == 0)
                    clips = bundle.LoadAssetWithSubAssets<AnimationClip>("assets/aveasura_soulswap.fbx");
                if (clips != null)
                {
                    foreach (var candidate in clips)
                    {
                        if (candidate == null) continue;
                        if (candidate.name == "SoulSwap" || candidate.name == "aveasura_soulswap")
                        {
                            clip = candidate;
                            break;
                        }
                    }

                    // Blender FBX imports can be named "Scene" by Unity.
                    if (clip == null)
                        foreach (var candidate in clips)
                            if (candidate != null && candidate.name == "Scene" &&
                                candidate.length > 0.28f && candidate.length < 0.39f)
                            {
                                clip = candidate;
                                break;
                            }

                    // A renamed single imported clip is still unambiguous.
                    if (clip == null && clips.Length == 1) clip = clips[0];
                }

                if (clip == null) throw new InvalidOperationException("SoulSwap AnimationClip was not found");
                return true;
            }
            catch (Exception ex)
            {
                if (!warned)
                {
                    warned = true;
                    HaldorExpansionPlugin.Log?.LogWarning(
                        "[Soul Swap Animation] Clip unavailable; using instant swap: " + ex.Message);
                }

                return false;
            }
        }
    }
}