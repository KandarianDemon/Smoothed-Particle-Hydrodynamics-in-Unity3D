#ifndef RAYCAST_INCLUDED
#define RAYCAST_INCLUDED

struct Ray{

    float3  origin;
    float3  direction;
    float   length;
};



struct HitInfo
{

    bool    isHit;
    bool    isInside;
    double  distance;
    float   hitTime;                //collider or bound hit point in worldspace

    float3  hitPoint;  
    float3  barycentricCoordinate;   //hitpoint of the triangle in barycentricCoordinates
    float3  normal;                  // surface normal -> does that store the triangle normal or the collision normal?

    int     hitTriIndex;
    int     collisionType;           // will track wether the collision is an intersection, overlap or edge/vertex collision.


};

struct CollisionInfo {
    HitInfo closestHit;
    float3 closestPoint;
    float closestDistance;
    int closestTriIndex;
    bool collided;
};


Ray ConstructRay(float3 origin, float3 direction, float length)
{
    Ray ray;

    ray.origin      = origin;
    ray.direction   = direction;
    ray.length      = length;

    return ray;
}

float3 GetRayEndPoint(Ray ray)
{
    return ray.origin + ray.direction * ray.length;
}

Ray InvertRay(Ray ray)
{

    float3 end = GetRayEndPoint(ray);
     return ConstructRay(end, -ray.direction, ray.length);

}

HitInfo GetHitInfo(float distance = 1.#INF, float3 hitPoint = float3(1.#INF,1.#INF,1.#INF), float3 barycentricCoordinate = float3(1.#INF,1.#INF,1.#INF), float3 normal= float3(1.#INF,1.#INF,1.#INF), int hitTriIndex = -1, bool isHit = false,bool isInside = false,int collisionType = 0,float hitTime = 0.0f)
{

    HitInfo info;

    info.isHit = isHit;
    info.isInside = isInside;
    info.hitTime = hitTime;
    info.collisionType = collisionType;
    info.distance = distance;
    info.hitPoint = hitPoint;
    info.barycentricCoordinate = barycentricCoordinate;
    info.normal = normal;
    info.hitTriIndex = hitTriIndex;

    return info;
    

}

float CalculateHitTime(float3 velocity, float timestep, float t)
{
    float speed                 = length(velocity);
    float distanceTravelled     = speed * timestep;

    return timestep * (t/distanceTravelled);

}

bool isParallel(float3 direction, float3 normal)
{
    return dot(direction,normal)/(length(direction)*length(normal))>= 0;
}

bool isRayParallel(Ray ray, float3 normal)
{
    return isParallel(ray.direction,normal);
}



#endif