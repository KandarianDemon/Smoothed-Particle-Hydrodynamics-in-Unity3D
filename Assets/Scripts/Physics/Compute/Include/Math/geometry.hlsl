#ifndef GEOMETRY_INCLUDED
#define GEOMETRY_INCLUDED

struct Plane{

    // Defines a Plane for Math-related stuff, like intersection tests.


    float3 normal;  // normal = (a,b,c);
    float3 origin;  // origin = (x,y,z);
    float  d;       // ax + by + cz - d = 0;
};

Plane ConstructPlane(float3 origin, float3 normal)
{
    Plane p;
    p.origin    = origin;
    p.normal    = normal;
    p.d         = origin.x*normal.x + origin.y*normal.y + origin.z*normal.z; 

    return p;
}


#endif