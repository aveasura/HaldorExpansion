using System;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace HaldorExpansion.Features.HelOath
{
    // Overlapping, expanding clouds: no pointed tips or vertical billboard strands.
    internal sealed class HelOathGhostAura : IDisposable
    {
        private const int TextureWidth = 128;
        private const int TextureHeight = 128;
        private const int MaxParticlesPerLayer = 128;
        private readonly ParticleSystem[] layers = new ParticleSystem[3];
        private readonly float[] rates = new float[3];
        private readonly float[] baseOpacities = new float[3];
        private static readonly Color SwapCloudTint = new Color(0.56f, 0.94f, 1f, 1f);
        private static readonly Color SwapTipTint = new Color(0.28f, 0.72f, 0.98f, 1f);
        private static Texture2D sharedTexture;
        private static int sharedTextureUsers;
        private Texture2D texture;
        private bool holdsSharedTexture;
        private Material material;
        private GameObject root;
        private Transform owner;
        private bool started;
        private bool disposed;
        private float fade;
        private bool swapTintActive;
        private float swapTintAt;
        private float swapHold;
        private float swapFade;
        private float lastSwapBlend;
        internal bool Released { get; private set; }

        internal void Initialize(Transform parent, Transform owner)
        {
            this.owner = owner;
            if (!HelOathGhostVisualSettings.AuraEnabled) return;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null || !shader.isSupported) return;
            if (sharedTexture == null) sharedTexture = CreateSoftCloudTexture();
            texture = sharedTexture;
            sharedTextureUsers++;
            holdsSharedTexture = true;
            material = new Material(shader)
            {
                name = "HE_GhostSoulVeil",
                mainTexture = texture,
                // Hide even a prewarmed emitter until the body's first fade update.
                color = FadeColor(0f)
            };
            root = new GameObject("HelGhost_RisingSoulAura");
            root.transform.SetParent(parent, false);
            CreateLayer(0, false);
            CreateLayer(1, true);
            CreateLayer(2, false, true);
        }

        private Texture2D CreateSoftCloudTexture()
        {
            var result = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, true, false)
            {
                name = "HE_SoftRisingSoul",
                filterMode = FilterMode.Trilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            // Track ownership before uploading pixels, so failed construction is cleaned up too.
            texture = result;
            var pixels = new Color[TextureWidth * TextureHeight];
            for (int y = 0; y < TextureHeight; y++)
            for (int x = 0; x < TextureWidth; x++)
            {
                float u = x / (float)(TextureWidth - 1);
                float v = y / (float)(TextureHeight - 1);
                // Asymmetric overlapping lobes with broad, continuous falloff. The border
                // is exactly transparent: no rectangular edges, even after mip filtering.
                float lobes = CloudLobe(u, v, 0.40f, 0.43f, 0.36f, 0.37f)
                              + 0.65f * CloudLobe(u, v, 0.62f, 0.53f, 0.30f, 0.34f)
                              + 0.45f * CloudLobe(u, v, 0.43f, 0.66f, 0.28f, 0.29f);
                float silk = 0.65f + 0.35f * Mathf.PerlinNoise(u * 3.1f + 13f, v * 3.4f + 29f);
                pixels[y * TextureWidth + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(lobes) * silk);
            }

            result.SetPixels(pixels);
            result.Apply(true, true);
            return result;
        }

        private static float CloudLobe(float u, float v, float x, float y, float rx, float ry)
        {
            float dx = (u - x) / rx;
            float dy = (v - y) / ry;
            float density = Mathf.Clamp01(1f - dx * dx - dy * dy);
            return density * density * density;
        }

        private static Color FadeColor(float alpha)
        {
            float brightness = Mathf.Clamp01(HelOathGhostVisualSettings.AuraBrightness);
            return new Color(brightness, brightness, brightness, Mathf.Clamp01(alpha));
        }

        private void CreateLayer(int index, bool veil, bool ground = false)
        {
            var go = new GameObject(ground ? "SoulGroundMist" : veil ? "SoftSoulVeil" : "RisingSoulClouds");
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = Vector3.up * (ground
                ? HelOathGhostVisualSettings.AuraGroundCenterY
                : HelOathGhostVisualSettings.AuraEmitterCenterY);
            var ps = go.AddComponent<ParticleSystem>();
            layers[index] = ps;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            float life = Mathf.Max(0.1f, HelOathGhostVisualSettings.AuraLifetime) * (veil ? 1.12f : 1f);
            main.loop = true;
            main.playOnAwake = false;
            main.duration = life * 1.15f;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.85f, life * 1.15f);
            main.startSpeed = 0f;
            main.startColor = Color.white; // Tint only once, in colorOverLifetime.
            main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            main.startSize3D = true;
            float width = Mathf.Max(0.01f, HelOathGhostVisualSettings.AuraWidth) * (veil ? 1.75f : 1f);
            float length = Mathf.Max(0.01f, HelOathGhostVisualSettings.AuraLength) * (veil ? 1.2f : 1f);
            main.startSizeX = new ParticleSystem.MinMaxCurve(width * 0.8f, width * 1.15f);
            main.startSizeY = new ParticleSystem.MinMaxCurve(length * 0.85f, length * 1.15f);
            main.startSizeZ = 1f;
            main.maxParticles = MaxParticlesPerLayer;
            main.gravityModifier = 0f;
            // Attached aura follows the ghost: no long torn trail on a sprint or recall.
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var emission = ps.emission;
            rates[index] = Mathf.Max(0f, HelOathGhostVisualSettings.AuraParticlesPerSecond)
                           * (ground ? Mathf.Max(0f, HelOathGhostVisualSettings.AuraGroundRateMultiplier)
                               : veil ? Mathf.Max(0f, HelOathGhostVisualSettings.AuraVeilRateMultiplier) : 1f);
            emission.rateOverTime = rates[index];
            emission.rateOverDistance = 0f;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            float radius = Mathf.Max(0.01f, HelOathGhostVisualSettings.AuraEmitterRadius);
            // Continuous volumes, not bone-mounted spots. Equal X/Z coverage wraps
            // the silhouette from the front/back as well as from the sides.
            float height = Mathf.Max(0.01f, ground
                ? HelOathGhostVisualSettings.AuraGroundHeight
                : HelOathGhostVisualSettings.AuraEmitterHeight);
            shape.scale = new Vector3(radius * 2f, height, radius * 2f);
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            float rise = Mathf.Max(0f, ground
                ? HelOathGhostVisualSettings.AuraGroundRiseSpeed
                : HelOathGhostVisualSettings.AuraRiseSpeed) * (veil ? 0.8f : 1f);
            // Same MinMaxCurve mode for all three axes, as required by Unity.
            velocity.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
            velocity.y = new ParticleSystem.MinMaxCurve(rise * 0.85f, rise * 1.15f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
            var noise = ps.noise;
            noise.enabled = HelOathGhostVisualSettings.AuraNoiseStrength > 0f;
            noise.strength = Mathf.Max(0f, HelOathGhostVisualSettings.AuraNoiseStrength);
            noise.frequency = Mathf.Max(0.01f, HelOathGhostVisualSettings.AuraNoiseFrequency);
            noise.scrollSpeed = Mathf.Max(0f, HelOathGhostVisualSettings.AuraNoiseScrollSpeed);
            noise.octaveCount = 1;
            noise.quality = ParticleSystemNoiseQuality.Medium;
            noise.damping = true;
            float opacity = Mathf.Clamp01(HelOathGhostVisualSettings.AuraOpacity
                                          * (ground ? HelOathGhostVisualSettings.AuraGroundOpacityMultiplier
                                              : veil ? HelOathGhostVisualSettings.AuraVeilOpacityMultiplier : 1f));
            baseOpacities[index] = opacity;
            ApplyLayerTint(index, 0f);
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.separateAxes = true;
            size.x = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.75f, 1f, 1.65f));
            size.y = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.80f, 1f, 1.50f));
            size.z = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 1f));
            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-0.16f, 0.16f);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        internal void Start()
        {
            if (started || root == null || !root.activeInHierarchy) return;
            started = true;
            foreach (var ps in layers)
                if (ps != null)
                    ps.Play(false);
        }

        // Player Soul Swap keeps its emitters between casts, completely idle.
        // Ghost departure still uses SetFade/Released and owns its normal cleanup.
        internal void Suspend()
        {
            if (root == null || Released || disposed) return;
            foreach (var ps in layers)
                if (ps != null)
                    ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            root.SetActive(false);
            started = false;
        }

        internal void Resume()
        {
            if (root == null || Released || disposed) return;
            root.SetActive(true);
            Start();
        }

        // Temporarily use the icy-blue Soul Swap palette on the same three-layer
        // aura. Each body owns its particle systems; no shared ghost assets are modified.
        internal void FlashSwap(float hold, float fadeOut)
        {
            if (material == null || Released) return;
            swapTintActive = true;
            swapTintAt = Time.time;
            swapHold = Mathf.Max(0f, hold);
            swapFade = Mathf.Max(0.01f, fadeOut);
            ApplySwapBlend(1f);
        }

        private void UpdateSwapTint()
        {
            if (!swapTintActive) return;
            float elapsed = Time.time - swapTintAt - swapHold;
            float blend = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / swapFade));
            if (Mathf.Abs(blend - lastSwapBlend) > 0.02f || blend <= 0f) ApplySwapBlend(blend);
            if (blend <= 0f) swapTintActive = false;
        }

        private void ApplySwapBlend(float blend)
        {
            lastSwapBlend = blend;
            for (int i = 0; i < layers.Length; i++)
                if (layers[i] != null)
                    ApplyLayerTint(i, blend);
        }

        private void ApplyLayerTint(int index, float blend)
        {
            var gradient = new Gradient();
            float opacity = Mathf.Clamp01(baseOpacities[index] * Mathf.Lerp(1f, 2.7f, blend));
            Color core = Color.Lerp(HelOathGhostVisualSettings.AuraTint, SwapCloudTint, blend);
            Color tip = Color.Lerp(HelOathGhostVisualSettings.AuraTipTint, SwapTipTint, blend);
            gradient.SetKeys(new[]
            {
                new GradientColorKey(core, 0f),
                new GradientColorKey(core, 0.3f),
                new GradientColorKey(tip, 1f)
            }, new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(opacity * 0.20f, 0.10f),
                new GradientAlphaKey(opacity * 0.80f, 0.24f),
                new GradientAlphaKey(opacity, 0.38f),
                new GradientAlphaKey(opacity * 0.75f, 0.56f),
                new GradientAlphaKey(opacity * 0.28f, 0.78f),
                new GradientAlphaKey(opacity * 0.06f, 0.92f),
                new GradientAlphaKey(0f, 1f)
            });
            var color = layers[index].colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        internal void SetFade(float amount, bool ending)
        {
            if (material == null || Released) return;
            UpdateSwapTint();
            if (ending && fade > 0f)
            {
                // Transfer lifetime to the detached root before the body can dispose it.
                var release = root.AddComponent<HelOathGhostSoulRelease>();
                root.transform.SetParent(null, true);
                release.Initialize(this, owner);
                Released = true;
                return;
            }

            fade = Mathf.Clamp01(amount);
            material.color = FadeColor(fade);
        }

        internal void UpdateRelease(float alpha, float emissionAmount)
        {
            if (material == null) return;
            material.color = FadeColor(fade * alpha);
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] == null) continue;
                var emission = layers[i].emission;
                emission.rateOverTime = rates[i] * Mathf.Clamp01(emissionAmount);
            }
        }

        public void Dispose()
        {
            // Released effects are owned by their controller, not by the vanished body.
            if (!Released) DestroyResources();
        }

        internal void DestroyResources()
        {
            if (disposed) return;
            disposed = true;
            GameObject oldRoot = root;
            root = null;
            if (oldRoot != null)
            {
                oldRoot.SetActive(false);
                Object.Destroy(oldRoot);
            }

            if (material != null) Object.Destroy(material);
            // Detached souls hold their lease until their own effect finishes.
            if (holdsSharedTexture)
            {
                holdsSharedTexture = false;
                if (--sharedTextureUsers == 0)
                {
                    if (sharedTexture != null) Object.Destroy(sharedTexture);
                    sharedTexture = null;
                }
            }
            else if (texture != null) Object.Destroy(texture); // Failed texture construction.

            material = null;
            texture = null;
        }
    }
}