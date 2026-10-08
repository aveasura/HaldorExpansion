using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace HaldorExpansion.Features.Berserker
{
    // Distinct from the large red Wrath aura: pale steel/gold focal flashes at
    // the single enemy, then a narrow life-stream only after confirmed healing.
    // No combat effects or gameplay decisions are made here.
    internal static class BerserkerReckoningFx
    {
        private static AudioClip swingClip, hitClip, siphonClip;
        private static bool searchedNative;
        private static AudioClip nativeHit, nativeCharge;

        // Cosmetic-only audio state, separate for each rendered player. The original
        // charge sound has a long tail and was formerly spawned as a new pair of
        // AudioSources on every StartAttack callback, which caused overlapping audio.
        private sealed class SwingSoundState
        {
            internal GameObject ActiveSound;
            internal float LastStart = float.NegativeInfinity;
        }

        private static readonly ConditionalWeakTable<Player, SwingSoundState> SwingSounds =
            new ConditionalWeakTable<Player, SwingSoundState>();

        // A short re-trigger guard against repeated input during the same wind-up.
        // This is NOT an ability cooldown and does not affect the attack or impact.
        private const float SwingSoundRetriggerGuard = 1.60f;

        internal static void Swing(Player player)
        {
            if (player == null || player.IsDead()) return;

            SwingSoundState state = SwingSounds.GetValue(player, ignored => new SwingSoundState());
            float now = Time.unscaledTime;
            if (now >= state.LastStart && now - state.LastStart < SwingSoundRetriggerGuard)
                return;

            // Preserve the original clips, simultaneous start, pitches and mix.
            AudioClip native = NativeCharge();
            AudioClip generated = swingClip ?? (swingClip = BuildAudio("Reckoning_BladeCharge", 0.34f, 707));
            if (native == null && generated == null) return;

            // Stop BOTH previous layers immediately, including the long native growl.
            // Destroying the old audio root alone is deferred by Unity and can cause
            // one more frame of overlap, so stop its sources first.
            if (state.ActiveSound != null)
            {
                foreach (AudioSource oldSource in state.ActiveSound.GetComponents<AudioSource>())
                    if (oldSource != null) oldSource.Stop();
                UnityEngine.Object.Destroy(state.ActiveSound);
                state.ActiveSound = null;
            }

            bool local = player == Player.m_localPlayer;
            var root = new GameObject("Berserker_ReckoningSfx");
            root.transform.position = player.GetCenterPoint();
            float volume = AudioMan.GetSFXVolume();
            float duration = 0.5f;

            if (native != null)
            {
                AudioSource a = SetupAudio(root, native, local, 0.42f * volume, 1.06f);
                a.Play();
                duration = Mathf.Max(duration, native.length / 1.06f);
            }
            if (generated != null)
            {
                AudioSource b = SetupAudio(root, generated, local, 0.52f * volume, 1f);
                b.Play();
                duration = Mathf.Max(duration, generated.length);
            }

            state.ActiveSound = root;
            state.LastStart = now;
            UnityEngine.Object.Destroy(root, duration + 0.18f);
        }

        internal static void Impact(Player attacker, Vector3 hitPoint, float intensity)
        {
            if (attacker == null) return;
            var root = new GameObject("Berserker_ReckoningSingleTargetImpact");
            root.transform.position = hitPoint;
            Vector3 facing = hitPoint - attacker.GetCenterPoint();
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.001f) facing = attacker.transform.forward;
            root.transform.rotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
            root.AddComponent<BerserkerReckoningCutVisual>().Initialize(intensity);

            // Native Abomination slam supplies the stone/debris impact and its own
            // distinctive hit sound. The generated bass is only a quiet support layer.
            // Visuals are spawned locally (never through the prefab's ZNetView) and
            // this does not apply damage or affect Reckoning's single target.
            AudioClip abominationHit = SpawnAbominationSlamAtTarget(hitPoint);
            PlayAudio(hitPoint, attacker == Player.m_localPlayer,
                abominationHit ?? NativeHit(),
                hitClip ?? (hitClip = BuildAudio("Reckoning_HeavyHit", 0.55f, 711)),
                abominationHit != null ? 0.95f : 0.72f,
                abominationHit != null ? 0.24f : 0.88f, 0.94f);
        }

        // fx_Abomination_attack2 has ZNetView, CamShaker and TimedDestruction.
        // The impact is already broadcast via the player's existing visual RPC;
        // cloning its network object on every receiver would create extra world
        // entities. Instead copy it under an inactive local holder, remove all
        // non-visual behaviours *before* activation, and play only the particles.
        // Its original ZSFX clip is played separately through our 2D/localized
        // Reckoning audio path so the hit remains audible without duplicated SFX.
        private static AudioClip SpawnAbominationSlamAtTarget(Vector3 hitPoint)
        {
            GameObject source = ZNetScene.instance?.GetPrefab("fx_Abomination_attack2");
            if (source == null) return null;

            AudioClip sound = FindAudioInPrefab(source);
            GameObject holder = null;
            try
            {
                Vector3 ground = hitPoint;
                RaycastHit groundHit;
                int groundMask = LayerMask.GetMask("terrain", "static_solid", "piece");
                if (groundMask != 0 && Physics.Raycast(hitPoint + Vector3.up * 1.75f,
                        Vector3.down, out groundHit, 5.0f, groundMask))
                    ground = groundHit.point + Vector3.up * 0.035f;
                else
                    ground.y -= 0.65f;

                holder = new GameObject("Berserker_ReckoningAbominationSlam_LocalOnly");
                holder.SetActive(false);
                holder.transform.position = ground;
                GameObject effect = UnityEngine.Object.Instantiate(source, holder.transform, false);
                effect.name = "AbominationSlam_VisualsOnly";
                effect.transform.localPosition = Vector3.zero;
                effect.transform.localRotation = Quaternion.identity;
                effect.transform.localScale = Vector3.one * 0.82f;

                // Reckoning's impact runs from Unity's hit/physics callbacks.
                // DestroyImmediate is illegal there, even for inactive clones.
                // Queue removal while the clone remains inactive and only enable
                // the particle-only hierarchy on the following frame, AFTER the
                // unwanted ZNetView/ZSFX/CamShaker components have been destroyed.
                foreach (Component component in effect.GetComponentsInChildren<Component>(true))
                {
                    if (component == null || component is Transform ||
                        component is ParticleSystem || component is ParticleSystemForceField ||
                        component is Renderer || component is MeshFilter || component is Light)
                        continue;
                    UnityEngine.Object.Destroy(component);
                }

                if (effect.GetComponentsInChildren<ParticleSystem>(true).Length == 0)
                {
                    UnityEngine.Object.Destroy(holder);
                    return sound;
                }

                // Use a separate active host: coroutines on the inactive holder
                // cannot run, and enabling it immediately would wake up the
                // queued-for-destruction network/audio behaviours.
                var activator = new GameObject("Berserker_ReckoningAbominationSlam_Activator");
                activator.AddComponent<BerserkerReckoningSlamActivator>().ActivateNextFrame(holder);
                UnityEngine.Object.Destroy(holder, 3.5f);
            }
            catch (Exception exception)
            {
                if (holder != null) UnityEngine.Object.Destroy(holder);
                HaldorExpansionPlugin.Log?.LogWarning("[Berserker] Native Reckoning slam VFX unavailable: " + exception.Message);
            }
            return sound;
        }

        private static AudioClip FindAudioInPrefab(GameObject prefab)
        {
            if (prefab == null) return null;
            foreach (ZSFX sound in prefab.GetComponentsInChildren<ZSFX>(true))
            {
                if (sound == null || sound.m_audioClips == null) continue;
                foreach (AudioClip clip in sound.m_audioClips)
                    if (clip != null) return clip;
            }
            foreach (AudioSource source in prefab.GetComponentsInChildren<AudioSource>(true))
                if (source != null && source.clip != null) return source.clip;
            return null;
        }

        internal static void Heal(Player player, Vector3 origin, float actualHeal)
        {
            if (player == null || player.IsDead() || actualHeal <= 0f) return;
            var root = new GameObject("Berserker_ReckoningLifeReturn");
            root.transform.position = origin;
            root.AddComponent<BerserkerReckoningSiphonVisual>().Initialize(player, origin);
            PlayAudio(player.GetCenterPoint(), player == Player.m_localPlayer, null,
                siphonClip ?? (siphonClip = BuildAudio("Reckoning_LifeReturn", 0.63f, 719)),
                0f, 0.54f, 1.12f);
        }

        // Original game's flipbook, not synthetic round smoke. Deliberately small
        // and short-lived to avoid adding another red aura at the attacker's body.
        internal static void SmallImpactCloud(Transform parent)
        {
            var prefab = ZNetScene.instance?.GetPrefab("BattleaxeGold_BloodLightning");
            Transform attach = prefab != null ? prefab.transform.Find("attach") : null;
            Transform axe = attach != null ? attach.Find("battleaxe") : null;
            Transform vfx = axe != null ? axe.Find("VFX (1)") : null;
            Transform clouds = vfx != null ? vfx.Find("Clouds") : null;
            if (clouds == null) return;
            ParticleSystem source = clouds.GetComponent<ParticleSystem>();
            if (source == null) return;

            var clone = UnityEngine.Object.Instantiate(clouds.gameObject, parent);
            clone.name = "Reckoning_ThunderbloodContactWisp";
            clone.transform.localPosition = Vector3.zero;
            clone.transform.localRotation = Quaternion.identity;
            clone.transform.localScale = Vector3.one * 0.52f;
            var ps = clone.GetComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.60f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.13f, 0.24f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.06f, 0.20f);
            main.startColor = new Color(0.32f, 0.05f, 0.065f, 0.34f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.16f;
            ps.Play();
            ps.Emit(13);
            UnityEngine.Object.Destroy(clone, 1.25f);
        }

        internal static ParticleSystem Burst(Transform parent, Color color, float sizeMin, float sizeMax,
            float speedMin, float speedMax, float lifetime, int count)
        {
            var ps = BerserkerEffects.MakeParticles(parent, BerserkerEffects.SoftMaterial(), false, lifetime,
                sizeMin, sizeMax, speedMin, speedMax, count + 12);
            var main = ps.main;
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(0.85f, 0.46f, 0.24f), 0.43f),
                new GradientColorKey(new Color(0.14f, 0.015f, 0.035f), 1f)
            }, new[]
            {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0.82f, 0.22f),
                new GradientAlphaKey(0f, 1f)
            });
            fade.color = gradient;
            ps.Play();
            ps.Emit(count);
            return ps;
        }

        private static AudioClip NativeHit()
        {
            FindNative();
            return nativeHit;
        }

        private static AudioClip NativeCharge()
        {
            FindNative();
            return nativeCharge;
        }

        private static void FindNative()
        {
            if (searchedNative) return;
            if (ZNetScene.instance == null) return;
            searchedNative = true;
            // Optional vanilla layers. The generated audio is always played as well;
            // missing or quietly mixed vanilla prefabs cannot suppress the cue.
            nativeHit = FindFirst("sfx_sledge_hit", "sfx_golem_attack_hit", "sfx_troll_hit", "sfx_axe_hit");
            nativeCharge = FindFirst("sfx_golem_alerted", "sfx_fenring_alerted", "sfx_wraith_alerted");
        }

        private static AudioClip FindFirst(params string[] prefabs)
        {
            foreach (string name in prefabs)
            {
                GameObject prefab = ZNetScene.instance.GetPrefab(name);
                if (prefab == null) continue;
                foreach (ZSFX sound in prefab.GetComponentsInChildren<ZSFX>(true))
                {
                    if (sound.m_audioClips == null) continue;
                    foreach (AudioClip clip in sound.m_audioClips)
                        if (clip != null) return clip;
                }
                foreach (AudioSource source in prefab.GetComponentsInChildren<AudioSource>(true))
                    if (source.clip != null) return source.clip;
            }
            return null;
        }

        private static void PlayAudio(Vector3 point, bool local, AudioClip native, AudioClip generated,
            float nativeLevel, float generatedLevel, float pitch)
        {
            if (native == null && generated == null) return;
            var go = new GameObject("Berserker_ReckoningSfx");
            go.transform.position = point;
            float volume = AudioMan.GetSFXVolume();
            float duration = 0.5f;
            if (native != null && nativeLevel > 0f)
            {
                AudioSource a = SetupAudio(go, native, local, nativeLevel * volume, pitch);
                a.Play();
                duration = Mathf.Max(duration, native.length / Mathf.Max(0.1f, pitch));
            }
            if (generated != null)
            {
                AudioSource b = SetupAudio(go, generated, local, generatedLevel * volume, 1f);
                b.Play();
                duration = Mathf.Max(duration, generated.length);
            }
            UnityEngine.Object.Destroy(go, duration + 0.18f);
        }

        private static AudioSource SetupAudio(GameObject go, AudioClip clip, bool local, float volume, float pitch)
        {
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.clip = clip;
            source.spatialBlend = local ? 0f : 0.90f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 3f;
            source.maxDistance = 32f;
            source.dopplerLevel = 0f;
            source.pitch = pitch;
            source.volume = Mathf.Clamp01(volume);
            return source;
        }

        private static AudioClip BuildAudio(string name, float duration, int seed)
        {
            // A short designed impact/inhale layer. No Span API reference: Unity's
            // SetData(float[],int) overload is explicitly chosen through reflection.
            const int sampleRate = 22050;
            int count = Mathf.RoundToInt(sampleRate * duration);
            var data = new float[count];
            var random = new System.Random(seed);
            double low = 0d, high = 0d, phase = 0d;
            bool hit = seed == 711;
            bool heal = seed == 719;
            for (int i = 0; i < count; ++i)
            {
                double t = (double)i / sampleRate;
                double x = t / duration;
                double noise = random.NextDouble() * 2d - 1d;
                low = low * 0.96d + noise * 0.04d;
                high = high * 0.60d + (noise - low) * 0.40d;
                double envelope = hit ? Math.Exp(-t * 11d)
                    : heal ? Math.Min(1d, t / 0.09d) * Math.Pow(1d - x, 1.3d)
                    : Math.Pow(x, 1.4d) * Math.Min(1d, (duration - t) / 0.09d);
                double hz = hit ? (108d - 80d * x) : heal ? (145d + 255d * x) : (65d + 320d * x * x);
                phase += 2d * Math.PI * hz / sampleRate;
                double bass = Math.Sin(phase) + 0.32d * Math.Sin(phase * 0.5d);
                double impactTick = hit ? high * Math.Exp(-t * 55d) : high * (heal ? 0.10d : 0.08d);
                double sample = (bass * (hit ? 0.60d : 0.38d) + low * 1.55d + impactTick * 0.75d) * envelope;
                data[i] = (float)Math.Max(-0.94d, Math.Min(0.94d, sample));
            }
            try
            {
                AudioClip clip = AudioClip.Create(name, count, 1, sampleRate, false);
                MethodInfo setData = typeof(AudioClip).GetMethod("SetData", new[] { typeof(float[]), typeof(int) });
                if (setData == null) { UnityEngine.Object.Destroy(clip); return null; }
                setData.Invoke(clip, new object[] { data, 0 });
                return clip;
            }
            catch (Exception) { return null; }
        }
    }

    // Destroy(Component) completes at the end of the current Unity frame.
    // This helper waits one frame before activating the otherwise inert clone.
    internal sealed class BerserkerReckoningSlamActivator : MonoBehaviour
    {
        internal void ActivateNextFrame(GameObject holder)
        {
            StartCoroutine(ActivateAfterCleanup(holder));
        }

        private System.Collections.IEnumerator ActivateAfterCleanup(GameObject holder)
        {
            yield return null;
            if (holder != null)
            {
                holder.SetActive(true);
                var systems = holder.GetComponentsInChildren<ParticleSystem>(true);
                foreach (ParticleSystem ps in systems)
                    if (ps != null)
                        ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                foreach (ParticleSystem ps in systems)
                    if (ps != null)
                        ps.Play(false);
            }
            UnityEngine.Object.Destroy(gameObject);
        }
    }

    internal sealed class BerserkerReckoningCutVisual : MonoBehaviour
    {
        private const float Lifetime = 0.48f;
        private LineRenderer slash, edge, counter;
        private float created;
        private float intensity;

        internal void Initialize(float strength)
        {
            created = Time.time;
            intensity = Mathf.Clamp(strength, 0.9f, 1.7f);
            Material glow = BerserkerEffects.SoftMaterial();
            slash = BerserkerEffects.MakeLine(transform, glow, 0.16f * intensity, 20, false);
            edge = BerserkerEffects.MakeLine(transform, BerserkerEffects.SolidMaterial(), 0.034f * intensity, 20, false);
            counter = BerserkerEffects.MakeLine(transform, glow, 0.08f * intensity, 14, false);
            // A bright slash across the enemy, not a 360-degree explosion or ring.
            for (int i = 0; i < slash.positionCount; i++)
            {
                float u = i / (float)(slash.positionCount - 1);
                Vector3 p = new Vector3(Mathf.Lerp(-0.75f, 0.75f, u), Mathf.Lerp(0.53f, -0.53f, u),
                    0.06f + Mathf.Sin(u * Mathf.PI) * 0.08f);
                slash.SetPosition(i, p * intensity);
                edge.SetPosition(i, p * intensity + Vector3.forward * 0.012f);
            }
            for (int i = 0; i < counter.positionCount; i++)
            {
                float u = i / (float)(counter.positionCount - 1);
                counter.SetPosition(i, new Vector3(Mathf.Lerp(0.37f, -0.38f, u),
                    Mathf.Lerp(0.29f, -0.34f, u), 0.07f));
            }
            BerserkerReckoningFx.Burst(transform, new Color(1f, 0.89f, 0.53f, 0.95f),
                0.055f, 0.14f, 1.5f, 4.2f, 0.28f, 27);
            BerserkerReckoningFx.SmallImpactCloud(transform);
            UnityEngine.Object.Destroy(gameObject, Lifetime + 0.72f);
        }

        private void Update()
        {
            float u = Mathf.Clamp01((Time.time - created) / Lifetime);
            float opacity = Mathf.Pow(1f - u, 2.2f);
            Color main = new Color(1f, 0.86f, 0.54f, opacity * 0.95f);
            Color white = new Color(1f, 0.98f, 0.86f, opacity);
            Color dark = new Color(0.30f, 0.016f, 0.055f, opacity * 0.68f);
            if (slash != null) { slash.startColor = slash.endColor = main; slash.widthMultiplier = 0.16f * intensity * (1f - 0.75f * u); }
            if (edge != null) { edge.startColor = edge.endColor = white; edge.widthMultiplier = 0.034f * intensity * (1f - u); }
            if (counter != null) { counter.startColor = counter.endColor = dark; counter.widthMultiplier = 0.08f * intensity * (1f - u); }
        }
    }

    internal sealed class BerserkerReckoningSiphonVisual : MonoBehaviour
    {
        private const float Duration = 0.62f;
        private readonly LineRenderer[] streams = new LineRenderer[3];
        private Player target;
        private Vector3 from;
        private float created;

        internal void Initialize(Player receiver, Vector3 start)
        {
            target = receiver;
            from = start;
            created = Time.time;
            for (int i = 0; i < streams.Length; ++i)
            {
                var line = BerserkerEffects.MakeLine(transform, i == 0 ? BerserkerEffects.SolidMaterial()
                    : BerserkerEffects.SoftMaterial(), i == 0 ? 0.040f : 0.027f, 20, true);
                line.numCapVertices = 5;
                line.startColor = line.endColor = new Color(1f, 0.80f, 0.50f, 0f);
                streams[i] = line;
            }
            UnityEngine.Object.Destroy(gameObject, Duration + 0.15f);
        }

        private void Update()
        {
            if (target == null || target.IsDead()) return;
            float t = Mathf.Clamp01((Time.time - created) / Duration);
            float progress = Mathf.SmoothStep(0f, 1f, t);
            Vector3 to = target.GetCenterPoint();
            Vector3 axis = to - from;
            Vector3 side = Vector3.Cross(axis.normalized, Vector3.up);
            if (side.sqrMagnitude < 0.001f) side = Vector3.right;
            side.Normalize();
            for (int j = 0; j < streams.Length; ++j)
            {
                LineRenderer line = streams[j];
                float head = Mathf.Clamp01(progress * 1.45f - j * 0.09f);
                float tail = Mathf.Clamp01((progress - 0.30f) * 1.45f - j * 0.09f);
                float alpha = (1f - Mathf.Clamp01((t - 0.64f) / 0.36f)) * (j == 0 ? 0.95f : 0.56f);
                Color c = j == 0 ? new Color(1f, 0.94f, 0.70f, alpha)
                    : new Color(0.86f, 0.31f, 0.22f, alpha);
                line.startColor = line.endColor = c;
                for (int k = 0; k < line.positionCount; k++)
                {
                    float u = k / (float)(line.positionCount - 1);
                    float p = Mathf.Lerp(tail, head, u);
                    float curve = Mathf.Sin(p * Mathf.PI) * (0.13f + j * 0.065f);
                    float jitter = Mathf.Sin(p * 11f + Time.time * 12f + j * 2.3f) * 0.025f;
                    Vector3 pos = Vector3.Lerp(from, to, p) + side * (curve + jitter) * (j == 1 ? -1f : 1f);
                    pos.y += Mathf.Sin(p * Mathf.PI) * (0.06f + 0.04f * j);
                    line.SetPosition(k, pos);
                }
            }
            // Brief focal glow at the chest on arrival, never a new player-sized aura.
            if (t > 0.76f && !pulsePlayed)
            {
                pulsePlayed = true;
                var go = new GameObject("Reckoning_HealArrival");
                go.transform.position = to;
                BerserkerReckoningFx.Burst(go.transform, new Color(1f, 0.95f, 0.68f, 0.75f),
                    0.035f, 0.075f, 0.05f, 0.42f, 0.18f, 12);
                UnityEngine.Object.Destroy(go, 0.40f);
            }
        }

        private bool pulsePlayed;
    }
}
