using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace HaldorExpansion.Features.HelOath
{
    internal static class HelOathEffects
    {
        private const string PreparedKey = "he_hel_prepared_fx";
        private const string ActivationRpc = "HE_HelActivationFx_v1";
        private const string ExplosionRpc = "HE_HelExplosion_v2";
        private static readonly FieldInfo LeftVisual = AccessTools.Field(typeof(VisEquipment), "m_leftItemInstance");
        private static readonly FieldInfo LeftHash = AccessTools.Field(typeof(VisEquipment), "m_currentLeftItemHash");
        private sealed class VisualState { internal GameObject Weapon; internal HelOathWeaponGlow Glow; }
        private static readonly ConditionalWeakTable<Player, VisualState> Visuals = new ConditionalWeakTable<Player, VisualState>();
        private static Material material;
        private static Material lineMaterial;
        private static Material spikeIceMaterial;
        private static Material spikeDarkMaterial;
        private static Texture2D texture;
        private static bool CanRender => SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;

        internal static void Register(Player player, ZNetView view)
        {
            if (view.IsOwner()) view.GetZDO().Set(PreparedKey, false);
            view.Register<Vector3>(ActivationRpc, (sender, point) =>
            {
                if (CanRender && view.IsValid() && sender == view.GetZDO().GetOwner()) HelOathAudio.Play(point, false);
            });
            view.Register<Vector3, float, bool>(ExplosionRpc, (sender, point, radius, empowered) =>
            {
                if (!view.IsValid() || sender != view.GetZDO().GetOwner()) return;
                if (float.IsNaN(radius) || float.IsInfinity(radius) || radius <= 0f || radius > 50f) return;
                if (CanRender) PlayExplosion(point, radius, empowered);
            });
        }

        internal static void SyncPrepared(Player player, bool prepared)
        {
            var view = HelOathNetwork.View(player);
            if (view == null || !view.IsValid() || !view.IsOwner()) return;
            if (view.GetZDO().GetBool(PreparedKey) != prepared)
            {
                view.GetZDO().Set(PreparedKey, prepared);
                if (prepared) view.InvokeRPC(ZNetView.Everybody, ActivationRpc, player.GetCenterPoint());
            }
            // Owner sees the change immediately; other clients read the same ZDO flag.
            UpdateWeapon(player);
        }

        internal static void UpdateWeapon(Player player)
        {
            if (!CanRender || player == null) return;
            var view = HelOathNetwork.View(player);
            bool prepared = view != null && view.IsValid() && view.GetZDO().GetBool(PreparedKey) && !player.IsDead();
            VisualState state;
            if (!prepared && !Visuals.TryGetValue(player, out state)) return;
            state = Visuals.GetValue(player, ignored => new VisualState());

            if (!prepared)
            {
                // Do not snap the prepared aura off on release. Let the existing visual
                // exhale from the bow while its particles finish naturally.
                if (state.Glow != null) state.Glow.BeginFadeOut(0.62f);
                state.Weapon = null;
                state.Glow = null;
                return;
            }

            var equipment = player.GetComponent<VisEquipment>();
            var weapon = equipment != null ? LeftVisual.GetValue(equipment) as GameObject : null;
            if (weapon != null)
            {
                // VisEquipment clones the child named "attach", not the item's root prefab.
                var prefab = ObjectDB.instance?.GetItemPrefab((int)LeftHash.GetValue(equipment));
                if (prefab == null || prefab.name != HelOathItemRegistration.HelOathPrefabName) weapon = null;
            }
            if (state.Weapon == weapon && weapon != null && state.Glow != null) return;

            // A visual clone can be replaced during equipment refreshes. Cross-fade rather
            // than destroying the previous aura so remote players do not see a hard pop.
            if (state.Glow != null) state.Glow.BeginFadeOut(0.38f);
            state.Weapon = weapon;
            state.Glow = null;
            if (weapon == null) return;

            var go = new GameObject("HelOath_PreparedLightning");
            go.transform.SetParent(weapon.transform, false);
            state.Glow = go.AddComponent<HelOathWeaponGlow>();
            state.Glow.Initialize(weapon.transform, GetMaterial());
        }

        internal static void BroadcastExplosion(Player player, Vector3 point, float radius, bool empowered)
        {
            var view = HelOathNetwork.View(player);
            if (view != null && view.IsValid()) view.InvokeRPC(ZNetView.Everybody, ExplosionRpc, point, radius, empowered);
            else if (CanRender) PlayExplosion(point, radius, empowered);
        }

        internal static Material GetMaterial()
        {
            if (material != null) return material;
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Hidden/Internal-Colored");
            if (shader == null) throw new InvalidOperationException("Hel Oath: no built-in visual shader available.");
            material = new Material(shader) { name = "HelOath_IceLight" };
            texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var pixels = new Color[1024];
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            {
                float dx = (x - 15.5f) / 15.5f, dy = (y - 15.5f) / 15.5f;
                float alpha = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                pixels[y * 32 + x] = new Color(1f, 1f, 1f, alpha * alpha);
            }
            texture.SetPixels(pixels); texture.Apply();
            material.mainTexture = texture;
            return material;
        }

        internal static Material GetSpikeMaterial(bool dark)
        {
            if (dark && spikeDarkMaterial != null) return spikeDarkMaterial;
            if (!dark && spikeIceMaterial != null) return spikeIceMaterial;

            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Hidden/Internal-Colored");
            if (shader == null) return GetMaterial();
            var mat = new Material(shader)
            {
                name = dark ? "HelOath_BlackIceSpike" : "HelOath_FrostIceSpike",
                mainTexture = Texture2D.whiteTexture,
                color = dark
                    ? new Color(0.035f, 0.075f, 0.11f, 0.88f)
                    : new Color(0.50f, 0.87f, 0.98f, 0.82f)
            };
            mat.renderQueue = 3000;
            if (dark) spikeDarkMaterial = mat; else spikeIceMaterial = mat;
            return mat;
        }

        private static void PlayExplosion(Vector3 point, float radius, bool empowered)
        {
            var go = new GameObject("HelOath_FrostExplosion");
            go.transform.position = point + Vector3.up * 0.12f;
            // Double only the visible reach; the damage calculation and transmitted radius stay unchanged.
            go.AddComponent<HelOathFrostBurst>().Initialize(radius * 2f, GetMaterial(), empowered);
            HelOathAudio.Play(point, true);
        }

        internal static LineRenderer Line(Transform parent, Material mat, float width, int count)
        {
            var go = new GameObject("IceArc"); go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false; line.positionCount = count;
            // A radial sprite is suitable for sparks but would fade out the ends of a long bolt.
            if (lineMaterial == null) lineMaterial = new Material(mat) { name = "HelOath_SolidLightning", mainTexture = Texture2D.whiteTexture };
            line.sharedMaterial = lineMaterial; line.widthMultiplier = width;
            line.startColor = line.endColor = new Color(0.4f, 0.85f, 1f, 1f);
            line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
            return line;
        }

        internal static ParticleSystem Particles(Transform parent, Material mat, bool loop, float size, float speed, int burstCount = 150, float lifetime = 0.65f)
        {
            var go = new GameObject("IceSparks"); go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = loop; main.playOnAwake = false; main.duration = 1f;
            main.startLifetime = loop ? 0.3f : lifetime; main.startSize = size; main.startSpeed = speed;
            main.startColor = new Color(0.55f, 0.9f, 1f, 0.95f); main.maxParticles = 512;
            main.simulationSpace = loop ? ParticleSystemSimulationSpace.Local : ParticleSystemSimulationSpace.World;
            var emission = ps.emission; emission.enabled = loop; emission.rateOverTime = 35f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = loop ? 0.25f : 0.12f;
            var fade = ps.colorOverLifetime; fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.3f, 0.7f, 1f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var renderer = ps.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            ps.Play(); if (!loop && burstCount > 0) ps.Emit(burstCount);
            return ps;
        }
    }

    internal sealed class HelOathWeaponGlow : MonoBehaviour
    {
        private const int ArcPoints = 64;
        private const float FadeInDuration = 0.78f;
        private readonly LineRenderer[] arcs = new LineRenderer[2];
        private readonly ParticleSystem[] sparks = new ParticleSystem[5];
        private float extent, spread, bornAt;
        private bool fadingOut;
        private float fadeOutStarted, fadeOutDuration;
        private Vector3 axis, side, forward;

        internal void Initialize(Transform weapon, Material mat)
        {
            bornAt = Time.time;

            // Bounds in weapon space, including every mesh piece and each child's rotation/scale.
            Bounds combined = new Bounds(Vector3.zero, Vector3.zero);
            bool found = false;
            foreach (var renderer in weapon.GetComponentsInChildren<Renderer>())
            {
                if (renderer is ParticleSystemRenderer || renderer is LineRenderer) continue;
                Bounds bounds = renderer.localBounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 sign = new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f);
                    Vector3 point = weapon.InverseTransformPoint(renderer.transform.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, sign)));
                    if (!found) { combined = new Bounds(point, Vector3.zero); found = true; }
                    else combined.Encapsulate(point);
                }
            }
            if (!found) combined = new Bounds(Vector3.zero, new Vector3(0.3f, 1.6f, 0.3f));
            transform.localPosition = combined.center;
            Vector3 e = combined.extents;
            axis = e.x > e.y && e.x > e.z ? Vector3.right : e.z > e.y ? Vector3.forward : Vector3.up;
            extent = Mathf.Max(0.4f, Mathf.Max(e.x, Mathf.Max(e.y, e.z))) * 1.05f;
            side = Vector3.Cross(axis, axis == Vector3.up ? Vector3.forward : Vector3.up).normalized;
            forward = Vector3.Cross(axis, side).normalized;
            spread = Mathf.Clamp((e.x + e.y + e.z - extent) * 0.45f, 0.06f, 0.12f);

            // Reuse Touch's soft ribbon renderer: it has filtered edges and rounded caps,
            // so the bow aura belongs to the same visual family instead of looking jagged.
            for (int i = 0; i < arcs.Length; i++)
            {
                arcs[i] = HelOathTouchEffects.SmoothLine(transform, i == 0 ? 0.037f : 0.021f, ArcPoints, false);
                arcs[i].startColor = arcs[i].endColor = new Color(0.8f, 0.97f, 1f, 0f);
            }

            // Several emitters rather than a spark cloud concentrated at the grip.
            // Start at zero emission; Update() fades them in with the ribbon.
            for (int i = 0; i < sparks.Length; i++)
            {
                sparks[i] = HelOathEffects.Particles(transform, mat, true, 0.06f, 0.4f);
                sparks[i].transform.localPosition = axis * Mathf.Lerp(-extent, extent, i / (float)(sparks.Length - 1));
                var emission = sparks[i].emission;
                emission.rateOverTime = 0f;
                var shape = sparks[i].shape;
                shape.radius = 0.08f;
            }
        }

        internal void BeginFadeOut(float duration)
        {
            if (fadingOut) return;
            fadingOut = true;
            fadeOutStarted = Time.time;
            fadeOutDuration = Mathf.Max(0.18f, duration);
            // Keep the object alive long enough for the last soft particles to dissipate.
            UnityEngine.Object.Destroy(gameObject, fadeOutDuration + 0.55f);
        }

        private void Update()
        {
            if (arcs[0] == null) return;

            float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Time.time - bornAt) / FadeInDuration));
            float fadeOut = 1f;
            if (fadingOut)
                fadeOut = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Time.time - fadeOutStarted) / fadeOutDuration));
            float visibility = fadeIn * fadeOut;

            // Continuous deterministic motion replaces the old ~22 Hz random jitter.
            // The aura still feels electrically alive, but its path flows around the bow.
            for (int arc = 0; arc < arcs.Length; arc++)
            {
                float alpha = (arc == 0 ? 0.94f : 0.62f) * visibility;
                Color color = arc == 0
                    ? new Color(0.80f, 0.97f, 1f, alpha)
                    : new Color(0.34f, 0.76f, 0.92f, alpha);
                arcs[arc].startColor = arcs[arc].endColor = color;

                float direction = arc == 0 ? 1f : -1f;
                for (int i = 0; i < ArcPoints; i++)
                {
                    float t = i / (float)(ArcPoints - 1);
                    float envelope = Mathf.Sin(t * Mathf.PI);
                    float angle = t * Mathf.PI * (3.0f + arc * 0.34f)
                        + Time.time * (2.05f + arc * 0.23f) * direction
                        + arc * Mathf.PI * 0.73f;
                    float breathe = 1f + Mathf.Sin(Time.time * 1.15f + t * 7.2f + arc) * 0.055f;
                    float secondary = Mathf.Sin(t * Mathf.PI * 5f - Time.time * 1.35f + arc * 1.7f) * spread * 0.22f * envelope;
                    Vector3 curl = side * Mathf.Cos(angle) + forward * Mathf.Sin(angle);
                    Vector3 drift = (side * Mathf.Sin(angle * 0.52f + arc) + forward * Mathf.Cos(angle * 0.47f - arc)) * secondary;
                    Vector3 point = axis * Mathf.Lerp(-extent, extent, t)
                        + curl * spread * envelope * breathe
                        + drift;
                    arcs[arc].SetPosition(i, point);
                }
            }

            for (int i = 0; i < sparks.Length; i++)
            {
                if (sparks[i] == null) continue;
                var emission = sparks[i].emission;
                emission.rateOverTime = 7f * visibility;
            }
        }
    }

    internal sealed class HelOathFrostBurst : MonoBehaviour
    {
        private const int WavePoints = 112;
        private const int TendrilPoints = 28;
        private const int SpiralPoints = 64;
        private const int CrownPoints = 36;

        private static Mesh spikeMesh;
        private LineRenderer coreDark;
        private LineRenderer coreFrost;
        private LineRenderer[] shockwaves;
        private LineRenderer[] tendrils;
        private LineRenderer[] spirals;
        private LineRenderer[] crown;
        private Vector3[] tendrilDirections;
        private Vector3[] tendrilSides;
        private Vector3[] tendrilForwards;
        private float[] tendrilSeeds;
        private float[] spiralSeeds;
        private float[] crownSeeds;
        private GameObject[] iceSpikes;
        private Vector3[] spikeBasePositions;
        private Vector3[] spikeFullScales;
        private float[] spikeDelays;
        private float radius;
        private float born;
        private bool empowered;

        internal void Initialize(float blastRadius, Material mat, bool touchV)
        {
            radius = blastRadius;
            born = Time.time;
            empowered = touchV;

            coreDark = HelOathTouchEffects.SmoothLine(transform, touchV ? 0.34f : 0.29f, WavePoints, true);
            coreFrost = HelOathTouchEffects.SmoothLine(transform, touchV ? 0.21f : 0.18f, WavePoints, true);

            shockwaves = new LineRenderer[touchV ? 4 : 3];
            for (int i = 0; i < shockwaves.Length; i++)
            {
                float width = i == 0 ? 0.28f : (i == 1 ? 0.18f : (i == 2 ? 0.13f : 0.10f));
                shockwaves[i] = HelOathTouchEffects.SmoothLine(transform, width, WavePoints, true);
            }

            int tendrilCount = touchV ? 14 : 10;
            tendrils = new LineRenderer[tendrilCount];
            tendrilDirections = new Vector3[tendrilCount];
            tendrilSides = new Vector3[tendrilCount];
            tendrilForwards = new Vector3[tendrilCount];
            tendrilSeeds = new float[tendrilCount];
            for (int i = 0; i < tendrilCount; i++)
            {
                float width = touchV ? 0.083f : 0.071f;
                if (i % 3 == 1) width *= 0.78f;
                tendrils[i] = HelOathTouchEffects.SmoothLine(transform, width, TendrilPoints, false);
                float angle = Mathf.PI * 2f * i / tendrilCount + Mathf.Sin(i * 2.17f) * 0.085f;
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)).normalized;
                tendrilDirections[i] = dir;
                Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
                tendrilSides[i] = side;
                tendrilForwards[i] = Vector3.Cross(dir, side).normalized;
                tendrilSeeds[i] = 0.31f + i * 0.61f;
            }

            int spiralCount = touchV ? 8 : 5;
            spirals = new LineRenderer[spiralCount];
            spiralSeeds = new float[spiralCount];
            for (int i = 0; i < spiralCount; i++)
            {
                spirals[i] = HelOathTouchEffects.SmoothLine(transform, touchV ? 0.090f : 0.074f, SpiralPoints, false);
                spiralSeeds[i] = i * Mathf.PI * 2f / spiralCount;
            }

            int crownCount = touchV ? 12 : 8;
            crown = new LineRenderer[crownCount];
            crownSeeds = new float[crownCount];
            for (int i = 0; i < crownCount; i++)
            {
                crown[i] = HelOathTouchEffects.SmoothLine(transform, touchV ? 0.105f : 0.086f, CrownPoints, false);
                crownSeeds[i] = i * Mathf.PI * 2f / crownCount + (i % 2 == 0 ? 0.06f : -0.05f);
            }

            CreateGroundSpikes(touchV);
            CreateParticles(mat, touchV);
            UnityEngine.Object.Destroy(gameObject, touchV ? 2.36f : 2.08f);
        }

        private void CreateParticles(Material mat, bool touchV)
        {
            // Dense central bloom. Large soft sprites fill the centre while smaller frost fragments
            // preserve detail, so the burst reads as a mass of black frost rather than empty lines.
            var darkBloom = HelOathEffects.Particles(transform, mat, false, touchV ? 2.9f : 2.35f, radius * 0.10f, 0, touchV ? 0.82f : 0.70f);
            var darkBloomMain = darkBloom.main;
            darkBloomMain.startColor = new Color(0.018f, 0.028f, 0.050f, touchV ? 0.52f : 0.43f);
            var darkBloomShape = darkBloom.shape;
            darkBloomShape.radius = radius * 0.09f;
            darkBloom.Emit(touchV ? 54 : 38);

            var frostBloom = HelOathEffects.Particles(transform, mat, false, touchV ? 0.62f : 0.52f, radius * (touchV ? 0.38f : 0.31f), 0, 0.72f);
            var frostBloomMain = frostBloom.main;
            frostBloomMain.startColor = new Color(0.55f, 0.88f, 1f, touchV ? 0.82f : 0.72f);
            var frostBloomShape = frostBloom.shape;
            frostBloomShape.radius = radius * 0.11f;
            frostBloom.Emit(touchV ? 105 : 76);

            var frost = HelOathEffects.Particles(transform, mat, false, touchV ? 0.21f : 0.18f, radius * (touchV ? 0.78f : 0.67f), 0, 1.02f);
            var frostMain = frost.main;
            frostMain.startColor = new Color(0.72f, 0.94f, 1f, 0.94f);
            var frostShape = frost.shape;
            frostShape.radius = radius * 0.15f;
            frost.Emit(touchV ? 220 : 160);

            var shards = HelOathEffects.Particles(transform, mat, false, touchV ? 0.39f : 0.34f, radius * (touchV ? 0.67f : 0.55f), 0, 1.18f);
            var shardMain = shards.main;
            shardMain.startColor = new Color(0.66f, 0.91f, 1f, 0.90f);
            var shardRenderer = shards.GetComponent<ParticleSystemRenderer>();
            shardRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            shardRenderer.lengthScale = 3.5f;
            shardRenderer.velocityScale = 0.055f;
            shards.Emit(touchV ? 112 : 78);

            var mist = HelOathEffects.Particles(transform, mat, false, touchV ? 2.45f : 2.05f, radius * (touchV ? 0.23f : 0.20f), 0, touchV ? 1.62f : 1.42f);
            var mistMain = mist.main;
            mistMain.startColor = new Color(0.055f, 0.082f, 0.125f, touchV ? 0.42f : 0.34f);
            var mistShape = mist.shape;
            mistShape.radius = radius * 0.26f;
            mist.Emit(touchV ? 132 : 92);

            var snow = HelOathEffects.Particles(transform, mat, false, touchV ? 0.19f : 0.16f, radius * 0.27f, 0, touchV ? 1.58f : 1.34f);
            var snowMain = snow.main;
            snowMain.startColor = new Color(0.97f, 0.995f, 1f, touchV ? 0.80f : 0.66f);
            var snowShape = snow.shape;
            snowShape.radius = radius * 0.22f;
            snow.Emit(touchV ? 210 : 145);

            var shadowFlecks = HelOathEffects.Particles(transform, mat, false, touchV ? 0.23f : 0.19f, radius * 0.36f, 0, touchV ? 1.34f : 1.12f);
            var shadowMain = shadowFlecks.main;
            shadowMain.startColor = new Color(0.018f, 0.030f, 0.052f, touchV ? 0.78f : 0.62f);
            var shadowShape = shadowFlecks.shape;
            shadowShape.radius = radius * 0.16f;
            shadowFlecks.Emit(touchV ? 115 : 72);

            var flash = HelOathEffects.Particles(transform, mat, false, touchV ? 1.65f : 1.30f, 0f, 0, 0.25f);
            var flashMain = flash.main;
            flashMain.startColor = new Color(0.36f, 0.82f, 0.97f, touchV ? 0.32f : 0.27f);
            flash.Emit(touchV ? 7 : 5);

            var inhale = HelOathEffects.Particles(transform, mat, false, touchV ? 0.20f : 0.18f, -(radius * 0.42f), 0, 0.24f);
            var inhaleMain = inhale.main;
            inhaleMain.startColor = new Color(0.60f, 0.88f, 1f, 0.38f);
            var inhaleShape = inhale.shape;
            inhaleShape.radius = radius * 0.36f;
            inhale.Emit(touchV ? 52 : 34);

            if (touchV)
            {
                var aftermath = HelOathEffects.Particles(transform, mat, false, 1.55f, radius * 0.16f, 0, 1.82f);
                var aftermathMain = aftermath.main;
                aftermathMain.startColor = new Color(0.035f, 0.060f, 0.105f, 0.33f);
                var aftermathShape = aftermath.shape;
                aftermathShape.radius = radius * 0.30f;
                aftermath.Emit(52);
            }
        }

        private void Update()
        {
            float age = Time.time - born;
            UpdateCore(age);
            UpdateShockwaves(age);
            UpdateTendrils(age);
            UpdateCrown(age);
            UpdateSpirals(age);
            UpdateGroundSpikes(age);
        }

        private void CreateGroundSpikes(bool touchV)
        {
            int count = touchV ? 22 : 14;
            iceSpikes = new GameObject[count];
            spikeBasePositions = new Vector3[count];
            spikeFullScales = new Vector3[count];
            spikeDelays = new float[count];
            Mesh mesh = GetSpikeMesh();
            float fieldRadius = radius * (touchV ? 0.52f : 0.46f);

            for (int i = 0; i < count; i++)
            {
                float sequence = Mathf.Repeat(i * 0.6180339f + (touchV ? 0.17f : 0.03f), 1f);
                float radial01 = 0.24f + 0.76f * sequence;
                float angle = i * 2.3999632f + Mathf.Sin(i * 1.73f) * 0.20f;
                float distance = fieldRadius * radial01;
                Vector3 basePos = new Vector3(Mathf.Cos(angle) * distance, -0.13f, Mathf.Sin(angle) * distance);

                float heightNoise = 0.5f + 0.5f * Mathf.Sin(i * 2.11f + 0.8f);
                float height = Mathf.Lerp(touchV ? 1.55f : 1.20f, touchV ? 3.25f : 2.35f, heightNoise);
                if (i % 5 == 0) height *= touchV ? 1.18f : 1.10f;
                float widthNoise = 0.5f + 0.5f * Mathf.Cos(i * 1.37f + 0.4f);
                float width = Mathf.Lerp(touchV ? 0.32f : 0.27f, touchV ? 0.56f : 0.46f, widthNoise);

                var go = new GameObject("HelOath_GroundIceSpike_" + i);
                go.transform.SetParent(transform, false);
                go.transform.localPosition = basePos;
                go.transform.localRotation = Quaternion.Euler(
                    Mathf.Sin(i * 1.31f) * (touchV ? 12f : 9f),
                    -angle * Mathf.Rad2Deg + 90f,
                    Mathf.Cos(i * 1.87f) * (touchV ? 10f : 7f));
                go.transform.localScale = Vector3.zero;

                var filter = go.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = HelOathEffects.GetSpikeMaterial(i % 4 == 1 || (touchV && i % 5 == 3));
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                iceSpikes[i] = go;
                spikeBasePositions[i] = basePos;
                spikeFullScales[i] = new Vector3(width, height, width * (0.78f + 0.16f * sequence));
                spikeDelays[i] = 0.07f + (i % 7) * 0.018f + sequence * 0.055f;
            }
        }

        private static Mesh GetSpikeMesh()
        {
            if (spikeMesh != null) return spikeMesh;

            const int sides = 6;
            var vertices = new Vector3[sides * 2 + 1];
            var uv = new Vector2[vertices.Length];
            for (int i = 0; i < sides; i++)
            {
                float angle = Mathf.PI * 2f * i / sides;
                float c = Mathf.Cos(angle);
                float s = Mathf.Sin(angle);
                vertices[i] = new Vector3(c * 0.50f, 0f, s * 0.50f);
                vertices[sides + i] = new Vector3(c * 0.22f, 0.62f, s * 0.22f);
                uv[i] = new Vector2(i / (float)sides, 0f);
                uv[sides + i] = new Vector2(i / (float)sides, 0.62f);
            }
            vertices[sides * 2] = new Vector3(0.08f, 1f, -0.04f);
            uv[sides * 2] = new Vector2(0.5f, 1f);

            var triangles = new int[sides * 12];
            int t = 0;
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                triangles[t++] = i;
                triangles[t++] = next;
                triangles[t++] = sides + i;
                triangles[t++] = next;
                triangles[t++] = sides + next;
                triangles[t++] = sides + i;

                triangles[t++] = sides + i;
                triangles[t++] = sides + next;
                triangles[t++] = sides * 2;

                triangles[t++] = i;
                triangles[t++] = 0;
                triangles[t++] = next;
            }

            spikeMesh = new Mesh { name = "HelOath_ProceduralIceSpike" };
            spikeMesh.vertices = vertices;
            spikeMesh.uv = uv;
            spikeMesh.triangles = triangles;
            spikeMesh.RecalculateNormals();
            spikeMesh.RecalculateBounds();
            return spikeMesh;
        }

        private void UpdateGroundSpikes(float age)
        {
            if (iceSpikes == null) return;
            float holdUntil = empowered ? 1.56f : 1.34f;
            float vanishDuration = empowered ? 0.58f : 0.50f;

            for (int i = 0; i < iceSpikes.Length; i++)
            {
                var spike = iceSpikes[i];
                if (spike == null) continue;

                float delay = spikeDelays[i];
                float grow = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((age - delay) / (empowered ? 0.25f : 0.22f)));
                float vanish = 1f;
                if (age > holdUntil)
                    vanish = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((age - holdUntil) / vanishDuration));

                float widthScale = Mathf.Lerp(0.18f, 1f, grow) * Mathf.Lerp(0.30f, 1f, vanish);
                float heightScale = grow * Mathf.Lerp(0.18f, 1f, vanish);
                Vector3 full = spikeFullScales[i];
                spike.transform.localScale = new Vector3(full.x * widthScale, full.y * heightScale, full.z * widthScale);

                // They do not fly away: after erupting they remain rooted in the impact field,
                // then sink back under the surface during the final black-frost aftermath.
                float sink = (1f - vanish) * full.y * 0.30f;
                spike.transform.localPosition = spikeBasePositions[i] + Vector3.down * sink;
            }
        }

        private void UpdateCore(float age)
        {
            float collapse = Mathf.Clamp01(age / 0.095f);
            float bloom = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((age - 0.045f) / (empowered ? 0.46f : 0.39f)));
            float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / (empowered ? 0.88f : 0.73f)));

            float darkRadius = Mathf.Lerp(radius * 0.42f, radius * 0.055f, collapse);
            darkRadius = Mathf.Lerp(darkRadius, radius * (empowered ? 0.24f : 0.20f), bloom * 0.86f);
            SetCircle(coreDark, darkRadius, 0.055f, radius * 0.025f * (1f - bloom * 0.52f), age * 1.65f, 0f);
            coreDark.startColor = coreDark.endColor = new Color(0.012f, 0.025f, 0.045f, 0.96f * fade);

            float frostRadius = Mathf.Lerp(radius * 0.055f, radius * (empowered ? 0.43f : 0.35f), bloom);
            SetCircle(coreFrost, frostRadius, 0.075f, radius * 0.030f * (1f - bloom * 0.28f), -age * 2.15f, 0.42f);
            coreFrost.startColor = coreFrost.endColor = new Color(0.28f, 0.79f, 0.95f, 0.76f * fade);
        }

        private void UpdateShockwaves(float age)
        {
            for (int i = 0; i < shockwaves.Length; i++)
            {
                float delay = i == 0 ? 0.065f : (i == 1 ? 0.135f : (i == 2 ? 0.24f : 0.38f));
                float duration = i == 0 ? 0.63f : (i == 1 ? 0.76f : (i == 2 ? 0.84f : 0.68f));
                float t = Mathf.Clamp01((age - delay) / duration);
                float alpha = age < delay ? 0f : 1f - Mathf.SmoothStep(0f, 1f, t);
                float eased = 1f - (1f - t) * (1f - t);
                float scale = i == 1 ? 0.78f : (i == 2 ? 1.02f : (i == 3 ? 1.15f : 1f));
                float waveRadius = radius * (0.08f + 0.92f * eased) * scale;
                SetCircle(shockwaves[i], waveRadius, 0.025f + i * 0.011f, radius * (0.034f - i * 0.004f) * (1f - t), age * (i % 2 == 0 ? 1.55f : -1.25f), i * 0.73f);

                Color color;
                if (i == 0) color = new Color(0.025f, 0.050f, 0.085f, alpha * 0.98f);
                else if (i == 1) color = new Color(0.35f, 0.84f, 0.98f, alpha * 0.79f);
                else if (i == 2) color = new Color(0.08f, 0.15f, 0.21f, alpha * 0.70f);
                else color = new Color(0.46f, 0.88f, 1f, alpha * 0.54f);
                shockwaves[i].startColor = shockwaves[i].endColor = color;
            }
        }

        private void UpdateTendrils(float age)
        {
            float start = 0.075f;
            float growthDuration = empowered ? 0.74f : 0.63f;
            float fadeDuration = empowered ? 1.16f : 0.98f;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((age - start) / growthDuration));
            float alphaBase = age < start ? 0f : 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((age - start) / fadeDuration));

            for (int i = 0; i < tendrils.Length; i++)
            {
                float alpha = alphaBase * (0.86f - (i % 4) * 0.055f);
                Color color = i % 3 == 0
                    ? new Color(0.42f, 0.89f, 1f, alpha)
                    : new Color(0.075f, 0.12f, 0.17f, alpha * 0.96f);
                tendrils[i].startColor = tendrils[i].endColor = color;

                for (int point = 0; point < TendrilPoints; point++)
                {
                    float t = point / (float)(TendrilPoints - 1);
                    float envelope = Mathf.Sin(t * Mathf.PI);
                    float reach = radius * progress * (0.08f + 0.92f * t);
                    float wobble = radius * (0.052f + (i % 3) * 0.007f) * envelope * (1f - 0.20f * progress);
                    float waveA = Mathf.Sin(t * (5.2f + tendrilSeeds[i]) + age * 3.8f + tendrilSeeds[i] * 1.45f);
                    float waveB = Mathf.Cos(t * (3.5f + tendrilSeeds[i] * 0.45f) - age * 2.7f + tendrilSeeds[i]);
                    Vector3 pos = tendrilDirections[i] * reach
                        + tendrilSides[i] * wobble * waveA
                        + tendrilForwards[i] * (wobble * 0.68f) * waveB;
                    pos.y = 0.032f + 0.18f * envelope * (1f - t) * (0.86f + 0.14f * Mathf.Sin(age * 2.0f + i));
                    tendrils[i].SetPosition(point, pos);
                }
            }
        }

        private void UpdateCrown(float age)
        {
            float start = 0.09f;
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((age - start) / (empowered ? 0.46f : 0.40f)));
            float alphaBase = age < start ? 0f : 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((age - start) / (empowered ? 0.88f : 0.72f)));
            float crownRadius = radius * (empowered ? 0.46f : 0.39f);
            float crownHeight = empowered ? 2.75f : 2.18f;

            for (int i = 0; i < crown.Length; i++)
            {
                float alpha = alphaBase * (0.78f + 0.16f * ((i & 1) == 0 ? 1f : 0f));
                Color color = (i % 3 == 0)
                    ? new Color(0.46f, 0.90f, 1f, alpha)
                    : new Color(0.055f, 0.085f, 0.13f, alpha * 0.96f);
                crown[i].startColor = crown[i].endColor = color;

                float angle = crownSeeds[i] + age * ((i & 1) == 0 ? 0.28f : -0.22f);
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 tangent = new Vector3(-radial.z, 0f, radial.x);
                for (int point = 0; point < CrownPoints; point++)
                {
                    float t = point / (float)(CrownPoints - 1);
                    float arch = Mathf.Sin(t * Mathf.PI);
                    float outward = crownRadius * rise * (0.10f + 0.90f * t);
                    float curl = Mathf.Sin(t * Mathf.PI * 1.7f + i * 0.72f) * crownRadius * 0.10f * arch;
                    float y = 0.03f + crownHeight * rise * Mathf.Pow(t, 0.78f) * (0.72f + 0.28f * arch);
                    crown[i].SetPosition(point, radial * outward + tangent * curl + Vector3.up * y);
                }
            }
        }

        private void UpdateSpirals(float age)
        {
            float start = 0.14f;
            float growthDuration = empowered ? 0.94f : 0.80f;
            float fadeDuration = empowered ? 1.36f : 1.12f;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((age - start) / growthDuration));
            float alphaBase = age < start ? 0f : 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((age - start) / fadeDuration));
            float maxHeight = empowered ? 2.65f : 2.05f;
            float maxOrbit = radius * (empowered ? 0.27f : 0.21f);

            for (int i = 0; i < spirals.Length; i++)
            {
                float alpha = alphaBase * (empowered ? 0.91f : 0.81f);
                Color color = i % 2 == 0
                    ? new Color(0.12f, 0.15f, 0.20f, alpha)
                    : new Color(0.40f, 0.86f, 0.98f, alpha * 0.79f);
                spirals[i].startColor = spirals[i].endColor = color;

                float direction = i % 2 == 0 ? 1f : -1f;
                Vector3 offset = new Vector3(Mathf.Cos(spiralSeeds[i]), 0f, Mathf.Sin(spiralSeeds[i])) * radius * 0.08f * progress;
                for (int point = 0; point < SpiralPoints; point++)
                {
                    float t = point / (float)(SpiralPoints - 1);
                    float orbit = Mathf.Lerp(radius * 0.035f, maxOrbit, t) * (1f - 0.10f * progress);
                    float angle = spiralSeeds[i] + direction * (age * 2.15f + t * (empowered ? 8.2f : 6.7f));
                    float y = 0.055f + maxHeight * progress * t;
                    Vector3 pos = new Vector3(Mathf.Cos(angle) * orbit, y, Mathf.Sin(angle) * orbit) + offset;
                    spirals[i].SetPosition(point, pos);
                }
            }
        }

        private void SetCircle(LineRenderer line, float circleRadius, float y, float wobble, float phase, float angleOffset)
        {
            for (int i = 0; i < WavePoints; i++)
            {
                float t = i / (float)(WavePoints - 1);
                float angle = t * Mathf.PI * 2f + angleOffset;
                float localRadius = circleRadius
                    + Mathf.Sin(angle * 3f + phase) * wobble
                    + Mathf.Cos(angle * 5f - phase * 0.7f) * wobble * 0.35f;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * localRadius, y, Mathf.Sin(angle) * localRadius));
            }
        }
    }
}
