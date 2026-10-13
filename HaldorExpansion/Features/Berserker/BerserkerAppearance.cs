using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace HaldorExpansion.Features.Berserker
{
    // Texture/material copies only. Never recolor vanilla shared materials or the axe's particle VFX.
    // Reuse the same crimson-black palette for the Cuirass of Silent Reckoning.
    internal static class BerserkerAppearance
    {
        internal enum Style
        {
            Axe,
            Cloak,
            Helmet,

            // Cuirass of Silent Reckoning uses the same crimson-black finish.
            Cuirass
        }

        private const string Suffix = "_HE_Berserker_";

        private static readonly Dictionary<Material, Material>[] Materials =
        {
            new Dictionary<Material, Material>(),
            new Dictionary<Material, Material>(),
            new Dictionary<Material, Material>(),
            new Dictionary<Material, Material>()
        };

        private static readonly Dictionary<Texture, Texture2D>[] Textures =
        {
            new Dictionary<Texture, Texture2D>(),
            new Dictionary<Texture, Texture2D>(),
            new Dictionary<Texture, Texture2D>(),
            new Dictionary<Texture, Texture2D>()
        };

        internal static void Apply(GameObject prefab, Style style)
        {
            if (prefab == null || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;

            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                // Do not touch particle/trail renderers; Ragnarök / Reckoning FX must stay as approved.
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;

                Material[] current = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < current.Length; i++)
                {
                    Material painted = MakeMaterial(current[i], style);
                    if (ReferenceEquals(painted, current[i])) continue;
                    current[i] = painted;
                    changed = true;
                }

                if (changed) renderer.sharedMaterials = current;
            }
        }

        internal static void ApplyArmorMaterial(ItemDrop.ItemData.SharedData shared, Style style)
        {
            if (shared == null || shared.m_armorMaterial == null) return;
            shared.m_armorMaterial = MakeMaterial(shared.m_armorMaterial, style);
        }

        private static Material MakeMaterial(Material source, Style style)
        {
            if (source == null || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return source;
            string suffix = Suffix + style;
            if (source.name.EndsWith(suffix, StringComparison.Ordinal)) return source;

            int index = (int)style;
            if (Materials[index].TryGetValue(source, out Material cached)) return cached;

            Material painted;
            try
            {
                painted = Paint(source, style, suffix);
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogWarning(
                    "[Berserker] Appearance fallback for " + source.name + ": " + e.Message);
                painted = new Material(source) { name = source.name + suffix };
                SetTint(painted, new Color(0.20f, 0.055f, 0.070f, 1f));
            }

            Materials[index][source] = painted;
            return painted;
        }

        private static Material Paint(Material source, Style style, string suffix)
        {
            Material painted = new Material(source) { name = source.name + suffix };
            bool hasTexture = false;
            foreach (string property in new[] { "_MainTex", "_BaseMap", "_BaseColorMap" })
            {
                if (!source.HasProperty(property)) continue;
                Texture texture = source.GetTexture(property);
                if (texture == null) continue;

                int index = (int)style;
                if (!Textures[index].TryGetValue(texture, out Texture2D recolored))
                {
                    recolored = PaintTexture(texture, style, suffix);
                    Textures[index][texture] = recolored;
                }

                painted.SetTexture(property, recolored);
                hasTexture = true;
            }

            SetTint(painted, hasTexture ? Color.white : new Color(0.20f, 0.055f, 0.070f, 1f));
            // Retain normals, metallic/roughness, and every game-provided effect/material flag.
            if (painted.HasProperty("_EmissionColor"))
            {
                if (style == Style.Helmet)
                    painted.SetColor("_EmissionColor", new Color(0.55f, 0.06f, 0.05f, 1f));
                else
                    painted.SetColor("_EmissionColor", Color.black);
            }

            return painted;
        }

        private static void SetTint(Material material, Color tint)
        {
            if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
        }

        // Shared Berserker gear finish: near-black steel transitioning into deep crimson.
        // Helmet/axe keep the approved original breakup; the cloak needs larger, softer fields
        // because the deer-hide mesh otherwise turns those small regions into noisy spots.
        private static Color PaintCrimsonSteel(float u, float v, float brightness, float seed, bool softCloak)
        {
            float broad = softCloak
                ? Mathf.PerlinNoise(u * 1.8f + 9.3f + seed, v * 2.2f + 4.8f)
                : Mathf.PerlinNoise(u * 5.2f + 9.3f + seed, v * 6.8f + 4.8f);
            float breakup = softCloak
                ? Mathf.PerlinNoise(u * 3.4f + 2.1f, v * 4.0f + 8.7f + seed)
                : Mathf.PerlinNoise(u * 17.5f + 2.1f, v * 19.4f + 8.7f + seed);
            float value = broad + (breakup - 0.5f) * (softCloak ? 0.05f : 0.16f);

            // The cloak blends over wider transitions rather than isolated spot-shaped islands.
            // Keep the existing coverage pattern for the axe and helmet.
            float chromeCoverage = Mathf.SmoothStep(0f, 1f,
                softCloak
                    ? Mathf.InverseLerp(0.32f, 0.68f, value)
                    : Mathf.InverseLerp(0.43f, 0.57f, value));

            // Suppress mottled brightness variations inherited from the vanilla deer-hide map.
            // Keep enough of the original luminance to retain the fabric's volume and folds.
            if (softCloak) brightness = Mathf.Lerp(0.5f, brightness, 0.40f);

            // Dark surfaces retain subtle steel highlights instead of becoming flat black.
            Color blackSteel = Color.Lerp(
                new Color(0.022f, 0.018f, 0.023f),
                new Color(0.12f, 0.060f, 0.072f),
                Mathf.Clamp01(brightness * 0.70f + breakup * 0.30f));

            // Crimson is deep and saturated, contrasting clearly with the black steel.
            Color crimson = Color.Lerp(
                new Color(0.30f, 0.023f, 0.043f),
                new Color(0.73f, 0.080f, 0.115f),
                Mathf.Clamp01(0.22f + brightness * 0.47f + breakup * 0.31f));

            // Narrow scarlet highlights remain inside the crimson regions.
            float glint = Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(0.59f, 0.82f, breakup));
            crimson = Color.Lerp(crimson, new Color(0.94f, 0.20f, 0.22f),
                glint * (softCloak ? 0.065f : 0.15f));
            return Color.Lerp(blackSteel, crimson, chromeCoverage);
        }

        private static Texture2D PaintTexture(Texture source, Style style, string suffix)
        {
            int width = Mathf.Min(source.width, 1024);
            int height = Mathf.Min(source.height, 1024);
            RenderTexture temporary = RenderTexture.GetTemporary(width, height, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            Texture2D result = null;
            try
            {
                // Works for non-CPU-readable vanilla textures (same approach as Hel Oath).
                Graphics.Blit(source, temporary);
                RenderTexture.active = temporary;
                result = new Texture2D(width, height, TextureFormat.RGBA32, true, false)
                {
                    name = source.name + suffix,
                    wrapMode = source.wrapMode,
                    filterMode = FilterMode.Bilinear
                };
                result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                Color[] pixels = result.GetPixels();

                float seed = (int)style * 3.71f;
                for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int pixelIndex = y * width + x;
                    Color original = pixels[pixelIndex];
                    float brightness = original.r * 0.2126f + original.g * 0.7152f + original.b * 0.0722f;
                    float u = (float)x / width, v = (float)y / height;

                    // Preserve the existing texture breakup; only the cloak receives a softer pattern.
                    // The helmet's separate red-eye recolor below remains unchanged.
                    Color color = PaintCrimsonSteel(u, v, brightness, seed, style == Style.Cloak);

                    if (style == Style.Helmet)
                    {
                        // Preserve the identity of the original helmet by letting bright eye details read as red.
                        float eyeAccent = Mathf.SmoothStep(0f, 1f,
                            Mathf.InverseLerp(0.52f, 0.82f, original.r - original.g * 0.35f));
                        eyeAccent *= Mathf.SmoothStep(0f, 1f, brightness + 0.12f);
                        Color eyeRed = Color.Lerp(new Color(0.42f, 0.03f, 0.04f), new Color(0.95f, 0.08f, 0.06f),
                            brightness);
                        color = Color.Lerp(color, eyeRed, eyeAccent * 0.95f);
                    }

                    color.a = original.a;
                    pixels[pixelIndex] = color;
                }

                result.SetPixels(pixels);
                result.Apply(true, true);
                return result;
            }
            catch
            {
                if (result != null) Object.Destroy(result);
                throw;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(temporary);
            }
        }
    }
}