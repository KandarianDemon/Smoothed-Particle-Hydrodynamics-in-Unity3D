using UnityEngine;
using System.Collections.Generic;
using System;

public class BVHNode
{
    public Bounds bounds;
    public BVHNode left;
    public BVHNode right;
    public int triangleIndex_A; // Leaf nodes will have a valid index, internal nodes will have -1
    public int triangleIndex_B;
    public int triangleIndex_C;
    public int triangleIndex_D;

    public bool IsLeaf() { return left == null && right == null; }
}

public class BVHComponent : MonoBehaviour
{
    private BVHNode root;
    private List<Vector3> vertices;
    private List<int> triangles;
    private int max_depth;

    [SerializeField] private int maxTrianglesPerLeaf = 2;
    [SerializeField] private bool visualizeBounds = true;

    ComputeBuffer nodeBuffer;
    ComputeBuffer triangleBuffer;

    public ComputeShader computeShader;

    private void Start()
    {
        BuildBVH();
        Debug.Log($" Maximum Depth of the BVH {FindMaxDepth(root)}");
        CreateBuffers();
        // List<BVHNodeData> nodeDataList = new List<BVHNodeData>();

        // FlattenBVHTree(root,nodeDataList);
        // Debug.Log($" The tree contains {nodeDataList.Count} nodes!");
    }

    private void Update()
    {
        if(this.transform.hasChanged)
        {
            
            BuildBVH();
           
            
            transform.hasChanged = false;
        }
    }

    public void BuildBVH()
    {
        DateTime start = DateTime.Now;
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
            bvhTriangles.Add(new BVHTriangle(v1, v2, v3, Mathf.RoundToInt(i / 3)));
        }

         root = BuildBVHRecursive(bvhTriangles);
         DateTime end = DateTime.Now;
         Debug.Log($"This function took {(end-start).TotalSeconds} seconds to perform!");
    }

    private BVHNode BuildBVHRecursive(List<BVHTriangle> triangles)
    {
        BVHNode node = new BVHNode();
        node.triangleIndex_A = -1;
        node.triangleIndex_B = -1;
        node.triangleIndex_C = -1;
        node.triangleIndex_D = -1;


        node.bounds = CalculateBounds(triangles);

        if (triangles.Count <= maxTrianglesPerLeaf)
        {
           

            node.triangleIndex_A = 1;
            // if(triangles.Count == 1)
            // {
            //     node.triangleIndex_A = triangles[0].index;
            // }

            //  if(triangles.Count == 2)
            // {
            //     node.triangleIndex_A = triangles[0].index;
            //     node.triangleIndex_B = triangles[1].index;
            // }

            //  if(triangles.Count == 3)
            // {
            //     node.triangleIndex_A = triangles[0].index;
            //     node.triangleIndex_B = triangles[1].index;
            //     node.triangleIndex_C = triangles[2].index;
            // }

            //  if(triangles.Count == 4)
            // {
            //     node.triangleIndex_A = triangles[0].index;
            //     node.triangleIndex_B = triangles[1].index;
            //     node.triangleIndex_C = triangles[2].index;
            //     node.triangleIndex_D = triangles[3].index;

            // }
            
            
            
            
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

        if(node.IsLeaf())
        {
        Gizmos.color = node.IsLeaf() ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(node.bounds.center, node.bounds.size);
        }

        if (node.left != null) DrawBVHNode(node.left);
        if (node.right != null) DrawBVHNode(node.right);
    }

    private void CreateBuffers()
    {
        if(computeShader == null)
        {
            Debug.LogError("Compute Shader is not set!");
            return;
        }

         if (root == null)
        {
            Debug.LogError("BVH not built. Call BuildBVH() first.");
            return;
        }

        List<BVHNodeData> nodeDataList = new List<BVHNodeData>();
        FlattenBVHTree(root, nodeDataList);

        
        

        nodeBuffer = new ComputeBuffer(nodeDataList.Count, sizeof(float) * 6 + sizeof(int) * 6);
        nodeBuffer.SetData(nodeDataList.ToArray());

        // Create triangle buffer
        List<TriangleData> triangleDataList = new List<TriangleData>();
        for (int i = 0; i < triangles.Count; i += 3)
        {
            triangleDataList.Add(new TriangleData
            {
                a = triangles[i],
                b = triangles[i + 1],
                c = triangles[i + 2]
            });
        }

        triangleBuffer = new ComputeBuffer(triangleDataList.Count, sizeof(int) * 6);
        triangleBuffer.SetData(triangleDataList);

        // Set buffers to your compute shader
        int kernelHandle = computeShader.FindKernel("Update");
        computeShader.SetBuffer(kernelHandle, "_BVHNodes", nodeBuffer);
        computeShader.SetBuffer(kernelHandle, "_Triangles", triangleBuffer);


        // Creates the BVHNode buffer. And maybe the triangle buffer. triangles and vertices should be added to the particle world

    }

    private void ReleaseBuffer()
    {

        if (nodeBuffer != null)
        {
            nodeBuffer.Release();
            nodeBuffer = null;
        }
        
        if (triangleBuffer != null)
        {
            triangleBuffer.Release();
            triangleBuffer = null;
        }
    }

    private void DestroyBuffer()
    {

        ReleaseBuffer();

        if (nodeBuffer != null)
        {
            nodeBuffer.Dispose();
            nodeBuffer = null;
        }

        if (triangleBuffer != null)
        {
            triangleBuffer.Dispose();
            triangleBuffer = null;
        }
    }

    private void RecalculateBVH()
    {
        // recalculates the buffers if something changed
        ReleaseBuffer();
        BuildBVH();
        CreateBuffers();
    }

    private void FlattenBVHTree(BVHNode node, List<BVHNodeData> nodeDataList)
    {

       int index = nodeDataList.Count;
        
        BVHNodeData nodeData = new BVHNodeData
        {
            min = node.bounds.min,
            max = node.bounds.max,
            leftChild = node.left != null ? index+ 1 : -1,
            rightChild = node.right != null ? index + 2 : -1,

            triangleIndex_A = node.triangleIndex_A,
            triangleIndex_B = node.triangleIndex_B,
            triangleIndex_C = node.triangleIndex_C,
            triangleIndex_D = node.triangleIndex_D,

        };

        nodeDataList.Add(nodeData);

        if (node.left != null)
            FlattenBVHTree(node.left, nodeDataList);
        if (node.right != null)
            FlattenBVHTree(node.right, nodeDataList);

        

        
    }

    public static int FindMaxDepth(BVHNode node)
{
    // Base case: leaf node
    if (node.IsLeaf())
    {
        return 0;
    }

    // Recursive case: internal node
    int leftDepth = FindMaxDepth(node.left);
    int rightDepth = FindMaxDepth(node.right);

    // Return the maximum depth of the two subtrees plus one for the current node
    return Math.Max(leftDepth, rightDepth) + 1;
}

    private struct BVHNodeData
    {
        public Vector3 min;
        public Vector3 max;
        public int leftChild;
        public int rightChild;
        public int triangleIndex_A;
        public int triangleIndex_B;
        public int triangleIndex_C;
        public int triangleIndex_D;
    }

        private struct TriangleData
    {
        public int a, b, c;
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