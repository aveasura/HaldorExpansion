using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace HaldorExpansion.Features.Berserker
{
    // Distinct visual identity for ONLY the projectiles spawned by Ragnarök.
    // The vanilla projectile, colliders, trajectory, hit data and spawn-on-hit
    // gameplay are deliberately retained. Clients recognize the projectile by
    // a networked ZDO marker, NOT by globally changing projectile_meteor.
    internal static class BerserkerRagnarokMeteorFx
    {
        private const string MeteorPrefabName = "projectile_meteor";
        private const string Marker = "HE_BerserkerRagnarokMeteor_v1";
        private const string IntensityKey = "HE_BerserkerMeteorIntensity_v1";
        private const string ThunderbloodPrefab = "BattleaxeGold_BloodLightning";
        private const string ImpactSoundPrefab = "sfx_golem_attack_hit";

        // Cosmetic tuning: these never influence meteor damage, number or radius.
        internal const float RagnarokMeteorAuraScale = 1.95f;
        internal const float RagnarokMeteorSmokeDarkness = 0.74f;
        internal const float RagnarokMeteorCrimsonGlow = 0.88f;
        internal const float MeteorImpactSmokeScale = 1.15f;

        private static AudioClip impactClip;
        private static bool searchedImpactClip;

        internal static void MarkOwnedMeteor(GameObject meteor, float intensity)
        {
            if (meteor == null) return;
            ZNetView view = meteor.GetComponent<ZNetView>();
            if (view == null || !view.IsValid() || !view.IsOwner() || view.GetZDO() == null)
                return;

            // Only the owner generates the hit effect prefabs. Removing those
            // vanilla ORANGE particles doesn't alter SpawnOnHit, DoAOE or damage.
            view.GetZDO().Set(Marker, true);
            view.GetZDO().Set(IntensityKey, Mathf.Clamp(intensity, 1f, 1.8f));
            Projectile projectile = meteor.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.m_hitEffects = new EffectList();
                projectile.m_hitWaterEffects = new EffectList();
                projectile.m_spawnOnHitEffects = new EffectList();
            }
        }

        internal static void CheckNetworkMeteor(Projectile projectile)
        {
            if (projectile == null || !BerserkerEffects.CanRender) return;
            GameObject go = projectile.gameObject;
            if (go == null || !go.name.StartsWith(MeteorPrefabName, StringComparison.Ordinal) ||
                go.GetComponent<BerserkerMeteorVisual>() != null) return;
            ZNetView view = go.GetComponent<ZNetView>();
            if (view == null || !view.IsValid() || view.GetZDO() == null ||
                !view.GetZDO().GetBool(Marker)) return;

            float intensity = Mathf.Clamp(view.GetZDO().GetFloat(IntensityKey, 1f), 1f, 1.8f);
            BerserkerEffects.AttachMeteorVisualByIntensity(go, intensity);
        }

        internal static Transform ConfigureFlight(GameObject meteor, float intensity)
        {
            if (meteor == null) return null;

            // Strip ONLY the native orange flight presentation from this replicated
            // Ragnarök meteor instance. Projectile, colliders, trajectory and hit
            // data are deliberately left untouched.
            foreach (ParticleSystem native in meteor.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (native == null) continue;
                native.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ParticleSystemRenderer psRenderer = native.GetComponent<ParticleSystemRenderer>();
                if (psRenderer != null) psRenderer.enabled = false;
            }
            foreach (TrailRenderer nativeTrail in meteor.GetComponentsInChildren<TrailRenderer>(true))
                if (nativeTrail != null) nativeTrail.enabled = false;
            foreach (Light nativeLight in meteor.GetComponentsInChildren<Light>(true))
                if (nativeLight != null) nativeLight.enabled = false;

            // The rock is now deliberately dark and only partly readable through the
            // storm. The crimson emission gives the impression of pressure/heat inside
            // cracks without turning the projectile into a conventional orange fireball.
            MaterialPropertyBlock dark = new MaterialPropertyBlock();
            dark.SetColor("_Color", new Color(0.075f, 0.012f, 0.018f, 1f));
            dark.SetColor("_BaseColor", new Color(0.075f, 0.012f, 0.018f, 1f));
            dark.SetColor("_EmissionColor", new Color(0.62f, 0.010f, 0.026f, 1f));
            foreach (MeshRenderer renderer in meteor.GetComponentsInChildren<MeshRenderer>(true))
                if (renderer != null) renderer.SetPropertyBlock(dark);
            foreach (SkinnedMeshRenderer renderer in meteor.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (renderer != null) renderer.SetPropertyBlock(dark);

            GameObject vfxRoot = new GameObject("Ragnarok_MeteorFlightVFX");
            vfxRoot.transform.SetParent(meteor.transform, false);
            vfxRoot.transform.localPosition = Vector3.zero;
            vfxRoot.transform.localRotation = Quaternion.identity;

            // Keep the emitter alive independently so WORLD-space smoke can fade
            // after the projectile is destroyed. The visual driver follows the rock.
            vfxRoot.transform.SetParent(null, true);
            vfxRoot.transform.localScale = Vector3.one;

            float radius = 0.65f;
            foreach (MeshRenderer renderer in meteor.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer == null || !renderer.enabled) continue;
                Vector3 extent = renderer.bounds.extents;
                radius = Mathf.Max(radius, Mathf.Max(extent.x, Mathf.Max(extent.y, extent.z)));
            }
            radius = Mathf.Clamp(radius, 0.45f, 1.8f);

            // Independent materials: textured soot is alpha blended; flame uses a
            // tapered, broken silhouette, never the axe's round cloud sprite.
            CreateFlightLayer(vfxRoot.transform, "BlackMantle", false, true, radius,
                0.20f, 0.34f, 0.55f, 1.00f, 0.8f, 2.0f, 100f * intensity,
                new Color(0.085f, 0.013f, 0.020f), new Color(0.035f, 0.025f, 0.030f), 0.52f);
            CreateFlightLayer(vfxRoot.transform, "CrimsonSkin", true, true, radius,
                0.16f, 0.30f, 0.38f, 0.75f, 2.5f, 5.5f, 115f * intensity,
                new Color(1f, 0.12f, 0.065f), new Color(0.40f, 0.006f, 0.020f), 0.82f);
            CreateFlightLayer(vfxRoot.transform, "FlameWake", true, false, radius,
                0.18f, 0.36f, 0.50f, 1.00f, 2.5f, 5.5f, 125f * intensity,
                new Color(1f, 0.30f, 0.12f), new Color(0.32f, 0.008f, 0.022f), 0.86f);
            CreateFlightLayer(vfxRoot.transform, "SmokeWake", false, false, radius,
                0.65f, 1.10f, 0.85f, 1.45f, 0.4f, 1.5f, 65f * intensity,
                new Color(0.14f, 0.025f, 0.032f), new Color(0.065f, 0.058f, 0.060f), 0.38f);

            Light coreLight = vfxRoot.AddComponent<Light>();
            coreLight.type = LightType.Point;
            coreLight.color = new Color(1f, 0.045f, 0.025f);
            coreLight.range = 4.5f * radius;
            coreLight.intensity = 1.8f * intensity;
            coreLight.shadows = LightShadows.None;
            return vfxRoot.transform;
        }

        private static Material flameMaterial;
        private static Material smokeMaterial;

        private static Material FlightMaterial(bool flame)
        {
            Material cached = flame ? flameMaterial : smokeMaterial;
            if (cached != null) return cached;
            Material source = BerserkerEffects.SoftMaterial();
            if (source == null) return null;
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = flame ? "Ragnarok_TornFlame" : "Ragnarok_TurbulentSoot";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)(size - 1);
                float v = y / (float)(size - 1);
                float noise = 0.60f * Mathf.PerlinNoise(u * 6.3f + 13f, v * 6.3f + 29f)
                    + 0.28f * Mathf.PerlinNoise(u * 14f + 7f, v * 14f + 3f)
                    + 0.12f * Mathf.PerlinNoise(u * 31f, v * 31f);
                float alpha;
                if (flame)
                {
                    // Broad root, several ragged tapering tips, no rectangular
                    // ends or opaque circular outline when sprites overlap.
                    float center = 0.50f + 0.12f * Mathf.Sin(v * 11f) * v;
                    float width = Mathf.Lerp(0.40f, 0.025f, v) * (0.65f + noise);
                    float edge = Mathf.Clamp01((width - Mathf.Abs(u - center)) * 12f);
                    float ends = Mathf.Clamp01(v * 12f) * Mathf.Clamp01((1f - v) * 8f);
                    alpha = edge * ends * Mathf.Clamp01((noise - 0.27f) * 3.3f);
                }
                else
                {
                    float dx = (u - 0.5f) * 2f;
                    float dy = (v - 0.5f) * 2f;
                    float edge = Mathf.Clamp01((1f - Mathf.Sqrt(dx * dx + dy * dy)) * 2f);
                    alpha = edge * edge * Mathf.Clamp01((noise - 0.30f) * 2.5f);
                }
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            cached = new Material(source);
            cached.name = texture.name;
            cached.mainTexture = texture;
            // Sprites/Default uses premultiplied alpha and can render black smoke.
            // Do not use an additive material: black would become invisible.
            if (flame) flameMaterial = cached;
            else smokeMaterial = cached;
            return cached;
        }

        private static void CreateFlightLayer(Transform root, string name, bool flame,
            bool attached, float radius, float lifeMin, float lifeMax,
            float sizeMin, float sizeMax, float speedMin, float speedMax,
            float rate, Color hot, Color cold, float opacity)
        {
            ParticleSystem ps = BerserkerEffects.MakeParticles(root, FlightMaterial(flame),
                true, lifeMax, sizeMin * radius, sizeMax * radius,
                speedMin * radius, speedMax * radius, attached ? 128 : 240);
            ps.name = "Ragnarok_" + name;
            var main = ps.main;
            main.simulationSpace = attached ? ParticleSystemSimulationSpace.Local : ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startColor = Color.white; // Lifetime colour is not multiplied by a dark start tint.
            main.startRotation = flame ? new ParticleSystem.MinMaxCurve(0f)
                : new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            main.gravityModifier = 0f;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = attached ? ParticleSystemShapeType.Sphere : ParticleSystemShapeType.Cone;
            shape.radius = radius * (attached ? 0.83f : 0.52f);
            shape.radiusThickness = attached ? 0.15f : 1f;
            shape.angle = 12f;
            if (attached)
            {
                main.startSpeed = 0f;
                var velocity = ps.velocityOverLifetime;
                velocity.enabled = true;
                velocity.space = ParticleSystemSimulationSpace.Local;
                velocity.z = new ParticleSystem.MinMaxCurve(speedMin * radius, speedMax * radius);
                // All axes use the same curve mode, as required by Unity.
                velocity.x = new ParticleSystem.MinMaxCurve(-0.35f * radius, 0.35f * radius);
                velocity.y = new ParticleSystem.MinMaxCurve(-0.35f * radius, 0.35f * radius);
            }
            // World wake uses only initial cone velocity: old smoke never turns
            // when the emitter changes direction. No inherited projectile speed.
            var emission = ps.emission;
            emission.rateOverTime = rate;
            emission.rateOverDistance = attached ? 0f : (flame ? 5f : 3f) / radius;
            var noiseModule = ps.noise;
            noiseModule.enabled = true;
            noiseModule.strength = (flame ? 0.65f : 0.85f) * radius;
            noiseModule.frequency = flame ? 2.1f : 1.1f;
            noiseModule.scrollSpeed = flame ? 1.6f : 0.55f;
            noiseModule.octaveCount = 2;
            noiseModule.damping = true;

            Gradient gradient = new Gradient();
            gradient.SetKeys(new[] {
                new GradientColorKey(hot, 0f),
                new GradientColorKey(Color.Lerp(hot, cold, 0.45f), 0.38f),
                new GradientColorKey(cold, 1f)
            }, new[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(opacity, 0.08f),
                new GradientAlphaKey(opacity * 0.65f, 0.48f),
                new GradientAlphaKey(0f, 1f)
            });
            var color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(gradient);
            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, flame
                ? new AnimationCurve(new Keyframe(0f, 0.65f), new Keyframe(0.18f, 1f), new Keyframe(1f, 0.12f))
                : new AnimationCurve(new Keyframe(0f, 0.65f), new Keyframe(0.5f, 1.15f), new Keyframe(1f, 1.75f)));
            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = flame ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            renderer.lengthScale = 1.65f;
            renderer.velocityScale = 0.035f;
            renderer.cameraVelocityScale = 0f;
            renderer.sortingFudge = flame ? 0.15f : 0f;
            // Started by the driver only after its first measured movement;
            // otherwise the first particles shoot along the prefab's forward axis.
        }

        internal static void StopFlight(Transform root)
        {
            if (root == null) return;
            foreach (ParticleSystem ps in root.GetComponentsInChildren<ParticleSystem>())
                ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            Light light = root.GetComponent<Light>();
            if (light != null) light.enabled = false;
            UnityEngine.Object.Destroy(root.gameObject, 1.3f);
        }

        private static ParticleSystem CloneThunderbloodParticle(Transform parent, string child)
        {
            GameObject weapon = ZNetScene.instance != null
                ? ZNetScene.instance.GetPrefab(ThunderbloodPrefab) : null;
            Transform attach = weapon != null ? weapon.transform.Find("attach") : null;
            Transform axe = attach != null ? attach.Find("battleaxe") : null;
            Transform vfx = axe != null ? axe.Find("VFX (1)") : null;
            Transform source = vfx != null ? vfx.Find(child) : null;
            if (source == null) return null;
            GameObject copy = UnityEngine.Object.Instantiate(source.gameObject, parent, false);
            copy.name = "Ragnarok_Meteor_" + source.name;
            copy.transform.localPosition = Vector3.zero;
            copy.transform.localRotation = Quaternion.identity;
            copy.transform.localScale = Vector3.one;
            copy.SetActive(true);
            ParticleSystem ps = copy.GetComponent<ParticleSystem>();
            if (ps == null)
            {
                UnityEngine.Object.Destroy(copy);
                return null;
            }
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return ps;
        }

        internal static void SpawnCrimsonImpact(Vector3 hitPoint, float intensity)
        {
            GameObject root = new GameObject("Ragnarok_CharredMeteorImpact");
            root.transform.position = hitPoint;

            // Low charcoal-red ground cloud instead of Yagluth's orange fireball.
            for (int i = 0; i < 5; i++)
            {
                ParticleSystem smoke = CloneThunderbloodParticle(root.transform, "Clouds");
                if (smoke == null) break;
                smoke.transform.localPosition = new Vector3(
                    UnityEngine.Random.Range(-1.4f, 1.4f), 0.12f,
                    UnityEngine.Random.Range(-1.4f, 1.4f));
                var main = smoke.main;
                main.loop = false;
                main.playOnAwake = false;
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 2.2f);
                main.startSize = new ParticleSystem.MinMaxCurve(1.0f * MeteorImpactSmokeScale,
                    2.0f * MeteorImpactSmokeScale);
                main.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(0.11f, 0.015f, 0.025f, 0.80f),
                    new Color(0.40f, 0.035f, 0.05f, 0.67f));
                main.startSpeed = new ParticleSystem.MinMaxCurve(1.3f, 3.8f);
                main.gravityModifier = -0.05f;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                var emission = smoke.emission;
                emission.rateOverTime = 0f;
                var shape = smoke.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.65f;
                smoke.Play();
                smoke.Emit(Mathf.RoundToInt(18f * intensity));
            }

            // Rock fragments: concentrated, low, dark pieces; don't use a
            // Yagluth-like orange sphere and avoid unbounded per-impact particles.
            ParticleSystem rubble = BerserkerEffects.MakeParticles(root.transform,
                BerserkerEffects.SolidMaterial(), false, 0.95f,
                0.10f, 0.28f, 3.3f, 8.0f, 100);
            var rocks = rubble.main;
            rocks.simulationSpace = ParticleSystemSimulationSpace.World;
            rocks.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.12f, 0.08f, 0.095f, 0.95f),
                new Color(0.45f, 0.06f, 0.075f, 0.95f));
            rocks.gravityModifier = 2.5f;
            var rubbleShape = rubble.shape;
            rubbleShape.shapeType = ParticleSystemShapeType.Hemisphere;
            rubbleShape.radius = 0.34f;
            rubble.Play();
            rubble.Emit(Mathf.RoundToInt(52f * intensity));

            // Radial impact cracks use genuine Thunderblood trails, not the
            // sharp procedurally regenerated lines from the early cast versions.
            for (int i = 0; i < 3; i++)
            {
                ParticleSystem arc = CloneThunderbloodParticle(root.transform, "Right/Lightning (3)");
                if (arc == null) break;
                arc.transform.localPosition = new Vector3(
                    UnityEngine.Random.Range(-0.50f, 0.50f), 0.24f,
                    UnityEngine.Random.Range(-0.50f, 0.50f));
                var main = arc.main;
                main.loop = false;
                main.playOnAwake = false;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.48f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.09f);
                main.startColor = new Color(1f, 0.15f, 0.15f, 0.83f);
                var emission = arc.emission;
                emission.rateOverTime = 0f;
                arc.Play();
                arc.Emit(Mathf.RoundToInt(12f * intensity));
            }

            PlayImpactAudio(hitPoint);
            UnityEngine.Object.Destroy(root, 3.6f);
        }

        private static void PlayImpactAudio(Vector3 point)
        {
            if (!searchedImpactClip)
            {
                GameObject prefab = ZNetScene.instance != null
                    ? ZNetScene.instance.GetPrefab(ImpactSoundPrefab) : null;
                if (prefab != null)
                {
                    searchedImpactClip = true;
                    foreach (ZSFX sound in prefab.GetComponentsInChildren<ZSFX>(true))
                    {
                        if (sound == null || sound.m_audioClips == null) continue;
                        foreach (AudioClip clip in sound.m_audioClips)
                        {
                            if (clip == null) continue;
                            impactClip = clip;
                            break;
                        }
                        if (impactClip != null) break;
                    }
                }
            }
            if (impactClip == null) return;
            GameObject host = new GameObject("Ragnarok_MeteorStoneImpactSfx");
            host.transform.position = point;
            AudioSource audio = host.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.clip = impactClip;
            audio.spatialBlend = 1f;
            audio.minDistance = 4f;
            audio.maxDistance = 48f;
            audio.rolloffMode = AudioRolloffMode.Logarithmic;
            audio.pitch = UnityEngine.Random.Range(0.78f, 0.91f);
            audio.volume = 0.58f * AudioMan.GetSFXVolume();
            audio.Play();
            UnityEngine.Object.Destroy(host, impactClip.length / audio.pitch + 0.2f);
        }
    }

    // A non-invasive hook: the game creates projectile_meteor on each client
    // from the vanilla prefab. Once the replicated marker arrives, attach the
    // Ragnarök-only cosmetic replacement on that client, including dedicated MP.
    [HarmonyPatch(typeof(Projectile), "Update")]
    internal static class BerserkerRagnarokMeteorNetworkVisualPatch
    {
        private static void Postfix(Projectile __instance)
        {
            BerserkerRagnarokMeteorFx.CheckNetworkMeteor(__instance);
        }
    }
}
