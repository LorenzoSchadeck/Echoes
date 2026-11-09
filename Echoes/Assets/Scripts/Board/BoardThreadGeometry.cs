using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Barbante tubular leve, atualizado somente quando a ligação muda.</summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class BoardThreadGeometry : MonoBehaviour
{
    private const int Sides = 8;
    private Mesh mesh;
    private Material cotton, highlight;
    private Vector3[] vertices, normals;
    private Vector2[] uvs;
    private int[] triangles;

    public void Configure(Material material, Material selected)
    {
        cotton = material;
        highlight = selected;
        mesh = new Mesh { name = "Barbante procedural" };
        mesh.MarkDynamic();
        GetComponent<MeshFilter>().sharedMesh = mesh;
        GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
    }

    public void Rebuild(LineRenderer path)
    {
        int count = path.positionCount;
        if (vertices == null || vertices.Length != count * Sides)
        {
            vertices = new Vector3[count * Sides];
            normals = new Vector3[vertices.Length];
            uvs = new Vector2[vertices.Length];
            triangles = new int[(count - 1) * Sides * 6];
            int index = 0;
            for (int i = 0; i < count - 1; i++)
                for (int j = 0; j < Sides; j++)
                {
                    int a = i * Sides + j, b = i * Sides + (j + 1) % Sides;
                    triangles[index++] = a; triangles[index++] = b; triangles[index++] = a + Sides;
                    triangles[index++] = b; triangles[index++] = b + Sides; triangles[index++] = a + Sides;
                }
        }
        Vector3 start = path.GetPosition(0), end = path.GetPosition(count - 1);
        Vector3 planeNormal = Vector3.Cross(end - start, Vector3.down).normalized;
        if (planeNormal.sqrMagnitude < .5f) planeNormal = transform.parent.forward;
        float length = 0;
        for (int i = 0; i < count; i++)
        {
            Vector3 point = path.GetPosition(i);
            if (i > 0) length += Vector3.Distance(point, path.GetPosition(i - 1));
            Vector3 tangent = (path.GetPosition(Mathf.Min(count - 1, i + 1)) - path.GetPosition(Mathf.Max(0, i - 1))).normalized;
            Vector3 side = Vector3.Cross(tangent, planeNormal).normalized;
            for (int j = 0; j < Sides; j++)
            {
                float angle = j * Mathf.PI * 2 / Sides;
                Vector3 normal = side * Mathf.Cos(angle) + planeNormal * Mathf.Sin(angle);
                int vertex = i * Sides + j;
                vertices[vertex] = transform.InverseTransformPoint(point + normal * path.widthMultiplier * .5f);
                normals[vertex] = transform.InverseTransformDirection(normal);
                uvs[vertex] = new Vector2(j / (float)Sides, length / .02f);
            }
        }
        mesh.vertices = vertices; mesh.normals = normals; mesh.uv = uvs; mesh.triangles = triangles;
        mesh.RecalculateBounds();
        GetComponent<MeshRenderer>().sharedMaterial = path.sharedMaterial == highlight ? highlight : cotton;
    }

    private void OnDestroy() { if (mesh != null) Destroy(mesh); }
}
