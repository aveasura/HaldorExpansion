using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace HaldorExpansion.Features.Berserker
{
    // Texture/material copies only. Never recolor vanilla shared materials or the axe's particle VFX.
    internal static class BerserkerAppearance
    {
        internal enum Style
        {
            Axe,
            Cloak,
            Helmet
        }

        private const string Suffix = "_HE_Berserker_";

        private static readonly Dictionary<Material, Material>[] Materials =
        {
            new Dictionary<Material, Material>(),
            new Dictionary<Material, Material>(),
            new Dictionary<Material, Material>()
        };

        private static readonly Dictionary<Texture, Texture2D>[] Textures =
        {
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
                SetTint(painted, new Color(0.34f, 0.36f, 0.40f, 1f));
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

            SetTint(painted, hasTexture ? Color.white : new Color(0.34f, 0.36f, 0.40f, 1f));
            // Retain normals, metallic/roughness, and every game-provided effect/material flag.
            if (painted.HasProperty("_EmissionColor"))
            {
                if (style == Style.Helmet)
                {
                    // Strong dark-scarlet glow so the original red eyes read clearly and throw a soft aura.
                    painted.SetColor("_EmissionColor", new Color(1.65f, 0.10f, 0.08f, 1f));
                    if (painted.IsKeywordEnabled("_EMISSION") == false)
                        painted.EnableKeyword("_EMISSION");
                    if (painted.globalIlluminationFlags != MaterialGlobalIlluminationFlags.None)
                        painted.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                else
                {
                    painted.SetColor("_EmissionColor", Color.black);
                }
            }

            return painted;
        }

        private static void SetTint(Material material, Color tint)
        {
            if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
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

                    // Dark metallic gunmetal base with steel transitions and restrained crimson accents.
                    float broad = Mathf.PerlinNoise(u * 6.7f + 7.1f + seed, v * 9.2f + 3.3f);
                    float detail = Mathf.PerlinNoise(u * 34f + seed, v * 39f + 8f);
                    float grain = Mathf.PerlinNoise(u * 169f + 4f, v * 163f + seed);
                    float flow = Mathf.PerlinNoise(u * 13f + 11f + seed, v * 18f + 5f);
                    float ember = Mathf.PerlinNoise(u * 21f + 13f + seed, v * 25f + 5f);

                    float steelMask = Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(0.36f, 0.70f, broad + (detail - 0.5f) * 0.22f));
                    float brightSteelMask = Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(0.60f, 0.90f, grain * 0.55f + flow * 0.45f + (detail - 0.5f) * 0.12f));
                    float redMask = Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(0.56f, 0.82f, ember + (detail - 0.5f) * 0.20f));
                    float auraMask = Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(0.74f, 0.94f, grain + ember * 0.25f));

                    Color iron = Color.Lerp(new Color(0.12f, 0.12f, 0.14f),
                        new Color(0.24f, 0.25f, 0.28f), brightness);
                    Color gunmetal = Color.Lerp(new Color(0.18f, 0.19f, 0.22f),
                        new Color(0.36f, 0.38f, 0.42f), brightness);
                    Color steel = Color.Lerp(new Color(0.32f, 0.34f, 0.38f),
                        new Color(0.65f, 0.68f, 0.72f), brightness);
                    Color silver = Color.Lerp(new Color(0.58f, 0.60f, 0.64f),
                        new Color(0.88f, 0.90f, 0.93f), brightness);
                    Color crimson = Color.Lerp(new Color(0.22f, 0.02f, 0.03f),
                        new Color(0.58f, 0.05f, 0.07f), brightness);
                    Color scarlet = Color.Lerp(new Color(0.42f, 0.03f, 0.04f),
                        new Color(0.90f, 0.07f, 0.05f), brightness);

                    float steelStrength;
                    float brightSteelStrength;
                    float redStrength;
                    switch (style)
                    {
                        case Style.Cloak:
                            steelStrength = 0.74f;
                            brightSteelStrength = 0.42f;
                            redStrength = 0.22f;
                            break;
                        case Style.Helmet:
                            steelStrength = 0.82f;
                            brightSteelStrength = 0.54f;
                            redStrength = 0.28f;
                            break;
                        default: // Axe
                            steelStrength = 0.86f;
                            brightSteelStrength = 0.60f;
                            redStrength = 0.26f;
                            break;
                    }

                    Color color = Color.Lerp(iron, gunmetal, steelMask * steelStrength);
                    color = Color.Lerp(color, steel, brightSteelMask * brightSteelStrength);
                    color = Color.Lerp(color, silver, brightSteelMask * brightSteelStrength * 0.33f);
                    color = Color.Lerp(color, crimson, redMask * redStrength);

                    if (style == Style.Helmet)
                    {
                        // Keep the eyes distinctly dark-red and give them a broader scarlet halo.
                        float eyeAccent = Mathf.SmoothStep(0f, 1f,
                            Mathf.InverseLerp(0.48f, 0.80f, original.r - original.g * 0.30f + brightness * 0.18f));
                        float eyeGlow = Mathf.Max(eyeAccent, auraMask * eyeAccent * 0.85f);
                        color = Color.Lerp(color, crimson, eyeAccent * 0.65f);
                        color = Color.Lerp(color, scarlet, eyeGlow * 0.98f);
                    }
                    else
                    {
                        // Other items keep only restrained crimson accents among the metal transitions.
                        color = Color.Lerp(color, crimson, redMask * redStrength * 0.45f);
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