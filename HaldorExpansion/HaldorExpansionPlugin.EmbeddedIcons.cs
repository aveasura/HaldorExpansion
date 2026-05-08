using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace HaldorExpansion
{
    public partial class HaldorExpansionPlugin
    {
        private const string EmbeddedIconPrefix = "HaldorExpansion.Assets.Icons.";

        private static bool TrySetEmbeddedIcon(ItemDrop.ItemData.SharedData shared, string iconFileName)
        {
            if (shared == null)
                return false;

            Sprite icon = LoadEmbeddedIcon(EmbeddedIconPrefix + iconFileName);
            if (icon == null)
                return false;

            shared.m_icons = new[] { icon };
            DebugLog("[HaldorExpansion] Embedded icon applied: " + iconFileName);
            return true;
        }

        private static Sprite LoadEmbeddedIcon(string resourceName)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        DebugLog("[HaldorExpansion] Embedded icon not found: " + resourceName);
                        return null;
                    }

                    byte[] bytes = ReadAllBytes(stream);

                    Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

                    if (!TryLoadTextureImage(texture, bytes))
                    {
                        DebugLog("[HaldorExpansion] Failed to load embedded icon image: " + resourceName);
                        return null;
                    }

                    texture.name = resourceName;

                    return Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f)
                    );
                }
            }
            catch (Exception ex)
            {
                DebugLog("[HaldorExpansion] Failed to load embedded icon " + resourceName + ": " + ex);
                return null;
            }
        }

        private static bool TryLoadTextureImage(Texture2D texture, byte[] bytes)
        {
            try
            {
                Type imageConversionType = Type.GetType(
                    "UnityEngine.ImageConversion, UnityEngine.ImageConversionModule"
                );

                if (imageConversionType == null)
                {
                    DebugLog("[HaldorExpansion] UnityEngine.ImageConversion type not found");
                    return false;
                }

                MethodInfo loadImageMethod = imageConversionType.GetMethod(
                    "LoadImage",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(Texture2D), typeof(byte[]), typeof(bool) },
                    null
                );

                if (loadImageMethod != null)
                {
                    object result = loadImageMethod.Invoke(null, new object[] { texture, bytes, false });
                    return result is bool loaded && loaded;
                }

                loadImageMethod = imageConversionType.GetMethod(
                    "LoadImage",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(Texture2D), typeof(byte[]) },
                    null
                );

                if (loadImageMethod != null)
                {
                    object result = loadImageMethod.Invoke(null, new object[] { texture, bytes });
                    return result is bool loaded && loaded;
                }

                DebugLog("[HaldorExpansion] UnityEngine.ImageConversion.LoadImage method not found");
                return false;
            }
            catch (Exception ex)
            {
                DebugLog("[HaldorExpansion] TryLoadTextureImage error: " + ex);
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