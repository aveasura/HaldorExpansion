using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HaldorExpansion.Features.PeltOfHelheim
{
    internal static class PeltOfHelheimAppearance
    {
        private const string Suffix = "_PeltOfHelheimBlack";
        private static readonly Dictionary<Material, Material> Materials = new Dictionary<Material, Material>();
        private static readonly Dictionary<Texture, Texture2D> Textures = new Dictionary<Texture, Texture2D>();

        internal static Material MakeBlackMaterial(Material source)
        {
            if (source == null || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return source;
            if (source.name.EndsWith(Suffix, StringComparison.Ordinal)) return source;

            if (Materials.TryGetValue(source, out Material cached)) return cached;

            Material painted;
            try
            {
                painted = Paint(source);
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogWarning(
                    "[Pelt of Helheim] Appearance fallback for " + source.name + ": " + e.Message);
                painted = new Material(source) { name = source.name + Suffix };
                Color fallback = new Color(0.055f, 0.060f, 0.070f, 1f);
                if (painted.HasProperty("_Color")) painted.SetColor("_Color", fallback);
                if (painted.HasProperty("_BaseColor")) painted.SetColor("_BaseColor", fallback);
                if (painted.HasProperty("_EmissionColor")) painted.SetColor("_EmissionColor", Color.black);
            }

            Materials[source] = painted;
            return painted;
        }

        internal static void ApplyToPrefab(GameObject prefab)
        {
            if (prefab == null || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;

            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;

                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (source == null) continue;
                    Material painted = MakeBlackMaterial(source);
                    if (ReferenceEquals(painted, source)) continue;
                    materials[i] = painted;
                    changed = true;
                }

                if (changed) renderer.sharedMaterials = materials;
            }
        }

        private static Material Paint(Material source)
        {
            var painted = new Material(source) { name = source.name + Suffix };
            bool hasTexture = false;

            foreach (string property in new[] { "_MainTex", "_BaseMap", "_BaseColorMap" })
            {
                if (!source.HasProperty(property)) continue;
                Texture texture = source.GetTexture(property);
                if (texture == null) continue;

                if (!Textures.TryGetValue(texture, out Texture2D recolored))
                {
                    recolored = PaintTexture(texture);
                    Textures[texture] = recolored;
                }

                painted.SetTexture(property, recolored);
                hasTexture = true;
            }

            Color tint = hasTexture ? Color.white : new Color(0.055f, 0.060f, 0.070f, 1f);
            if (painted.HasProperty("_Color")) painted.SetColor("_Color", tint);
            if (painted.HasProperty("_BaseColor")) painted.SetColor("_BaseColor", tint);
            if (painted.HasProperty("_EmissionColor")) painted.SetColor("_EmissionColor", Color.black);
            return painted;
        }

        private static Texture2D PaintTexture(Texture source)
        {
            int width = Mathf.Min(source.width, 1024);
            int height = Mathf.Min(source.height, 1024);
            RenderTexture temporary = RenderTexture.GetTemporary(
                width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            Texture2D result = null;

            try
            {
                Graphics.Blit(source, temporary);
                RenderTexture.active = temporary;
                result = new Texture2D(width, height, TextureFormat.RGBA32, true, false)
                {
                    name = source.name + Suffix,
                    wrapMode = source.wrapMode,
                    filterMode = FilterMode.Bilinear
                };
                result.ReadPixels(new Rect(0, 0, width, height), 0, 0);

                Color[] pixels = result.GetPixels();
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color original = pixels[i];
                    float luminance = original.r * 0.2126f + original.g * 0.7152f + original.b * 0.0722f;
                    // Keep the Troll Cape's cloth/fur detail, but compress the palette into charcoal-black.
                    float shade = Mathf.Lerp(0.018f, 0.145f, Mathf.Pow(luminance, 0.72f));
                    pixels[i] = new Color(shade * 0.88f, shade * 0.92f, shade, original.a);
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
