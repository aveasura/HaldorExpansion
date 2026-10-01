using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HaldorExpansion.Features.HelOath
{
    internal static class HelOathAppearance
    {
        private const string Suffix = "_HelBlackSnow";
        private static readonly Dictionary<Material, Material> Materials = new Dictionary<Material, Material>();
        private static readonly Dictionary<Texture, Texture2D> Textures = new Dictionary<Texture, Texture2D>();

        internal static void Apply(GameObject prefab)
        {
            if (prefab == null || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
                var materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    var source = materials[i];
                    if (source == null || source.name.EndsWith(Suffix, StringComparison.Ordinal)) continue;
                    Material painted;
                    if (!Materials.TryGetValue(source, out painted))
                    {
                        try { painted = Paint(source); }
                        catch (Exception e)
                        {
                            // Cache fallback too, so repeated shop refreshes cannot spam a failed texture conversion.
                            HaldorExpansionPlugin.Log?.LogWarning("[Hel Oath] Appearance fallback for " + source.name + ": " + e.Message);
                            painted = new Material(source) { name = source.name + Suffix };
                            if (painted.HasProperty("_Color")) painted.SetColor("_Color", new Color(0.13f, 0.15f, 0.18f));
                        }
                        Materials[source] = painted;
                    }
                    materials[i] = painted;
                    changed = true;
                }
                if (changed) renderer.sharedMaterials = materials;
            }
        }

        private static Material Paint(Material source)
        {
            // All edits are made on private copies: the original Spinesnap materials remain intact.
            var painted = new Material(source) { name = source.name + Suffix };
            bool hasTexture = false;
            foreach (string property in new[] { "_MainTex", "_BaseMap", "_BaseColorMap" })
            {
                if (!source.HasProperty(property)) continue;
                Texture texture = source.GetTexture(property);
                if (texture == null) continue;
                Texture2D recolored;
                if (!Textures.TryGetValue(texture, out recolored))
                {
                    recolored = PaintTexture(texture);
                    Textures[texture] = recolored;
                }
                painted.SetTexture(property, recolored);
                hasTexture = true;
            }
            Color tint = hasTexture ? Color.white : new Color(0.12f, 0.14f, 0.17f);
            if (painted.HasProperty("_Color")) painted.SetColor("_Color", tint);
            if (painted.HasProperty("_BaseColor")) painted.SetColor("_BaseColor", tint);
            if (painted.HasProperty("_EmissionColor")) painted.SetColor("_EmissionColor", Color.black);
            // Normal maps, roughness and metallic maps are retained for the original surface detail.
            return painted;
        }

        private static Texture2D PaintTexture(Texture source)
        {
            int width = Mathf.Min(source.width, 1024), height = Mathf.Min(source.height, 1024);
            var temporary = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active;
            Texture2D result = null;
            try
            {
                // Vanilla textures need not be CPU-readable; read only our GPU copy.
                Graphics.Blit(source, temporary);
                RenderTexture.active = temporary;
                result = new Texture2D(width, height, TextureFormat.RGBA32, true, false)
                { name = source.name + Suffix, wrapMode = source.wrapMode, filterMode = FilterMode.Bilinear };
                result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                Color[] pixels = result.GetPixels();
                for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    Color original = pixels[index];
                    float luminance = original.r * 0.2126f + original.g * 0.7152f + original.b * 0.0722f;
                    float u = (float)x / width, v = (float)y / height;
                    float broad = Mathf.PerlinNoise(u * 9f + 3.2f, v * 9f + 7.1f);
                    float grain = Mathf.PerlinNoise(u * 115f + 8f, v * 115f + 2f);
                    // Graphite base with uneven, pale snow deposits; retain texture shadows and alpha.
                    float mask = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.52f, 0.70f, broad + (grain - 0.5f) * 0.14f));
                    mask *= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.12f, 0.58f, luminance));
                    Color graphite = Color.Lerp(new Color(0.018f, 0.023f, 0.032f), new Color(0.16f, 0.18f, 0.21f), luminance);
                    Color snow = Color.Lerp(new Color(0.48f, 0.57f, 0.64f), new Color(0.95f, 0.98f, 1f), luminance);
                    Color color = Color.Lerp(graphite, snow, mask);
                    color.a = original.a;
                    pixels[index] = color;
                }
                result.SetPixels(pixels);
                result.Apply(true, true);
                return result;
            }
            catch
            {
                if (result != null) UnityEngine.Object.Destroy(result);
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
