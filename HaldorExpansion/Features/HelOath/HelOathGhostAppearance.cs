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
        private readonly List<Texture2D> ownedTextures = new List<Texture2D>();
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
                        copy.mainTexture = BakeClothedBody(original);
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
            ownedTextures.Add(result);
            result.SetPixels(pixels);
            result.Apply(true, true);
            return result;
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
            foreach (Texture2D texture in ownedTextures)
                if (texture != null)
                    Object.Destroy(texture);
            ownedTextures.Clear();
            aura.Dispose();
        }
    }
}