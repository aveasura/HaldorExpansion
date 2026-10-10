using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace HaldorExpansion.Features.Berserker
{
    // Cosmetic network-synced aura for Wrath I-V. The state itself still lives in
    // BerserkerRuntime/BerserkerState; this class only mirrors the current stage so
    // every client can render the same escalating visual language around the player.
    internal static class BerserkerWrathEffects
    {
        private sealed class VisualState
        {
            internal int Stage = -1;
            internal GameObject Root;
        }

        private static readonly ConditionalWeakTable<Player, VisualState> Visuals =
            new ConditionalWeakTable<Player, VisualState>();

        private static readonly System.Reflection.FieldInfo NetworkViewField =
            AccessTools.Field(typeof(Character), "m_nview");

        private static Material auraMaterial;
        private static Material lineMaterial;
        private static Texture2D auraTexture;
        private static Texture2D lineTexture;
        private static AudioClip stageFallback;

        internal static bool CanRender => SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;

        internal static void Register(Player player, ZNetView view)
        {
            if (player == null || view == null || !view.IsValid()) return;
            if (view.IsOwner()) view.GetZDO().Set(BerserkerIds.WrathStageZdoKey, 0);
        }

        internal static void Sync(Player player, int stage)
        {
            var view = View(player);
            if (view == null || !view.IsValid() || !view.IsOwner()) return;
            stage = Mathf.Clamp(stage, 0, BerserkerState.MaxWrathStage);
            if (view.GetZDO().GetInt(BerserkerIds.WrathStageZdoKey) != stage)
                view.GetZDO().Set(BerserkerIds.WrathStageZdoKey, stage);
            UpdatePlayer(player);
        }

        internal static void UpdatePlayer(Player player)
        {
            if (!CanRender || player == null) return;
            ZNetView view = View(player);
            int stage = view != null && view.IsValid() && !player.IsDead()
                ? Mathf.Clamp(view.GetZDO().GetInt(BerserkerIds.WrathStageZdoKey), 0, BerserkerState.MaxWrathStage)
                : 0;

            VisualState state;
            if (stage <= 0 && !Visuals.TryGetValue(player, out state)) return;
            state = Visuals.GetValue(player, ignored => new VisualState());
            if (state.Stage == stage && (stage <= 0 || state.Root != null)) return;

            int previous = state.Stage;
            if (state.Root != null)
            {
                var oldAura = state.Root.GetComponent<BerserkerWrathAura>();
                if (oldAura != null) oldAura.BeginFadeOut(0.45f);
                else Object.Destroy(state.Root);
            }

            state.Root = null;
            state.Stage = stage;
            if (stage <= 0) return;

            var root = new GameObject("Berserker_WrathAura_" + stage);
            root.transform.SetParent(player.transform, false);
            root.transform.localPosition = Vector3.zero;
            root.AddComponent<BerserkerWrathAura>().Initialize(stage, GetAuraMaterial(), GetLineMaterial());
            state.Root = root;

            if (stage > previous)
            {
                PlayStagePulse(player.GetCenterPoint(), stage);
                PlayStageSound(player, stage);
            }
        }

        internal static LineRenderer MakeSoftLine(Transform parent, float width, int count, bool loop)
        {
            var go = new GameObject("Berserker_WrathLine");
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = count;
            line.loop = loop;
            line.sharedMaterial = GetLineMaterial();
            line.widthMultiplier = width;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.numCornerVertices = 4;
            line.numCapVertices = loop ? 0 : 5;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private static ZNetView View(Character character)
        {
            if (character == null) return null;
            var field = NetworkViewField;
            return field != null ? field.GetValue(character) as ZNetView : null;
        }

        private static Material GetAuraMaterial()
        {
            if (auraMaterial != null) return auraMaterial;
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Hidden/Internal-Colored");
            if (shader == null) return null;

            auraTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            auraTexture.name = "Berserker_WrathParticle";
            auraTexture.filterMode = FilterMode.Bilinear;
            auraTexture.wrapMode = TextureWrapMode.Clamp;
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

            auraTexture.SetPixels(pixels);
            auraTexture.Apply();
            auraMaterial = new Material(shader) { name = "Berserker_WrathAuraMaterial", mainTexture = auraTexture };
            return auraMaterial;
        }

        private static Material GetLineMaterial()
        {
            if (lineMaterial != null) return lineMaterial;
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Hidden/Internal-Colored");
            if (shader == null) return GetAuraMaterial();

            const int width = 64;
            const int height = 16;
            lineTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            lineTexture.name = "Berserker_WrathRibbon";
            lineTexture.filterMode = FilterMode.Bilinear;
            lineTexture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float dy = Mathf.Abs((y - (height - 1) * 0.5f) / ((height - 1) * 0.5f));
                float a = Mathf.Clamp01(1f - dy);
                a = a * a * (3f - 2f * a);
                for (int x = 0; x < width; x++) pixels[y * width + x] = new Color(1f, 1f, 1f, a);
            }

            lineTexture.SetPixels(pixels);
            lineTexture.Apply();
            lineMaterial = new Material(shader) { name = "Berserker_WrathLineMaterial", mainTexture = lineTexture };
            return lineMaterial;
        }

        private static void PlayStagePulse(Vector3 point, int stage)
        {
            var root = new GameObject("Berserker_WrathStagePulse_" + stage);
            root.transform.position = point;
            var ps = root.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = stage >= 5 ? 0.90f : 0.72f;
            main.startSize = 0.10f + stage * 0.014f;
            main.startSpeed = 0.70f + stage * 0.08f;
            main.startColor = stage >= 5
                ? new Color(1f, 0.26f, 0.08f, 0.88f)
                : new Color(0.86f, 0.10f, 0.04f, 0.46f + stage * 0.08f);
            main.maxParticles = 72;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.28f + stage * 0.045f;
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(stage >= 5 ? new Color(1f, 0.72f, 0.34f) : new Color(1f, 0.38f, 0.10f), 0f),
                    new GradientColorKey(new Color(0.18f, 0.01f, 0.01f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(stage >= 5 ? 0.95f : 0.75f, 0.18f),
                    new GradientAlphaKey(0f, 1f)
                });
            fade.color = gradient;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GetAuraMaterial();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            ps.Play();
            ps.Emit(stage >= 5 ? 34 : 14 + stage * 4);
            Object.Destroy(root, stage >= 5 ? 1.10f : 0.95f);
        }

        private static void PlayStageSound(Player player, int stage)
        {
            if (player == null) return;
            if (stageFallback == null) stageFallback = BuildStageFallback();
            AudioClip fallback = stageFallback;
            AudioClip primary = FindStageSound();
            if (fallback == null && primary == null) return;

            bool local = player == Player.m_localPlayer;
            Vector3 point = player.GetCenterPoint() + Vector3.up * 0.55f;
            var root = new GameObject("Berserker_WrathSound_" + stage);
            root.transform.position = point;
            float sfx = Mathf.Max(0.01f, AudioMan.GetSFXVolume());
            float lifetime = 1.25f;

            // Guaranteed local cue: a 2D fallback growl/surge so the owner clearly hears every stage.
            if (fallback != null)
            {
                var cue = root.AddComponent<AudioSource>();
                cue.playOnAwake = false;
                cue.clip = fallback;
                cue.spatialBlend = local ? 0f : 0.72f;
                cue.rolloffMode = AudioRolloffMode.Logarithmic;
                cue.minDistance = 3f;
                cue.maxDistance = 26f;
                cue.dopplerLevel = 0f;
                cue.pitch = stage >= 5 ? 0.76f : 1.08f - (stage - 1) * 0.05f;
                cue.volume = Mathf.Min(1f,
                    (local
                        ? (stage >= 5 ? 1.00f : 0.88f + stage * 0.03f)
                        : (stage >= 5 ? 0.48f : 0.24f + stage * 0.035f)) * sfx);
                cue.Play();
                lifetime = Mathf.Max(lifetime, fallback.length / Mathf.Max(0.01f, cue.pitch) + 0.25f);
            }

            // Optional creature layer for extra body. Keep it audible locally too.
            if (primary != null)
            {
                var body = root.AddComponent<AudioSource>();
                body.playOnAwake = false;
                body.clip = primary;
                body.spatialBlend = local ? 0.12f : 0.90f;
                body.rolloffMode = AudioRolloffMode.Logarithmic;
                body.minDistance = 3f;
                body.maxDistance = 32f;
                body.dopplerLevel = 0f;
                body.pitch = stage >= 5 ? 0.84f : 0.96f - (stage - 1) * 0.035f;
                body.volume = Mathf.Min(1f,
                    (local
                        ? (stage >= 5 ? 0.68f : 0.48f + stage * 0.04f)
                        : (stage >= 5 ? 0.32f : 0.16f + stage * 0.03f)) * sfx);
                body.PlayDelayed(fallback != null ? 0.015f : 0f);
                lifetime = Mathf.Max(lifetime,
                    (fallback != null ? 0.015f : 0f) + primary.length / Mathf.Max(0.01f, body.pitch) + 0.25f);
            }

            if (stage >= 5)
            {
                AudioClip secondary = FindAwakenedAccent();
                if (secondary != null)
                {
                    var accent = root.AddComponent<AudioSource>();
                    accent.playOnAwake = false;
                    accent.clip = secondary;
                    accent.spatialBlend = local ? 0.08f : 0.92f;
                    accent.rolloffMode = AudioRolloffMode.Logarithmic;
                    accent.minDistance = 3f;
                    accent.maxDistance = 34f;
                    accent.dopplerLevel = 0f;
                    accent.pitch = 0.86f;
                    accent.volume = Mathf.Min(1f, (local ? 0.58f : 0.24f) * sfx);
                    accent.PlayDelayed(0.05f);
                    lifetime = Mathf.Max(lifetime, 0.05f + secondary.length / accent.pitch + 0.25f);
                }
            }

            Object.Destroy(root, lifetime);
        }

        private static AudioClip FindStageSound()
        {
            string[] names =
            {
                "sfx_fenring_alerted",
                "sfx_abomination_alerted",
                "sfx_blob_attack",
                "sfx_surtling_idle"
            };

            foreach (string name in names)
            {
                var clip = FromPrefab(ZNetScene.instance?.GetPrefab(name));
                if (clip != null) return clip;
            }

            return null;
        }

        private static AudioClip FindAwakenedAccent()
        {
            string[] names = { "sfx_gdking_alerted", "sfx_wraith_alerted", "sfx_dragon_hurt" };
            foreach (string name in names)
            {
                var clip = FromPrefab(ZNetScene.instance?.GetPrefab(name));
                if (clip != null) return clip;
            }

            return null;
        }

        private static AudioClip FromPrefab(GameObject prefab)
        {
            if (prefab == null) return null;
            foreach (var sound in prefab.GetComponentsInChildren<ZSFX>(true))
            {
                var source = sound != null ? sound.GetComponent<AudioSource>() : null;
                if (source != null && source.clip != null) return source.clip;
            }

            foreach (var source in prefab.GetComponentsInChildren<AudioSource>(true))
            {
                if (source != null && source.clip != null) return source.clip;
            }

            return null;
        }

        private static AudioClip BuildStageFallback()
        {
            const int sampleRate = 22050;
            const float duration = 0.58f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float n = Mathf.PerlinNoise(t * 34f, 0.17f) * 2f - 1f;
                float growl = Mathf.Sin(2f * Mathf.PI * (92f - t * 30f) * t);
                float hiss = Mathf.Sin(2f * Mathf.PI * 184f * t) * 0.26f;
                float env = t < 0.12f
                    ? t / 0.12f
                    : Mathf.Clamp01(1f - (t - 0.12f) / (duration - 0.12f));
                data[i] = (growl * 0.58f + hiss * 0.18f + n * 0.12f) * env * 0.55f;
            }

            var clip = AudioClip.Create("BerserkerWrathFallback", samples, 1, sampleRate, false);
            // Invoke the float[] overload through reflection: newer Unity AudioModule
            // also exposes a ReadOnlySpan<float> overload, which cannot be resolved
            // by the .NET Framework 4.8 compiler without netstandard 2.1 types.
            var setData = typeof(AudioClip).GetMethod("SetData", new[] { typeof(float[]), typeof(int) });
            if (setData == null)
            {
                Object.Destroy(clip);
                return null;
            }

            try
            {
                setData.Invoke(clip, new object[] { data, 0 });
                return clip;
            }
            catch (Exception)
            {
                Object.Destroy(clip);
                return null;
            }
        }
    }

    internal sealed class BerserkerWrathAura : MonoBehaviour
    {
        private const int WispPoints = 32;

        private readonly LineRenderer[] wisps = new LineRenderer[2];

        private int stage;
        private float bornAt;
        private bool fadingOut;
        private float fadeStarted;
        private float fadeDuration;

        internal void Initialize(int wrathStage, Material particleMaterial, Material lineMaterial)
        {
            stage = Mathf.Clamp(wrathStage, 1, BerserkerState.MaxWrathStage);
            bornAt = Time.time;

            // Prefer the original Thunderblood wispy-smoke material and animated
            // flipbook. The procedural implementation remains as a fallback.
            if (TryCreateThunderbloodCloudAura()) return;

            float stage01 = stage / (float)BerserkerState.MaxWrathStage;
            float smokeRate = Mathf.Lerp(6.0f, 12.5f, stage01);
            float smokeSize = Mathf.Lerp(0.72f, 1.18f, stage01);
            float emberRate = Mathf.Lerp(2.6f, 6.2f, stage01);
            bool awakened = stage >= 5;

            // Volumetric haze around the whole silhouette.
            CreateSmokePocket(particleMaterial, new Vector3(0f, 0.18f, 0f), smokeRate * 1.10f, smokeSize * 1.08f, 0.52f,
                awakened);
            CreateSmokePocket(particleMaterial, new Vector3(-0.42f, 0.46f, 0.06f), smokeRate * 1.05f, smokeSize * 1.00f,
                0.42f, awakened);
            CreateSmokePocket(particleMaterial, new Vector3(0.42f, 0.46f, 0.06f), smokeRate * 1.05f, smokeSize * 1.00f,
                0.42f, awakened);
            CreateSmokePocket(particleMaterial, new Vector3(0f, 0.86f, 0.36f), smokeRate * 1.12f, smokeSize * 1.16f,
                0.50f, awakened);
            CreateSmokePocket(particleMaterial, new Vector3(0f, 0.94f, -0.36f), smokeRate * 1.12f, smokeSize * 1.16f,
                0.50f, awakened);
            CreateSmokePocket(particleMaterial, new Vector3(-0.38f, 1.30f, 0f), smokeRate * 1.00f, smokeSize * 1.02f,
                0.36f, awakened);
            CreateSmokePocket(particleMaterial, new Vector3(0.38f, 1.30f, 0f), smokeRate * 1.00f, smokeSize * 1.02f,
                0.36f, awakened);
            CreateSmokePocket(particleMaterial, new Vector3(0f, 1.58f, 0f), smokeRate * 0.94f, smokeSize * 0.94f, 0.40f,
                awakened);

            CreateEmberPocket(particleMaterial, new Vector3(0f, 0.32f, 0f), emberRate * 0.80f, 0.052f + stage * 0.004f,
                0.28f, 0.16f + stage * 0.03f);
            CreateEmberPocket(particleMaterial, new Vector3(0f, 1.00f, 0f), emberRate * 0.95f, 0.060f + stage * 0.004f,
                0.34f, 0.18f + stage * 0.03f);
            if (stage >= 3)
            {
                CreateEmberPocket(particleMaterial, new Vector3(-0.22f, 1.24f, 0f), emberRate * 0.80f, 0.066f, 0.18f,
                    0.18f + stage * 0.028f);
                CreateEmberPocket(particleMaterial, new Vector3(0.22f, 1.24f, 0f), emberRate * 0.80f, 0.066f, 0.18f,
                    0.18f + stage * 0.028f);
            }

            // Same silhouette language for all stages: smoky tongues on 1-4, awakened red inferno on 5.
            float tongueRateBase = awakened ? 10.5f : Mathf.Lerp(4.2f, 7.2f, stage01);
            float tongueScale = awakened ? 1.18f : Mathf.Lerp(0.82f, 1.00f, stage01);
            CreateFlameJet(particleMaterial, new Vector3(0f, 0.00f, 0f), tongueRateBase * 1.00f, 0.24f * tongueScale,
                0.46f * tongueScale, 0.46f * tongueScale, awakened);
            CreateFlameJet(particleMaterial, new Vector3(-0.34f, 0.18f, 0f), tongueRateBase * 0.88f,
                0.18f * tongueScale, 0.34f * tongueScale, 0.26f * tongueScale, awakened);
            CreateFlameJet(particleMaterial, new Vector3(0.34f, 0.18f, 0f), tongueRateBase * 0.88f, 0.18f * tongueScale,
                0.34f * tongueScale, 0.26f * tongueScale, awakened);
            CreateFlameJet(particleMaterial, new Vector3(-0.24f, 0.76f, 0.08f), tongueRateBase * 0.82f,
                0.16f * tongueScale, 0.30f * tongueScale, 0.18f * tongueScale, awakened);
            CreateFlameJet(particleMaterial, new Vector3(0.24f, 0.76f, 0.08f), tongueRateBase * 0.82f,
                0.16f * tongueScale, 0.30f * tongueScale, 0.18f * tongueScale, awakened);
            CreateFlameJet(particleMaterial, new Vector3(0f, 0.94f, 0.24f), tongueRateBase * 0.92f, 0.20f * tongueScale,
                0.38f * tongueScale, 0.24f * tongueScale, awakened);
            CreateFlameJet(particleMaterial, new Vector3(0f, 1.00f, -0.24f), tongueRateBase * 0.92f,
                0.20f * tongueScale, 0.38f * tongueScale, 0.24f * tongueScale, awakened);
            CreateFlameJet(particleMaterial, new Vector3(-0.18f, 1.38f, 0f), tongueRateBase * 0.72f,
                0.15f * tongueScale, 0.28f * tongueScale, 0.16f * tongueScale, awakened);
            CreateFlameJet(particleMaterial, new Vector3(0.18f, 1.38f, 0f), tongueRateBase * 0.72f, 0.15f * tongueScale,
                0.28f * tongueScale, 0.16f * tongueScale, awakened);
            CreateFlameJet(particleMaterial, new Vector3(0f, 1.64f, 0f), tongueRateBase * 0.62f, 0.14f * tongueScale,
                0.26f * tongueScale, 0.14f * tongueScale, awakened);

            if (awakened)
                CreateFlameCrown(particleMaterial);

            if (stage >= 1 && stage < 5)
            {
                for (int i = 0; i < wisps.Length; i++)
                {
                    float width = stage >= 4 ? 0.022f : stage >= 2 ? 0.019f : 0.016f;
                    wisps[i] = BerserkerWrathEffects.MakeSoftLine(transform, width, WispPoints, false);
                    wisps[i].startColor = wisps[i].endColor = new Color(0.58f, 0.12f, 0.12f, 0f);
                }
            }
        }

        private bool TryCreateThunderbloodCloudAura()
        {
            var scene = ZNetScene.instance;
            var prefab = scene != null ? scene.GetPrefab("BattleaxeGold_BloodLightning") : null;
            if (prefab == null) return false;
            var attach = prefab.transform.Find("attach");
            var axe = attach != null ? attach.Find("battleaxe") : null;
            var vfx = axe != null ? axe.Find("VFX (1)") : null;
            var clouds = vfx != null ? vfx.Find("Clouds") : null;
            if (clouds == null || clouds.GetComponent<ParticleSystem>() == null) return false;

            // Keep the compact, body-hugging Thunderblood wispy smoke, but
            // give it a vertical, flame-like silhouette rather than a round halo.
            // The lower emitters stay where they were; the rising central plume
            // gets progressively smaller, thinner and more transparent.
            const float volumeScale = 0.70f;
            bool awakened = stage == BerserkerState.MaxWrathStage;
            Vector3[] centers =
            {
                // Feet and ankles (same low coverage as the previous version).
                new Vector3(-0.23f, 0.09f, 0.02f),
                new Vector3(0.23f, 0.09f, 0.02f),
                new Vector3(0.00f, 0.22f, 0.00f),
                // Main volume follows legs, torso and shoulders.
                new Vector3(-0.36f, 0.58f, 0.00f),
                new Vector3(0.36f, 0.58f, 0.00f),
                new Vector3(0.00f, 0.88f, 0.30f),
                new Vector3(0.00f, 0.88f, -0.30f),
                new Vector3(-0.29f, 1.32f, 0.00f),
                new Vector3(0.29f, 1.32f, 0.00f),
                new Vector3(0.00f, 1.54f, 0.00f),
                // Narrow, upward-moving smoke above the helmet. No broad cap.
                new Vector3(0.00f, 1.80f, 0.00f),
                new Vector3(0.00f, 2.06f, 0.00f)
            };

            for (int i = 0; i < centers.Length; i++)
            {
                bool foot = i < 2;
                bool rising = i >= 10;
                bool plumeTip = i == 11;
                var clone = Object.Instantiate(clouds.gameObject, transform);
                clone.name = foot ? "Wrath_ThunderbloodFootCloud_" + i : "Wrath_ThunderbloodCloud_" + i;
                clone.transform.localPosition = centers[i];
                clone.transform.localRotation = Quaternion.identity;
                clone.transform.localScale = Vector3.one;
                clone.SetActive(true);

                var ps = clone.GetComponent<ParticleSystem>();
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main;
                main.playOnAwake = false;
                main.loop = true;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                // The central plume rises above the helmet, while each upper
                // layer tapers instead of turning into another circular cloud.
                main.startLifetime = rising
                    ? new ParticleSystem.MinMaxCurve(0.78f, 1.18f)
                    : new ParticleSystem.MinMaxCurve(0.88f, 1.38f);
                main.startSpeed = rising
                    ? new ParticleSystem.MinMaxCurve(0.03f, 0.11f)
                    : new ParticleSystem.MinMaxCurve(0.05f, 0.16f);
                float sizeScale = foot ? 0.76f : plumeTip ? 0.54f : rising ? 0.76f : 1f;
                main.startSize = new ParticleSystem.MinMaxCurve(
                    (0.56f + stage * 0.10f) * volumeScale * sizeScale,
                    (0.88f + stage * 0.16f) * volumeScale * sizeScale);
                // Preserve the already approved red/ashen palette. Only the
                // upper plume becomes progressively more translucent.
                Color tint = awakened
                    ? new Color(1.00f, 0.34f, 0.29f, 0.44f)
                    : new Color(0.61f, 0.54f, 0.54f, 0.24f + stage * 0.035f);
                if (rising) tint.a *= plumeTip ? 0.60f : 0.82f;
                main.startColor = tint;
                main.maxParticles = foot ? 60 : plumeTip ? 35 : rising ? 48 : 75;

                var emission = ps.emission;
                emission.enabled = true;
                float emissionRate = foot
                    ? (awakened ? 10f : 4.8f + stage * 1.0f)
                    : (awakened ? 14f : 6.5f + stage * 1.5f);
                emission.rateOverTime = rising
                    ? emissionRate * (plumeTip ? 0.40f : 0.65f)
                    : emissionRate;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = rising
                    ? (plumeTip ? 0.18f : 0.25f) * volumeScale
                    : (foot ? 0.32f : i == 2 ? 0.50f : 0.37f) * volumeScale;

                var velocity = ps.velocityOverLifetime;
                velocity.enabled = true;
                velocity.x = new ParticleSystem.MinMaxCurve(rising ? -0.035f : -0.075f, rising ? 0.035f : 0.075f);
                velocity.y = rising
                    ? new ParticleSystem.MinMaxCurve(0.32f, 0.72f)
                    : new ParticleSystem.MinMaxCurve(0.18f, 0.48f);
                velocity.z = new ParticleSystem.MinMaxCurve(rising ? -0.035f : -0.075f, rising ? 0.035f : 0.075f);
                var noise = ps.noise;
                noise.enabled = true;
                noise.strength = rising
                    ? new ParticleSystem.MinMaxCurve(0.10f, 0.19f)
                    : new ParticleSystem.MinMaxCurve(0.12f, 0.23f);
                noise.frequency = 0.70f;
                noise.scrollSpeed = 0.46f;

                var renderer = clone.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                {
                    // Preserve the native WispySmoke_Flipbook material/shader.
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }

                ps.Play();
            }

            // Ground-hugging Thunderblood smoke fills the gap around the boots.
            // It is intentionally separate from the approved torso/head plume:
            // short-lived, low velocity particles never form a second tall cloud.
            CreateThunderbloodFootMist(clouds.gameObject, awakened);

            if (awakened)
            {
                var lightObject = new GameObject("Wrath_RedGlow");
                lightObject.transform.SetParent(transform, false);
                lightObject.transform.localPosition = Vector3.up * 1.02f;
                var glow = lightObject.AddComponent<Light>();
                glow.type = LightType.Point;
                glow.color = new Color(1.00f, 0.29f, 0.24f);
                glow.range = 2.65f;
                glow.intensity = 0.48f;
                glow.shadows = LightShadows.None;
            }

            return true;
        }

        private void CreateThunderbloodFootMist(GameObject sourceCloud, bool awakened)
        {
            // Left/right boot wisps and a subtle ground-level bridge between them.
            // All use the original Thunderblood WispySmoke_Flipbook material.
            Vector3[] positions =
            {
                new Vector3(-0.27f, 0.09f, 0.08f),
                new Vector3(0.27f, 0.09f, 0.08f),
                new Vector3(0.00f, 0.06f, 0.06f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                bool central = i == 2;
                var clone = Instantiate(sourceCloud, transform);
                clone.name = central ? "Wrath_ThunderbloodGroundMist" : "Wrath_ThunderbloodBootMist_" + i;
                clone.transform.localPosition = positions[i];
                clone.transform.localRotation = Quaternion.identity;
                clone.transform.localScale = Vector3.one;
                clone.SetActive(true);

                var ps = clone.GetComponent<ParticleSystem>();
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main;
                main.playOnAwake = false;
                main.loop = true;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.65f, 1.05f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.05f);
                float stageScale = 0.85f + stage * 0.03f;
                main.startSize = new ParticleSystem.MinMaxCurve(
                    (central ? 0.30f : 0.32f) * stageScale,
                    (central ? 0.48f : 0.52f) * stageScale);
                main.startColor = awakened
                    ? new Color(1.00f, 0.38f, 0.32f, 0.46f)
                    : new Color(0.61f, 0.54f, 0.54f, 0.22f + stage * 0.035f);
                main.maxParticles = 45;

                var emission = ps.emission;
                emission.enabled = true;
                emission.rateOverTime = central
                    ? (awakened ? 7f : 2.5f + stage * 0.7f)
                    : (awakened ? 10f : 3.5f + stage * 0.9f);

                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = central ? 0.21f : 0.15f;

                var velocity = ps.velocityOverLifetime;
                velocity.enabled = true;
                velocity.x = new ParticleSystem.MinMaxCurve(-0.075f, 0.075f);
                velocity.y = new ParticleSystem.MinMaxCurve(0.015f, 0.085f);
                velocity.z = new ParticleSystem.MinMaxCurve(-0.075f, 0.075f);

                var noise = ps.noise;
                noise.enabled = true;
                noise.strength = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
                noise.frequency = 0.65f;
                noise.scrollSpeed = 0.35f;

                var renderer = clone.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }

                ps.Play();
            }
        }

        internal void BeginFadeOut(float duration)
        {
            if (fadingOut) return;
            fadingOut = true;
            fadeStarted = Time.time;
            fadeDuration = Mathf.Max(0.15f, duration);
            foreach (var ps in GetComponentsInChildren<ParticleSystem>())
                ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            Destroy(gameObject, fadeDuration + 1.15f);
        }

        private void CreateSmokePocket(Material material, Vector3 localPos, float rate, float size, float radius,
            bool awakened)
        {
            var go = new GameObject(awakened ? "WrathInfernoSmoke" : "WrathSmokePocket");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = awakened ? 2.15f : 1.78f + stage * 0.11f;
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.80f, size * 1.04f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, awakened ? 0.20f : 0.13f);
            main.startColor = awakened
                ? new Color(0.66f, 0.14f, 0.12f, 0.20f)
                : new Color(0.28f, 0.28f, 0.28f, 0.16f + stage * 0.02f);
            main.maxParticles = awakened ? 145 : 120;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.03f, awakened ? 0.18f : 0.12f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
            var force = ps.forceOverLifetime;
            force.enabled = true;
            force.x = new ParticleSystem.MinMaxCurve(-0.015f, 0.015f);
            force.z = new ParticleSystem.MinMaxCurve(-0.015f, 0.015f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.separateAxes = true;
            noise.strengthX = new ParticleSystem.MinMaxCurve(awakened ? 0.12f : 0.08f, awakened ? 0.26f : 0.16f);
            noise.strengthY = new ParticleSystem.MinMaxCurve(awakened ? 0.06f : 0.04f, awakened ? 0.14f : 0.10f);
            noise.strengthZ = new ParticleSystem.MinMaxCurve(awakened ? 0.12f : 0.08f, awakened ? 0.26f : 0.16f);
            noise.frequency = awakened ? 0.64f : 0.52f;
            noise.scrollSpeed = awakened ? 0.54f : 0.36f;
            noise.octaveCount = 2;
            noise.quality = ParticleSystemNoiseQuality.Medium;
            noise.damping = true;
            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-22f, 22f);
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(awakened ? new Color(1f, 0.24f, 0.14f) : new Color(0.42f, 0.40f, 0.40f), 0f),
                    new GradientColorKey(awakened ? new Color(0.20f, 0.03f, 0.03f) : new Color(0.10f, 0.10f, 0.10f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(awakened ? 0.28f : 0.24f + stage * 0.02f, 0.16f),
                    new GradientAlphaKey(awakened ? 0.10f : 0.09f, 0.66f),
                    new GradientAlphaKey(0f, 1f)
                });
            fade.color = gradient;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            ps.Play();
        }

        private void CreateEmberPocket(Material material, Vector3 localPos, float rate, float size, float radius,
            float alpha)
        {
            var go = new GameObject("WrathEmberPocket");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = 0.86f + stage * 0.06f;
            main.startSize = size;
            main.startSpeed = 0.08f + stage * 0.016f;
            main.startColor = stage >= 5
                ? new Color(1f, 0.72f, 0.22f, Mathf.Clamp01(alpha + 0.02f))
                : new Color(0.72f, 0.34f, 0.24f, alpha * 0.72f);
            main.maxParticles = 110;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.y = new ParticleSystem.MinMaxCurve(stage >= 5 ? 0.16f : 0.08f, stage >= 5 ? 0.30f : 0.16f);
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(stage >= 5 ? new Color(1f, 0.94f, 0.48f) : new Color(1f, 0.34f, 0.14f), 0f),
                    new GradientColorKey(new Color(0.22f, 0.03f, 0.02f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(stage >= 5 ? 0.95f : Mathf.Clamp01(alpha + 0.18f), 0.18f),
                    new GradientAlphaKey(0f, 1f)
                });
            fade.color = gradient;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            ps.Play();
        }

        private void CreateFlameJet(Material material, Vector3 localPos, float rate, float sizeMin, float sizeMax,
            float radius, bool awakened)
        {
            var go = new GameObject(awakened ? "WrathInfernoTongue" : "WrathSmokeTongue");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.56f, awakened ? 1.02f : 0.92f);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(awakened ? 0.60f : 0.44f, awakened ? 1.35f : 1.00f);
            main.startColor = awakened
                ? new Color(1f, 0.30f, 0.14f, 0.44f)
                : new Color(0.34f, 0.34f, 0.34f, 0.26f + stage * 0.01f);
            main.maxParticles = awakened ? 110 : 84;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = radius;
            shape.angle = awakened ? 12f : 10f;
            shape.length = 0.05f;
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
            velocity.y = new ParticleSystem.MinMaxCurve(awakened ? 0.55f : 0.36f, awakened ? 1.45f : 1.10f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
            var force = ps.forceOverLifetime;
            force.enabled = true;
            force.x = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);
            force.z = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.separateAxes = true;
            noise.strengthX = new ParticleSystem.MinMaxCurve(awakened ? 0.18f : 0.14f, awakened ? 0.32f : 0.24f);
            noise.strengthY = new ParticleSystem.MinMaxCurve(awakened ? 0.08f : 0.06f, awakened ? 0.18f : 0.14f);
            noise.strengthZ = new ParticleSystem.MinMaxCurve(awakened ? 0.18f : 0.14f, awakened ? 0.32f : 0.24f);
            noise.frequency = awakened ? 1.00f : 0.82f;
            noise.scrollSpeed = awakened ? 0.72f : 0.56f;
            noise.octaveCount = 2;
            noise.quality = ParticleSystemNoiseQuality.Medium;
            noise.damping = true;
            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-45f, 45f);
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.28f);
            sizeCurve.AddKey(0.18f, 1f);
            sizeCurve.AddKey(0.72f, awakened ? 0.72f : 0.62f);
            sizeCurve.AddKey(1f, 0.08f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(awakened ? new Color(1f, 0.92f, 0.54f) : new Color(0.56f, 0.56f, 0.56f), 0f),
                    new GradientColorKey(awakened ? new Color(1f, 0.28f, 0.10f) : new Color(0.34f, 0.34f, 0.34f),
                        0.40f),
                    new GradientColorKey(awakened ? new Color(0.26f, 0.03f, 0.02f) : new Color(0.14f, 0.14f, 0.14f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(awakened ? 0.56f : 0.34f, 0.12f),
                    new GradientAlphaKey(awakened ? 0.24f : 0.16f, 0.60f),
                    new GradientAlphaKey(0f, 1f)
                });
            fade.color = gradient;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.30f;
            renderer.lengthScale = awakened ? 2.20f : 1.75f;
            renderer.cameraVelocityScale = 0f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            ps.Play();
        }

        private void CreateFlameCrown(Material material)
        {
            var go = new GameObject("WrathFlameCrown");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 1.70f, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.42f, 0.74f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.30f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.60f, 1.22f);
            main.startColor = new Color(1f, 0.44f, 0.18f, 0.56f);
            main.maxParticles = 85;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 8f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = 0.20f;
            shape.angle = 10f;
            shape.length = 0.04f;
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.44f, 1.04f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(0.10f, 0.22f);
            noise.frequency = 1.08f;
            noise.scrollSpeed = 0.64f;
            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-28f, 28f);
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(1f, 0.30f, 0.08f), 0.50f),
                    new GradientColorKey(new Color(0.28f, 0.02f, 0.02f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.58f, 0.12f),
                    new GradientAlphaKey(0.22f, 0.58f),
                    new GradientAlphaKey(0f, 1f)
                });
            fade.color = gradient;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.82f;
            renderer.lengthScale = 2.40f;
            renderer.cameraVelocityScale = 0f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            ps.Play();
        }

        private void Update()
        {
            float age = Time.time - bornAt;
            float fade = fadingOut
                ? Mathf.Clamp01(1f - (Time.time - fadeStarted) / Mathf.Max(0.01f, fadeDuration))
                : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / 0.22f));

            float time = Time.time;
            for (int i = 0; i < wisps.Length; i++) UpdateWisp(wisps[i], i, time, fade);
        }

        private void UpdateWisp(LineRenderer line, int index, float time, float fade)
        {
            if (line == null) return;
            Color color = line.startColor;
            color.a = (0.44f + stage * 0.06f) * fade;
            line.startColor = line.endColor = color;

            Vector3 anchor = index == 0
                ? new Vector3(-0.22f, 0.26f, -0.06f)
                : new Vector3(0.22f, 0.26f, 0.06f);
            float swirl = index == 0 ? 1f : -1f;
            for (int i = 0; i < line.positionCount; i++)
            {
                float u = i / (float)(line.positionCount - 1);
                float angle = time * 0.90f * swirl + u * 1.55f + index * 1.1f;
                float orbit = Mathf.Lerp(0.06f, stage >= 4 ? 0.22f : 0.16f, u);
                float side = Mathf.Sin(angle) * orbit;
                float depth = Mathf.Cos(angle) * orbit * 0.70f;
                float y = Mathf.Lerp(anchor.y, stage >= 4 ? 1.74f : 1.52f, u) +
                          Mathf.Sin(time * 1.7f + u * 6.5f + index) * 0.04f;
                line.SetPosition(i, new Vector3(anchor.x + side, y, anchor.z + depth));
            }
        }
    }
}