yx using UnityEngine;
using System.Collections.Generic;

public class BVHNode
{
    public Bounds bounds;
    public BVHNode left;
    public BVHNode right;
    public int triangleIndex; // Leaf nodes will have a valid index, internal nodes will have -1

    public bool IsLeaf() { return left == null && right == null; }
}

public class BVHComponent : MonoBehaviour
{
    private BVHNode root;
    private List<Vector3> vertices;
    private List<int> triangles;

    [SerializeField] private int maxTrianglesPerLeaf = 2;
    [SerializeField] private bool visualizeBounds = true;

    private void Start()
    {
        BuildBVH();
    }

    public void BuildBVH()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("MeshFilter or Mesh is missing!");
            return;
        }

        Mesh mesh = meshFilter.sharedMesh;
        vertices = new List<Vector3>(mesh.vertices);
        triangles = new List<int>(mesh.triangles);

        List<BVHTriangle> bvhTriangles = new List<BVHTriangle>();
        for (int i = 0; i < triangles.Count; i += 3)
        {
            Vector3 v1 = transform.TransformPoint(vertices[triangles[i]]);
            Vector3 v2 = transform.TransformPoint(vertices[triangles[i + 1]]);
            Vector3 v3 = transform.TransformPoint(vertices[triangles[i + 2]]);
            bvhTriangles.Add(new BVHTriangle(v1, v2, v3, i / 3));
        }

        root = BuildBVHRecursive(bvhTriangles);
    }

    private BVHNode BuildBVHRecursive(List<BVHTriangle> triangles)
    {
        BVHNode node = new BVHNode();
        node.bounds = CalculateBounds(triangles);

        if (triangles.Count <= maxTrianglesPerLeaf)
        {
            node.triangleIndex = triangles[0].index;
            return node;
        }

        List<BVHTriangle> leftTriangles = new List<BVHTriangle>();
        List<BVHTriangle> rightTriangles = new List<BVHTriangle>();

        int axis = GetLongestAxis(node.bounds);
        float splitPos = (node.bounds.min[axis] + node.bounds.max[axis]) / 2f;

        foreach (BVHTriangle tri in triangles)
        {
            if (tri.GetCenter()[axis] < splitPos)
                leftTriangles.Add(tri);
            else
                rightTriangles.Add(tri);
        }

        // Handle case where all triangles end up on one side
        if (leftTriangles.Count == 0 || rightTriangles.Count == 0)
        {
            int mid = triangles.Count / 2;
            leftTriangles = triangles.GetRange(0, mid);
            rightTriangles = triangles.GetRange(mid, triangles.Count - mid);
        }

        node.left = BuildBVHRecursive(leftTriangles);
        node.right = BuildBVHRecursive(rightTriangles);

        return node;
    }

    private Bounds CalculateBounds(List<BVHTriangle> triangles)
    {
        Bounds bounds = new Bounds(triangles[0].GetCenter(), Vector3.zero);
        foreach (BVHTriangle tri in triangles)
        {
            bounds.Encapsulate(tri.v1);
            bounds.Encapsulate(tri.v2);
            bounds.Encapsulate(tri.v3);
        }
        return bounds;
    }

    private int GetLongestAxis(Bounds bounds)
    {
        Vector3 size = bounds.size;
        if (size.x > size.y && size.x > size.z) return 0;
        if (size.y > size.z) return 1;
        return 2;
    }

    private void OnDrawGizmos()
    {
        if (visualizeBounds && root != null)
        {
            DrawBVHNode(root);
        }
    }

    private void DrawBVHNode(BVHNode node)
    {
        Gizmos.color = node.IsLeaf() ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(node.bounds.center, node.bounds.size);

        if (node.left != null) DrawBVHNode(node.left);
        if (node.right != null) DrawBVHNode(node.right);
    }
}

public class BVHTriangle
{
    public Vector3 v1, v2, v3;
    public int index;

    public BVHTriangle(Vector3 v1, Vector3 v2, Vector3 v3, int index)
    {
        this.v1 = v1;
        this.v2 = v2;
        this.v3 = v3;
        this.index = index;
    }

    public Vector3 GetCenter()
    {
        return (v1 + v2 + v3) / 3f;
    }
}







// ComputeBuffer bvhBuffer;
// ComputeBuffer triangleBuffer;

// void SetupBVH(Mesh mesh)
// {
//     // Build your BVH here...
//     // This is a simplified example, you'll need to implement the actual BVH construction

//     BVHNode[] bvhNodes = BuildBVH(mesh);
//     bvhBuffer = new ComputeBuffer(bvhNodes.Length, sizeof(float) * 6 + sizeof(int) * 3);
//     bvhBuffer.SetData(bvhNodes);

//     Vector3[] vertices = mesh.vertices;
//     int[] triangles = mesh.triangles;
//     Triangle[] triangleArray = new Triangle[triangles.Length / 3];
//     for (int i = 0; i < triangles.Length; i += 3)
//     {
//         triangleArray[i / 3] = new Triangle
//         {
//             v0 = vertices[triangles[i]],
//             v1 = vertices[triangles[i + 1]],
//             v2 = vertices[triangles[i + 2]]
//         };
//     }
//     triangleBuffer = new ComputeBuffer(triangleArray.Length, sizeof(float) * 9);
//     triangleBuffer.SetData(triangleArray);

//     computeShader.SetBuffer(kernelHandle, "_BVHNodes", bvhBuffer);
//     computeShader.SetBuffer(kernelHandle, "_Triangles", triangleBuffer);
// }

// void OnDisable()
// {
//     if (bvhBuffer != null)
//         bvhBuffer.Release();
//     if (triangleBuffer != null)
//         triangleBuffer.Release();
// }