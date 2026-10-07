using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace HaldorExpansion.Features.Berserker
{
    // Shared visual language for the Berserker kit. Everything here is cosmetic:
    // gameplay damage, Debt spending and Wrath state remain in their own runtimes.
    internal static class BerserkerEffects
    {
        private const string ReckoningSwingRpc = "HE_BerserkerReckoningSwingFx_v1";
        private const string ReckoningImpactRpc = "HE_BerserkerReckoningImpactFx_v1";
        private const string RagnarokCastRpc = "HE_BerserkerRagnarokCastFx_v3";
        private const string RagnarokStopRpc = "HE_BerserkerRagnarokStopFx_v1";
        private const string RagnarokReleaseRpc = "HE_BerserkerRagnarokReleaseFx_v1";

        private static readonly FieldInfo CharacterView = AccessTools.Field(typeof(Character), "m_nview");
        private static readonly FieldInfo RightVisual = AccessTools.Field(typeof(VisEquipment), "m_rightItemInstance");
        private static readonly FieldInfo LeftVisual = AccessTools.Field(typeof(VisEquipment), "m_leftItemInstance");
        private static readonly FieldInfo RightHash = AccessTools.Field(typeof(VisEquipment), "m_currentRightItemHash");
        private static readonly FieldInfo LeftHash = AccessTools.Field(typeof(VisEquipment), "m_currentLeftItemHash");

        private sealed class PlayerVisualState
        {
            internal BerserkerRagnarokCastVisual RagnarokCast;
        }

        private static readonly ConditionalWeakTable<Player, PlayerVisualState> PlayerVisuals =
            new ConditionalWeakTable<Player, PlayerVisualState>();

        private static Material softMaterial;
        private static Material solidMaterial;
        private static Texture2D softTexture;

        internal static bool CanRender => SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;

        internal static void Register(Player player, ZNetView view)
        {
            if (player == null || view == null) return;

            view.Register<float>(ReckoningSwingRpc, (sender, debt) =>
            {
                if (!ValidVisualRpc(view, sender) || !FinitePositive(debt) || !CanRender) return;
                PlayReckoningSwing(player, debt);
            });

            view.Register<Vector3, float>(ReckoningImpactRpc, (sender, point, debt) =>
            {
                if (!ValidVisualRpc(view, sender) || !FiniteVector(point) || !FinitePositive(debt) || !CanRender) return;
                PlayReckoningImpact(point, debt);
            });

            view.Register<float>(RagnarokCastRpc, (sender, duration) =>
            {
                if (!ValidVisualRpc(view, sender) || !FinitePositive(duration) || duration > 10f || !CanRender) return;
                PlayRagnarokCast(player, duration);
            });

            view.Register<bool>(RagnarokStopRpc, (sender, ignored) =>
            {
                if (!ValidVisualRpc(view, sender) || !CanRender) return;
                StopRagnarokCast(player);
            });

            view.Register<Vector3, float>(RagnarokReleaseRpc, (sender, targetPoint, debt) =>
            {
                if (!ValidVisualRpc(view, sender) || !FiniteVector(targetPoint) || !FinitePositive(debt) || !CanRender) return;
                PlayRagnarokRelease(player, targetPoint, debt);
            });
        }

        internal static void BroadcastReckoningSwing(Player player, double debt)
        {
            float value = SafeDebt(debt);
            if (player == null || value <= 0f) return;
            ZNetView view = View(player);
            if (view != null && view.IsValid()) view.InvokeRPC(ZNetView.Everybody, ReckoningSwingRpc, value);
            else if (CanRender) PlayReckoningSwing(player, value);
        }

        internal static void BroadcastReckoningImpact(Player player, Vector3 point, double debt)
        {
            float value = SafeDebt(debt);
            if (player == null || value <= 0f || !FiniteVector(point)) return;
            ZNetView view = View(player);
            if (view != null && view.IsValid()) view.InvokeRPC(ZNetView.Everybody, ReckoningImpactRpc, point, value);
            else if (CanRender) PlayReckoningImpact(point, value);
        }

        internal static void BroadcastRagnarokCast(Player player, float duration)
        {
            if (player == null || !FinitePositive(duration)) return;
            ZNetView view = View(player);
            if (view != null && view.IsValid()) view.InvokeRPC(ZNetView.Everybody, RagnarokCastRpc, duration);
            else if (CanRender) PlayRagnarokCast(player, duration);
        }

        internal static void BroadcastRagnarokStop(Player player)
        {
            if (player == null) return;
            ZNetView view = View(player);
            if (view != null && view.IsValid()) view.InvokeRPC(ZNetView.Everybody, RagnarokStopRpc, true);
            else if (CanRender) StopRagnarokCast(player);
        }

        internal static void BroadcastRagnarokRelease(Player player, Vector3 targetPoint, double debt)
        {
            float value = SafeDebt(debt);
            if (player == null || value <= 0f || !FiniteVector(targetPoint)) return;
            ZNetView view = View(player);
            if (view != null && view.IsValid()) view.InvokeRPC(ZNetView.Everybody, RagnarokReleaseRpc, targetPoint, value);
            else if (CanRender) PlayRagnarokRelease(player, targetPoint, value);
        }

        internal static void AttachMeteorVisual(GameObject meteor, float perMeteorDamage)
        {
            if (!CanRender || meteor == null) return;
            BerserkerMeteorVisual visual = meteor.GetComponent<BerserkerMeteorVisual>();
            if (visual == null) visual = meteor.AddComponent<BerserkerMeteorVisual>();
            visual.Initialize(EffectIntensity(Mathf.Max(100f, perMeteorDamage)));
        }

        internal static void PlayMeteorImpact(Vector3 point, float intensity)
        {
            if (!CanRender || !FiniteVector(point)) return;
            GameObject root = new GameObject("Berserker_RagnarokMeteorImpact");
            root.transform.position = point;
            BerserkerImpactVisual impact = root.AddComponent<BerserkerImpactVisual>();
            impact.Initialize(Mathf.Clamp(intensity * 1.12f, 1f, 1.9f), true, SoftMaterial(), SolidMaterial(),
                BerserkerRagnarokRuntime.MeteorExplosionRadius);
        }

        private static void PlayReckoningSwing(Player player, float debt)
        {
            if (player == null || player.IsDead()) return;
            GameObject weapon = FindBerserkerWeaponVisual(player);
            float intensity = EffectIntensity(debt);

            if (weapon != null)
            {
                BerserkerWeaponSwingVisual old = weapon.GetComponentInChildren<BerserkerWeaponSwingVisual>();
                if (old != null) UnityEngine.Object.Destroy(old.gameObject);

                GameObject root = new GameObject("Berserker_ReckoningWeaponFx");
                root.transform.SetParent(weapon.transform, false);
                BerserkerWeaponSwingVisual swing = root.AddComponent<BerserkerWeaponSwingVisual>();
                swing.Initialize(weapon.transform, intensity, SoftMaterial(), SolidMaterial());
                return;
            }

            // Fallback for the first frame after an equipment visual refresh.
            GameObject fallback = new GameObject("Berserker_ReckoningFallbackFx");
            fallback.transform.position = player.GetCenterPoint() + player.transform.forward * 0.5f;
            BerserkerImpactVisual pulse = fallback.AddComponent<BerserkerImpactVisual>();
            pulse.Initialize(intensity * 0.55f, false, SoftMaterial(), SolidMaterial());
        }

        private static void PlayReckoningImpact(Vector3 point, float debt)
        {
            GameObject root = new GameObject("Berserker_ReckoningImpact");
            root.transform.position = point;
            BerserkerImpactVisual impact = root.AddComponent<BerserkerImpactVisual>();
            impact.Initialize(EffectIntensity(debt), false, SoftMaterial(), SolidMaterial());
        }

        private static void PlayRagnarokCast(Player player, float duration)
        {
            if (player == null || player.IsDead()) return;
            PlayerVisualState state = PlayerVisuals.GetValue(player, ignored => new PlayerVisualState());
            if (state.RagnarokCast != null) state.RagnarokCast.StopFx(0.12f);

            GameObject root = new GameObject("Berserker_RagnarokCast");
            root.transform.SetParent(player.transform, false);
            BerserkerRagnarokCastVisual cast = root.AddComponent<BerserkerRagnarokCastVisual>();
            cast.Initialize(player, BerserkerRagnarokRuntime.MeteorImpactRadius, duration, SoftMaterial(), SolidMaterial());
            state.RagnarokCast = cast;
        }

        private static void StopRagnarokCast(Player player)
        {
            if (player == null) return;
            PlayerVisualState state;
            if (!PlayerVisuals.TryGetValue(player, out state)) return;
            if (state.RagnarokCast != null)
            {
                state.RagnarokCast.StopFx(0.18f);
                state.RagnarokCast = null;
            }
        }

        private static void PlayRagnarokRelease(Player player, Vector3 centerPoint, float debt)
        {
            StopRagnarokCast(player);
            float intensity = Mathf.Clamp(EffectIntensity(debt) + 0.18f, 1.15f, 1.9f);

            if (player != null)
            {
                GameObject root = new GameObject("Berserker_RagnarokRelease");
                root.transform.position = player.GetCenterPoint();
                BerserkerRagnarokReleaseVisual release = root.AddComponent<BerserkerRagnarokReleaseVisual>();
                release.Initialize(intensity, SoftMaterial(), SolidMaterial());
            }

            // A restrained center pulse marks the exact point where the self-cast area
            // was frozen when Ragnarök completed.
            GameObject zone = new GameObject("Berserker_RagnarokCenterPulse");
            zone.transform.position = centerPoint;
            BerserkerImpactVisual marker = zone.AddComponent<BerserkerImpactVisual>();
            marker.Initialize(intensity * 0.68f, true, SoftMaterial(), SolidMaterial());
        }

        private static GameObject FindBerserkerWeaponVisual(Player player)
        {
            if (player == null) return null;
            VisEquipment equipment = player.GetComponent<VisEquipment>();
            if (equipment == null) return null;

            GameObject right = RightVisual != null ? RightVisual.GetValue(equipment) as GameObject : null;
            if (VisualMatchesAxe(equipment, right, RightHash)) return right;

            GameObject left = LeftVisual != null ? LeftVisual.GetValue(equipment) as GameObject : null;
            if (VisualMatchesAxe(equipment, left, LeftHash)) return left;
            return null;
        }

        private static bool VisualMatchesAxe(VisEquipment equipment, GameObject visual, FieldInfo hashField)
        {
            if (equipment == null || visual == null || hashField == null || ObjectDB.instance == null) return false;
            object value = hashField.GetValue(equipment);
            if (!(value is int)) return false;
            GameObject prefab = ObjectDB.instance.GetItemPrefab((int)value);
            return prefab != null && prefab.name == BerserkerItemRegistration.PrefabName;
        }

        private static ZNetView View(Character character)
            => character == null || CharacterView == null ? null : CharacterView.GetValue(character) as ZNetView;

        private static bool ValidVisualRpc(ZNetView view, long sender)
        {
            return view != null && view.IsValid() && view.GetZDO() != null && sender == view.GetZDO().GetOwner();
        }

        private static bool FinitePositive(float value)
            => !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;

        private static bool FiniteVector(Vector3 point)
            => !float.IsNaN(point.x) && !float.IsInfinity(point.x) &&
               !float.IsNaN(point.y) && !float.IsInfinity(point.y) &&
               !float.IsNaN(point.z) && !float.IsInfinity(point.z);

        private static float SafeDebt(double debt)
        {
            if (double.IsNaN(debt) || double.IsInfinity(debt) || debt <= 0d) return 0f;
            return (float)Math.Min(debt, 1000000d);
        }

        private static float EffectIntensity(float debtOrDamage)
        {
            float normalized = Mathf.Max(1f, debtOrDamage / 100f);
            return Mathf.Clamp(1f + Mathf.Log10(normalized) * 0.24f, 1f, 1.75f);
        }

        internal static Material SoftMaterial()
        {
            if (softMaterial != null) return softMaterial;
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Hidden/Internal-Colored");
            if (shader == null) return null;

            softTexture = new Texture2D(48, 48, TextureFormat.RGBA32, false);
            softTexture.name = "Berserker_SoftBloodOrb";
            softTexture.filterMode = FilterMode.Bilinear;
            softTexture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[48 * 48];
            for (int y = 0; y < 48; y++)
            for (int x = 0; x < 48; x++)
            {
                float dx = (x - 23.5f) / 23.5f;
                float dy = (y - 23.5f) / 23.5f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - r);
                alpha = alpha * alpha * (3f - 2f * alpha);
                pixels[y * 48 + x] = new Color(1f, 1f, 1f, alpha);
            }
            softTexture.SetPixels(pixels);
            softTexture.Apply();

            softMaterial = new Material(shader)
            {
                name = "Berserker_BloodGlow",
                mainTexture = softTexture
            };
            softMaterial.renderQueue = 3000;
            return softMaterial;
        }

        internal static Material SolidMaterial()
        {
            if (solidMaterial != null) return solidMaterial;
            Material source = SoftMaterial();
            if (source == null) return null;
            solidMaterial = new Material(source)
            {
                name = "Berserker_BloodLightning",
                mainTexture = Texture2D.whiteTexture
            };
            return solidMaterial;
        }

        internal static LineRenderer MakeLine(Transform parent, Material material, float width, int points, bool worldSpace)
        {
            GameObject go = new GameObject("Berserker_BloodLine");
            if (parent != null) go.transform.SetParent(parent, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = worldSpace;
            line.positionCount = points;
            line.widthMultiplier = width;
            line.sharedMaterial = material;
            line.numCapVertices = 2;
            line.numCornerVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        internal static ParticleSystem MakeParticles(Transform parent, Material material, bool loop, float lifetime,
            float sizeMin, float sizeMax, float speedMin, float speedMax, int maxParticles)
        {
            GameObject go = new GameObject("Berserker_BloodParticles");
            if (parent != null) go.transform.SetParent(parent, false);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.loop = loop;
            main.playOnAwake = false;
            main.duration = 1f;
            main.startLifetime = lifetime;
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.98f, 0.08f, 0.025f, 0.95f),
                new Color(0.09f, 0.004f, 0.006f, 0.88f));
            main.maxParticles = maxParticles;

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }
    }

    internal sealed class BerserkerWeaponSwingVisual : MonoBehaviour
    {
        private const float Lifetime = 0.78f;
        private TrailRenderer trail;
        private LineRenderer[] arcs;
        private ParticleSystem sparks;
        private Vector3 axis;
        private Vector3 side;
        private float extent;
        private float intensity;
        private float bornAt;

        internal void Initialize(Transform weapon, float effectIntensity, Material soft, Material solid)
        {
            bornAt = Time.time;
            intensity = Mathf.Clamp(effectIntensity, 0.8f, 1.8f);
            Bounds bounds;
            Vector3 furthest;
            MeasureWeapon(weapon, out bounds, out furthest);

            Vector3 e = bounds.extents;
            axis = e.x > e.y && e.x > e.z ? Vector3.right : e.z > e.y ? Vector3.forward : Vector3.up;
            side = Vector3.Cross(axis, axis == Vector3.up ? Vector3.forward : Vector3.up).normalized;
            extent = Mathf.Max(0.45f, Mathf.Max(e.x, Mathf.Max(e.y, e.z)));
            transform.localPosition = bounds.center;

            GameObject trailPoint = new GameObject("Berserker_ReckoningTrailPoint");
            trailPoint.transform.SetParent(weapon, false);
            trailPoint.transform.localPosition = furthest;
            trail = trailPoint.AddComponent<TrailRenderer>();
            trail.time = 0.30f;
            trail.minVertexDistance = 0.025f;
            trail.startWidth = 0.20f * intensity;
            trail.endWidth = 0.025f;
            trail.sharedMaterial = soft;
            trail.startColor = new Color(1f, 0.055f, 0.018f, 0.92f);
            trail.endColor = new Color(0.055f, 0.002f, 0.006f, 0f);
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            UnityEngine.Object.Destroy(trailPoint, Lifetime + 0.42f);

            arcs = new LineRenderer[3];
            for (int i = 0; i < arcs.Length; i++)
            {
                arcs[i] = BerserkerEffects.MakeLine(transform, solid, (0.020f + i * 0.006f) * intensity, 9, false);
            }

            sparks = BerserkerEffects.MakeParticles(transform, soft, true, 0.24f, 0.035f, 0.085f, 0.05f, 0.28f, 160);
            ParticleSystem.EmissionModule emission = sparks.emission;
            emission.rateOverTime = 42f * intensity;
            ParticleSystem.ShapeModule shape = sparks.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = axis == Vector3.right
                ? new Vector3(extent * 1.6f, 0.14f, 0.14f)
                : axis == Vector3.forward
                    ? new Vector3(0.14f, 0.14f, extent * 1.6f)
                    : new Vector3(0.14f, extent * 1.6f, 0.14f);
            sparks.Play();

            UnityEngine.Object.Destroy(gameObject, Lifetime + 0.08f);
        }

        private void Update()
        {
            if (arcs == null) return;
            float age = Time.time - bornAt;
            float t = Mathf.Clamp01(age / Lifetime);
            float alpha = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01((t - 0.38f) / 0.62f));
            float now = Time.time * 23f;

            for (int a = 0; a < arcs.Length; a++)
            {
                LineRenderer line = arcs[a];
                if (line == null) continue;
                Color c = a == 0
                    ? new Color(1f, 0.07f, 0.025f, 0.95f * alpha)
                    : new Color(0.20f, 0.004f, 0.008f, 0.74f * alpha);
                line.startColor = line.endColor = c;
                for (int i = 0; i < line.positionCount; i++)
                {
                    float u = i / (float)(line.positionCount - 1);
                    float along = Mathf.Lerp(-extent, extent, u);
                    float jitter = Mathf.Sin(now + i * 2.3f + a * 4.7f) * (0.055f + 0.018f * a) * intensity;
                    float jitter2 = Mathf.Cos(now * 0.71f + i * 1.9f + a) * 0.035f * intensity;
                    line.SetPosition(i, axis * along + side * jitter + Vector3.Cross(axis, side) * jitter2);
                }
            }
        }

        private static void MeasureWeapon(Transform weapon, out Bounds combined, out Vector3 furthest)
        {
            combined = new Bounds(Vector3.zero, new Vector3(0.30f, 1.6f, 0.30f));
            furthest = Vector3.up * 0.8f;
            bool found = false;
            float best = -1f;

            Renderer[] renderers = weapon.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (renderer is ParticleSystemRenderer || renderer is LineRenderer || renderer is TrailRenderer) continue;
                Bounds bounds = renderer.localBounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 sign = new Vector3(
                        (corner & 1) == 0 ? -1f : 1f,
                        (corner & 2) == 0 ? -1f : 1f,
                        (corner & 4) == 0 ? -1f : 1f);
                    Vector3 point = weapon.InverseTransformPoint(
                        renderer.transform.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, sign)));
                    if (!found)
                    {
                        combined = new Bounds(point, Vector3.zero);
                        found = true;
                    }
                    else combined.Encapsulate(point);

                    float distance = point.sqrMagnitude;
                    if (distance > best)
                    {
                        best = distance;
                        furthest = point;
                    }
                }
            }
        }
    }

    internal sealed class BerserkerImpactVisual : MonoBehaviour
    {
        private const float Lifetime = 0.72f;
        private LineRenderer[] rings;
        private LineRenderer[] spokes;
        private float bornAt;
        private float intensity;
        private bool meteor;
        private float exactRadius;

        internal void Initialize(float effectIntensity, bool meteorImpact, Material soft, Material solid, float visualRadius = 0f)
        {
            bornAt = Time.time;
            intensity = Mathf.Clamp(effectIntensity, 0.45f, 2f);
            meteor = meteorImpact;
            exactRadius = Mathf.Max(0f, visualRadius);

            rings = new LineRenderer[2];
            for (int i = 0; i < rings.Length; i++)
            {
                rings[i] = BerserkerEffects.MakeLine(transform, solid, (0.055f - i * 0.016f) * intensity, 49, false);
                rings[i].loop = true;
            }

            spokes = new LineRenderer[6];
            for (int s = 0; s < spokes.Length; s++)
            {
                LineRenderer line = BerserkerEffects.MakeLine(transform, solid, 0.028f * intensity, 5, false);
                spokes[s] = line;
                float angle = (360f / spokes.Length) * s + UnityEngine.Random.Range(-10f, 10f);
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;
                for (int i = 0; i < line.positionCount; i++)
                {
                    float u = i / (float)(line.positionCount - 1);
                    line.SetPosition(i, direction * u * 2.4f * intensity + side * UnityEngine.Random.Range(-0.16f, 0.16f));
                }
            }

            ParticleSystem burst = BerserkerEffects.MakeParticles(transform, soft, false, meteor ? 0.70f : 0.50f,
                0.045f, meteor ? 0.18f : 0.13f, meteor ? 2.0f : 1.5f, meteor ? 6.8f : 5.2f, 320);
            ParticleSystem.MainModule main = burst.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.ShapeModule shape = burst.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.16f;
            burst.Play();
            burst.Emit(meteor ? 118 : 82);

            ParticleSystem smoke = BerserkerEffects.MakeParticles(transform, soft, false, 0.86f, 0.10f, 0.28f, 0.25f, 1.1f, 160);
            ParticleSystem.MainModule smokeMain = smoke.main;
            smokeMain.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.12f, 0.004f, 0.006f, 0.62f),
                new Color(0.34f, 0.015f, 0.008f, 0.45f));
            smokeMain.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.ShapeModule smokeShape = smoke.shape;
            smokeShape.shapeType = ParticleSystemShapeType.Sphere;
            smokeShape.radius = 0.32f;
            smoke.Play();
            smoke.Emit(meteor ? 48 : 30);

            UnityEngine.Object.Destroy(gameObject, Lifetime + 0.25f);
        }

        private void Update()
        {
            if (rings == null) return;
            float t = Mathf.Clamp01((Time.time - bornAt) / Lifetime);
            float eased = 1f - (1f - t) * (1f - t);
            float alpha = 1f - t;
            // Meteor impacts can provide the exact gameplay AoE radius. The larger
            // of the two rings then finishes exactly at that radius, so the VFX does
            // not visually lie about how far the blast reaches.
            float baseRadius = exactRadius > 0f
                ? exactRadius / 3.20f
                : (meteor ? 1.2f : 0.85f) * intensity;

            for (int r = 0; r < rings.Length; r++)
            {
                LineRenderer line = rings[r];
                if (line == null) continue;
                float radius = baseRadius * (0.35f + eased * (2.3f + r * 0.55f));
                Color color = r == 0
                    ? new Color(1f, 0.055f, 0.018f, alpha * 0.88f)
                    : new Color(0.08f, 0.002f, 0.005f, alpha * 0.72f);
                line.startColor = line.endColor = color;
                for (int i = 0; i < line.positionCount; i++)
                {
                    float angle = i / (float)(line.positionCount - 1) * Mathf.PI * 2f;
                    line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0.05f + r * 0.035f, Mathf.Sin(angle) * radius));
                }
            }

            if (spokes == null) return;
            for (int s = 0; s < spokes.Length; s++)
            {
                if (spokes[s] == null) continue;
                Color color = s % 2 == 0
                    ? new Color(0.95f, 0.04f, 0.015f, alpha * 0.82f)
                    : new Color(0.09f, 0.002f, 0.005f, alpha * 0.72f);
                spokes[s].startColor = spokes[s].endColor = color;
                spokes[s].transform.localScale = Vector3.one * (0.58f + eased * 0.85f);
            }
        }
    }

    internal sealed class BerserkerRagnarokCastVisual : MonoBehaviour
    {
        private Player player;
        private Vector3 targetPoint;
        private float targetRadius;
        private LineRenderer[] bolts;
        private LineRenderer ring;
        private LineRenderer targetRing;
        private LineRenderer targetInnerRing;
        private ParticleSystem rise;
        private float bornAt;
        private float duration;
        private float stopAt = -1f;
        private float stopDuration;
        private float nextBoltRefresh;

        internal void Initialize(Player owner, float impactRadius, float castDuration, Material soft, Material solid)
        {
            player = owner;
            targetPoint = owner != null ? owner.transform.position : transform.position;
            targetRadius = Mathf.Max(0.5f, impactRadius);
            duration = Mathf.Clamp(castDuration, 0.2f, 10f);
            bornAt = Time.time;

            bolts = new LineRenderer[5];
            for (int i = 0; i < bolts.Length; i++)
                bolts[i] = BerserkerEffects.MakeLine(transform, solid, i == 0 ? 0.060f : 0.035f, 11, true);

            ring = BerserkerEffects.MakeLine(transform, solid, 0.045f, 49, false);
            ring.loop = true;

            // Exact gameplay telegraph: the outer ring uses the same radius constant
            // as Random.insideUnitCircle when meteor impact points are generated.
            targetRing = BerserkerEffects.MakeLine(transform, solid, 0.070f, 65, true);
            targetRing.loop = true;
            targetInnerRing = BerserkerEffects.MakeLine(transform, soft, 0.13f, 65, true);
            targetInnerRing.loop = true;

            rise = BerserkerEffects.MakeParticles(transform, soft, true, 0.48f, 0.045f, 0.12f, 0.05f, 0.28f, 280);
            ParticleSystem.EmissionModule emission = rise.emission;
            emission.rateOverTime = 74f;
            ParticleSystem.ShapeModule shape = rise.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.62f;
            ParticleSystem.VelocityOverLifetimeModule velocity = rise.velocityOverLifetime;
            velocity.enabled = true;
            velocity.y = new ParticleSystem.MinMaxCurve(2.2f, 5.2f);
            rise.Play();

            RefreshBolts(1f);
            UnityEngine.Object.Destroy(gameObject, duration + 0.8f);
        }

        internal void StopFx(float fadeDuration)
        {
            if (stopAt >= 0f) return;
            stopAt = Time.time;
            stopDuration = Mathf.Max(0.08f, fadeDuration);
            if (rise != null) rise.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            UnityEngine.Object.Destroy(gameObject, stopDuration + 0.38f);
        }

        private void Update()
        {
            if (player == null || player.IsDead())
            {
                StopFx(0.08f);
                return;
            }

            float natural = Mathf.Clamp01((Time.time - bornAt) / Mathf.Max(0.1f, duration));
            float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(natural / 0.18f));
            float fadeOut = 1f;
            if (stopAt >= 0f) fadeOut = 1f - Mathf.Clamp01((Time.time - stopAt) / stopDuration);
            float alpha = fadeIn * fadeOut;

            if (Time.time >= nextBoltRefresh)
            {
                nextBoltRefresh = Time.time + 0.055f;
                RefreshBolts(alpha);
            }
            else ApplyBoltAlpha(alpha);

            if (ring != null)
            {
                float radius = 0.9f + Mathf.Sin(Time.time * 7.5f) * 0.12f;
                ring.startColor = ring.endColor = new Color(0.88f, 0.025f, 0.012f, 0.60f * alpha);
                for (int i = 0; i < ring.positionCount; i++)
                {
                    float angle = i / (float)(ring.positionCount - 1) * Mathf.PI * 2f;
                    ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0.10f, Mathf.Sin(angle) * radius));
                }
            }

            UpdateCastTelegraph(alpha, natural);
        }

        private void UpdateCastTelegraph(float alpha, float castProgress)
        {
            // While the cast is charging, the exact gameplay telegraph follows the
            // berserker on every client. At release, runtime freezes the current
            // player position and all meteors use that fixed center.
            targetPoint = player.transform.position;

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 6.5f);
            float charge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.18f, 1f, castProgress));
            float outerAlpha = alpha * (0.45f + charge * 0.42f + pulse * 0.10f);
            float innerAlpha = alpha * (0.13f + charge * 0.18f + pulse * 0.08f);
            float y = targetPoint.y + 0.08f;

            if (targetRing != null)
            {
                targetRing.startColor = targetRing.endColor = new Color(0.98f, 0.035f, 0.012f, outerAlpha);
                SetWorldCircle(targetRing, targetPoint, y, targetRadius);
            }

            if (targetInnerRing != null)
            {
                float innerRadius = targetRadius * (0.70f + pulse * 0.035f);
                targetInnerRing.startColor = targetInnerRing.endColor = new Color(0.12f, 0.002f, 0.006f, innerAlpha);
                SetWorldCircle(targetInnerRing, targetPoint, y + 0.015f, innerRadius);
            }
        }

        private static void SetWorldCircle(LineRenderer line, Vector3 center, float y, float radius)
        {
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i / (float)(line.positionCount - 1) * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    y,
                    center.z + Mathf.Sin(angle) * radius));
            }
        }

        private void RefreshBolts(float alpha)
        {
            if (bolts == null || player == null) return;
            Vector3 origin = player.GetCenterPoint() + Vector3.up * 0.15f;
            for (int b = 0; b < bolts.Length; b++)
            {
                LineRenderer line = bolts[b];
                if (line == null) continue;
                Vector2 radial = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(0.35f, 1.45f);
                Vector3 end = origin + new Vector3(radial.x, UnityEngine.Random.Range(7.5f, 11.5f), radial.y);
                Vector3 direction = end - origin;
                Vector3 side = Vector3.Cross(direction.normalized, Vector3.up);
                if (side.sqrMagnitude < 0.01f) side = Vector3.right;
                side.Normalize();
                Vector3 secondSide = Vector3.Cross(direction.normalized, side).normalized;

                for (int i = 0; i < line.positionCount; i++)
                {
                    float u = i / (float)(line.positionCount - 1);
                    float envelope = Mathf.Sin(u * Mathf.PI);
                    Vector3 p = Vector3.Lerp(origin, end, u);
                    if (i > 0 && i + 1 < line.positionCount)
                    {
                        p += side * UnityEngine.Random.Range(-0.40f, 0.40f) * envelope;
                        p += secondSide * UnityEngine.Random.Range(-0.24f, 0.24f) * envelope;
                    }
                    line.SetPosition(i, p);
                }
            }
            ApplyBoltAlpha(alpha);
        }

        private void ApplyBoltAlpha(float alpha)
        {
            if (bolts == null) return;
            for (int b = 0; b < bolts.Length; b++)
            {
                if (bolts[b] == null) continue;
                Color color = b == 0
                    ? new Color(1f, 0.075f, 0.025f, 0.92f * alpha)
                    : new Color(0.26f, 0.004f, 0.008f, 0.74f * alpha);
                bolts[b].startColor = bolts[b].endColor = color;
            }
        }
    }

    internal sealed class BerserkerRagnarokReleaseVisual : MonoBehaviour
    {
        private LineRenderer[] bolts;
        private LineRenderer ring;
        private float bornAt;
        private float intensity;
        private float nextRefresh;

        internal void Initialize(float effectIntensity, Material soft, Material solid)
        {
            intensity = Mathf.Clamp(effectIntensity, 1f, 2f);
            bornAt = Time.time;
            bolts = new LineRenderer[7];
            for (int i = 0; i < bolts.Length; i++)
                bolts[i] = BerserkerEffects.MakeLine(transform, solid, (i == 0 ? 0.085f : 0.045f) * intensity, 10, true);

            ring = BerserkerEffects.MakeLine(transform, solid, 0.075f * intensity, 57, false);
            ring.loop = true;

            ParticleSystem burst = BerserkerEffects.MakeParticles(transform, soft, false, 0.82f, 0.06f, 0.19f, 1.0f, 5.8f, 380);
            ParticleSystem.ShapeModule shape = burst.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.45f;
            burst.Play();
            burst.Emit(145);

            RefreshBolts();
            UnityEngine.Object.Destroy(gameObject, 0.78f);
        }

        private void Update()
        {
            float t = Mathf.Clamp01((Time.time - bornAt) / 0.62f);
            float alpha = 1f - t;
            if (Time.time >= nextRefresh)
            {
                nextRefresh = Time.time + 0.07f;
                RefreshBolts();
            }

            if (bolts != null)
            {
                for (int i = 0; i < bolts.Length; i++)
                {
                    if (bolts[i] == null) continue;
                    Color c = i == 0
                        ? new Color(1f, 0.085f, 0.025f, 0.98f * alpha)
                        : new Color(0.18f, 0.003f, 0.007f, 0.82f * alpha);
                    bolts[i].startColor = bolts[i].endColor = c;
                }
            }

            if (ring != null)
            {
                float radius = Mathf.Lerp(0.55f, 4.0f * intensity, 1f - (1f - t) * (1f - t));
                ring.startColor = ring.endColor = new Color(0.95f, 0.04f, 0.012f, 0.78f * alpha);
                for (int i = 0; i < ring.positionCount; i++)
                {
                    float angle = i / (float)(ring.positionCount - 1) * Mathf.PI * 2f;
                    ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0.08f, Mathf.Sin(angle) * radius));
                }
            }
        }

        private void RefreshBolts()
        {
            if (bolts == null) return;
            Vector3 origin = transform.position;
            for (int b = 0; b < bolts.Length; b++)
            {
                LineRenderer line = bolts[b];
                if (line == null) continue;
                Vector2 radial = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(0.6f, 2.2f);
                Vector3 end = origin + new Vector3(radial.x, UnityEngine.Random.Range(9f, 15f), radial.y);
                Vector3 direction = end - origin;
                Vector3 side = Vector3.Cross(direction.normalized, Vector3.up);
                if (side.sqrMagnitude < 0.01f) side = Vector3.right;
                side.Normalize();
                for (int i = 0; i < line.positionCount; i++)
                {
                    float u = i / (float)(line.positionCount - 1);
                    Vector3 p = Vector3.Lerp(origin, end, u);
                    if (i > 0 && i + 1 < line.positionCount)
                        p += side * UnityEngine.Random.Range(-0.65f, 0.65f) * Mathf.Sin(u * Mathf.PI);
                    line.SetPosition(i, p);
                }
            }
        }
    }

    internal sealed class BerserkerMeteorVisual : MonoBehaviour
    {
        private TrailRenderer trail;
        private ParticleSystem embers;
        private float intensity = 1f;
        private float bornAt;
        private Vector3 lastPosition;
        private bool initialized;

        internal void Initialize(float effectIntensity)
        {
            if (initialized) return;
            initialized = true;
            intensity = Mathf.Clamp(effectIntensity, 1f, 1.8f);
            bornAt = Time.time;
            lastPosition = transform.position;

            trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.46f;
            trail.minVertexDistance = 0.10f;
            trail.startWidth = 0.55f * intensity;
            trail.endWidth = 0.06f;
            trail.sharedMaterial = BerserkerEffects.SoftMaterial();
            trail.startColor = new Color(1f, 0.07f, 0.015f, 0.80f);
            trail.endColor = new Color(0.06f, 0.002f, 0.004f, 0f);
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;

            embers = BerserkerEffects.MakeParticles(transform, BerserkerEffects.SoftMaterial(), true, 0.44f,
                0.05f, 0.15f, 0.15f, 0.65f, 260);
            ParticleSystem.EmissionModule emission = embers.emission;
            emission.rateOverTime = 70f * intensity;
            ParticleSystem.ShapeModule shape = embers.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.28f;
            embers.Play();
        }

        private void LateUpdate()
        {
            lastPosition = transform.position;
        }

        private void OnDestroy()
        {
            // Projectile_meteor is destroyed on impact. Ignore immediate teardown during
            // failed spawning, and leave the native projectile responsible for gameplay.
            if (!initialized || !BerserkerEffects.CanRender || Time.time - bornAt < 0.12f) return;
            BerserkerEffects.PlayMeteorImpact(lastPosition, intensity);
        }
    }
}
