using System;
using HarmonyLib;
using UnityEngine;

namespace HaldorExpansion.Features.HelOath
{
    internal static class HelOathAudio
    {
        private static AudioClip explosionFallback, touchWhisper;

        internal static void PlayTouchStage(Player player, int stage)
        {
            if (player == null) return;
            stage = Mathf.Clamp(stage, 1, HelOathState.MaxTouchStacks);
            if (touchWhisper == null) touchWhisper = BuildTouchWhisper();

            bool local = player == Player.m_localPlayer;
            Vector3 point = player.GetCenterPoint() + Vector3.up * 0.25f;
            AudioClip vanilla = FindTouchWhisperSound();
            var root = new GameObject("HelOath_TouchWhisper_" + stage);
            root.transform.position = point;

            if (vanilla != null)
            {
                var voice = root.AddComponent<AudioSource>();
                voice.playOnAwake = false;
                voice.clip = vanilla;
                voice.spatialBlend = local ? 0.12f : 0.90f;
                voice.rolloffMode = AudioRolloffMode.Logarithmic;
                voice.minDistance = 2.5f;
                voice.maxDistance = 18f;
                voice.dopplerLevel = 0f;
                voice.pitch = 1.22f - (stage - 1) * 0.035f;
                voice.volume = (local ? 0.48f + stage * 0.045f : 0.23f + stage * 0.025f) * AudioMan.GetSFXVolume();
                voice.Play();
            }

            if (touchWhisper != null)
            {
                var breath = root.AddComponent<AudioSource>();
                breath.playOnAwake = false;
                breath.clip = touchWhisper;
                breath.spatialBlend = local ? 0.05f : 0.75f;
                breath.rolloffMode = AudioRolloffMode.Logarithmic;
                breath.minDistance = 2f;
                breath.maxDistance = 14f;
                breath.dopplerLevel = 0f;
                breath.pitch = 1.04f - (stage - 1) * 0.035f;
                breath.volume = (local ? 0.42f + stage * 0.045f : 0.16f + stage * 0.02f) * AudioMan.GetSFXVolume();
                breath.Play();
            }

            float lifetime = vanilla != null ? Mathf.Min(2.0f, vanilla.length + 0.15f) : 1.15f;
            UnityEngine.Object.Destroy(root, Mathf.Max(lifetime, touchWhisper != null ? touchWhisper.length + 0.2f : 0f));
        }

        internal static void Play(Vector3 point, bool explosion)
        {
            if (!explosion)
            {
                PlayEmbraceWhisper(point);
                return;
            }

            // Explosion keeps the established icy impact cue. Only the activation cue changed.
            AudioClip clip = FindIceSound(true);
            if (clip == null)
            {
                if (explosionFallback == null) explosionFallback = BuildFallbackExplosion();
                clip = explosionFallback;
            }

            var go = new GameObject("HelOath_FrostSound");
            go.transform.position = point;
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.clip = clip;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 5f;
            source.maxDistance = 65f;
            source.dopplerLevel = 0f;
            source.pitch = 0.85f;
            source.volume = AudioMan.GetSFXVolume();
            source.Play();
            UnityEngine.Object.Destroy(go, clip.length / source.pitch + 0.2f);
        }

        private static void PlayEmbraceWhisper(Vector3 point)
        {
            // Embrace deliberately stays in the exact same sonic family as Hel's Touch.
            // The successful Touch cue is mostly Valheim's Wraith voice with our short
            // breath layer underneath, so do not replace it with a separate noise/drone.
            if (touchWhisper == null) touchWhisper = BuildTouchWhisper();

            bool local = Player.m_localPlayer != null && Vector3.Distance(Player.m_localPlayer.GetCenterPoint(), point) < 3.5f;
            AudioClip alerted = FromPrefab(ZNetScene.instance?.GetPrefab("sfx_wraith_alerted"));
            AudioClip idle = FromPrefab(ZNetScene.instance?.GetPrefab("sfx_wraith_idle"));
            var root = new GameObject("HelOath_EmbraceWhisper");
            root.transform.position = point;
            float sfx = AudioMan.GetSFXVolume();
            float life = 1.35f;

            // Main layer: almost natural Wraith pitch so it remains an evil whisper rather
            // than turning into a stretched groan.
            AudioClip primary = alerted ?? idle;
            if (primary != null)
            {
                var voice = root.AddComponent<AudioSource>();
                voice.playOnAwake = false;
                voice.clip = primary;
                voice.spatialBlend = local ? 0.08f : 0.88f;
                voice.rolloffMode = AudioRolloffMode.Logarithmic;
                voice.minDistance = 3f;
                voice.maxDistance = 28f;
                voice.dopplerLevel = 0f;
                voice.pitch = 0.96f;
                voice.volume = (local ? 0.78f : 0.40f) * sfx;
                voice.Play();
                life = Mathf.Max(life, primary.length / voice.pitch + 0.2f);
            }

            // A quieter delayed Wraith whisper gives the activation a darker response
            // without changing the character of the Touch sound. Keep pitch well above
            // the old 0.68 value that made it sound like a drain/groan.
            if (idle != null)
            {
                var echo = root.AddComponent<AudioSource>();
                echo.playOnAwake = false;
                echo.clip = idle;
                echo.spatialBlend = local ? 0.10f : 0.90f;
                echo.rolloffMode = AudioRolloffMode.Logarithmic;
                echo.minDistance = 3f;
                echo.maxDistance = 24f;
                echo.dopplerLevel = 0f;
                echo.pitch = 0.86f;
                echo.volume = (local ? 0.34f : 0.16f) * sfx;
                echo.PlayDelayed(0.055f);
                life = Mathf.Max(life, 0.055f + idle.length / echo.pitch + 0.2f);
            }

            // Reuse the exact Touch breath clip. This is intentionally not a new
            // 'Embrace' synthesizer: same texture, simply darker/slower.
            if (touchWhisper != null)
            {
                var breath = root.AddComponent<AudioSource>();
                breath.playOnAwake = false;
                breath.clip = touchWhisper;
                breath.spatialBlend = local ? 0.03f : 0.72f;
                breath.rolloffMode = AudioRolloffMode.Logarithmic;
                breath.minDistance = 2.5f;
                breath.maxDistance = 22f;
                breath.dopplerLevel = 0f;
                breath.pitch = 0.88f;
                breath.volume = (local ? 0.54f : 0.24f) * sfx;
                breath.PlayDelayed(0.018f);
                life = Mathf.Max(life, 0.018f + touchWhisper.length / breath.pitch + 0.2f);
            }

            UnityEngine.Object.Destroy(root, life);
        }

        private static AudioClip FindTouchWhisperSound()
        {
            string[] names = { "sfx_wraith_idle", "sfx_wraith_alerted" };
            foreach (string name in names)
            {
                AudioClip clip = FromPrefab(ZNetScene.instance?.GetPrefab(name));
                if (clip != null) return clip;
            }
            return null;
        }

        private static AudioClip FindIceSound(bool explosion)
        {
            var item = ObjectDB.instance?.GetItemPrefab("StaffIce")?.GetComponent<ItemDrop>();
            if (item != null)
            {
                var shared = item.m_itemData.m_shared;
                AudioClip clip;
                if (explosion)
                {
                    var projectile = shared.m_attack?.m_attackProjectile?.GetComponentInChildren<Projectile>(true);
                    clip = FromEffects(projectile?.m_hitEffects);
                }
                else
                {
                    clip = FromEffects(shared.m_attack?.m_startEffect) ?? FromEffects(shared.m_startEffect)
                        ?? FromEffects(shared.m_attack?.m_triggerEffect);
                }
                if (clip != null) return clip;
            }
            string[] names = explosion
                ? new[] { "sfx_dragon_coldball_explode", "sfx_ice_hit", "sfx_staff_ice_hit" }
                : new[] { "sfx_staff_ice_attack", "sfx_staff_lightning_attack" };
            foreach (string name in names)
            {
                var prefab = ZNetScene.instance?.GetPrefab(name);
                var clip = FromPrefab(prefab);
                if (clip != null) return clip;
            }
            return null;
        }

        private static AudioClip FromEffects(EffectList effects)
        {
            if (effects?.m_effectPrefabs == null) return null;
            foreach (var effect in effects.m_effectPrefabs)
            {
                if (!effect.m_enabled) continue;
                var clip = FromPrefab(effect.m_prefab);
                if (clip != null) return clip;
            }
            return null;
        }

        private static AudioClip FromPrefab(GameObject prefab)
        {
            if (prefab == null) return null;
            foreach (var sound in prefab.GetComponentsInChildren<ZSFX>(true))
                if (sound.m_audioClips != null)
                    foreach (var clip in sound.m_audioClips)
                        if (clip != null) return clip;
            foreach (var source in prefab.GetComponentsInChildren<AudioSource>(true))
                if (source.clip != null) return source.clip;
            return null;
        }

        private static AudioClip BuildTouchWhisper()
        {
            const int sampleRate = 44100;
            const float duration = 0.72f;
            var samples = new float[(int)(sampleRate * duration)];
            var random = new System.Random(9347);
            double low = 0d, breath = 0d;
            for (int i = 0; i < samples.Length; i++)
            {
                double t = (double)i / sampleRate;
                double n = random.NextDouble() * 2d - 1d;
                low = low * 0.965d + n * 0.035d;
                breath = breath * 0.78d + (n - low) * 0.22d;
                double attack = Math.Min(1d, t / 0.11d);
                double release = Math.Min(1d, (duration - t) / 0.20d);
                double envelope = attack * release;
                double pulse = 0.78d + 0.22d * Math.Sin(2d * Math.PI * 3.1d * t);
                double formant = 0.035d * Math.Sin(2d * Math.PI * 238d * t)
                    + 0.020d * Math.Sin(2d * Math.PI * 477d * t);
                double sound = (breath * 0.72d + low * 0.22d + formant * 2.1d) * envelope * pulse;
                samples[i] = (float)Math.Max(-0.92d, Math.Min(0.92d, sound));
            }
            var result = AudioClip.Create("HelOath_TouchWhisper", samples.Length, 1, sampleRate, false);
            AccessTools.Method(typeof(AudioClip), "SetData", new[] { typeof(float[]), typeof(int) })
                .Invoke(result, new object[] { samples, 0 });
            return result;
        }

        private static AudioClip BuildFallbackExplosion()
        {
            const int sampleRate = 44100;
            const float duration = 1.3f;
            var samples = new float[(int)(sampleRate * duration)];
            var random = new System.Random(821);
            double lowNoise = 0d, phase = 0d;
            for (int i = 0; i < samples.Length; i++)
            {
                double t = (double)i / sampleRate;
                double noise = random.NextDouble() * 2d - 1d;
                lowNoise = lowNoise * 0.88d + noise * 0.12d;
                double attack = Math.Min(1d, t / 0.008d);
                phase += 2d * Math.PI * (42d + 90d * Math.Exp(-t * 13d)) / sampleRate;
                double sound = 0.5d * Math.Sin(phase) * Math.Exp(-t * 6d)
                    + 0.6d * lowNoise * Math.Exp(-t * 5d)
                    + 0.16d * (noise - lowNoise) * Math.Exp(-t * 18d)
                    + 0.13d * Math.Sin(2d * Math.PI * 1733d * t) * Math.Exp(-t * 7d)
                    + 0.09d * Math.Sin(2d * Math.PI * 2711d * t) * Math.Exp(-t * 9d);
                double fade = Math.Min(1d, (duration - t) / 0.05d);
                samples[i] = (float)Math.Max(-0.95d, Math.Min(0.95d, sound * attack * fade));
            }
            var result = AudioClip.Create("HelOath_IceCrack", samples.Length, 1, sampleRate, false);
            AccessTools.Method(typeof(AudioClip), "SetData", new[] { typeof(float[]), typeof(int) })
                .Invoke(result, new object[] { samples, 0 });
            return result;
        }
    }
}
