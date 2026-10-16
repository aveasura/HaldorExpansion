using UnityEngine;
using UnityEngine.Rendering;
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
        private const float SurfaceRefreshInterval = 0.10f;
        private const float SurfaceDepth = 12f;
        private const float SurfaceRise = 1.25f;
        private static readonly int SurfaceMask = LayerMask.GetMask("terrain", "static_solid", "piece");

        private readonly GameObject root;
        private readonly Mesh mesh;
        private readonly Material material;
        private readonly MeshFilter filter;
        private readonly MeshRenderer renderer;
        private readonly Vector3[] vertices;
        private readonly LineRenderer outline;
        private readonly Material outlineMaterial;
        private bool sampled;
        private float nextSurfaceRefresh, sampledLength, sampledWidth;
        private Vector3 sampledCentre, sampledAxis, drawnCentre, drawnAxis;

        internal bool IsValid => root != null && mesh != null && filter != null &&
                                 renderer != null && outline != null && material != null && outlineMaterial != null;

        internal CestusFacebreakerPreview()
        {
            Shader shader = SupportedShader("Sprites/Default") ?? SupportedShader("UI/Default") ??
                SupportedShader("Unlit/Color");
            if (shader == null) return;
            root = new GameObject("HE_FacebreakerPreview_LocalOnly");
            filter = root.AddComponent<MeshFilter>();
            renderer = root.AddComponent<MeshRenderer>();
            mesh = new Mesh { name = "HE_FacebreakerArea" };
            mesh.MarkDynamic();
            filter.sharedMesh = mesh;

            material = new Material(shader);
            material.color = new Color(0.08f, 0.79f, 0.95f, 0.23f);
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

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
            outline.shadowCastingMode = ShadowCastingMode.Off;
            outline.receiveShadows = false;
            outline.widthMultiplier = 0.065f;
            outline.useWorldSpace = true;
            outline.loop = true;
            outline.positionCount = 2 * (SegmentsAlong + SegmentsAcross);
            outline.numCapVertices = 0;
            outline.numCornerVertices = 2;
        }

        internal void Set(Vector3 centre, Vector3 axis, float length, float width)
        {
            if (!IsValid) return;
            bool refresh = !sampled || Time.time >= nextSurfaceRefresh ||
                           (centre - sampledCentre).sqrMagnitude > 0.25f ||
                           Mathf.Abs(centre.y - sampledCentre.y) > 0.15f ||
                           Vector3.Dot(axis, sampledAxis) < 0.99f ||
                           length != sampledLength || width != sampledWidth;
            if (!refresh && (centre - drawnCentre).sqrMagnitude < 0.000001f &&
                (axis - drawnAxis).sqrMagnitude < 0.000001f) return;
            Vector3 right = Vector3.Cross(Vector3.up, axis).normalized;
            // Walk outward from the player's support, then across each row. Adjacent
            // heights let the marker climb ramps without starting above unrelated roofs.
            for (int row = 0; row <= SegmentsAlong; row++)
            for (int column = 0; column <= SegmentsAcross; column++)
            {
                int i = FromCentre(row, SegmentsAlong / 2);
                int j = FromCentre(column, SegmentsAcross / 2);
                float u = i / (float)SegmentsAlong - 0.5f;
                float v = j / (float)SegmentsAcross - 0.5f;
                Vector3 point = centre + axis * (u * length) + right * (v * width);
                int index = i * (SegmentsAcross + 1) + j;
                float support = centre.y;
                if (refresh && j != SegmentsAcross / 2)
                    support = vertices[index + (j > SegmentsAcross / 2 ? -1 : 1)].y - GroundOffset;
                else if (refresh && i != SegmentsAlong / 2)
                    support = vertices[index + (i > SegmentsAlong / 2 ? -1 : 1) * (SegmentsAcross + 1)].y -
                              GroundOffset;
                point.y = refresh ? SampleGround(point, support) + GroundOffset : vertices[index].y;
                vertices[index] = point;
            }

            drawnCentre = centre;
            drawnAxis = axis;
            if (refresh)
            {
                sampled = true;
                sampledCentre = centre;
                sampledAxis = axis;
                sampledLength = length;
                sampledWidth = width;
                nextSurfaceRefresh = Time.time + SurfaceRefreshInterval;
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
            // Start near the connected support so a roof/next storey is not selected.
            // Terrain alone can sit beneath bridges, rocks and dungeon floors.
            Vector3 origin = new Vector3(point.x, fallback + SurfaceRise, point.z);
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, SurfaceDepth + SurfaceRise,
                    SurfaceMask, QueryTriggerInteraction.Ignore) && hit.normal.y > 0.15f &&
                !float.IsNaN(hit.point.y) && !float.IsInfinity(hit.point.y))
                return hit.point.y;
            // Uphill terrain can be above the ray origin. A distant world's terrain
            // beneath an elevated dungeon is not a usable fallback.
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(point, out float height) &&
                !float.IsNaN(height) && !float.IsInfinity(height) && Mathf.Abs(height - fallback) <= SurfaceDepth)
                return height;
            return fallback;
        }

        private static int FromCentre(int step, int centre)
        {
            return (step & 1) == 0 ? centre - step / 2 : centre + (step + 1) / 2;
        }

        private static Shader SupportedShader(string name)
        {
            Shader shader = Shader.Find(name);
            return shader != null && shader.isSupported ? shader : null;
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