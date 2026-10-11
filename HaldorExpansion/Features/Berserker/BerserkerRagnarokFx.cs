using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HaldorExpansion.Features.Berserker
{
    // Cosmetic-only extension of the existing Ragnarök cast/release/meteor RPCs.
    // No projectile, AoE, damage, Debt, or cast commitment logic belongs here.
    // Clone only selected Thunderblood VFX children: native Clouds smoke and
    // short Lightning (3) trails, never the networked weapon or its gameplay.
    internal static class BerserkerRagnarokFx
    {
        private const string CastSoundPrefab = "sfx_fader_fissure_footslide";

        // AUDIO ONLY: delay from accepted E to the second sound. No gameplay timing changes.
        private const float CastBellDelaySeconds = 1.30f;
        
        private const string CastBellPrefab = "sfx_fader_death_start";
        private const string ThunderbloodPrefab = "BattleaxeGold_BloodLightning";

        // VFX ONLY: seconds after an accepted E before the smoke starts leaving
        // the berserker. This stopwatch begins immediately in StartCast().
        internal const float SkywardAuraStartDelaySeconds = 0.30f;

        // VFX ONLY: continuous skyward smoke emission AFTER the delay above.
        // Example: E at 0s, delay=0.30s, duration=1.10s => emission runs from 0.30s to 1.40s.
        // Neither timer affects the emote, cast sound, meteor release or gameplay.
        internal const float SkywardAuraEmissionSeconds = 1.10f;

        // VFX ONLY: move the Thunderblood lightning emission closer to the
        // raised axe/hand and higher toward the sky (Unity world units).
        // This applies to both cast and release lightning, not to the smoke.
        internal const float ThunderbloodLightningHeightOffset = 0.85f;

        // VFX ONLY: native Thunderblood lightning particle thickness.
        // 1.00f = previous thickness, 1.20f = 20% thicker, 1.50f = 50% thicker.
        internal const float ThunderbloodLightningThicknessMultiplier = 2.50f;

        // VFX ONLY: multiplies how far the lightning moves vertically during
        // its lifetime, without changing where it spawns, its thickness or its
        // lifetime. 1.00f = previous reach; 1.50f = ~50% more upward travel.
        // Applies to both casting trails and the final release accent.
        internal const float ThunderbloodLightningLengthMultiplier = 2.50f;

        private static bool searchedCastAudio;
        private static AudioClip castAudio;

        internal static void OnCast(Player player, Transform castVisualRoot, float duration, float auraStrength)
        {
            if (player == null || castVisualRoot == null) return;
            // Local caster already started the audio timeline and smoke stopwatch at E.
            // The subsequent cast RPC must not double-play audio or reset VFX.
            if (player == Player.m_localPlayer &&
                player.GetComponentInChildren<BerserkerRagnarokAscension>() != null)
                return;

            PlayCastSound(player);
            ScheduleCastBell(player, CastBellDelaySeconds);
            BeginSkywardAura(player, auraStrength);
        }

        // Called immediately after an accepted E press, before entering the
        // coroutine / sending any RPCs. The two editable timers are E-relative.
        internal static void BeginLocalSkywardAura(Player player, float auraStrength)
        {
            if (player == null || !BerserkerEffects.CanRender) return;
            // First sound starts at E; the second is scheduled independently of the smoke.
            PlayCastSound(player);
            ScheduleCastBell(player, CastBellDelaySeconds);
            BeginSkywardAura(player, auraStrength);
        }

        private static void BeginSkywardAura(Player player, float auraStrength)
        {
            if (player == null) return;

            // Freeze the body's palette at accepted E; RPC observers use the same snapshot.
            // World-space torn flames leave the player and survive the end of the cast.
            Gradient palette = BerserkerWrathAura.InfernoGradient(auraStrength);
            foreach (var previous in player.GetComponentsInChildren<BerserkerRagnarokAscension>())
                if (previous != null)
                    previous.Finish();

            GameObject smokeRoot = new GameObject("Berserker_RagnarokSkywardAura");
            smokeRoot.transform.SetParent(player.transform, false);
            smokeRoot.transform.localPosition = Vector3.zero;
            BerserkerRagnarokAscension ascent =
                smokeRoot.AddComponent<BerserkerRagnarokAscension>();
            ascent.Initialize(player, SkywardAuraStartDelaySeconds, SkywardAuraEmissionSeconds);

            // Staggered circumferential emitters make a cylindrical VOLUME instead
            // of a flat vertical sheet or a single streamer through the torso.
            Vector3[] sources =
            {
                new Vector3(-0.34f, 0.28f, 0.15f),
                new Vector3(0.34f, 0.28f, -0.15f),
                new Vector3(0.00f, 0.50f, 0.38f),
                new Vector3(0.00f, 0.52f, -0.38f),
                new Vector3(-0.46f, 1.02f, 0.04f),
                new Vector3(0.46f, 1.02f, -0.04f),
                new Vector3(0.06f, 1.15f, 0.39f),
                new Vector3(-0.06f, 1.17f, -0.39f),
                new Vector3(-0.31f, 1.70f, 0.02f),
                new Vector3(0.31f, 1.70f, -0.02f),
                new Vector3(0.00f, 2.08f, 0.18f),
                new Vector3(0.00f, 2.12f, -0.18f)
            };

            for (int i = 0; i < sources.Length; i++)
            {
                bool high = i >= 8;
                float radius = high ? 0.24f : 0.34f;
                float rate = high ? 12f : 16f;
                ParticleSystem ps = MakeAscendingFlame(smokeRoot.transform, sources[i], palette,
                    radius, rate, auraStrength);
                ascent.Register(ps, rate);
            }

            // Small hot sparks follow the same palette and upward flow.
            ParticleSystem sparks = BerserkerEffects.MakeParticles(smokeRoot.transform, BerserkerEffects.SoftMaterial(),
                true, 0.86f, 0.040f, 0.10f, 0.35f, 1.30f, 190);
            var main = sparks.main;
            main.startColor = palette.Evaluate(0.12f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var shape = sparks.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.position = new Vector3(0f, 1.08f, 0f);
            shape.radius = 0.44f;
            var emission = sparks.emission;
            emission.rateOverTime = 46f;
            var velocity = sparks.velocityOverLifetime;
            velocity.enabled = false;
            velocity.space = ParticleSystemSimulationSpace.World;
            // Unity requires x/y/z velocity curves to share a single mode.
            // TwoConstants on ALL three axes, including zero horizontal velocity.
            velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.y = new ParticleSystem.MinMaxCurve(5.5f, 9.5f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.enabled = true;
            // Sparks belong to the same delayed emission, not to the cast
            // telegraph which disappears when the meteors are released.
            ascent.RegisterSparks(sparks, 46f);

            // Native Thunderblood Lightning (3) trails replace the procedural,
            // frame-by-frame red LineRenderers. They share the smoke's VISUAL-ONLY
            // timers so no cracks shoot into the sky before Wrath is released.
            Vector3[] lightningAnchors =
            {
                new Vector3(-0.28f, 0.73f, 0.06f),
                new Vector3(0.28f, 0.91f, -0.06f),
                new Vector3(0.03f, 1.56f, 0.20f)
            };
            foreach (Vector3 anchor in lightningAnchors)
            {
                ParticleSystem lightning = CreateThunderbloodLightning(smokeRoot.transform, anchor, false);
                if (lightning != null)
                {
                    var lightningMain = lightning.main;
                    lightningMain.startColor = palette.Evaluate(0.12f);
                    ascent.RegisterSparks(lightning, 10f);
                }
            }
        }

        // Only the native VFX child is copied, not the weapon, networked root,
        // point lights, damage colliders or audio. Missing prefab -> skip safely.
        private static ParticleSystem CreateThunderbloodLightning(Transform parent, Vector3 localPosition, bool release)
        {
            var scene = ZNetScene.instance;
            GameObject weapon = scene != null ? scene.GetPrefab(ThunderbloodPrefab) : null;
            Transform attach = weapon != null ? weapon.transform.Find("attach") : null;
            Transform axe = attach != null ? attach.Find("battleaxe") : null;
            Transform vfx = axe != null ? axe.Find("VFX (1)") : null;
            Transform right = vfx != null ? vfx.Find("Right") : null;
            Transform source = right != null ? right.Find("Lightning (3)") : null;
            if (source == null) return null;

            GameObject copy = Object.Instantiate(source.gameObject, parent, false);
            copy.name = release ? "Ragnarok_ThunderbloodReleaseSpark" : "Ragnarok_ThunderbloodCastSpark";
            copy.transform.localPosition = localPosition +
                                           Vector3.up * ThunderbloodLightningHeightOffset;
            copy.transform.localRotation = Quaternion.identity;
            copy.transform.localScale = Vector3.one;
            copy.SetActive(true);
            ParticleSystem ps = copy.GetComponent<ParticleSystem>();
            if (ps == null)
            {
                Object.Destroy(copy);
                return null;
            }

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = !release;
            main.startLifetime = new ParticleSystem.MinMaxCurve(release ? 0.35f : 0.40f,
                release ? 0.60f : 0.72f);
            main.startSize = new ParticleSystem.MinMaxCurve(
                0.019f * ThunderbloodLightningThicknessMultiplier,
                (release ? 0.050f : 0.037f) * ThunderbloodLightningThicknessMultiplier);
            main.startSpeed = 0f;
            main.startColor = new Color(1f, 0.33f, 0.24f, release ? 0.62f : 0.43f);
            main.maxParticles = release ? 70 : 46;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = release ? 0.32f : 0.25f;

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = false;
            velocity.space = ParticleSystemSimulationSpace.World;
            // Same TwoConstants mode on x/y/z (Unity Particle Velocity rule).
            velocity.x = new ParticleSystem.MinMaxCurve(-0.45f, 0.45f);
            // Travel distance = upward velocity x particle lifetime. Scale only
            // the vertical component so the emission anchor stays unchanged.
            velocity.y = new ParticleSystem.MinMaxCurve(
                (release ? 4.5f : 3.0f) * ThunderbloodLightningLengthMultiplier,
                (release ? 7.0f : 5.5f) * ThunderbloodLightningLengthMultiplier);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.45f, 0.45f);
            velocity.enabled = true;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(0.08f, 0.19f);
            noise.frequency = 0.48f;
            noise.scrollSpeed = 0.30f;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            return ps;
        }

        // A short accent at meteor release. Never spawns the axe prefab itself.
        internal static void SpawnReleaseLightning(Transform parent, float intensity)
        {
            if (parent == null) return;
            Vector3[] anchors =
            {
                new Vector3(-0.34f, -0.54f, 0.10f),
                new Vector3(0.34f, -0.54f, -0.10f),
                new Vector3(0.00f, 0.04f, 0.24f),
                new Vector3(0.00f, 0.26f, -0.24f)
            };
            int particles = Mathf.RoundToInt(7f * Mathf.Clamp(intensity, 1f, 2f));
            foreach (Vector3 anchor in anchors)
            {
                ParticleSystem ps = CreateThunderbloodLightning(parent, anchor, true);
                if (ps == null) continue;
                ps.Play();
                ps.Emit(particles);
            }
        }

        internal static void OnRelease(Vector3 center, float intensity)
        {
            var root = new GameObject("Berserker_RagnarokNativeRelease");
            root.transform.position = center;
            // The rage accumulated through the cast now surges off the body.
            // Keep this world-space: the column remains where it was unleashed
            // even as the player regains control and the Wrath aura vanishes.
            Vector3[] burstOrigins =
            {
                new Vector3(-0.23f, 0.45f, 0.07f),
                new Vector3(0.23f, 0.45f, -0.07f),
                new Vector3(0.00f, 0.65f, 0.19f),
                new Vector3(0.00f, 0.65f, -0.19f),
                new Vector3(-0.17f, 1.35f, 0.04f),
                new Vector3(0.17f, 1.35f, -0.04f),
                new Vector3(0.00f, 2.00f, 0.00f)
            };
            for (int i = 0; i < burstOrigins.Length; i++)
            {
                bool high = i == burstOrigins.Length - 1;
                var cloud = MakeAscendingCloud(root.transform, burstOrigins[i],
                    high
                        ? new Color(1f, 0.39f, 0.30f, 0.55f)
                        : new Color(1f, 0.23f, 0.17f, 0.68f),
                    high ? 0.16f : 0.24f, 0f, 1.0f);
                if (cloud == null) continue;
                var cloudMain = cloud.main;
                cloudMain.loop = false;
                // This is just a release accent, not the main aura emission.
                // The old 500+ particle burst visually hid the E-timed plume.
                cloud.Emit(high ? 12 : 20);
            }

            var ash = BerserkerEffects.MakeParticles(root.transform, BerserkerEffects.SoftMaterial(), false,
                0.90f, 0.040f, 0.12f, 2.3f, 5.2f, 130);
            var ashMain = ash.main;
            ashMain.startColor = new Color(1f, 0.69f, 0.37f, 0.78f);
            ashMain.simulationSpace = ParticleSystemSimulationSpace.World;
            var ashShape = ash.shape;
            ashShape.shapeType = ParticleSystemShapeType.Sphere;
            ashShape.radius = 0.52f;
            ashShape.position = new Vector3(0f, 1.4f, 0f);
            var emission = ash.emission;
            emission.rateOverTime = 0f;
            ash.Play();
            ash.Emit(Mathf.RoundToInt(65f * Mathf.Clamp(intensity, 1f, 1.9f)));
            Object.Destroy(root, 3.5f);
        }

        // AUDIO ONLY: second sound has its own E-relative delay on each client.
        // Running on the player's MonoBehaviour stops the timer if they despawn
        // (including leaving the world), without touching Ragnarök gameplay.
        private static void ScheduleCastBell(Player player, float delaySeconds)
        {
            if (player == null) return;
            if (delaySeconds <= 0f)
            {
                PlayCastBell(player);
                return;
            }

            player.StartCoroutine(PlayCastBellDelayed(player, delaySeconds));
        }

        private static IEnumerator PlayCastBellDelayed(Player player, float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            if (player == null || player.IsDead() || Player.m_localPlayer == null || ZNetScene.instance == null)
                yield break;
            PlayCastBell(player);
        }

        // Play the actual second vanilla sound prefab, retaining its own ZSFX
        // clips, volume, pitch and spatial settings (just like an in-game spawn).
        private static void PlayCastBell(Player player)
        {
            if (player == null) return;
            try
            {
                GameObject bell = ZNetScene.instance != null
                    ? ZNetScene.instance.GetPrefab(CastBellPrefab)
                    : null;
                if (bell == null)
                {
                    HaldorExpansionPlugin.Log?.LogWarning(
                        "[Berserker] Ragnarok cast bell prefab unavailable: " + CastBellPrefab);
                    return;
                }

                // A networked vanilla prefab replicates from the caster once.
                // Non-networked sounds play once per client via the existing cast RPC.
                bool networked = bell.GetComponentInChildren<ZNetView>(true) != null;
                if (networked && player != Player.m_localPlayer) return;

                Object.Instantiate(bell, player.GetCenterPoint(), Quaternion.identity);
            }
            catch (Exception e)
            {
                // Missing/changed cosmetic assets cannot prevent Ragnarök casting.
                HaldorExpansionPlugin.Log?.LogWarning(
                    "[Berserker] Ragnarok cast bell failed: " + e.Message);
            }
        }

        private static void PlayCastSound(Player player)
        {
            if (!searchedCastAudio)
            {
                GameObject prefab = ZNetScene.instance != null
                    ? ZNetScene.instance.GetPrefab(CastSoundPrefab)
                    : null;
                if (prefab != null)
                {
                    foreach (ZSFX sfx in prefab.GetComponentsInChildren<ZSFX>(true))
                    {
                        if (sfx == null || sfx.m_audioClips == null) continue;
                        foreach (AudioClip clip in sfx.m_audioClips)
                        {
                            if (clip == null) continue;
                            castAudio = clip;
                            break;
                        }

                        if (castAudio != null) break;
                    }

                    if (castAudio == null)
                        foreach (AudioSource s in prefab.GetComponentsInChildren<AudioSource>(true))
                        {
                            if (s == null || s.clip == null) continue;
                            castAudio = s.clip;
                            break;
                        }

                    searchedCastAudio = true;
                }
            }

            if (castAudio == null) return;

            bool local = player == Player.m_localPlayer;
            var host = new GameObject("Berserker_RagnarokCastSfx");
            host.transform.position = player.GetCenterPoint();
            var audio = host.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.clip = castAudio;
            audio.spatialBlend = local ? 0f : 0.92f;
            audio.rolloffMode = AudioRolloffMode.Logarithmic;
            audio.minDistance = 3f;
            audio.maxDistance = 34f;
            audio.dopplerLevel = 0f;
            // ~30% slower playback than the previous 0.96 pitch: longer roar.
            audio.pitch = 1f; //0.96f * 0.70f;
            audio.volume = (local ? 0.82f : 0.62f) * AudioMan.GetSFXVolume();
            audio.Play();
            Object.Destroy(host, castAudio.length / audio.pitch + 0.3f);
        }

        private static ParticleSystem MakeCloud(Transform parent, Vector3 local, bool loop, float duration,
            float minSize, float maxSize, float speedMin, float speedMax, float radius, float rate,
            Color tint, int burst = 0, bool destroyWithTimer = true)
        {
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(ThunderbloodPrefab) : null;
            Transform attach = prefab != null ? prefab.transform.Find("attach") : null;
            Transform axe = attach != null ? attach.Find("battleaxe") : null;
            Transform vfx = axe != null ? axe.Find("VFX (1)") : null;
            Transform clouds = vfx != null ? vfx.Find("Clouds") : null;
            if (clouds == null) return null;

            // Clouds itself contains no gameplay/network components. All these
            // copies are local visuals, never registered with ZNetScene.
            var copy = Object.Instantiate(clouds.gameObject, parent, false);
            copy.name = loop ? "Ragnarok_CastWispySmoke" : "Ragnarok_BurstWispySmoke";
            copy.transform.localPosition = local;
            copy.transform.localRotation = Quaternion.identity;
            copy.transform.localScale = Vector3.one;
            copy.SetActive(true);
            ParticleSystem ps = copy.GetComponent<ParticleSystem>();
            if (ps == null)
            {
                Object.Destroy(copy);
                return null;
            }

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = loop;
            main.startLifetime = new ParticleSystem.MinMaxCurve(loop ? 0.76f : 0.55f, loop ? 1.42f : 1.14f);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startColor = tint;
            main.maxParticles = 95;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = rate;
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = false;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.09f, 0.09f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.35f, 0.92f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.09f, 0.09f);
            velocity.enabled = true;
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(0.08f, 0.20f);
            noise.frequency = 0.54f;
            noise.scrollSpeed = 0.38f;
            ps.Play();
            if (burst > 0) ps.Emit(burst);
            if (loop && destroyWithTimer) Object.Destroy(copy, duration + 1.2f);
            return ps;
        }

        private static ParticleSystem MakeAscendingFlame(Transform parent, Vector3 origin, Gradient palette,
            float radius, float rate, float strength)
        {
            Material material = BerserkerRagnarokMeteorFx.TornFlameMaterial();
            if (material == null) return null;
            float scale = Mathf.Lerp(0.85f, 1.25f, Mathf.Clamp01(strength));
            ParticleSystem ps = BerserkerEffects.MakeParticles(parent, material, true, 1.8f,
                0.36f * scale, 0.68f * scale, 0f, 0f, 80);
            ps.name = "Ragnarok_AscendingFlame";
            ps.transform.localPosition = origin;
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.20f, 1.80f);
            main.startColor = Color.white;
            main.startRotation = new ParticleSystem.MinMaxCurve(-0.10f, 0.10f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Local;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;
            var emission = ps.emission;
            emission.rateOverTime = rate;
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.18f, 0.18f);
            velocity.y = new ParticleSystem.MinMaxCurve(9f, 14f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.18f, 0.18f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.28f;
            noise.frequency = 2.1f;
            noise.scrollSpeed = 1.6f;
            noise.octaveCount = 2;
            noise.quality = ParticleSystemNoiseQuality.Medium;
            noise.damping = true;
            var color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(palette);
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 1.85f;
            renderer.velocityScale = 0.035f;
            renderer.cameraVelocityScale = 0f;
            return ps;
        }

        private static ParticleSystem MakeAscendingCloud(Transform parent, Vector3 origin, Color tint,
            float radius, float rate, float duration)
        {
            // Uses precisely the Clouds from Thunderblood, including its material,
            // flipbook, and shader; other weapon effects are never instantiated.
            ParticleSystem ps = MakeCloud(parent, origin, true, duration,
                0.62f, 1.16f, 0.18f, 0.48f, radius, rate, tint, 0, false);
            if (ps == null) return null;

            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.20f, 3.00f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.58f, 1.10f);
            main.startSpeed = 0f; // No random radial ejection from the axe prefab.
            main.gravityModifier = 0f;
            main.simulationSpeed = 1f;
            main.maxParticles = 230;
            // Detached skyward column: particles emitted near the body fly away
            // in WORLD coordinates even when the character moves or stops casting.
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var collision = ps.collision;
            collision.enabled = false;
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = false;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.14f, 0.14f);
            velocity.y = new ParticleSystem.MinMaxCurve(5.5f, 9.0f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.14f, 0.14f);
            velocity.enabled = true;
            var force = ps.forceOverLifetime;
            force.enabled = false;
            force.space = ParticleSystemSimulationSpace.World;
            force.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            force.y = new ParticleSystem.MinMaxCurve(1.0f, 2.0f);
            force.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            force.enabled = true;
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(0.19f, 0.42f);
            noise.frequency = 0.66f;
            noise.scrollSpeed = 0.62f;
            return ps;
        }
    }

    // Dedicated skyward plume with its own adjustable emission duration.
    // The cast telegraph ends when meteors release, but this emitter remains
    // active until its timer expires; launched particles persist in world space.
    internal sealed class BerserkerRagnarokAscension : MonoBehaviour
    {
        private readonly List<ParticleSystem> emitters =
            new List<ParticleSystem>();

        private readonly List<float> rates =
            new List<float>();

        private readonly List<Vector3> origins =
            new List<Vector3>();

        private readonly List<float> radii =
            new List<float>();

        private float begunAt;
        private float startDelay;
        private float duration;
        private Player owner;
        private bool finished;
        private bool started;

        private readonly List<ParticleSystem> sparks =
            new List<ParticleSystem>();

        private readonly List<float> sparksRates =
            new List<float>();

        internal void Initialize(Player player, float delaySeconds, float emissionDuration)
        {
            owner = player;
            begunAt = Time.time;
            startDelay = Mathf.Max(0f, delaySeconds);
            duration = Mathf.Max(0.2f, emissionDuration);
        }

        internal void Register(ParticleSystem ps, float rate)
        {
            if (ps == null) return;
            emitters.Add(ps);
            rates.Add(rate);
            origins.Add(ps.transform.localPosition);
            radii.Add(ps.shape.radius);

            // Native particle systems would otherwise begin emitting when they
            // are instantiated, defeating the E-relative start delay.
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var emission = ps.emission;
            emission.rateOverTime = 0f;

            // Taper each rising flame to a narrow tip over its lifetime.
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.68f),
                    new Keyframe(0.20f, 1.00f),
                    new Keyframe(0.58f, 0.72f),
                    new Keyframe(1f, 0.06f)));
        }

        internal void RegisterSparks(ParticleSystem ps, float rate)
        {
            if (ps == null) return;
            sparks.Add(ps);
            sparksRates.Add(rate);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var emission = ps.emission;
            emission.rateOverTime = 0f;
        }

        internal void Finish()
        {
            if (finished) return;
            finished = true;
            // Stop emitting ONLY after the adjustable duration has elapsed.
            // World-space particles already launched remain in the sky to fade.
            SetConvergence(1f);
            for (int i = 0; i < emitters.Count; i++)
            {
                ParticleSystem ps = emitters[i];
                if (ps == null) continue;
                ps.transform.SetParent(null, true);
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Destroy(ps.gameObject, 3.50f);
            }

            for (int i = 0; i < sparks.Count; i++)
            {
                ParticleSystem ps = sparks[i];
                if (ps == null) continue;
                ps.transform.SetParent(null, true);
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Destroy(ps.gameObject, 2.50f);
            }

            sparks.Clear();
            sparksRates.Clear();
            emitters.Clear();
            rates.Clear();
            origins.Clear();
            radii.Clear();
            enabled = false;
            Destroy(gameObject);
        }

        private void Update()
        {
            if (finished) return;
            if (owner == null || owner.IsDead())
            {
                Finish();
                return;
            }

            // The delay is measured from E/cast start, while emission duration
            // is measured from the first actual smoke/spark emission.
            float emissionElapsed = Time.time - begunAt - startDelay;
            if (emissionElapsed < 0f) return;

            if (!started)
            {
                started = true;
                for (int i = 0; i < emitters.Count; i++)
                {
                    ParticleSystem ps = emitters[i];
                    if (ps == null) continue;
                    var emission = ps.emission;
                    emission.rateOverTime = rates[i] * 1.25f;
                    ps.Play();
                    // Make the start visually obvious at the exact timer boundary
                    // instead of waiting for random rate-over-time emissions.
                    ps.Emit(5);
                }

                for (int i = 0; i < sparks.Count; i++)
                {
                    ParticleSystem ps = sparks[i];
                    if (ps == null) continue;
                    var emission = ps.emission;
                    emission.rateOverTime = sparksRates[i];
                    ps.Play();
                    ps.Emit(i == 0 ? 10 : 2);
                }
            }

            float progress = Mathf.Clamp01(emissionElapsed / duration);
            if (progress >= 1f)
            {
                Finish();
                return;
            }

            // Do NOT taper out halfway through the emote. The body keeps
            // shedding Wrath throughout the entire E cast. Only the final 20%
            // of EMISSION time funnels the plume into the central skyward axis.
            const float FunnelBeginAt = 0.80f;
            float converge = Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01((progress - FunnelBeginAt) / (1f - FunnelBeginAt)));
            SetConvergence(converge);

            // Continuously increasing output; no late reduction or premature
            // cutoff. The last emitted particles dissipate naturally in the sky.
            float multiplier = Mathf.Lerp(0.95f, 2.25f, progress);
            for (int i = 0; i < emitters.Count; i++)
            {
                if (emitters[i] == null) continue;
                var emission = emitters[i].emission;
                emission.rateOverTime = rates[i] * multiplier;
            }
        }

        private void SetConvergence(float amount)
        {
            for (int i = 0; i < emitters.Count; i++)
            {
                ParticleSystem ps = emitters[i];
                if (ps == null) continue;
                Vector3 origin = origins[i];
                float radialScale = Mathf.Lerp(1f, 0.22f, amount);
                ps.transform.localPosition = new Vector3(
                    origin.x * radialScale, origin.y + amount * 0.13f,
                    origin.z * radialScale);

                var shape = ps.shape;
                shape.radius = radii[i] * Mathf.Lerp(1f, 0.38f, amount);

                // A mild inward drift on the already rising particles creates
                // a funnel instead of separate parallel vertical smoke trails.
                // Velocity is WORLD-space because emitted particles are in
                // WORLD simulation space and should not follow a moving player.
                Vector3 axis = transform.position;
                Vector3 radial = ps.transform.position - axis;
                var velocity = ps.velocityOverLifetime;
                velocity.enabled = true;
                velocity.space = ParticleSystemSimulationSpace.World;
                // Stay in TwoConstants, matching the existing Y curve.
                // Both bounds equal => deterministic inward drift, without
                // switching the MinMaxCurve mode back to Constant.
                float inwardX = -radial.x * amount * 0.32f;
                float inwardZ = -radial.z * amount * 0.32f;
                velocity.x = new ParticleSystem.MinMaxCurve(inwardX, inwardX);
                velocity.z = new ParticleSystem.MinMaxCurve(inwardZ, inwardZ);
            }
        }
    }
}