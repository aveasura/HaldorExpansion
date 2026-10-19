using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace HaldorExpansion.Features.HelOath
{
    // Disposable presentation object. it never owns a Player, collider or damage source.
    internal sealed class HelOathTributeFx : MonoBehaviour
    {
        private const float Duration = 1.25f;
        private const int Segments = 64;
        private LineRenderer ring;
        private Material material;
        private AudioClip chord;
        private float started;

        internal static void Play(Vector3 point, bool local)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            var root = new GameObject("HelOath_TributeAccepted");
            root.transform.position = point;
            var effect = root.AddComponent<HelOathTributeFx>();
            try
            {
                effect.Initialize(local);
            }
            catch (Exception error)
            {
                // Cosmetic failure must never interrupt rescue or leave an orphan object.
                Destroy(root);
                HaldorExpansionPlugin.Log?.LogWarning("[HaldorExpansion] Tribute presentation unavailable: " +
                                                      error.Message);
            }
        }

        private void Initialize(bool local)
        {
            started = Time.time;
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Hidden/Internal-Colored");
            if (shader != null)
            {
                material = new Material(shader) { name = "HelOath_TributeSoulRing" };
                ring = gameObject.AddComponent<LineRenderer>();
                ring.sharedMaterial = material;
                ring.useWorldSpace = false;
                ring.loop = true;
                ring.positionCount = Segments;
                ring.startWidth = 0.08f;
                ring.endWidth = 0.08f;
                ring.shadowCastingMode = ShadowCastingMode.Off;
                ring.receiveShadows = false;
                DrawRing(0f);
            }

            chord = BuildChord();
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.clip = chord;
            source.spatialBlend = local ? 0.12f : 0.94f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 2.5f;
            source.maxDistance = 22f;
            source.dopplerLevel = 0f;
            source.volume = (local ? 0.48f : 0.30f) * AudioMan.GetSFXVolume();
            source.Play();
            Destroy(gameObject, Duration + 0.15f);
        }

        private void Update()
        {
            DrawRing(Mathf.Clamp01((Time.time - started) / Duration));
        }

        private void DrawRing(float progress)
        {
            if (ring == null) return;
            float radius = Mathf.Lerp(1.65f, 0.15f, progress * progress);
            float alpha = Mathf.Sin(progress * Mathf.PI) * 0.78f;
            ring.startColor = new Color(0.12f, 0.92f, 0.62f, alpha);
            ring.endColor = new Color(0.24f, 0.88f, 1f, alpha);
            for (int i = 0; i < Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius,
                    -0.45f + progress * 0.65f, Mathf.Sin(angle) * radius));
            }
        }

        private static AudioClip BuildChord()
        {
            const int rate = 22050;
            const float duration = 1.25f;
            int count = (int)(rate * duration);
            var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                double t = (double)i / rate;
                double swell = Math.Min(1d, t / 0.14d) * Math.Pow(Math.Max(0d, 1d - t / duration), 1.7d);
                double vibrato = 0.008d * Math.Sin(2d * Math.PI * 4.2d * t);
                double chord = Math.Sin(2d * Math.PI * 110d * t + vibrato) * 0.47d
                               + Math.Sin(2d * Math.PI * 164.81d * t + 0.3d) * 0.27d
                               + Math.Sin(2d * Math.PI * 220d * t + 0.7d) * 0.16d
                               + Math.Sin(2d * Math.PI * 329.63d * t) * 0.08d;
                samples[i] = (float)(chord * swell * 0.65d);
            }

            var clip = AudioClip.Create("HelOath_TributeSoulChord", count, 1, rate, false);
            try
            {
                AccessTools.Method(typeof(AudioClip), "SetData", new[] { typeof(float[]), typeof(int) })
                    .Invoke(clip, new object[] { samples, 0 });
                return clip;
            }
            catch
            {
                Destroy(clip);
                throw;
            }
        }

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
            if (chord != null) Destroy(chord);
        }
    }
}