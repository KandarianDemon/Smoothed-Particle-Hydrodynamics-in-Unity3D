#ifndef BVH_INCLUDED
#define BVH_INCLUDED

#define MAX_BVH_DEPTH 32

#include "structs.cginc"

struct BVHNode
{
    float3 min;
    float3 max;
    int leftChild;
    int rightChild;

    int triangleIndex_A;
    int triangleIndex_B;
    int triangleIndex_C;
    int triangleIndex_D;
};

struct ParticleNodeInfo
{
    int currentLeafNode;
    int potentialCollisionNode;
    float collisionDistance;
};

// struct Triangle
// {
//     float3 v0, v1, v2;
// };

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

bool IsPointInsideAABB(float3 p, float3 bounds_min, float3 bounds_max)
{
    return p.x >= bounds_min.x && p.x <= bounds_max.x &&
           p.y >= bounds_min.y && p.y <= bounds_max.y &&
           p.z >= bounds_min.z && p.z <= bounds_max.z;
}


bool IntersectTriangle(float3 rayOrigin, float3 rayDir, Triangle tri,RWStructuredBuffer<Particle> PARTICLES, out float t, out float3 barycentricCoords)
{

    
    float3 v0 = PARTICLES[tri.a].position;
    float3 v1 = PARTICLES[tri.b].position;
    float3 v2 = PARTICLES[tri.c].position;
    

    float3 edge1 = v1 - v0;
    float3 edge2 = v2 - v0;
    float3 h = cross(rayDir, edge2);
    float a = dot(edge1, h);

    if (abs(a) < 1e-6)
    {
        t = 0;
        barycentricCoords = float3(0, 0, 0);
        return false;
    }

    float f = 1.0 / a;
    float3 s = rayOrigin - v0;
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



bool TraverseBVH(float3 rayOrigin, float3 rayDir, RWStructuredBuffer<Particle> PARTICLES, out float hitDistance, out int hitTriIndex)
{
    int stack[MAX_BVH_DEPTH];
    int stackPtr = 0;

    stack[stackPtr++] = 0; // Start with root node
    hitDistance = 0.35f;
    hitTriIndex = -1;

    bool collision = false;

    while (stackPtr > 0)
    {
        int nodeIndex = stack[--stackPtr];
        BVHNode node = _BVHNodes[nodeIndex];


        

        float tMin, tMax;
        if (!IntersectAABB(rayOrigin, rayDir, node.min, node.max, tMin, tMax) || tMin > hitDistance)
            continue;

        
        
                  
        // if(!IsPointInsideAABB(rayOrigin, node.min,node.max))
        // {    
        //         continue;
        // }
      

            
        
        if (node.leftChild == -1 && node.rightChild ==-1 && IsPointInsideAABB(rayOrigin,node.min,node.max))
        {
            // Leaf node
            Triangle tri = _Triangles[node.triangleIndex_A];
            
      
            collision = true;
            float t;
            float3 barycentricCoords;
            hitTriIndex = node.triangleIndex_A;
            if (IntersectTriangle(rayOrigin, rayDir, tri, PARTICLES, t, barycentricCoords) && t < hitDistance)
            {
                hitDistance = t;
                hitTriIndex = node.triangleIndex_A;
               
            }
        }
        else
        {
            // Internal node
            stack[stackPtr++] = node.leftChild;
            stack[stackPtr++] = node.rightChild;
        }
    }

    return collision;
}


bool TraverseBVH_LeafNodeCollision(float3 rayOrigin, float3 rayDir, RWStructuredBuffer<Particle> PARTICLES, out float hitDistance, out int hitTriIndex)
{
    
    int leafNodeIndex = -1;
    int nodeIndex = 0;

    hitDistance = 0;
    hitTriIndex = 0;

  



    while(true)
    {


    BVHNode node = _BVHNodes[nodeIndex];

    if(IsPointInsideAABB(rayOrigin, node.min,node.max))
    {
        

        int left = node.leftChild;
        int right = node.leftChild;

        if(left == -1 || right == -1){
            return false;
        }

        BVHNode leftNode = _BVHNodes[left];
        BVHNode rightNode = _BVHNodes[right];

        bool isRight = IsPointInsideAABB(rayOrigin, rightNode.min,rightNode.max);
        bool isLeft  = IsPointInsideAABB(rayOrigin, leftNode.min,leftNode.max);

        int og_index = nodeIndex;
        nodeIndex = (isRight) ? right:nodeIndex;
        nodeIndex = (isLeft) ?  left:nodeIndex;

        if(nodeIndex == og_index)
        {
            return false;
        }
    }

    else{


        return false;
    }

    }

    

  

}

ParticleNodeInfo TraverseBVHForParticle(float3 particlePosition, float3 particleVelocity, float timeStep)
{
    ParticleNodeInfo info;
    info.currentLeafNode = -1;
    info.potentialCollisionNode = -1;
    info.collisionDistance = 3.402823466e+38; // FLT_MAX

    int stack[MAX_BVH_DEPTH];
    int stackPtr = 0;

    stack[stackPtr++] = 0; // Start with root node

    float3 rayDir = normalize(particleVelocity);
    float3 rayEnd = particlePosition + particleVelocity * timeStep;

    while (stackPtr > 0)
    {
        int nodeIndex = stack[--stackPtr];
        BVHNode node = _BVHNodes[nodeIndex];

        float tMin, tMax;
        bool intersectsNode = IntersectAABB(particlePosition, rayDir, node.min, node.max, tMin, tMax);
        bool isInside = IsPointInsideAABB(particlePosition, node.min, node.max);

        if (!intersectsNode && !isInside)
            continue;

        if (node.leftChild == -1 && node.rightChild == -1)
        {
            // Leaf node
            if (isInside)
            {
                info.currentLeafNode = nodeIndex;
            }

            if (intersectsNode && tMin < info.collisionDistance)
            {
                info.potentialCollisionNode = nodeIndex;
                info.collisionDistance = tMin;
            }

            if (info.currentLeafNode != -1 && info.potentialCollisionNode != -1)
            {
                return info; // We've found both current and potential nodes
            }
        }
        else
        {
            // Internal node, add children to stack
            stack[stackPtr++] = node.leftChild;
            stack[stackPtr++] = node.rightChild;
        }
    }

    return info;
}



#endif // BVH_INCLUDED