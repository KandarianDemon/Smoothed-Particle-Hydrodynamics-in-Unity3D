#ifndef INTERSECTIONTESTLIB_INCLUDED
#define INTERSECTIONTESTLIB_INCLUDED

#include "./Include/Math/geometry.hlsl"


// This file contains code for common geometrical intersection tests. Most of these are required for Collision test in the fluid physics.
// All the code is taken from Christer Ericsons "Real-Time-Collision-Detection";

// Closest Point Computations



float DistPointPlane(float3 p, Plane plane)
{
    return (dot(plane.normal, p) - plane.d) / dot(plane.normal,plane.normal);
}

float3 ClosestPointOnPlane(float3 p, Plane plane)
{
    // Takes point p and the plane.
    // returns the closest point on the plane.
    // closest point always in normal direction to the plane.

    float t = DistPointPlane(p, plane);
    

    return p - t*plane.normal;
}


float3 ClosestPointOnLineSegment(float3 a, float3 b, float3 c)
{

    float3 ab = b -a;
    float t = dot(c-a,ab)/dot(ab,ab);

    if(t < 0.0f) t = 0.0f;
    if(t> 1.0f) t = 1.0f;

    return a+t*ab; 

}

float SqDistanceToLineSegment(float3 a, float3 b, float3 c)
{

    // This can be used to determine the distance of a particle to an edge! 

    float3 ab = b-a;
    float3 ac = c-a;
    float3 bc = c-b;

    float e = dot(ac,ab);

    if(e<= 0.0f) return dot(ac,ac);
    
    float f = dot(ab,ab);

    if(e>=f) return dot(bc,bc);

    return dot(ac,ac) - e*e/f;

}

// Ray-Plane-Intersection Tests ()

// Testing Primitives  (Primitive Collisions)

// Additional Tests



bool IsPointInsideTriangle(float3 p, Triangle tri)
{

    // REWORK THIS. Currently it is fitted to the Particles-Struct. Make it more flexible.
    // PAss array of points instead of the Tri. But passing a tri is very convenient. Requires a very general
    // definition tho.

    #ifdef USE_SOA

        float3 a = POSITIONS[tri.a] - p;   //Get positions and transform to origin.
        float3 b = POSITIONS[tri.b] - p;
        float3 c = POSITIONS[tri.c] - p;



    #else
        float3 a = PARTICLES[tri.a].position - p;   //Get positions and transform to origin.
        float3 b = PARTICLES[tri.b].position - p;
        float3 c = PARTICLES[tri.c].position - p;
    
       
    #endif

    float3 u = cross(b,c);
    float3 v = cross(c,a);

    if(dot(u,v) < 0.0f) return false;

    float3 w = cross(a,b);
    if(dot(u,w) < 0.0f) return false;

    return true;


}

// Dynamic Intersection Test



#endif