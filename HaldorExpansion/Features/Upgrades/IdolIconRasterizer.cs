using UnityEngine;

namespace HaldorExpansion.Features.Upgrades
{
    // Sprite.rect is not an atlas address. Actual UVs also handle tight packing,
    // trimming and rotated entries; the original pivot restores transparent margins.
    internal static class IdolIconRasterizer
    {
        internal static Color[] Render(int width, int height, Vector2[] vertices, ushort[] triangles,
            Vector2[] uv, Color[] atlas, int atlasWidth, int atlasHeight)
        {
            var result = new Color[width * height];
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int ia = triangles[i], ib = triangles[i + 1], ic = triangles[i + 2];
                Vector2 a = vertices[ia], b = vertices[ib], c = vertices[ic];
                float area = Cross(b - a, c - a);
                if (Mathf.Abs(area) < 0.00001f) continue;
                int left = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
                int right = Mathf.Min(width - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
                int bottom = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
                int top = Mathf.Min(height - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));
                for (int y = bottom; y <= top; y++)
                for (int x = left; x <= right; x++)
                {
                    Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                    float wb = Cross(point - a, c - a) / area;
                    float wc = Cross(b - a, point - a) / area;
                    float wa = 1f - wb - wc;
                    if (wa < -0.00001f || wb < -0.00001f || wc < -0.00001f) continue;
                    Vector2 sample = uv[ia] * wa + uv[ib] * wb + uv[ic] * wc;
                    // Pixel centers map exactly for both unrotated and rotated atlas entries.
                    int sx = Mathf.Clamp(Mathf.FloorToInt(sample.x * atlasWidth), 0, atlasWidth - 1);
                    int sy = Mathf.Clamp(Mathf.FloorToInt(sample.y * atlasHeight), 0, atlasHeight - 1);
                    result[y * width + x] = atlas[sy * atlasWidth + sx];
                }
            }

            return result;
        }

        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }
    }
}