using UnityEngine;
using Object = UnityEngine.Object;

namespace HaldorExpansion.Features.Cestus
{
    /// <summary>Simple ground-conforming translucent rectangular targeting marker.</summary>
    internal sealed class CestusFacebreakerPreview
    {
        // Several terrain samples along both axes keep the rectangle usable on slopes.
        private const int SegmentsAlong = 12;
        private const int SegmentsAcross = 4;
        private const float GroundOffset = 0.10f;

        private readonly GameObject root;
        private readonly Mesh mesh;
        private readonly Material material;
        private readonly MeshFilter filter;
        private readonly MeshRenderer renderer;
        private readonly Vector3[] vertices;
        private readonly LineRenderer outline;
        private readonly Material outlineMaterial;

        internal CestusFacebreakerPreview()
        {
            root = new GameObject("HE_FacebreakerPreview_LocalOnly");
            filter = root.AddComponent<MeshFilter>();
            renderer = root.AddComponent<MeshRenderer>();
            mesh = new Mesh { name = "HE_FacebreakerArea" };
            mesh.MarkDynamic();
            filter.sharedMesh = mesh;

            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            material = new Material(shader);
            material.color = new Color(0.08f, 0.79f, 0.95f, 0.23f);
            renderer.sharedMaterial = material;

            int count = (SegmentsAlong + 1) * (SegmentsAcross + 1);
            vertices = new Vector3[count];
            int[] triangles = new int[SegmentsAlong * SegmentsAcross * 6];
            int t = 0;
            for (int i = 0; i < SegmentsAlong; i++)
                for (int j = 0; j < SegmentsAcross; j++)
                {
                    int a = i * (SegmentsAcross + 1) + j;
                    int b = a + SegmentsAcross + 1;
                    triangles[t++] = a;
                    triangles[t++] = b;
                    triangles[t++] = a + 1;
                    triangles[t++] = b;
                    triangles[t++] = b + 1;
                    triangles[t++] = a + 1;
                }
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            GameObject outlineObject = new GameObject("HE_FacebreakerOutline");
            outlineObject.transform.SetParent(root.transform, false);
            outline = outlineObject.AddComponent<LineRenderer>();
            outlineMaterial = new Material(shader);
            outlineMaterial.color = new Color(0.12f, 0.91f, 1f, 0.95f);
            outline.sharedMaterial = outlineMaterial;
            outline.widthMultiplier = 0.065f;
            outline.useWorldSpace = true;
            outline.loop = true;
            outline.positionCount = 2 * (SegmentsAlong + SegmentsAcross);
            outline.numCapVertices = 0;
            outline.numCornerVertices = 2;
        }

        internal void Set(Vector3 centre, Vector3 axis, float length, float width)
        {
            Vector3 right = Vector3.Cross(Vector3.up, axis).normalized;
            for (int i = 0; i <= SegmentsAlong; i++)
                for (int j = 0; j <= SegmentsAcross; j++)
                {
                    float u = i / (float)SegmentsAlong - 0.5f;
                    float v = j / (float)SegmentsAcross - 0.5f;
                    Vector3 point = centre + axis * (u * length) + right * (v * width);
                    point.y = SampleGround(point, centre.y) + GroundOffset;
                    vertices[i * (SegmentsAcross + 1) + j] = point;
                }
            mesh.vertices = vertices;
            mesh.RecalculateBounds();

            int cursor = 0;
            for (int i = 0; i < SegmentsAlong; i++)
                outline.SetPosition(cursor++, vertices[i * (SegmentsAcross + 1)]);
            for (int j = 0; j < SegmentsAcross; j++)
                outline.SetPosition(cursor++, vertices[SegmentsAlong * (SegmentsAcross + 1) + j]);
            for (int i = SegmentsAlong; i > 0; i--)
                outline.SetPosition(cursor++, vertices[i * (SegmentsAcross + 1) + SegmentsAcross]);
            for (int j = SegmentsAcross; j > 0; j--)
                outline.SetPosition(cursor++, vertices[j]);
        }

        private static float SampleGround(Vector3 point, float fallback)
        {
            if (ZoneSystem.instance != null)
                return ZoneSystem.instance.GetGroundHeight(point);
            return fallback;
        }

        internal void Dispose()
        {
            if (root != null) Object.Destroy(root);
            if (mesh != null) Object.Destroy(mesh);
            if (material != null) Object.Destroy(material);
            if (outlineMaterial != null) Object.Destroy(outlineMaterial);
        }
    }
}
