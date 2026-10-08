using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HaldorExpansion.Presentation
{
    internal static class EmbeddedIconLoader
    {
        private const string EmbeddedIconPrefix = "HaldorExpansion.Assets.Icons.";

        // Prefabs and their icons live across menu/world transitions. !Do not clear this!
        // cache on ObjectDB.Awake: inventory items can still reference these sprites.
        private static readonly Dictionary<string, Sprite> Icons = new Dictionary<string, Sprite>();
        private static readonly MethodInfo LoadImageMethod = FindLoadImageMethod();

        internal static bool TrySetEmbeddedIcon(ItemDrop.ItemData.SharedData shared, string iconFileName)
        {
            if (shared == null)
                return false;

            Sprite icon = LoadEmbeddedIcon(EmbeddedIconPrefix + iconFileName);
            if (icon == null)
                return false;

            if (shared.m_icons == null || shared.m_icons.Length != 1 || shared.m_icons[0] != icon)
                shared.m_icons = new[] { icon };
            HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Embedded icon applied: " + iconFileName);
            return true;
        }

        private static Sprite LoadEmbeddedIcon(string resourceName)
        {
            Sprite cached;
            if (Icons.TryGetValue(resourceName, out cached))
            {
                if (cached != null && cached.texture != null)
                    return cached;
                if (cached != null) Object.Destroy(cached);
                Icons.Remove(resourceName);
            }

            Texture2D texture = null;
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Embedded icon not found: " + resourceName);
                        return null;
                    }

                    byte[] bytes = ReadAllBytes(stream);

                    texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

                    if (!TryLoadTextureImage(texture, bytes))
                    {
                        HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Failed to load embedded icon image: " +
                                                       resourceName);
                        return null;
                    }

                    texture.name = resourceName;

                    Sprite sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f)
                    );
                    if (sprite == null) return null;
                    Icons[resourceName] = sprite;
                    texture = null; // Ownership transferred to the cached sprite.
                    return sprite;
                }
            }
            catch (Exception ex)
            {
                HaldorExpansionPlugin.DebugLog("[HaldorExpansion] Failed to load embedded icon " + resourceName + ": " +
                                               ex);
                return null;
            }
            finally
            {
                if (texture != null) Object.Destroy(texture);
            }
        }

        private static MethodInfo FindLoadImageMethod()
        {
            Type imageConversionType = Type.GetType(
                "UnityEngine.ImageConversion, UnityEngine.ImageConversionModule");
            if (imageConversionType == null) return null;
            return imageConversionType.GetMethod(
                "LoadImage",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Texture2D), typeof(byte[]), typeof(bool) },
                null
            ) ?? imageConversionType.GetMethod(
                "LoadImage",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Texture2D), typeof(byte[]) },
                null
            );
        }

        private static bool TryLoadTextureImage(Texture2D texture, byte[] bytes)
        {
            try
            {
                if (LoadImageMethod != null)
                {
                    // Icons are only rendered, never read/modified on the CPU afterwards.
                    object[] args = LoadImageMethod.GetParameters().Length == 3
                        ? new object[] { texture, bytes, true }
                        : new object[] { texture, bytes };
                    object result = LoadImageMethod.Invoke(null, args);
                    return result is bool loaded && loaded;
                }

                HaldorExpansionPlugin.DebugLog(
                    "[HaldorExpansion] UnityEngine.ImageConversion.LoadImage method not found");
                return false;
            }
            catch (Exception ex)
            {
                HaldorExpansionPlugin.DebugLog("[HaldorExpansion] TryLoadTextureImage error: " + ex);
                return false;
            }
        }

        private static byte[] ReadAllBytes(Stream stream)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}