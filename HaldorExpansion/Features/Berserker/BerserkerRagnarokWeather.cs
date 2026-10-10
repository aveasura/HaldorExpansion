using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Features.Berserker
{
    // A client-side visual overlay, NOT an environment override. The live EnvSetup
    // is never modified, so vanilla weather, biome transitions and other mods keep
    // their own state throughout Ragnarök and after it ends.
    internal static class BerserkerRagnarokWeather
    {
        // Cosmetic-only tuning; the radius is the observer's distance from the cast,
        // not the meteor hit radius. No server config or gameplay synchronization needed.
        internal const float WeatherRadius = 70f;
        private const float EdgeBlendWidth = 12f;
        private const float DarkenSeconds = 2.0f;
        private const float RecoverSeconds = 2.5f;
        private const float LateImpactGraceSeconds = 3.5f;
        private const float MaximumStormSeconds = 45f;

        private sealed class Storm
        {
            internal Player Caster;
            internal Vector3 Center;
            internal float StartedAt;
            internal bool Released;
            internal int ExpectedImpacts;
            internal int ReceivedImpacts;
            internal float StoppedAt = -1f;
            internal float FadeStartedAt = -1f;
        }

        private static readonly Dictionary<Player, Storm> Storms = new Dictionary<Player, Storm>();
        private static readonly List<Player> Expired = new List<Player>();
        private static float darkness;

        // Valheim builds differ in their cloud shader settings; the old rain-cloud
        // alpha is always supported, and the newer time-of-day cloud opacity
        // controls are applied when they exist.
        private static readonly FieldInfo CloudNight = AccessTools.Field(typeof(EnvSetup), "m_cloudOpacityNight");
        private static readonly FieldInfo CloudMorning = AccessTools.Field(typeof(EnvSetup), "m_cloudOpacityMorning");
        private static readonly FieldInfo CloudDay = AccessTools.Field(typeof(EnvSetup), "m_cloudOpacityDay");
        private static readonly FieldInfo CloudEvening = AccessTools.Field(typeof(EnvSetup), "m_cloudOpacityEvening");

        internal static void Begin(Player caster)
        {
            if (!BerserkerEffects.CanRender || caster == null) return;
            Storm old;
            if (Storms.TryGetValue(caster, out old) && !old.Released && old.FadeStartedAt < 0f)
                return; // The owner's local start and echoed cast RPC are the same cast.

            Storms[caster] = new Storm
            {
                Caster = caster,
                Center = caster.transform.position,
                StartedAt = Time.time
            };
        }

        internal static void Release(Player caster, Vector3 center, int meteorCount)
        {
            if (!BerserkerEffects.CanRender || caster == null) return;
            Storm storm;
            if (!Storms.TryGetValue(caster, out storm))
            {
                Begin(caster); // Tolerate a missed/reordered cast visual RPC.
                if (!Storms.TryGetValue(caster, out storm)) return;
            }
            storm.Center = center;
            storm.Released = true;
            storm.ExpectedImpacts = Mathf.Max(0, meteorCount);
        }

        internal static void MeteorImpact(Player caster)
        {
            if (caster == null) return;
            Storm storm;
            if (!Storms.TryGetValue(caster, out storm) || !storm.Released) return;
            storm.ReceivedImpacts++;
            if (storm.StoppedAt >= 0f && storm.ExpectedImpacts > 0 &&
                storm.ReceivedImpacts >= storm.ExpectedImpacts)
                BeginRecovery(storm);
        }

        internal static void Stop(Player caster)
        {
            if (caster == null) return;
            Storm storm;
            if (!Storms.TryGetValue(caster, out storm)) return;
            if (!storm.Released)
            {
                // Aborted cast: there are no meteors to wait for.
                BeginRecovery(storm);
                return;
            }
            if (storm.StoppedAt < 0f) storm.StoppedAt = Time.time;
            if (storm.ExpectedImpacts <= 0 || storm.ReceivedImpacts >= storm.ExpectedImpacts)
                BeginRecovery(storm);
            // Otherwise the sky stays dark until the last impact RPC, or a short
            // fallback timeout if a projectile despawns without reporting a hit.
        }

        private static void BeginRecovery(Storm storm)
        {
            if (storm.FadeStartedAt < 0f) storm.FadeStartedAt = Time.time;
        }

        internal static void Update()
        {
            // This also covers logout, changing worlds and return to the main menu.
            if (!BerserkerEffects.CanRender || Player.m_localPlayer == null || EnvMan.instance == null)
            {
                Reset();
                return;
            }

            Vector3 viewer = Player.m_localPlayer.transform.position;
            float desired = 0f;
            float now = Time.time;
            Expired.Clear();
            foreach (KeyValuePair<Player, Storm> entry in Storms)
            {
                Storm storm = entry.Value;
                if (storm.Caster == null || now - storm.StartedAt > MaximumStormSeconds ||
                    (storm.StoppedAt >= 0f && now - storm.StoppedAt >= LateImpactGraceSeconds))
                    BeginRecovery(storm);

                float alpha = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01((now - storm.StartedAt) / DarkenSeconds));
                if (storm.FadeStartedAt >= 0f)
                {
                    float recover = Mathf.Clamp01((now - storm.FadeStartedAt) / RecoverSeconds);
                    alpha *= 1f - Mathf.SmoothStep(0f, 1f, recover);
                    if (recover >= 1f)
                    {
                        Expired.Add(entry.Key);
                        continue;
                    }
                }

                // Follow the summoner until release, then keep the storm over the
                // frozen meteor center even if the summoner moves away.
                Vector3 center = !storm.Released && storm.Caster != null
                    ? storm.Caster.transform.position : storm.Center;
                float dx = center.x - viewer.x;
                float dz = center.z - viewer.z;
                float distance = Mathf.Sqrt(dx * dx + dz * dz);
                float edge = 1f - Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01((distance - (WeatherRadius - EdgeBlendWidth)) / EdgeBlendWidth));
                desired = Mathf.Max(desired, alpha * edge);
            }
            foreach (Player player in Expired) Storms.Remove(player);

            // Spatial transition is smooth as a client walks into or out of the
            // radius. Overlapping storms take MAX, never accumulate to black.
            darkness = Mathf.MoveTowards(darkness, desired, Time.deltaTime / 0.65f);
            if (darkness < 0.0001f) darkness = 0f;
        }

        internal static void Reset()
        {
            Storms.Clear();
            Expired.Clear();
            darkness = 0f;
        }

        // EnvMan receives a new clone for rendering only. Do not use
        // SetForceEnvironment/QueueEnvironment or modify m_environments:
        // those would change the actual weather and potentially its gameplay.
        internal static void Apply(ref EnvSetup environment)
        {
            if (environment == null || darkness <= 0f || !BerserkerEffects.CanRender) return;

            EnvSetup copy = environment.Clone();
            float strength = Mathf.Clamp01(darkness);
            copy.m_rainCloudAlpha = Mathf.Lerp(copy.m_rainCloudAlpha,
                Mathf.Max(copy.m_rainCloudAlpha, 0.94f), strength);
            IncreaseCloudOpacity(copy, CloudNight, strength);
            IncreaseCloudOpacity(copy, CloudMorning, strength);
            IncreaseCloudOpacity(copy, CloudDay, strength);
            IncreaseCloudOpacity(copy, CloudEvening, strength);

            copy.m_lightIntensityDay *= Mathf.Lerp(1f, 0.46f, strength);
            copy.m_lightIntensityNight *= Mathf.Lerp(1f, 0.60f, strength);
            copy.m_ambColorDay = Dim(copy.m_ambColorDay, strength, 0.40f);
            copy.m_ambColorNight = Dim(copy.m_ambColorNight, strength, 0.50f);
            copy.m_sunColorMorning = Dim(copy.m_sunColorMorning, strength, 0.45f);
            copy.m_sunColorDay = Dim(copy.m_sunColorDay, strength, 0.43f);
            copy.m_sunColorEvening = Dim(copy.m_sunColorEvening, strength, 0.45f);
            copy.m_sunColorNight = Dim(copy.m_sunColorNight, strength, 0.55f);

            // Slightly mute the horizon as well, rather than leaving a bright
            // blue daytime fog under nearly black storm clouds.
            copy.m_fogColorMorning = Dim(copy.m_fogColorMorning, strength, 0.78f);
            copy.m_fogColorDay = Dim(copy.m_fogColorDay, strength, 0.75f);
            copy.m_fogColorEvening = Dim(copy.m_fogColorEvening, strength, 0.78f);
            copy.m_fogColorNight = Dim(copy.m_fogColorNight, strength, 0.82f);
            copy.m_fogColorSunMorning = Dim(copy.m_fogColorSunMorning, strength, 0.72f);
            copy.m_fogColorSunDay = Dim(copy.m_fogColorSunDay, strength, 0.70f);
            copy.m_fogColorSunEvening = Dim(copy.m_fogColorSunEvening, strength, 0.72f);
            copy.m_fogColorSunNight = Dim(copy.m_fogColorSunNight, strength, 0.78f);
            environment = copy;
        }

        private static void IncreaseCloudOpacity(EnvSetup environment, FieldInfo field, float strength)
        {
            if (field == null || field.FieldType != typeof(float)) return;
            float opacity = (float)field.GetValue(environment);
            field.SetValue(environment, Mathf.Lerp(opacity, Mathf.Max(opacity, 0.94f), strength));
        }

        private static Color Dim(Color color, float strength, float multiplier)
        {
            float amount = Mathf.Lerp(1f, multiplier, strength);
            return new Color(color.r * amount, color.g * amount, color.b * amount, color.a);
        }
    }

    // Only changes the argument of the CURRENT rendering pass. No copy is stored
    // and no environment is patched in place, so restoration always follows the
    // world's up-to-date weather instead of an obsolete pre-cast snapshot.
    [HarmonyPatch(typeof(EnvMan), "SetEnv", new Type[]
    {
        typeof(EnvSetup), typeof(float), typeof(float), typeof(float), typeof(float), typeof(float)
    })]
    internal static class BerserkerRagnarokWeatherSetEnvPatch
    {
        private static void Prefix(ref EnvSetup env)
        {
            BerserkerRagnarokWeather.Apply(ref env);
        }
    }
}
