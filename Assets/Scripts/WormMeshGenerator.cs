using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WormMeshGenerator : MonoBehaviour
{
    public int segments = 100;
    public float radius = 0.05f;
    public float length = 1f;
    public int radialSegments = 12;
    public AnimationCurve wormCurve;

     private bool oldCulling;

    private Mesh mesh;

    void Start()
    {
        GenerateWormMesh();
    }

    void GenerateWormMesh()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        Vector3[] vertices = new Vector3[(segments + 1) * radialSegments];
        int[] triangles = new int[segments * radialSegments * 6];
        Vector2[] uv = new Vector2[vertices.Length];

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            Vector3 center = GetWormPosition(t);
            Vector3 forward = GetWormForward(t);
            Vector3 up = Vector3.Cross(forward, Vector3.right).normalized;
            Vector3 right = Vector3.Cross(up, forward);

            for (int j = 0; j < radialSegments; j++)
            {
                float angle = j * Mathf.PI * 2f / radialSegments;
                Vector3 offset = right * Mathf.Cos(angle) * radius + up * Mathf.Sin(angle) * radius;
                vertices[i * radialSegments + j] = center + offset;
                uv[i * radialSegments + j] = new Vector2((float)j / radialSegments, t);
            }
        }

        int ti = 0;
        for (int i = 0; i < segments; i++) {
                for (int j = 0; j < radialSegments; j++) {
                    int current = i * radialSegments + j;
                    int next = current + radialSegments;

                    // Original triangle generation
                    triangles[ti++] = current;
                    triangles[ti++] = next;
                    triangles[ti++] = (current + 1) % radialSegments + i * radialSegments;

                    triangles[ti++] = (current + 1) % radialSegments + i * radialSegments;
                    triangles[ti++] = next;
                    triangles[ti++] = (next + 1) % radialSegments + (i + 1) * radialSegments;
        }
}


         mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uv);
        mesh.RecalculateNormals();

        Vector3[] normals = new Vector3[mesh.normals.Length];

        for(int i = 0; i < normals.Length; i++)
        {
            normals[i] = mesh.normals[i]*-1;
        }

        mesh.normals = normals;

        mesh.RecalculateNormals();
        
        
       
    }

    Vector3 GetWormPosition(float t)
    {
        float x = wormCurve.Evaluate(t) * length;
        float y = Mathf.Sin(t * Mathf.PI * 2) * length * 0.2f;
        float z = t * length;
        return new Vector3(x, y, z);
    }

    Vector3 GetWormForward(float t)
    {
        float delta = 0.01f;
        Vector3 current = GetWormPosition(t);
        Vector3 next = GetWormPosition(Mathf.Min(1, t + delta));
        return (next - current).normalized;
    }

    public void OnPreRender()
    {
        oldCulling = GL.invertCulling;
        GL.invertCulling = true;
    }

    public void OnPostRender()
    {
        GL.invertCulling = oldCulling;
    }
}