struct Ray {

float3 origin;
float3 direction;

}


Ray ConstructRay(float3 origin, float3 direction){

    Ray ray;
    ray.origin = origin;
    ray.direction = direction;

    return ray;
}

bool RayIntersectPlane()
{
    return false;
}

