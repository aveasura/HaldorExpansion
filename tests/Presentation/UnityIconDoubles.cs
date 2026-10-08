// Minimal stand-ins for native Unity objects. Tests exercise the real loader's
// caching/ownership logic; they do not test PNG decoding or GPU rendering.
using System;
namespace UnityEngine
{
    public class Object
    {
        public string name;
        public bool Destroyed;
        public static void Destroy(Object value) { if (!ReferenceEquals(value, null)) value.Destroyed = true; }
        public static bool operator ==(Object a, Object b)
        {
            bool an = ReferenceEquals(a, null) || a.Destroyed;
            bool bn = ReferenceEquals(b, null) || b.Destroyed;
            return an || bn ? an == bn : ReferenceEquals(a, b);
        }
        public static bool operator !=(Object a, Object b) => !(a == b);
        public override bool Equals(object value) => ReferenceEquals(this, value);
        public override int GetHashCode() => base.GetHashCode();
    }
    public enum TextureFormat { RGBA32 }
    public class Texture2D : Object
    {
        public static Texture2D LastCreated;
        public int width, height;
        public Texture2D(int w, int h, TextureFormat format, bool mipmap) { width=w; height=h; LastCreated=this; }
    }
    public struct Rect { public Rect(float x, float y, float w, float h) { } }
    public struct Vector2 { public Vector2(float x, float y) { } }
    public class Sprite : Object
    {
        public Texture2D texture;
        public static Sprite Create(Texture2D t, Rect r, Vector2 pivot) => new Sprite { texture=t };
    }
    public static class ImageConversion
    {
        public static bool Succeed = true;
        public static bool Throw;
        public static int Loads;
        public static bool LastMarkNonReadable;
        public static bool LoadImage(Texture2D texture, byte[] data, bool markNonReadable)
        {
            Loads++;
            LastMarkNonReadable = markNonReadable;
            if (Throw) throw new InvalidOperationException("Test decode failure");
            return Succeed;
        }
    }
}
public class ItemDrop
{
    public class ItemData { public class SharedData { public UnityEngine.Sprite[] m_icons; } }
}
