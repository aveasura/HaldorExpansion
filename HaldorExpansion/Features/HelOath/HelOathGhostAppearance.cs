using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace HaldorExpansion.Features.HelOath
{
    // Every material is private. Never modify the player's materials or shared meshes.
    internal sealed class HelOathGhostAppearance : IDisposable
    {
        private readonly Dictionary<Material, Material> copies = new Dictionary<Material, Material>();

        private static readonly Dictionary<ClothingKey, ClothingEntry> clothingCache =
            new Dictionary<ClothingKey, ClothingEntry>();

        private readonly List<ClothingEntry> clothingLeases = new List<ClothingEntry>();
        private HelOathGhostAura aura = new HelOathGhostAura();
        private Transform auraParent;
        private Transform auraOwner;

        internal Material[] CopyMaterials(Material[] originals)
        {
            var result = new Material[originals.Length];
            for (int i = 0; i < result.Length; i++)
            {
                Material original = originals[i];
                if (original == null) continue;
                Material copy;
                if (!copies.TryGetValue(original, out copy))
                {
                    Shader shader = SupportedShader("Standard") ?? SupportedShader("Legacy Shaders/Transparent/Diffuse")
                        ?? SupportedShader("Sprites/Default");
                    if (shader == null) throw new InvalidOperationException("No supported transparent ghost shader.");
                    copy = new Material(shader) { name = "HE_Ghost_" + original.name };
                    copies.Add(original, copy);
                    string textureName = original.HasProperty("_MainTex") ? "_MainTex"
                        : original.HasProperty("_SkinTex") ? "_SkinTex" : null;
                    if (textureName != null && copy.HasProperty("_MainTex"))
                    {
                        copy.mainTexture = original.GetTexture(textureName);
                        copy.mainTextureScale = original.GetTextureScale(textureName);
                        copy.mainTextureOffset = original.GetTextureOffset(textureName);
                    }

                    if (original.HasProperty("_BumpMap") && copy.HasProperty("_BumpMap"))
                    {
                        Texture normal = original.GetTexture("_BumpMap");
                        if (normal != null)
                        {
                            copy.SetTexture("_BumpMap", normal);
                            copy.EnableKeyword("_NORMALMAP");
                        }
                    }

                    // Player skin shaders layer clothing and use skin alpha as a tint mask.
                    // Feeding only _MainTex to a transparent shader loses clothing and can erase skin.
                    if (original.HasProperty("_ChestTex") && original.HasProperty("_LegsTex"))
                    {
                        copy.mainTexture = AcquireClothedBody(original);
                        copy.mainTextureScale = Vector2.one;
                        copy.mainTextureOffset = Vector2.zero;
                    }

                    // Standard's Fade mode (alpha is actually blended, not just written to an opaque shader).
                    if (copy.HasProperty("_Mode")) copy.SetFloat("_Mode", 2f);
                    if (copy.HasProperty("_SrcBlend")) copy.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    if (copy.HasProperty("_DstBlend")) copy.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    if (copy.HasProperty("_ZWrite")) copy.SetInt("_ZWrite", 0);
                    if (copy.HasProperty("_Metallic")) copy.SetFloat("_Metallic", 0f);
                    if (copy.HasProperty("_Glossiness")) copy.SetFloat("_Glossiness", 0.12f);
                    copy.DisableKeyword("_ALPHATEST_ON");
                    copy.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    copy.EnableKeyword("_ALPHABLEND_ON");
                    copy.SetOverrideTag("RenderType", "Transparent");
                    copy.renderQueue = (int)RenderQueue.Transparent;
                    if (copy.HasProperty("_EmissionColor"))
                    {
                        copy.EnableKeyword("_EMISSION");
                        copy.SetColor("_EmissionColor", HelOathGhostVisualSettings.BodyEmission);
                    }

                    if (copy.HasProperty("_Color"))
                    {
                        Color tint = HelOathGhostVisualSettings.BodyTint;
                        tint.a = 0f;
                        copy.SetColor("_Color", tint);
                    }
                }

                result[i] = copy;
            }

            return result;
        }

        // Only baked pixels are shared. Each body keeps its own fade/emission materials.
        private struct ClothingKey : IEquatable<ClothingKey>
        {
            private readonly Texture skin, chest, legs;
            private readonly Color skinColor;
            private readonly int width, height;

            internal ClothingKey(Material source)
            {
                skin = source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") : null;
                chest = source.GetTexture("_ChestTex");
                legs = source.GetTexture("_LegsTex");
                skinColor = source.HasProperty("_SkinColor") ? source.GetColor("_SkinColor") : Color.white;
                width = skin != null ? Mathf.Clamp(skin.width, 32, 1024) : 256;
                height = skin != null ? Mathf.Clamp(skin.height, 32, 1024) : 256;
            }

            public bool Equals(ClothingKey other) => ReferenceEquals(skin, other.skin)
                                                     && ReferenceEquals(chest, other.chest) &&
                                                     ReferenceEquals(legs, other.legs)
                                                     && skinColor.Equals(other.skinColor) && width == other.width &&
                                                     height == other.height;

            public override bool Equals(object obj) => obj is ClothingKey && Equals((ClothingKey)obj);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = ReferenceEquals(skin, null) ? 0 : skin.GetHashCode();
                    hash = hash * 31 + (ReferenceEquals(chest, null) ? 0 : chest.GetHashCode());
                    hash = hash * 31 + (ReferenceEquals(legs, null) ? 0 : legs.GetHashCode());
                    hash = hash * 31 + skinColor.GetHashCode();
                    return (hash * 31 + width) * 31 + height;
                }
            }
        }

        private sealed class ClothingEntry
        {
            internal ClothingKey Key;
            internal Texture2D Texture;
            internal int Users;
        }

        private Texture2D AcquireClothedBody(Material source)
        {
            var key = new ClothingKey(source);
            ClothingEntry entry;
            if (!clothingCache.TryGetValue(key, out entry))
            {
                // Publish only after a successful bake; failed GPU reads leave no cache entry.
                entry = new ClothingEntry { Key = key, Texture = BakeClothedBody(source) };
                clothingCache.Add(key, entry);
            }

            entry.Users++;
            clothingLeases.Add(entry);
            return entry.Texture;
        }

        private void ReleaseClothing()
        {
            foreach (ClothingEntry entry in clothingLeases)
            {
                if (--entry.Users != 0) continue;
                clothingCache.Remove(entry.Key);
                if (entry.Texture != null) Object.Destroy(entry.Texture);
            }

            clothingLeases.Clear();
        }

        private Texture2D BakeClothedBody(Material source)
        {
            Texture skin = source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") : null;
            int width = skin != null ? Mathf.Clamp(skin.width, 32, 1024) : 256;
            int height = skin != null ? Mathf.Clamp(skin.height, 32, 1024) : 256;
            Color[] pixels = ReadPixels(skin, width, height);
            Color skinColor = source.HasProperty("_SkinColor") ? source.GetColor("_SkinColor") : Color.white;
            for (int i = 0; i < pixels.Length; i++)
            {
                Color p = pixels[i];
                p.r *= skinColor.r;
                p.g *= skinColor.g;
                p.b *= skinColor.b;
                p.a = 1f; // Uniform spectral transparency is controlled by our material, not the skin tint mask.
                pixels[i] = p;
            }

            foreach (string layer in new[] { "_ChestTex", "_LegsTex" })
            {
                Texture texture = source.GetTexture(layer);
                if (texture == null) continue;
                Color[] clothing = ReadPixels(texture, width, height);
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color color = Color.Lerp(pixels[i], clothing[i], clothing[i].a);
                    color.a = 1f;
                    pixels[i] = color;
                }
            }

            var result = new Texture2D(width, height, TextureFormat.RGBA32, true, false)
                { name = "HE_GhostClothing", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Repeat };
            try
            {
                result.SetPixels(pixels);
                result.Apply(true, true);
                return result;
            }
            catch
            {
                Object.Destroy(result);
                throw;
            }
        }

        private static Color[] ReadPixels(Texture texture, int width, int height)
        {
            if (texture == null)
            {
                var empty = new Color[width * height];
                for (int i = 0; i < empty.Length; i++) empty[i] = Color.white;
                return empty;
            }

            // Game textures need not be CPU-readable. Read a temporary GPU copy, never change their flags.
            var temporary = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active;
            Texture2D readable = null;
            try
            {
                Graphics.Blit(texture, temporary);
                RenderTexture.active = temporary;
                readable = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
                readable.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                readable.Apply(false, false);
                return readable.GetPixels();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(temporary);
                if (readable != null) Object.Destroy(readable);
            }
        }

        private static Shader SupportedShader(string name)
        {
            Shader shader = Shader.Find(name);
            return shader != null && shader.isSupported ? shader : null;
        }

        internal void AddAura(Transform root, Transform owner)
        {
            auraParent = root;
            auraOwner = owner;
            aura.Initialize(root, owner);
        }

        internal void StartEffects() => aura.Start();

        internal void SetFade(float amount, float time, int index, bool ending, float auraAmount = -1f)
        {
            // A tiny brightness pulse keeps the silhouette alive without flickering or hiding the armour.
            amount = Mathf.Clamp01(amount);
            float pulseAmount = Mathf.Clamp(HelOathGhostVisualSettings.BodyPulseAmount, 0f, 0.5f);
            float pulse = 1f - pulseAmount +
                          pulseAmount * Mathf.Sin(time * HelOathGhostVisualSettings.BodyPulseSpeed + index * 2f);
            foreach (Material material in copies.Values)
            {
                if (material.HasProperty("_Color"))
                {
                    Color tint = HelOathGhostVisualSettings.BodyTint;
                    tint.a = Mathf.Clamp01(HelOathGhostVisualSettings.BodyOpacity) * amount * pulse;
                    material.SetColor("_Color", tint);
                }

                if (material.HasProperty("_EmissionColor"))
                    material.SetColor("_EmissionColor", HelOathGhostVisualSettings.BodyEmission * amount);
            }

            // A new hit can revive a body during fade-out. Its previous soul keeps
            // ascending; create a fresh attached aura instead of pulling it back down.
            if (!ending && aura.Released)
            {
                aura = new HelOathGhostAura();
                aura.Initialize(auraParent, auraOwner);
                aura.Start();
            }

            aura.SetFade(auraAmount < 0f ? amount : auraAmount, ending);
        }

        public void Dispose()
        {
            foreach (Material material in copies.Values)
                if (material != null)
                    Object.Destroy(material);
            copies.Clear();
            ReleaseClothing();
            aura.Dispose();
        }
    }
}