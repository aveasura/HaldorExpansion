using System;
using System.Collections.Generic;
using System.Reflection;
using HaldorExpansion.Features.PitKing;
using HaldorExpansion.Features.StaminaCape;
using HaldorExpansion.Features.WoundedBeast;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace HaldorExpansion.Presentation
{
    // Registration-time visual work only. Vanilla materials/textures remain read-only.
    internal static class CustomApparelAppearance
    {
        internal enum Style { WoundedBeast, ScorchedEndurance, PitKing }

        private const int MaxTextureSize = 1024;
        private const string Suffix = "_HE_CustomApparel_";
        private static readonly string[] ColorTextures = { "_MainTex", "_BaseMap", "_BaseColorMap", "_ChestTex" };
        private static readonly MethodInfo CloneShared = typeof(object).GetMethod("MemberwiseClone",
            BindingFlags.Instance | BindingFlags.NonPublic);

        // Shadow / midtone / highlight. Adjust the palette here without touching item stats.
        private static readonly Color[] Shadows =
        {
            new Color(0.028f, 0.022f, 0.017f),
            new Color(0.045f, 0.040f, 0.036f),
            new Color(0.085f, 0.028f, 0.026f)
        };
        private static readonly Color[] Midtones =
        {
            new Color(0.17f, 0.105f, 0.060f),
            new Color(0.21f, 0.18f, 0.15f),
            new Color(0.34f, 0.125f, 0.090f)
        };
        private static readonly Color[] Highlights =
        {
            new Color(0.37f, 0.27f, 0.18f),
            new Color(0.53f, 0.51f, 0.47f),
            new Color(0.57f, 0.29f, 0.20f)
        };
        private static readonly Dictionary<Material, Material>[] Materials =
        {
            new Dictionary<Material, Material>(), new Dictionary<Material, Material>(),
            new Dictionary<Material, Material>()
        };
        private static readonly Dictionary<Texture, Texture2D>[] Textures =
        {
            new Dictionary<Texture, Texture2D>(), new Dictionary<Texture, Texture2D>(),
            new Dictionary<Texture, Texture2D>()
        };

        internal static void Apply(GameObject prefab, Style style)
        {
            if (prefab == null || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            // Explicit allowlist: even an accidental call on a vanilla source is harmless.
            string expected;
            switch (style)
            {
                case Style.WoundedBeast: expected = WoundedBeastItemRegistration.WoundedBeastCapePrefabName; break;
                case Style.ScorchedEndurance: expected = StaminaCapeItemRegistration.PrefabName; break;
                case Style.PitKing: expected = PitKingItemRegistration.PitKingChestPrefabName; break;
                default: return;
            }
            if (!string.Equals(prefab.name, expected, StringComparison.Ordinal)) return;

            ItemDrop drop = prefab.GetComponent<ItemDrop>();
            if (drop?.m_itemData?.m_shared == null) return;
            // Detach SharedData before replacing the equipped armor material. Some providers
            // may shallow-copy it; assigning a copied Material on a shared container is unsafe.
            drop.m_itemData.m_shared = (ItemDrop.ItemData.SharedData)CloneShared.Invoke(drop.m_itemData.m_shared, null);
            var shared = drop.m_itemData.m_shared;
            shared.m_armorMaterial = MakeMaterial(shared.m_armorMaterial, style);

            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
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

        private static Material MakeMaterial(Material source, Style style)
        {
            if (source == null) return null;
            string suffix = Suffix + style;
            if (source.name != null && source.name.EndsWith(suffix, StringComparison.Ordinal)) return source;
            int index = (int)style;
            if (Materials[index].TryGetValue(source, out Material cached) && cached != null) return cached;

            Material painted = new Material(source) { name = source.name + suffix };
            try
            {
                bool hasTexture = false;
                foreach (string property in ColorTextures)
                {
                    if (!source.HasProperty(property)) continue;
                    Texture texture = source.GetTexture(property);
                    if (texture == null) continue;
                    if (!Textures[index].TryGetValue(texture, out Texture2D recolored) || recolored == null)
                    {
                        recolored = PaintTexture(texture, style, suffix);
                        Textures[index][texture] = recolored;
                    }
                    painted.SetTexture(property, recolored);
                    hasTexture = true;
                }
                SetTint(painted, hasTexture ? Color.white : Midtones[index]);
            }
            catch (Exception error)
            {
                // A partially painted material must not leak or mix palettes on a failed readback.
                Object.Destroy(painted);
                painted = new Material(source) { name = source.name + suffix };
                SetTint(painted, Midtones[index]);
                HaldorExpansionPlugin.Log?.LogWarning(
                    "[Apparel] Texture recolor fallback for " + source.name + ": " + error.Message);
            }
            Materials[index][source] = painted;
            return painted;
        }

        private static void SetTint(Material material, Color tint)
        {
            foreach (string property in new[] { "_Color", "_BaseColor" })
            {
                if (!material.HasProperty(property)) continue;
                Color original = material.GetColor(property);
                tint.a = original.a; // Preserve material opacity as well as texture cutouts.
                material.SetColor(property, tint);
            }
        }

        private static Texture2D PaintTexture(Texture source, Style style, string suffix)
        {
            int width = Mathf.Min(source.width, MaxTextureSize);
            int height = Mathf.Min(source.height, MaxTextureSize);
            RenderTexture temporary = RenderTexture.GetTemporary(width, height, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            Texture2D result = null;
            try
            {
                Graphics.Blit(source, temporary);
                RenderTexture.active = temporary;
                result = new Texture2D(width, height, TextureFormat.RGBA32, true, false)
                {
                    name = source.name + suffix,
                    wrapMode = source.wrapMode,
                    filterMode = source.filterMode,
                    anisoLevel = source.anisoLevel
                };
                result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                Color[] pixels = result.GetPixels();
                int index = (int)style;
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color original = pixels[i];
                    float light = Mathf.Clamp01(original.r * 0.2126f + original.g * 0.7152f + original.b * 0.0722f);
                    // Retain fur, seams and folds; ash appears in the lighter worn areas.
                    Color color = light < 0.5f
                        ? Color.Lerp(Shadows[index], Midtones[index], light * 2f)
                        : Color.Lerp(Midtones[index], Highlights[index], (light - 0.5f) * 2f);
                    color.a = original.a;
                    pixels[i] = color;
                }
                result.SetPixels(pixels);
                result.Apply(true, true); // Free CPU pixels after the one-time upload.
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
