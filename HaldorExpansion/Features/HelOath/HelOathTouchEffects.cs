using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace HaldorExpansion.Features.HelOath
{
    internal static class HelOathTouchEffects
    {
        private const string TouchKey = "he_hel_touch_stacks";
        private const string BreakRpc = "HE_HelTouchBreak_v1";

        private sealed class VisualState
        {
            internal int Stacks = -1;
            internal GameObject Root;
        }

        private static readonly ConditionalWeakTable<Player, VisualState> Visuals = new ConditionalWeakTable<Player, VisualState>();
        private static Material auraMaterial;
        private static Material softLineMaterial;
        private static Texture2D auraTexture;
        private static Texture2D lineTexture;
        private static bool CanRender => SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;

        internal static void Register(Player player, ZNetView view)
        {
            if (view.IsOwner()) view.GetZDO().Set(TouchKey, 0);
            view.Register<Vector3>(BreakRpc, (sender, point) =>
            {
                if (!view.IsValid() || sender != view.GetZDO().GetOwner()) return;
                if (CanRender) PlayBreak(point);
            });
        }

        internal static void Sync(Player player, int stacks)
        {
            var view = HelOathNetwork.View(player);
            if (view == null || !view.IsValid() || !view.IsOwner()) return;
            stacks = Mathf.Clamp(stacks, 0, HelOathState.MaxTouchStacks);
            if (view.GetZDO().GetInt(TouchKey) != stacks) view.GetZDO().Set(TouchKey, stacks);
            UpdatePlayer(player);
        }

        internal static void BroadcastBreak(Player player)
        {
            var view = HelOathNetwork.View(player);
            Vector3 point = player != null ? player.GetCenterPoint() : Vector3.zero;
            if (view != null && view.IsValid()) view.InvokeRPC(ZNetView.Everybody, BreakRpc, point);
            else if (CanRender) PlayBreak(point);
        }

        internal static void UpdatePlayer(Player player)
        {
            if (!CanRender || player == null) return;
            var view = HelOathNetwork.View(player);
            int stacks = view != null && view.IsValid() && !player.IsDead()
                ? Mathf.Clamp(view.GetZDO().GetInt(TouchKey), 0, HelOathState.MaxTouchStacks)
                : 0;

            VisualState state;
            if (stacks <= 0 && !Visuals.TryGetValue(player, out state)) return;
            state = Visuals.GetValue(player, ignored => new VisualState());
            if (state.Stacks == stacks && (stacks <= 0 || state.Root != null)) return;

            int previous = state.Stacks;
            if (state.Root != null)
            {
                var oldAura = state.Root.GetComponent<HelOathTouchAura>();
                if (oldAura != null) oldAura.BeginFadeOut(0.55f);
                else UnityEngine.Object.Destroy(state.Root);
            }
            state.Root = null;
            state.Stacks = stacks;
            if (stacks <= 0) return;

            var root = new GameObject("HelOath_TouchAura_" + stacks);
            root.transform.SetParent(player.transform, false);
            root.transform.localPosition = Vector3.zero;
            root.AddComponent<HelOathTouchAura>().Initialize(stacks, GetAuraMaterial());
            state.Root = root;

            if (stacks > previous)
            {
                PlayStagePulse(player.GetCenterPoint(), stacks);
                HelOathAudio.PlayTouchStage(player, stacks);
            }
        }

        internal static LineRenderer SmoothLine(Transform parent, float width, int count, bool loop)
        {
            var go = new GameObject("HelTouch_SoftLine");
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = count;
            line.loop = loop;
            line.sharedMaterial = GetSoftLineMaterial();
            line.widthMultiplier = width;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.numCornerVertices = 4;
            line.numCapVertices = loop ? 0 : 5;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private static Material GetAuraMaterial()
        {
            if (auraMaterial != null) return auraMaterial;
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Hidden/Internal-Colored");
            if (shader == null) return HelOathEffects.GetMaterial();

            auraTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            auraTexture.name = "HelTouch_SoftParticle";
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
            auraMaterial = new Material(shader) { name = "HelTouch_SoftAura", mainTexture = auraTexture };
            return auraMaterial;
        }

        private static Material GetSoftLineMaterial()
        {
            if (softLineMaterial != null) return softLineMaterial;
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Hidden/Internal-Colored");
            if (shader == null) return HelOathEffects.GetMaterial();

            const int width = 64;
            const int height = 16;
            lineTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            lineTexture.name = "HelTouch_SoftRibbon";
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
            softLineMaterial = new Material(shader) { name = "HelTouch_SoftRibbonMaterial", mainTexture = lineTexture };
            return softLineMaterial;
        }

        private static void PlayStagePulse(Vector3 point, int stage)
        {
            var root = new GameObject("HelOath_TouchStagePulse_" + stage);
            root.transform.position = point;
            var ps = root.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.65f;
            main.startSize = 0.085f + stage * 0.008f;
            main.startSpeed = 0.48f + stage * 0.055f;
            main.startColor = new Color(0.60f, 0.86f, 1f, 0.42f + stage * 0.055f);
            main.maxParticles = 64;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.26f + stage * 0.035f;
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.24f, 0.56f, 0.72f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.75f, 0.20f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GetAuraMaterial();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            ps.Play();
            ps.Emit(8 + stage * 3);
            UnityEngine.Object.Destroy(root, 0.95f);
        }

        private static void PlayBreak(Vector3 point)
        {
            var root = new GameObject("HelOath_TouchBreak");
            root.transform.position = point;
            var frost = HelOathEffects.Particles(root.transform, GetAuraMaterial(), false, 0.14f, 3.7f, 68, 0.68f);
            var frostMain = frost.main;
            frostMain.startColor = new Color(0.65f, 0.9f, 1f, 0.88f);

            var shadow = HelOathEffects.Particles(root.transform, GetAuraMaterial(), false, 0.52f, 1.75f, 40, 0.82f);
            var shadowMain = shadow.main;
            shadowMain.startColor = new Color(0.018f, 0.032f, 0.052f, 0.68f);
            UnityEngine.Object.Destroy(root, 1.2f);
        }
    }

    internal sealed class HelOathTouchAura : MonoBehaviour
    {
        private const int RingPoints = 96;
        private const int WispPoints = 72;
        private readonly LineRenderer[] rings = new LineRenderer[5];
        private readonly LineRenderer[] halos = new LineRenderer[5];
        private readonly LineRenderer[] wisps = new LineRenderer[4];
        private int stacks;
        private int ringCount;
        private float bornAt;
        private bool fadingOut;
        private float fadeOutStarted;
        private float fadeOutDuration;

        internal void Initialize(int stage, Material material)
        {
            stacks = stage;
            bornAt = Time.time;

            // Slightly larger, lower-density particles read as soft frost instead of pixel sparks.
            CreateBodyMotes(material, 0.24f, 3.0f + stage * 0.9f, 0.068f + stage * 0.004f, 0.24f, 0.24f + stage * 0.040f);
            CreateBodyMotes(material, 0.84f, 2.6f + stage * 1.0f, 0.072f + stage * 0.004f, 0.31f, 0.22f + stage * 0.045f);
            if (stage >= 2)
                CreateBodyMotes(material, 1.42f, 2.3f + stage * 1.05f, 0.070f + stage * 0.004f, 0.29f, 0.23f + stage * 0.045f);

            // Stages I-IV use soft orbits as readable progression. Stage V sheds the rings entirely:
            // only the rising black-frost spirals/mist/snow remain for a cleaner final form.
            ringCount = stage >= 5 ? 0 : stage;
            for (int i = 0; i < ringCount; i++)
            {
                float coreWidth = stage >= 5 ? 0.028f : 0.014f + stage * 0.0018f;
                float haloWidth = coreWidth + (stage >= 5 ? 0.040f : 0.024f);
                halos[i] = HelOathTouchEffects.SmoothLine(transform, haloWidth, RingPoints, true);
                rings[i] = HelOathTouchEffects.SmoothLine(transform, coreWidth, RingPoints, true);
                halos[i].startColor = halos[i].endColor = new Color(0.22f, 0.60f, 0.80f, 0f);
                rings[i].startColor = rings[i].endColor = new Color(0.42f, 0.78f, 0.92f, 0f);
            }

            if (stage >= 3)
            {
                CreateTrail(material, 0.38f, stage >= 5 ? 11f : stage == 4 ? 8f : 5f,
                    stage >= 5 ? 0.125f : stage == 4 ? 0.105f : 0.090f,
                    stage >= 5 ? 1.28f : stage == 4 ? 1.05f : 0.88f, 0.32f);
                CreateTrail(material, 1.12f, stage >= 5 ? 9f : stage == 4 ? 6f : 3.8f,
                    stage >= 5 ? 0.110f : 0.092f,
                    stage >= 5 ? 1.16f : 0.92f, 0.29f);
            }

            if (stage >= 4)
            {
                CreateMist(material, 0.52f, stage >= 5 ? 8f : 5f, stage >= 5 ? 0.82f : 0.64f, 0.38f);
                CreateMist(material, 1.25f, stage >= 5 ? 7f : 4f, stage >= 5 ? 0.72f : 0.58f, 0.34f);
            }

            if (stage >= 5)
            {
                CreateSnow(material, 0.30f, 8f, 0.52f);
                CreateSnow(material, 0.92f, 10f, 0.56f);
                CreateSnow(material, 1.52f, 8f, 0.48f);
                for (int i = 0; i < wisps.Length; i++)
                {
                    wisps[i] = HelOathTouchEffects.SmoothLine(transform, i % 2 == 0 ? 0.030f : 0.023f, WispPoints, false);
                    wisps[i].startColor = wisps[i].endColor = new Color(0.16f, 0.42f, 0.55f, 0f);
                }
            }
        }

        internal void BeginFadeOut(float duration)
        {
            if (fadingOut) return;
            fadingOut = true;
            fadeOutStarted = Time.time;
            fadeOutDuration = Mathf.Max(0.15f, duration);
            foreach (var ps in GetComponentsInChildren<ParticleSystem>())
                ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            UnityEngine.Object.Destroy(gameObject, fadeOutDuration + 1.35f);
        }

        private void CreateBodyMotes(Material material, float y, float rate, float size, float radius, float alpha)
        {
            var go = new GameObject("FrostMotes");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * y;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = 0.90f + stacks * 0.07f;
            main.startSize = size;
            main.startSpeed = 0.075f + stacks * 0.010f;
            main.startColor = new Color(0.72f, 0.92f, 1f, alpha);
            main.maxParticles = 100;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.y = new ParticleSystem.MinMaxCurve(0.055f + stacks * 0.012f);
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(0.82f, 0.96f, 1f), 0f), new GradientColorKey(new Color(0.16f, 0.39f, 0.54f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(Mathf.Clamp01(alpha + 0.16f), 0.23f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            ps.Play();
        }

        private void CreateTrail(Material material, float y, float rate, float size, float lifetime, float radius)
        {
            var go = new GameObject("FrostTrail");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * y;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = lifetime;
            main.startSize = size;
            main.startSpeed = 0.12f;
            main.startColor = new Color(0.68f, 0.90f, 1f, stacks >= 5 ? 0.78f : 0.58f);
            main.maxParticles = 140;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.20f, 0.52f, 0.70f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.78f, 0.20f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            ps.Play();
        }

        private void CreateMist(Material material, float y, float rate, float size, float radius)
        {
            var go = new GameObject("BlackFrostMist");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * y;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = stacks >= 5 ? 1.45f : 1.15f;
            main.startSize = size;
            main.startSpeed = 0.13f;
            main.startColor = new Color(0.014f, 0.028f, 0.045f, stacks >= 5 ? 0.52f : 0.34f);
            main.maxParticles = 110;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.y = new ParticleSystem.MinMaxCurve(0.08f, 0.19f);
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(0.025f, 0.05f, 0.072f), 0f), new GradientColorKey(new Color(0.005f, 0.014f, 0.025f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(stacks >= 5 ? 0.60f : 0.40f, 0.32f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            ps.Play();
        }

        private void CreateSnow(Material material, float y, float rate, float radius)
        {
            var go = new GameObject("BlackSnow");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * y;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = 1.05f;
            main.startSize = 0.080f;
            main.startSpeed = 0.22f;
            main.startColor = new Color(0.80f, 0.95f, 1f, 0.82f);
            main.maxParticles = 140;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.y = new ParticleSystem.MinMaxCurve(0.09f, 0.28f);
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.35f, 0.70f, 0.84f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.88f, 0.18f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            ps.Play();
        }

        private void Update()
        {
            float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Time.time - bornAt) / 0.78f));
            float fadeOut = 1f;
            if (fadingOut)
                fadeOut = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Time.time - fadeOutStarted) / fadeOutDuration));
            float visibility = fadeIn * fadeOut;

            for (int ring = 0; ring < ringCount; ring++)
            {
                if (rings[ring] == null || halos[ring] == null) continue;

                float normalized = ringCount <= 1 ? 0.5f : ring / (float)(ringCount - 1);
                float baseRadius = 0.42f + stacks * 0.014f + Mathf.Sin(normalized * Mathf.PI) * 0.055f;
                float pulse = 1f + Mathf.Sin(Time.time * (1.05f + ring * 0.08f) + ring * 1.4f) * 0.025f;
                float radius = baseRadius * pulse;
                float height = ringCount == 1 ? 0.72f : Mathf.Lerp(0.24f, 1.58f, normalized);
                height += Mathf.Sin(Time.time * 0.92f + ring * 1.3f) * 0.024f;

                float stageAlpha = 0.24f + stacks * 0.075f;
                float coreAlpha = stageAlpha * visibility * (1f - ring * 0.035f);
                float haloAlpha = coreAlpha * (stacks >= 5 ? 0.40f : 0.32f);

                Color core;
                if (stacks >= 5)
                {
                    // Dark core + cold rim: the black ring stays readable even against dark terrain.
                    float darkness = 0.018f + ring * 0.006f;
                    core = new Color(darkness, darkness * 1.8f, darkness * 2.6f, Mathf.Min(0.82f, coreAlpha + 0.16f));
                }
                else if (stacks == 4 && (ring & 1) == 1)
                {
                    core = new Color(0.07f, 0.17f, 0.23f, coreAlpha * 0.88f);
                }
                else
                {
                    core = new Color(0.30f, 0.68f, 0.84f, coreAlpha);
                }
                rings[ring].startColor = rings[ring].endColor = core;
                halos[ring].startColor = halos[ring].endColor = new Color(0.24f, 0.68f, 0.90f, haloAlpha);

                float direction = (ring & 1) == 0 ? 1f : -1f;
                for (int i = 0; i < RingPoints; i++)
                {
                    float u = i / (float)RingPoints;
                    float angle = u * Mathf.PI * 2f + Time.time * (0.24f + ring * 0.035f) * direction;
                    float wave = Mathf.Sin(angle * 3f + Time.time * 0.72f + ring) * (stacks >= 5 ? 0.010f : 0.006f);
                    float radial = radius + wave;
                    float verticalWave = Mathf.Sin(angle * 2f - Time.time * 0.55f + ring * 0.6f) * (stacks >= 5 ? 0.012f : 0.006f);
                    Vector3 point = new Vector3(Mathf.Cos(angle) * radial, height + verticalWave, Mathf.Sin(angle) * radial);
                    rings[ring].SetPosition(i, point);
                    halos[ring].SetPosition(i, point);
                }
            }

            if (stacks < 5) return;
            for (int wisp = 0; wisp < wisps.Length; wisp++)
            {
                if (wisps[wisp] == null) continue;
                float alpha = (wisp % 2 == 0 ? 0.48f : 0.39f) * visibility;
                wisps[wisp].startColor = wisps[wisp].endColor = wisp % 2 == 1
                    ? new Color(0.025f, 0.075f, 0.11f, alpha + 0.13f * visibility)
                    : new Color(0.26f, 0.64f, 0.82f, alpha);

                for (int i = 0; i < WispPoints; i++)
                {
                    float t = i / (float)(WispPoints - 1);
                    float y = 0.10f + t * 1.72f;
                    float envelope = Mathf.Sin(t * Mathf.PI);
                    float radius = 0.34f + envelope * 0.17f;
                    float direction = (wisp & 1) == 0 ? 1f : -1f;
                    float angle = t * Mathf.PI * (3.15f + wisp * 0.10f)
                        + Time.time * (0.37f + wisp * 0.025f) * direction
                        + wisp * Mathf.PI * 2f / wisps.Length;
                    float breathe = 1f + Mathf.Sin(Time.time * 0.75f + t * 5f + wisp) * 0.025f;
                    Vector3 point = new Vector3(Mathf.Cos(angle) * radius * breathe, y, Mathf.Sin(angle) * radius * breathe);
                    wisps[wisp].SetPosition(i, point);
                }
            }
        }
    }
}
