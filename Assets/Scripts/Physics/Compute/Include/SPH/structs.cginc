#ifndef STRUCTS_CGINC_INCLUDED
#define STRUCTS_CGINC_INCLUDED

// struct BoundingBox{

//     float3 high;
//     float3 low;

//     //AABB should be aligned to world axes

//     // Bounding box of object needs to be oriented along the object axis -> RTS matrices.
// };

struct NeighborIndexData
{
    int indices[27];
};
struct Particle 
{
    
    float3 color;
    float3 position;
    float3 velocity;
    float3 offset;
    float3 predictedPosition;

    float pressure;
    float density;
    float radius;
    float mass;

    int hash;
    int index;
    int _static;
    int type;
};

struct Vertex{


    // Stores indices of associated tris
    int tris [6];
    int numberOfTris;

    // stores the index of the particle in the PARTICLE Buffer
    int particleID;
};

struct Triangle{

    int a,b,c,hash;
    float3 center, normal;
};

struct int36
{
    int values [36];
};


struct Connection{

    int p1;
    int p2;
};

struct TriangleCollisionInfo{

    
    float t;
    float3 normal;
    float3 contactPoint;

};




#endif