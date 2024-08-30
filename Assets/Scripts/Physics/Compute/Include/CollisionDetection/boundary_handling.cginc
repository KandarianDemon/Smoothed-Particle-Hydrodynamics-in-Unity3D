#ifndef BOUNDARY_HANDLING_INCLUDED



#define BOUNDARY_HANDLING_INCLUDED

#include "./Include/SPH/sph_settings.cginc"
#include "./Include/SPH/structs.cginc"


float3 TranslatePoint(float3 p, float3 origin)
{   

    // Translates a point to the world coordinates of another point

    return origin + p;
}

bool IsWallHit(float3 dist){

    return dist.x < SMOOTHING_RADIUS || dist.y < SMOOTHING_RADIUS || dist.z < SMOOTHING_RADIUS;
}


// Claude AI suggestions

float3 CalculateNearestBoundaryPoint(float3 particlePosition, float3 boxMin, float3 boxMax)
{
    return clamp(particlePosition, boxMin, boxMax);
}

float3 CalculateBoundaryForce(float3 particlePosition, float3 boxMin, float3 boxMax, float maxDistance, float boundaryStiffness)
{
    float3 nearestPoint = CalculateNearestBoundaryPoint(particlePosition, boxMin, boxMax);
    float3 r = particlePosition - nearestPoint;
    float distance = length(r);
    
    if (distance > maxDistance || distance < 1e-6)
        return float3(0, 0, 0);
    
    float force = boundaryStiffness * (1.0 - distance / maxDistance) * (1.0 - distance / maxDistance);
    return normalize(r) * force;
}





#endif