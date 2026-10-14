using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HaldorExpansion.Features.Upgrades
{
    internal static class AdamantineIdolAppearance
    {
        private static readonly Color Dark = new Color32(0x25, 0x2e, 0x3a, 255);
        private static readonly Color Steel = new Color32(0x49, 0x62, 0x79, 255);
        private static readonly Color Silver = new Color32(0xa8, 0xc1, 0xd1, 255);
        private static readonly Color Turquoise = new Color32(0x30, 0xd8, 0xe6, 255);
        private static readonly Color Ice = new Color32(0xc5, 0xff, 0xff, 255);

        private static readonly Dictionary<Material, Material> Originals = new Dictionary<Material, Material>();

        internal static void Apply(GameObject prefab, Sprite sourceIcon, bool adamantine = true)
        {
            // A dedicated server needs the registered network prefab, but no GPU readback.
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            try
            {
                var textures = new Dictionary<Texture, Texture2D>();
                foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        if (materials[i] == null) continue;
                        Material original;
                        if (!Originals.TryGetValue(materials[i], out original)) original = materials[i];
                        var material = new Material(original) { name = original.name + (adamantine ? "_he_adamantine" : "_he_haldor") };
                        Originals[material] = original;
                        materials[i] = material;
                        if (material.HasProperty("_MainTex") && material.GetTexture("_MainTex") != null)
                        {
                            Texture texture = material.GetTexture("_MainTex");
                            if (!textures.TryGetValue(texture, out Texture2D recolored))
                            {
                                recolored = Recolor(texture, adamantine);
                                textures.Add(texture, recolored);
                            }
                            material.SetTexture("_MainTex", recolored);
                            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
                        }
                        else if (material.HasProperty("_Color")) material.SetColor("_Color", adamantine ? Dark : (Color)new Color32(0x49, 0x2d, 0x1e, 255));
                        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", adamantine ? 0.8f : 0.65f);
                        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", adamantine ? 0.55f : 0.35f);
                        // Keep the original emission mask; never turn the entire surface into a lamp.
                        if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", (adamantine ? Turquoise : new Color(1f, 0.48f, 0.10f)) * 0.18f);
                    }
                    renderer.sharedMaterials = materials;
                }
                foreach (Light light in prefab.GetComponentsInChildren<Light>(true))
                {
                    light.color = adamantine ? Turquoise : new Color(1f, 0.48f, 0.10f);
                    light.intensity = Mathf.Min(light.intensity, 0.35f);
                }
                if (sourceIcon != null)
                {
                    // rect describes the original sprite, not its position in a packed atlas.
                    // Reconstruct the silhouette from actual vertices/triangles and atlas UVs.
                    Texture2D atlas = Recolor(sourceIcon.texture, adamantine);
                    Sprite sprite;
                    try
                    {
                        int width = Mathf.RoundToInt(sourceIcon.rect.width);
                        int height = Mathf.RoundToInt(sourceIcon.rect.height);
                        var positions = sourceIcon.vertices;
                        for (int i = 0; i < positions.Length; i++)
                            positions[i] = positions[i] * sourceIcon.pixelsPerUnit + sourceIcon.pivot;
                        var icon = new Texture2D(width, height, TextureFormat.RGBA32, false);
                        icon.SetPixels(IdolIconRasterizer.Render(width, height, positions, sourceIcon.triangles,
                            sourceIcon.uv, atlas.GetPixels(), atlas.width, atlas.height));
                        icon.wrapMode = TextureWrapMode.Clamp;
                        icon.filterMode = FilterMode.Bilinear;
                        icon.Apply();
                        sprite = Sprite.Create(icon, new Rect(0, 0, width, height),
                            new Vector2(sourceIcon.pivot.x / width, sourceIcon.pivot.y / height), sourceIcon.pixelsPerUnit);
                    }
                    finally { UnityEngine.Object.Destroy(atlas); }
                    sprite.name = adamantine ? "he_adamantine_idol_icon" : "he_haldor_idol_icon";
                    prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons = new[] { sprite };
                }
            }
            catch (Exception error)
            {
                HaldorExpansionPlugin.Log?.LogWarning("[HE Idol] Appearance could not be fully recolored: " + error);
            }
        }

        private static Texture2D Recolor(Texture source, bool adamantine)
        {
            RenderTexture target = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Graphics.Blit(source, target);
                RenderTexture.active = target;
                var texture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                texture.name = source.name + (adamantine ? "_he_adamantine" : "_he_haldor");
                texture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                Color[] pixels = texture.GetPixels();
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color old = pixels[i];
                    float l = Mathf.Max(old.r, Mathf.Max(old.g, old.b));
                    float saturation = l - Mathf.Min(old.r, Mathf.Min(old.g, old.b));
                    Color dark = adamantine ? Dark : (Color)new Color32(0x49, 0x2d, 0x1e, 255);
                    Color middle = adamantine ? Steel : (Color)new Color32(0x9c, 0x63, 0x30, 255);
                    Color edge = adamantine ? Color.Lerp(Silver, Ice, Mathf.Max(0f, l - 0.97f) * 10f) : (Color)new Color32(0xe1, 0xb4, 0x6b, 255);
                    Color metal = l < 0.65f ? Color.Lerp(dark * 0.5f, middle, l / 0.65f)
                        : Color.Lerp(middle, edge, (l - 0.65f) / 0.35f);
                    // Only the brightest colored details get an accent. Gold is the native
                    // idol's main surface, so saturation alone must not turn it all cyan.
                    if (saturation > 0.25f && l > 0.9f)
                        metal = Color.Lerp(metal, adamantine ? Turquoise : new Color(1f, 0.48f, 0.10f),
                            0.35f * (l - 0.9f) / 0.1f);
                    metal.a = old.a;
                    pixels[i] = metal;
                }
                texture.SetPixels(pixels);
                texture.wrapMode = source.wrapMode;
                texture.filterMode = source.filterMode;
                texture.Apply();
                return texture;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
            }
        }
    }
}
