namespace HaldorExpansion
{
    internal static class HaldorExpansionPlugin
    {
        internal static void DebugLog(string s)
        {
        }
    }
}

class EmbeddedIconLoaderTests
{
    static int failures;

    static void Check(bool ok, string name)
    {
        Console.WriteLine((ok ? "PASS: " : "FAIL: ") + name);
        if (!ok) failures++;
    }

    static void Main()
    {
        var first = new ItemDrop.ItemData.SharedData();
        var second = new ItemDrop.ItemData.SharedData();
        Check(EmbeddedIconLoader.TrySetEmbeddedIcon(first, "first.png"), "initial icon loads");
        var sprite = first.m_icons[0];
        var array = first.m_icons;
        EmbeddedIconLoader.TrySetEmbeddedIcon(first, "first.png");
        EmbeddedIconLoader.TrySetEmbeddedIcon(second, "first.png");
        Check(ReferenceEquals(sprite, first.m_icons[0]) && ReferenceEquals(sprite, second.m_icons[0]),
            "same resource shares one Sprite across items");
        Check(ReferenceEquals(array, first.m_icons), "reapplying an unchanged icon preserves its array");
        Check(ImageConversion.Loads == 1, "repeated applications decode PNG only once");
        Check(ImageConversion.LastMarkNonReadable, "decoded icon releases its CPU pixel copy");
        UnityEngine.Object.Destroy(sprite.texture);
        Check(
            EmbeddedIconLoader.TrySetEmbeddedIcon(first, "first.png") && first.m_icons[0].texture != null &&
            !ReferenceEquals(sprite, first.m_icons[0]), "destroyed cached texture is reloaded");
        ImageConversion.Succeed = false;
        var original = first.m_icons;
        Check(!EmbeddedIconLoader.TrySetEmbeddedIcon(first, "retry.png"), "failed decode reports failure");
        Check(ReferenceEquals(original, first.m_icons), "failed decode preserves existing icon");
        Check(Texture2D.LastCreated == null, "failed decode destroys allocated texture");
        ImageConversion.Succeed = true;
        Check(EmbeddedIconLoader.TrySetEmbeddedIcon(first, "retry.png"), "failed resource can be retried");
        ImageConversion.Throw = true;
        Check(!EmbeddedIconLoader.TrySetEmbeddedIcon(second, "throws.png") && Texture2D.LastCreated == null,
            "exception path also destroys allocated texture");
        ImageConversion.Throw = false;
        Check(!EmbeddedIconLoader.TrySetEmbeddedIcon(second, "missing.png"), "missing resource reports failure");
        Check(!EmbeddedIconLoader.TrySetEmbeddedIcon(null, "first.png"), "null shared data is ignored");
        if (failures != 0) Environment.Exit(1);
    }
}