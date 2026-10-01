using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace HaldorExpansion.Features.NornThread
{
    internal static class NornThreadEffects
    {
        private const string BreakRpc = "HE_NornThreadBreak_v1";
        private static readonly FieldInfo CharacterView = AccessTools.Field(typeof(Character), "m_nview");
        private static Material material;
        private static Texture2D texture;
        private static AudioClip threadSnap;

        internal static void Register(Player player)
        {
            if (player == null) return;
            var view = CharacterView?.GetValue(player) as ZNetView;
            if (view == null || !view.IsValid()) return;

            view.Register<Vector3>(BreakRpc, (sender, point) =>
            {
                if (!view.IsValid() || sender != view.GetZDO().GetOwner()) return;
                PlayLocal(player, point);
            });
        }

        internal static void Broadcast(Player player)
        {
            if (player == null) return;
            Vector3 point = player.GetCenterPoint();
            var view = CharacterView?.GetValue(player) as ZNetView;
            if (view != null && view.IsValid() && view.IsOwner())
            {
                view.InvokeRPC(ZNetView.Everybody, BreakRpc, point);
                return;
            }

            PlayLocal(player, point);
        }

        private static void PlayLocal(Player player, Vector3 point)
        {
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                PlaySpheres(point);
            PlayThreadSnap(player, point);
        }

        private static void PlaySpheres(Vector3 point)
        {
            var root = new GameObject("NornThread_Break");
            root.transform.position = point;

            // Main layer: large crimson/orange spheres travelling roughly 3 metres
            // away from the player. They are soft billboards, not sparks or streaks.
            var ps = root.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.66f, 0.90f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.34f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.8f, 5.4f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.22f, 0.08f, 1f),
                new Color(0.72f, 0.025f, 0.015f, 0.98f));
            main.maxParticles = 240;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled = false;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.28f;
            shape.radiusThickness = 1f;

            // Sphere shape + startSpeed already provides the radial 3 m burst.
            // Do not use VelocityOverLifetime here: Unity requires X/Y/Z curves to share
            // the same MinMaxCurve mode, and the previous Y-only TwoConstants setup
            // aborted the particle effect before it could render.

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            var sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.35f),
                new Keyframe(0.08f, 1f),
                new Keyframe(0.72f, 0.82f),
                new Keyframe(1f, 0f));
            size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.42f, 0.16f), 0f),
                    new GradientColorKey(new Color(0.92f, 0.07f, 0.025f), 0.55f),
                    new GradientColorKey(new Color(0.42f, 0.012f, 0.008f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.05f),
                    new GradientAlphaKey(0.92f, 0.64f),
                    new GradientAlphaKey(0f, 1f)
                });
            fade.color = gradient;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = GetMaterial();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            ps.Play();
            ps.Emit(132);

            // Smaller inner burst makes the break feel like a thread snapping at the body
            // before the larger spheres fly away.
            var innerObject = new GameObject("NornThread_InnerBreak");
            innerObject.transform.SetParent(root.transform, false);
            var inner = innerObject.AddComponent<ParticleSystem>();
            inner.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var innerMain = inner.main;
            innerMain.loop = false;
            innerMain.playOnAwake = false;
            innerMain.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.48f);
            innerMain.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.17f);
            innerMain.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 2.8f);
            innerMain.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.34f, 0.12f, 1f),
                new Color(0.82f, 0.025f, 0.012f, 1f));
            innerMain.maxParticles = 128;
            innerMain.simulationSpace = ParticleSystemSimulationSpace.World;
            var innerEmission = inner.emission;
            innerEmission.enabled = false;
            var innerShape = inner.shape;
            innerShape.shapeType = ParticleSystemShapeType.Sphere;
            innerShape.radius = 0.20f;
            var innerFade = inner.colorOverLifetime;
            innerFade.enabled = true;
            var innerGradient = new Gradient();
            innerGradient.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.55f, 0.24f), 0f), new GradientColorKey(new Color(0.68f, 0.018f, 0.01f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            innerFade.color = innerGradient;
            var innerRenderer = inner.GetComponent<ParticleSystemRenderer>();
            innerRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            innerRenderer.sharedMaterial = GetMaterial();
            innerRenderer.shadowCastingMode = ShadowCastingMode.Off;
            innerRenderer.receiveShadows = false;
            inner.Play();
            inner.Emit(54);

            // Slower, larger embers make the break readable even in a busy fight.
            var emberObject = new GameObject("NornThread_RedEmbers");
            emberObject.transform.SetParent(root.transform, false);
            var embers = emberObject.AddComponent<ParticleSystem>();
            embers.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var emberMain = embers.main;
            emberMain.loop = false;
            emberMain.playOnAwake = false;
            emberMain.startLifetime = new ParticleSystem.MinMaxCurve(0.80f, 1.10f);
            emberMain.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.30f);
            emberMain.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 3.1f);
            emberMain.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.95f, 0.055f, 0.018f, 0.90f),
                new Color(0.48f, 0.012f, 0.008f, 0.80f));
            emberMain.maxParticles = 96;
            emberMain.simulationSpace = ParticleSystemSimulationSpace.World;
            var emberEmission = embers.emission;
            emberEmission.enabled = false;
            var emberShape = embers.shape;
            emberShape.shapeType = ParticleSystemShapeType.Sphere;
            emberShape.radius = 0.34f;
            emberShape.radiusThickness = 1f;
            var emberFade = embers.colorOverLifetime;
            emberFade.enabled = true;
            var emberGradient = new Gradient();
            emberGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.20f, 0.07f), 0f),
                    new GradientColorKey(new Color(0.62f, 0.02f, 0.012f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.9f, 0.08f),
                    new GradientAlphaKey(0f, 1f)
                });
            emberFade.color = emberGradient;
            var emberRenderer = embers.GetComponent<ParticleSystemRenderer>();
            emberRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            emberRenderer.sharedMaterial = GetMaterial();
            emberRenderer.shadowCastingMode = ShadowCastingMode.Off;
            emberRenderer.receiveShadows = false;
            embers.Play();
            embers.Emit(46);

            UnityEngine.Object.Destroy(root, 1.45f);
        }

        private static void PlayThreadSnap(Player player, Vector3 point)
        {
            if (threadSnap == null) threadSnap = BuildThreadSnap();
            if (threadSnap == null) return;

            bool local = player != null && player == Player.m_localPlayer;
            var root = new GameObject("NornThread_SnapSound");
            root.transform.position = point;
            var source = root.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.clip = threadSnap;
            source.spatialBlend = local ? 0.08f : 0.95f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 2.5f;
            source.maxDistance = 22f;
            source.dopplerLevel = 0f;
            source.pitch = 1f;
            source.volume = (local ? 0.74f : 0.42f) * AudioMan.GetSFXVolume();
            source.Play();
            UnityEngine.Object.Destroy(root, threadSnap.length + 0.15f);
        }

        private static AudioClip BuildThreadSnap()
        {
            // Pure resonant pluck/snap: no white-noise bed. A short high string transient
            // followed by a low, quickly dying resonance reads closer to a thread breaking.
            const int sampleRate = 44100;
            const float duration = 0.48f;
            int count = (int)(sampleRate * duration);
            var samples = new float[count];
            double phaseA = 0d, phaseB = 0d, phaseC = 0d;

            for (int i = 0; i < count; i++)
            {
                double t = (double)i / sampleRate;
                double freqA = 1280d - 520d * Math.Min(1d, t / 0.12d);
                double freqB = 690d - 180d * Math.Min(1d, t / 0.18d);
                double freqC = 235d;
                phaseA += 2d * Math.PI * freqA / sampleRate;
                phaseB += 2d * Math.PI * freqB / sampleRate;
                phaseC += 2d * Math.PI * freqC / sampleRate;

                double transient = Math.Sin(phaseA) * Math.Exp(-t * 26d) * 0.78d
                    + Math.Sin(phaseA * 1.93d + 0.45d) * Math.Exp(-t * 34d) * 0.28d;
                double stringTone = Math.Sin(phaseB) * Math.Exp(-t * 12d) * 0.36d;
                double body = Math.Sin(phaseC) * Math.Exp(-t * 9d) * 0.15d;
                double attack = Math.Min(1d, t / 0.0025d);
                double fade = Math.Min(1d, (duration - t) / 0.06d);
                double value = (transient + stringTone + body) * attack * fade;
                samples[i] = (float)Math.Max(-0.92d, Math.Min(0.92d, value));
            }

            var clip = AudioClip.Create("NornThread_ThreadSnap", count, 1, sampleRate, false);
            AccessTools.Method(typeof(AudioClip), "SetData", new[] { typeof(float[]), typeof(int) })
                .Invoke(clip, new object[] { samples, 0 });
            return clip;
        }

        private static Material GetMaterial()
        {
            if (material != null) return material;
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Hidden/Internal-Colored");
            if (shader == null) return null;

            texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            texture.name = "NornThread_SoftSphere";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[64 * 64];
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float dx = (x - 31.5f) / 31.5f;
                float dy = (y - 31.5f) / 31.5f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - r);
                a = a * a * (3f - 2f * a);
                pixels[y * 64 + x] = new Color(1f, 1f, 1f, a);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            material = new Material(shader) { name = "NornThread_SoftSphereMaterial", mainTexture = texture };
            return material;
        }
    }

    [HarmonyPatch(typeof(Character), "Awake")]
    internal static class NornThreadNetworkPatch
    {
        private static void Postfix(Character __instance)
        {
            var player = __instance as Player;
            if (player != null) NornThreadEffects.Register(player);
        }
    }
}
