#ifndef BVH_INCLUDED
#define BVH_INCLUDED

#define MAX_BVH_DEPTH 32

struct BVHNode
{
    float3 min;
    float3 max;
    int leftChild;
    int rightChild;
    int triangleIndex;
};

struct Triangle
{
    float3 v0, v1, v2;
};

StructuredBuffer<BVHNode> _BVHNodes;
StructuredBuffer<Triangle> _Triangles;

bool IntersectAABB(float3 rayOrigin, float3 rayDir, float3 boxMin, float3 boxMax, out float tMin, out float tMax)
{
    float3 invDir = 1.0 / rayDir;
    float3 t0 = (boxMin - rayOrigin) * invDir;
    float3 t1 = (boxMax - rayOrigin) * invDir;
    float3 tmin = min(t0, t1);
    float3 tmax = max(t0, t1);

    tMin = max(max(tmin.x, tmin.y), tmin.z);
    tMax = min(min(tmax.x, tmax.y), tmax.z);

    return tMax >= tMin && tMax >= 0;
}

bool IntersectTriangle(float3 rayOrigin, float3 rayDir, Triangle tri, out float t, out float3 barycentricCoords)
{
    float3 edge1 = tri.v1 - tri.v0;
    float3 edge2 = tri.v2 - tri.v0;
    float3 h = cross(rayDir, edge2);
    float a = dot(edge1, h);

    if (abs(a) < 1e-6)
    {
        t = 0;
        barycentricCoords = float3(0, 0, 0);
        return false;
    }

    float f = 1.0 / a;
    float3 s = rayOrigin - tri.v0;
    float u = f * dot(s, h);

    if (u < 0.0 || u > 1.0)
    {
        t = 0;
        barycentricCoords = float3(0, 0, 0);
        return false;
    }

    float3 q = cross(s, edge1);
    float v = f * dot(rayDir, q);

    if (v < 0.0 || u + v > 1.0)
    {
        t = 0;
        barycentricCoords = float3(0, 0, 0);
        return false;
    }

    t = f * dot(edge2, q);
    barycentricCoords = float3(1 - u - v, u, v);
    return t > 0;
}

bool TraverseBVH(float3 rayOrigin, float3 rayDir, out float hitDistance, out int hitTriIndex)
{
    int stack[MAX_BVH_DEPTH];
    int stackPtr = 0;

    stack[stackPtr++] = 0; // Start with root node
    hitDistance = 1.#INF;
    hitTriIndex = -1;

    while (stackPtr > 0)
    {
        int nodeIndex = stack[--stackPtr];
        BVHNode node = _BVHNodes[nodeIndex];

        float tMin, tMax;
        if (!IntersectAABB(rayOrigin, rayDir, node.min, node.max, tMin, tMax) || tMin > hitDistance)
            continue;

        if (node.triangleIndex >= 0)
        {
            // Leaf node
            Triangle tri = _Triangles[node.triangleIndex];
            float t;
            float3 barycentricCoords;
            if (IntersectTriangle(rayOrigin, rayDir, tri, t, barycentricCoords) && t < hitDistance)
            {
                hitDistance = t;
                hitTriIndex = node.triangleIndex;
            }
        }
        else
        {
            // Internal node
            stack[stackPtr++] = node.leftChild;
            stack[stackPtr++] = node.rightChild;
        }
    }

    return hitTriIndex >= 0;
}

#endif // BVH_INCLUDED