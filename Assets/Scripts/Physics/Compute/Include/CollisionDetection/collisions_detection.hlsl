

#pragma target 5.0
#ifndef COLLISIONS_DETECTION_INCLUDED
// Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays

// Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays


#define COLLISIONS_DETECTION_INCLUDED

#include "./Include/SPH/sph_buffers.cginc"
#include "./Include/SPH/sph_settings.cginc"
#include "./boundary_handling.cginc"
#include "./raycast.cginc"
#include "./IntersectionTestLib.hlsl"
#include "./BVH.cginc"

// Pseudo-random number generation (example using a simple hash function)

uint MurmurHash(uint seed) {
    const uint m = 0xc6a4a793; // Magic constant
    const int r = 15;          // Mix bits
    const uint n = 13;         // Final mix (integer truncation)

    uint h = seed ^ m; // Initialize hash to seed

    // Mix 4-byte chunks of the seed
    for (int i = 0; i < 4; ++i) {
        uint k = (h & 0xffffffff) << r | (h >> (32 - r)); // Bitwise rotation
        h = h ^ k; // XOR mix
        h = h * m; // Addition
        h = h ^ (k >> 16); // Final mix
    }

    // Handle the last few bytes of the input
    const uint c = 0xc5a926f7; // Constant
    h = h ^ c; // XOR mix
    h = h * m; // Addition
    h = h ^ (c >> 16); // Final mix

    // Truncate to 32 bits
    h = h >> n;

    return h;
}
uint Hash(uint seed) {
    return uint(MurmurHash(seed) % 4294967295u);
}

// Normalizes a vector
float3 Normalize(float3 v) {
    return v / length(v);
}

// Rotates a vector by a given angle around an axis
float3 RotateVector(float3 vec, float angle) {
    float c = cos(angle);
    float s = sin(angle);
    float3x3 m = float3x3(
        c + (1 - c) * vec.x * vec.x,
        (1 - c) * vec.x * vec.y - s * vec.z,
        (1 - c) * vec.x * vec.z + s * vec.y,
        (1 - c) * vec.y * vec.x + s * vec.z,
        c + (1 - c) * vec.y * vec.y,
        (1 - c) * vec.y * vec.z - s * vec.x,
        (1 - c) * vec.z * vec.x + s * vec.y,
        (1 - c) * vec.z * vec.y - s * vec.x,
        c + (1 - c) * vec.z * vec.z
    );
    return mul(m, vec);
}

float3 PerturbDirection(float3 direction, float maxPerturbationAngle) {
    // Generate a random orthogonal vector
    float3 orthogonal = Normalize(cross(direction, float3(0, 1, 0)));
    
    // Generate a random angle within [-maxPerturbationAngle, maxPerturbationAngle]
    float randomAngle = ((Hash((uint)direction.x) % 65536u) - 32768u) / 32767.0f * 2.0f * maxPerturbationAngle - maxPerturbationAngle;
    
    // Rotate the orthogonal vector by the random angle
    float3 rotatedOrthogonal = RotateVector(orthogonal, randomAngle);
    
    // Project the direction onto the rotated orthogonal vector
    float projection = dot(direction, rotatedOrthogonal);
    
    // Calculate the perturbation vector
    float3 perturbation = Normalize(rotatedOrthogonal) * projection;
    
    // Return the original direction plus the perturbation
    return Normalize(direction + perturbation);
}




void ResolveCollisions(float3 position, float3 velocity,uint particleIndex)
{
	// Transform position/velocity to the local space of the bounding box (scale not included)
	float3 posLocal = mul(worldToLocal, float4(position, 1)).xyz;
	float3 velocityLocal = mul(worldToLocal, float4(velocity, 0)).xyz;

	// Calculate distance from box on each axis (negative values are inside box)
	float3 halfSize = HALF_BOUNDSIZE/length(HALF_BOUNDSIZE);
    halfSize = mul(worldToLocal,float4(halfSize,1)).xyz;

	const float3 edgeDst = (HALF_BOUNDSIZE - PARTICLE_RADIUS) - abs(position);
    //const float3 edgeDst = halfSize - abs(posLocal);

    float collisionDamping = 0.15f;
    float mass = ((4*pow(PARTICLE_RADIUS,3)*pi)/(NUMBER_OF_PARTICLES*3))*1000;

	// Resolve collisions
	if (edgeDst.x <= 0)
	{
		position.x = HALF_BOUNDSIZE.x * sign(position.x) + sign(position.x)*-1*PARTICLE_RADIUS ;
        
		velocity.x *= -1 * collisionDamping;

       

        

       
	}
	if (edgeDst.y <= 0)
	{
		position.y = HALF_BOUNDSIZE.y * sign(position.y) + sign(position.y)*-1*PARTICLE_RADIUS;
		velocity.y *= -1 * collisionDamping;

      


       
	}
	if (edgeDst.z <= 0)
	{

      
		position.z = HALF_BOUNDSIZE.z * sign(position.z) + sign(position.z)*-1*PARTICLE_RADIUS;
		velocity.z *= -1 *collisionDamping;

         

	}
    
   

  

    // if(IsWallHit(edgeDst))
    // {
    //     PARTICLES[particleIndex].color = float3(0,1,0);

    //     // Loop over virtual Particles for density estimation;


    // }
    // else{
    //     PARTICLES[particleIndex].color = float3(0,0,0);
    // }

    

	// Transform resolved position/velocity back to world space
	PARTICLES[particleIndex].position =  position;
	PARTICLES[particleIndex].velocity =  velocity;

}


float LennardJonesPotential(float r, float epsilon, float sigma) {
    float sr = r / sigma;
    return 4.0 * epsilon * (pow(sr, -12) - pow(sr, -6));
}

float3 LennardJonesForce(float3 r, float epsilon, float sigma, float mass) {
    float rMag = length(r);
    float sr = rMag / sigma;
    float potentialEnergy = LennardJonesPotential(rMag, epsilon, sigma);
    // Correct the force magnitude calculation
    //float forceMagnitude = -24.0 * epsilon * ((pow(sigma / rMag, 12) - pow(sigma / rMag, 6)) * (-sigma / (rMag * rMag)));
    return normalize(r) * potentialEnergy*mass;
}

float3 AddBoundaryRepulsion(float3 position,float epsilon, float sigma,float mass)
{
    // Calculate distances to each boundary

    const float3 edgeDst = (HALF_BOUNDSIZE - PARTICLE_RADIUS+0.01f) - abs(position);
    float polarity = sign(position);

    float3 repulsionForce;

    repulsionForce.x = LennardJonesForce(float3(edgeDst.x,0,0), epsilon,sigma,mass) * polarity;
    repulsionForce.y = LennardJonesForce(float3(0,edgeDst.y,0), epsilon,sigma,mass) * polarity;
    repulsionForce.z = LennardJonesForce(float3(0,0,edgeDst.z), epsilon,sigma,mass) * polarity;

    return repulsionForce;
    
}





float3 Barycentric2Euclidian(float4 tuv, Triangle tri)
{
    float3 A = PARTICLES[tri.a].position;
    float3 B = PARTICLES[tri.b].position;
    float3 C = PARTICLES[tri.c].position;

    return A*tuv.x + B*tuv.y + C*tuv.z;
}





float3 FindClosestPointOnPlane(float3 origin, Triangle tri)
{
    float3 ap = origin - PARTICLES[tri.b].position;

    float d = dot(ap,tri.normal);

    

    // Find projection:

    float inv_squared_normal_magnitude = 1/(pow(length(ap),2)+0.000001f);

    float3 projection = inv_squared_normal_magnitude*d*tri.normal;
    float3 Q = PARTICLES[tri.a].position + projection;

    return Q;

    // misses information regarding if the point lies within the triangle or not

}

 float GetDistanceToPlane(float3 origin, Triangle tri)
 {

    float3 P = PARTICLES[tri.a].position;
    float3 n = tri.normal;

    float3 v = origin - P;
    float dot_vn = dot(v,n);
    float inv_squ_mag = 1/pow(length(n),2);

    return dot_vn*inv_squ_mag*n;
 }

bool IntersectTri(float3 origin, float3 dir, Triangle tri, inout float4 info)
{


    
    
    float3 direction = dir;
    
    // Implement Möller-Trumbore-Intersection-Algorithm
    // First: if ray is parallel to tri, skip

    
    float3 edge1 = PARTICLES[tri.b].position - PARTICLES[tri.a].position;
    float3 edge2 = PARTICLES[tri.c].position - PARTICLES[tri.a].position;

    float3 normal = normalize(cross(edge1,edge2));

    


    // float3 edge1 = -HALF_BOUNDSIZE - HALF_BOUNDSIZE;
    // float edge2 = -HALF_BOUNDSIZE - float3(HALF_BOUNDSIZE.x,HALF_BOUNDSIZE.y,0);

    float3 pVec = cross(direction,edge2);
    float det = dot(edge1,pVec);

    // check if ray is parallel to plane
    if(abs(det)<0.00000001f ){

        return false ;
    }


   

    // Find u and determine wether it is inside the triangle
    float inv_det = 1.0f/det;
    float3 tVec = origin-PARTICLES[tri.a].position;


    float u = inv_det * dot(tVec,pVec);

    if(u < 0.0f || u>1.0f)
    {
        return false;
    }

    

    //find v and determine wether it is inside the triangle

    float3 qVec = cross(tVec,edge1);
    float v = inv_det*dot(direction,qVec);

    if(v < 0.0f || v > 1.0f)
    {
        return false;
    }

    if(u+v > 1.0f)
    {return false;}

    float t = inv_det*dot(edge2,qVec);

    if( t + v + u > 1.0f)
    {
        return false;
    }

    // t *= inv_det;
    // u *= inv_det;
    // v *= inv_det;

   
    info.x = (1-u-v); 
    info.y = u;
    info.z = v;

    // return the world space point instead
   
    

    return true, t;

    
}

bool isPointOnEdge(float4 info)
    {

        float tolerance = 1e-10;
        bool on_edge_AB = (info.x < tolerance) && (info.y >= 0) && (info.y <= 1) && (info.z >= 0) && (info.z <= 1);
        bool on_edge_BC = (info.y < tolerance) && (info.z >= 0) && (info.z <= 1) && (info.x >= 0) && (info.x <= 1);
        bool on_edge_CA = (info.z < tolerance) && (info.x >= 0) && (info.x <= 1) && (info.y >= 0) && (info.y <= 1);
        return on_edge_AB ;


    }

bool PointInsideTriangle(float3 p, Triangle tri)
{

    float sum = 0;

    float3 ap = tri.a-p;
    float3 bp = tri.b - p;
    float3 cp = tri.c - p;
    
    float angle_a = 1/(cos(dot(ap,bp)/(length(ap)*length(bp))))/pi * 180;
    float angle_b = 1/(cos(dot(bp,cp)/(length(bp)*length(cp))))/ pi * 180;
    float angle_c = 1/(cos(dot(cp,ap)/(length(cp)*length(ap))))/ pi * 180;

    if(angle_a + angle_b + angle_c == 360.0f) {return true;}
    else{return false;}




}

float4 Intersect(Triangle tri, float3 direction, float3 startPos)
{
    // Get parametric form of the plane
    
    float3 n = tri.normal;
    
    float3 a = tri.a;

    float d = -(n.x*a.x + n.y*a.y + n.z*a.z);
    float3 dir = direction/length(direction);
    float t = -(n.x*startPos.x + n.y*startPos.y + n.z * startPos.z + d)/ (n.x*dir.x + n.y*dir.y + n.z*dir.z);


    float3 P = startPos + t*dir;

    return float4(P,t);


    // get line equation

  

    // Find intersection

}

float3 BoundaryReplusionForce(uint particleIndex, float multiplier)
{
	// Transform position/velocity to the local space of the bounding box (scale not included)
	float3 posLocal = mul(worldToLocal, float4(PARTICLES[particleIndex].position, 1)).xyz;
	float3 velocityLocal = mul(worldToLocal, float4(PARTICLES[particleIndex].velocity, 0)).xyz;

	// Calculate distance from box on each axis (negative values are inside box)
	const float3 halfSize = 0.5;
	const float3 edgeDst = halfSize - abs(posLocal);
    const float dist = sqrt(dot(edgeDst,edgeDst));

    const float3 dir = normalize(edgeDst);

    float3 forceVector = float3(dir.x * sign(posLocal.x), dir.y * sign(posLocal.y),dir.z * sign(posLocal.z)); 

    if(abs(edgeDst.x<= 0.003) || abs(edgeDst.y<= 0.003) ||abs(edgeDst.z<= 0.003 )) {return multiplier *normalize(edgeDst)*1.0f;}

    else 
    
    {
        return float3(0,0,0);
    }
    

	

}



void MembraneCollisions_Felix(uint3 id,inout float3 position,inout float3 velocity, float timestep)
{


    if(TRIANGLES.Length <= 1) return;


    float closest_t = 1000000.0f;
     int collisionTriIndex = -1;

     int projectionTriIndex = -1;
     float closest_projection = 1000000.0f;

     float3 direction = PARTICLES[id.x].position - position;
     double directionLength = length(direction);
     
     

     // Vector3 contains collision info: Triangle index, t, and bool
     float4 info = float4(100,100,100,-1);
     float4 overlapInfo = info;
     
     double contactDistance;
     float overlapDistance = 100.0f;
     PARTICLES[id.x].color = float3(0,0,0);

    //   if(directionLength > PARTICLE_RADIUS)
    //  {
    //     PARTICLES[id.x].color = float3(1,0,1);
    //  }

    bool overlapPointIsInsideTriangle = false;


   

    
    // Find Collision
      for(int i = 0;i<TRIANGLES.Length;i++)
     {
       
        Triangle tri = TRIANGLES[i];

       

        // Find Collisions during movement
        // Find Collisions by overlapping particles/Polygon

        
        // float3 overlap = FindClosestPointOnPlane(PARTICLES[id.x].position,tri);
        overlapPointIsInsideTriangle = IntersectTri(PARTICLES[id.x].position, tri.normal,tri,overlapInfo);
        float3 closestPoint = Barycentric2Euclidian(overlapInfo,tri);

        float3 o2cp = PARTICLES[id.x].position - closestPoint;
        //float od = GetDistanceToPlane(PARTICLES[id.x].position,tri);
        float od = length(o2cp);

        bool onEdge = isPointOnEdge(info);
       

        
        
       
       

        
        bool intersect = IntersectTri(PARTICLES[id.x].position,direction, tri, info);
        



        
       
        bool overlap_detected = ( overlapPointIsInsideTriangle && od <= PARTICLE_RADIUS) ? true:false;
       
    
        //PARTICLES[id.x].color =(overlapPointIsInsideTriangle) ? float3(1,1,0):float3(0,0,1);

        if(intersect || overlap_detected)
        {


            info = (overlap_detected && intersect) ? overlapInfo:info;
            PARTICLES[id.x].color = info.xyz;
            
           //PARTICLES[id.x].color = (onEdge)? float3(1,0,1):PARTICLES[id.x].color;
            
            float3 A,B,C;

            A = PARTICLES[tri.a].position;
            B = PARTICLES[tri.b].position;
            C = PARTICLES[tri.c].position;

            float3 contactPoint = info.x * A + info.y*B + info.z*C;
          
            float3 overlapVector = contactPoint - PARTICLES[id.x].position;
            contactDistance = (length(overlapVector) > od) ? length(overlapVector):od;
            float3 contactVector = PARTICLES[id.x].position - contactPoint;

            

         


            if(contactDistance < length(velocity*100*timestep) || contactDistance <= PARTICLE_RADIUS)
            {
                
                 closest_t = (contactDistance < closest_t) ? contactDistance:closest_t;
                 
                 info.w = 1;
                 collisionTriIndex = i;
                
                 
                 
                 
                 
            }

           
        }
 
     }



    if(PARTICLES[id.x]._static == 1)
    {
        PARTICLES[id.x].color = float3(0,1,0);
    }
     // Resolve the collision


    //   if( info.x < length(direction) && info.z >=1)
    //     {
            
    //             PARTICLES[id.x].color = float3(1,0,0);

    

    if(info.w > 0)
    {   
        //PARTICLES[id.x].color = float3(1,1,0);
        PARTICLES[id.x].color = float3(1,0,0);

       float3 A,B,C;
       int a,b,c;

       a = TRIANGLES[collisionTriIndex].a;
       b = TRIANGLES[collisionTriIndex].b;
       c = TRIANGLES[collisionTriIndex].c;

       A = PARTICLES[a].position;
       B = PARTICLES[b].position;
       C = PARTICLES[c].position;

      
       //bool intersect = IntersectTri(PARTICLES[id.x].position, direction,TRIANGLES[collisionTriIndex],info);
        
        

        float3 contactPoint = info.x * A + info.y*B + info.z*C;
       
      
        float tri_normal = TRIANGLES[collisionTriIndex].normal;
       float3 R = reflect(velocity, tri_normal);
       
       // To compute the reset position we need to find the length of vector contactPoint-resetPoint.
       // Find Orthogonal projection of r*n on v by applying: w = (dot(u,v)/ length(v)^2) * v;
       // v is the vector to be projected on
       // u is the vector to project
       
       float3 vector_v = PARTICLES[id.x].position + closest_t*length(velocity);
       float ism = 1/pow(length(vector_v),2);
       float3 projVector= dot(PARTICLE_RADIUS*TRIANGLES[collisionTriIndex].normal,vector_v) * ism + vector_v;

    //    position = PARTICLES[id.x].position + vector_v - projVector;
       
       //position = PARTICLES[id.x].position - tri_normal*PARTICLE_RADIUS;

         position = position - tri_normal*PARTICLE_RADIUS*3.0f;
        //position = contactPoint + tri_normal*PARTICLE_RADIUS;





       velocity = R*0.5f;
       
        
       
        
        
    }

//     if(info.w > 0)
// {   
//     float3 A,B,C;
//     int a,b,c;

//     a = TRIANGLES[collisionTriIndex].a;
//     b = TRIANGLES[collisionTriIndex].b;
//     c = TRIANGLES[collisionTriIndex].c;

//     A = PARTICLES[a].position;
//     B = PARTICLES[b].position;
//     C = PARTICLES[c].position;

//     float3 contactPoint = info.x * A + info.y*B + info.z*C;
//     float tri_normal = TRIANGLES[collisionTriIndex].normal;
    
//     // Adjust the position correction factor to reduce turbulence
//     float3 displacement = contactPoint - PARTICLES[id.x].position;
//     position += displacement; // Reduced from 0.8 to 0.7

//     // Review the velocity update logic to ensure it aligns with realistic physics
//     float3 incident_velocity = normalize(velocity);
//     float cos_theta = dot(incident_velocity, tri_normal);
//     float3 reflected_velocity = reflect(incident_velocity, tri_normal);
//     // Adjust the scaling factor based on material properties or experimentally
//     velocity = reflected_velocity*length(velocity) * 0.05f; // Adjusted from 0.62
// }
   
}



bool RayIntersectsNode(Ray ray, BVHNode node, float threshold, out float intersectionDistance)
{

    if(IsRayOriginInsideNode(ray,node)) return false;
    float3 invDir = 1.0 / ray.direction;
    float3 t0 = (node.min - ray.origin) * invDir;
    float3 t1 = (node.max - ray.origin) * invDir;
    float3 tmin = min(t0, t1);
    float3 tmax = max(t0, t1);

    float maxComponent = max(max(tmin.x, tmin.y), tmin.z);
    float minComponent = min(min(tmax.x, tmax.y), tmax.z);

    intersectionDistance = max(0, maxComponent);

    return maxComponent <= minComponent && minComponent >= 0 && intersectionDistance <= threshold;
}

bool ResolveMeshCollisions(Ray ray, in out HitInfo info)
{

    // |                                                           |
    // ===================== Broad Phase ==========================
    
    // Traverse the BVH tree to find:     Nodes containing the current paricle 
    //                                    Nodes intersected by the current particle
    
    int containedIDX, intersectedIDX;
    bool hit = BVH_BroadPhase(ray, containedIDX,intersectedIDX);

    





    

    //int containing_nodeIndex = ResolveBVHCollisions(id,PARTICLES,velocity,PARTICLES[id.x].position,ray);
    //int intersecting_nodeIndex = ResolveBVHCollisions(id,PARTICLES,velocity,PARTICLES[id.x].position,ray,false);

    // ==================== Narrow Phase ==========================

    //  if nodes have been found, check geometry for overlaps and intersection

    if(hit)
    {

        BVHNode node = _BVHNodes[containedIDX];
        int triangleIndices[4] = {node.triangleIndex_A, node.triangleIndex_B, node.triangleIndex_C, node.triangleIndex_D};

        for (int i = 0; i<4; i++)
        {
            Triangle tri = TRIANGLES[triangleIndices[i]];

            Plane plane = ConstructPlane(PARTICLES[tri.a].position, tri.normal);
            float t = DistPointPlane(ray.origin,plane);

            if(t < PARTICLE_RADIUS && IsPointInsideTriangle(ray.origin,tri)) 
            
            {
                
                
                info.isHit      = hit;
                info.distance   = t;
                info.hitPoint   = ray.origin + t*(tri.normal);
                info.normal     = tri.normal;
                return hit;
            }


        }
        // FIND THE TRIANGLE INTERSECTIONS
        // CollisionInfo info                  = FindTriangleCollision(containedIDX, ray, PARTICLE_RADIUS);
        // HitInfo hitInfo                     = info.closestHit;

        // Determine if there is an Overlap, and intersection or if any of the edgecases apply.
        // Overlap information should be contained in the collision info
        // intersection information in the hitInfo.

        // edge cases: average out the normals if particle gets close to an edge or vertex.
        // means: I need to store edge information as well.


        // in this step we need to decide if there is a collision we need to resolve and if so, which is the closest one.

        // bool collision = (info.collided || hitInfo.isHit) ? : true:false;


        // if(collision)
        // {

        //  // im quite sure that there are errors in generating the collision info.


        //  // first check which one is true.
        //  // simple distance check. 
        //  // distance to intersection
        //  // distance to closestpoint on triangle        !!!! Is the point actually inside the triangle??



        // }


    }

    return false;



    // ================ Collision Resolution ======================
    
    
    //  if collision was detected. Resolve it!









}

bool BruteForceCollisions(uint3 id, RWStructuredBuffer<Particle> PARTICLES, float3 velocity, float3 position,float timestep, in out HitInfo info)
{

    for(int i = 0; i<TRIANGLES.Length;i++)
    {
        Triangle tri    = TRIANGLES[i];
        Ray ray         = ConstructRay(PARTICLES[id.x].position,normalize(velocity),length(velocity)*timestep);
        Plane plane     = ConstructPlane(PARTICLES[tri.a].position, tri.normal);

        double t        = DistPointPlane(ray.origin,plane);
        float3 hitPoint = ray.origin - t*tri.normal;

        if(t <= PARTICLE_RADIUS && IsPointInsideTriangle(ray.origin,tri)) 
        
        
        {
            
            info.isHit      = true;
            info.distance   = t;
            info.hitPoint   = hitPoint;
            info.normal     = tri.normal;
            return true;

        }



    }

    return false;
}


bool BVHGeometryCollisions(uint3 id, RWStructuredBuffer<Particle> PARTICLES, int nodeIndex, float3 velocity, float3 position, float timestep, in out HitInfo info)
{

    BVHNode node = _BVHNodes[nodeIndex];

    int triangleIndices[4] = {node.triangleIndex_A, node.triangleIndex_B, node.triangleIndex_C,node.triangleIndex_D};

     for(int i = 0; i<triangleIndices.Length;i++)
    {

        int idx         = triangleIndices[i];

        if(idx < 0) return false;



        Triangle tri    = TRIANGLES[idx];
        Ray ray         = ConstructRay(PARTICLES[id.x].position,normalize(velocity),length(velocity)*timestep);
        Plane plane     = ConstructPlane(PARTICLES[tri.a].position, tri.normal);

        double t        = DistPointPlane(ray.origin,plane);
        float3 hitPoint = ray.origin - t*tri.normal;

        if(t <= PARTICLE_RADIUS && IsPointInsideTriangle(ray.origin,tri)) 
        
        
        {
            
            info.isHit      = true;
            info.distance   = t;
            info.hitPoint   = hitPoint;
            info.normal     = tri.normal;
            return true;

        }



    }

    return false;




}



bool CheckBVHCollisions(Ray ray, out int nodeIndex)
{

    int max_depth = 64;
    nodeIndex = 0;
    
    int counter = 0;

    // first check if both start and endpoint are in the node

    while(true)
    {

        BVHNode node = _BVHNodes[nodeIndex];



        // Check if node is leaf node
        if(node.leftChild == -1 || node.rightChild == -1 ) 
        {
        return true;
        }


        // Get Children

        int indexLeft       = node.leftChild;
        int indexRight      = node.rightChild;

        BVHNode left        = _BVHNodes[node.leftChild];
        BVHNode right       = _BVHNodes[node.rightChild];

        bool insideThisNode = IsRayOriginInsideNode(ray,node);
        bool insideLeft     = IsRayOriginInsideNode(ray,left);
        bool insideRight    = IsRayOriginInsideNode(ray,right);

        
        if(!insideThisNode) return false;


        if(insideLeft)   nodeIndex = node.leftChild;
        if(insideRight)  nodeIndex = node.rightChild;



        counter++;

        if(counter >= max_depth)
        {
            return false;
        }
        


    }

    

    return false;



}

 

 int ResolveBVHCollisions(uint3 id, RWStructuredBuffer<Particle> PARTICLES, float3 velocity, float3 position, Ray ray, bool containing = true)
{
    // mode determines wether cotaining nodes or intersecting nodes are being returned
    int nodeIndex = 0;
    int maxDepth = 64; // Prevent infinite loops
    float intersectionThreshold = 2.0f*PARTICLE_RADIUS; // Adjust this value as needed

    
    int counter = 0;
    PARTICLES[id.x].color = float3(0,0,0);
    
    while(true)
    {
        BVHNode currentNode = _BVHNodes[nodeIndex];
       
        

        // Check if the particle is inside the node or if its trajectory intersects the node within the threshold
        bool isInside = IsRayOriginInsideNode(ray, currentNode);

       
        float intersectionDistance;
        bool intersects = RayIntersectsNode(ray, currentNode, intersectionThreshold, intersectionDistance);

        bool collision = (containing) ? IsRayOriginInsideNode(ray,currentNode):RayIntersectsNode(ray, currentNode, intersectionThreshold, intersectionDistance);
        if(!collision)
        {
            PARTICLES[id.x].color = float3(0,0,0); // Black if outside and not intersecting within threshold
            return -1;
        }


        


            
            
        //     return nodeIndex;
        // }
        if(checkLeafNodeAndReturnIndex(currentNode)) return nodeIndex;

        PARTICLES[id.x].color = float3(0,0,0);

        BVHNode left = _BVHNodes[currentNode.leftChild];
        BVHNode right = _BVHNodes[currentNode.rightChild];

        float childIntersectionDistance;

        bool intersectLeft = (containing) ? IsRayOriginInsideNode(ray, left):RayIntersectsNode(ray, left, intersectionThreshold,  childIntersectionDistance);
        bool intersectRight = (containing) ? IsRayOriginInsideNode(ray, right):RayIntersectsNode(ray, right, intersectionThreshold, childIntersectionDistance);

        if(intersectLeft) 
        {
            nodeIndex = currentNode.leftChild;
        }
        else if(intersectRight)
        {
            nodeIndex = currentNode.rightChild;
        }
        
        counter++;
        

        if(counter >= maxDepth)
        {
            //PARTICLES[id.x].color = float3(1,0,1); // Green if max depth reached
            return-1;
        }
    }

    
}
    // Color the particle based on whether it's inside a leaf node

  HitInfo RayTriangleIntersection(Ray ray, Triangle tri, int triIndex, out bool hit)
{
    HitInfo hitInfo;
    
    // Initialize hitInfo with default "no hit" values
    hitInfo.distance = 1.0e30; // Use a very large number
    hitInfo.hitPoint = float3(0, 0, 0);
    hitInfo.barycentricCoordinate = float3(0, 0, 0);
    hitInfo.normal = float3(0, 1, 0); // Default normal pointing up
    hitInfo.hitTriIndex = -1;
    hitInfo.isHit = false;

    hit = false; // Initialize hit to false

    // ========== Trumbore-Moeller-Algorithm =============
    float3 v0 = PARTICLES[tri.a].position;
    float3 v1 = PARTICLES[tri.b].position;
    float3 v2 = PARTICLES[tri.c].position;

    float3 edge1 = v1 - v0;
    float3 edge2 = v2 - v0;
    float3 h = cross(ray.direction, edge2);
    float a = dot(edge1, h);

    // If a is too close to 0, the ray is parallel to the triangle
    if (abs(a) < EPSILON)
        return hitInfo;

    float f = 1.0 / a;
    float3 s = ray.origin - v0;
    float u = f * dot(s, h);

    // If u is not between 0 and 1, the intersection point is outside the triangle
    if (u < 0.0 || u > 1.0)
        return hitInfo;

    float3 q = cross(s, edge1);
    float v = f * dot(ray.direction, q);

    // If v is negative or u + v is greater than 1, the intersection point is outside the triangle
    if (v < 0.0 || u + v > 1.0)
        return hitInfo;

    float t = f * dot(edge2, q);

    // If t is negative, the triangle is behind the ray
    // If t is greater than the ray length, the intersection is beyond the ray's reach
    if (t < EPSILON || t > ray.length)
        return hitInfo;

    // ================ End Trumbore-Moeller ==================

    // Intersection found, update hitInfo
    hit = true;
    hitInfo.isHit = hit;
    hitInfo.distance = t;
    hitInfo.hitPoint = ray.origin + t * ray.direction;
    hitInfo.barycentricCoordinate = float3(1 - u - v, u, v);
    hitInfo.normal = tri.normal;
    hitInfo.hitTriIndex = triIndex; // Assuming Triangle has an index field

    return hitInfo;
}

float3 ClosestPointOnTriangle(float3 p, Triangle tri)
{

    float3 tri_v0 = PARTICLES[tri.a].position;
    float3 tri_v1 = PARTICLES[tri.b].position;
    float3 tri_v2 = PARTICLES[tri.c].position;


    float3 edge0 = tri_v1 - tri_v0;
    float3 edge1 = tri_v2 - tri_v0;
    float3 v0 = tri_v0 - p;

    float a = dot(edge0, edge0);
    float b = dot(edge0, edge1);
    float c = dot(edge1, edge1);
    float d = dot(edge0, v0);
    float e = dot(edge1, v0);

    float det = a*c - b*b;
    float s = b*e - c*d;
    float t = b*d - a*e;

    if (s + t < det)
    {
        if (s < 0.f)
        {
            if (t < 0.f)
            {
                if (d < 0.f)
                {
                    s = clamp(-d / a, 0.f, 1.f);
                    t = 0.f;
                }
                else
                {
                    s = 0.f;
                    t = clamp(-e / c, 0.f, 1.f);
                }
            }
            else
            {
                s = 0.f;
                t = clamp(-e / c, 0.f, 1.f);
            }
        }
        else if (t < 0.f)
        {
            s = clamp(-d / a, 0.f, 1.f);
            t = 0.f;
        }
        else
        {
            float invDet = 1.f / det;
            s *= invDet;
            t *= invDet;
        }
    }
    else
    {
        if (s < 0.f)
        {
            float tmp0 = b + d;
            float tmp1 = c + e;
            if (tmp1 > tmp0)
            {
                float numer = tmp1 - tmp0;
                float denom = a - 2*b + c;
                s = clamp(numer / denom, 0.f, 1.f);
                t = 1.f - s;
            }
            else
            {
                t = clamp(-e / c, 0.f, 1.f);
                s = 0.f;
            }
        }
        else if (t < 0.f)
        {
            if (a + d > b + e)
            {
                float numer = c + e - b - d;
                float denom = a - 2*b + c;
                s = clamp(numer / denom, 0.f, 1.f);
                t = 1.f - s;
            }
            else
            {
                s = clamp(-e / c, 0.f, 1.f);
                t = 0.f;
            }
        }
        else
        {
            float numer = c + e - b - d;
            float denom = a - 2*b + c;
            s = clamp(numer / denom, 0.f, 1.f);
            t = 1.f - s;
        }
    }

    return tri_v0 + s * edge0 + t * edge1;
}

float3 rand3(float3 p)
{
    p = float3(dot(p, float3(127.1, 311.7, 74.7)),
               dot(p, float3(269.5, 183.3, 246.1)),
               dot(p, float3(113.5, 271.9, 124.6)));

    return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
}
    



CollisionInfo FindTriangleCollision(int nodeIndex, Ray ray, float particleRadius)
{
    CollisionInfo result;
    result.closestHit.distance = 1.0e30;
    result.closestHit.hitTriIndex = -1;
    result.closestPoint = float3(0, 0, 0);
    result.closestDistance = 1.0e30;
    result.closestTriIndex = -1;
    result.collided = false;
    
    if(nodeIndex < 0) return result;

    BVHNode node = _BVHNodes[nodeIndex];
    
    int triangleIndices[4] = {node.triangleIndex_A, node.triangleIndex_B, node.triangleIndex_C, node.triangleIndex_D};
    
    for(int i = 0; i < 4; i++)
    {
        if(triangleIndices[i] >= 0)
        {
            Triangle tri = TRIANGLES[triangleIndices[i]];
            
            // Check for ray-triangle intersection
            bool hitIntersection;
            HitInfo intersectionHit = RayTriangleIntersection(ray, tri, triangleIndices[i], hitIntersection);
            
            if(hitIntersection && intersectionHit.distance < result.closestHit.distance)
            {
                result.closestHit = intersectionHit;
                result.collided = true;
                
            }

            // Check for closest point on triangle
            float3 closestPoint = ClosestPointOnTriangle(ray.origin, tri);
            float distanceToTriangle = length(closestPoint - ray.origin);
            
            if(distanceToTriangle < result.closestDistance)
            {
                result.closestPoint = closestPoint;
                result.closestDistance = distanceToTriangle;
                result.closestTriIndex = triangleIndices[i];
                
                if(distanceToTriangle < particleRadius)
                {
                    result.collided = true;
                }
            }
        }
    }

    return result;
}

bool IsPointInsideMesh(float3 position, int rootNodeIndex)
{
    int intersectionCount = 0;
    Ray ray = ConstructRay(position, float3(1,0,0),10000.0f);
    //Cast ray in any fixed direction
    
    // Traverse the BVH and count intersections
    int stack[64];
    int stackPtr = 0;
    stack[stackPtr++] = rootNodeIndex;
    
    while (stackPtr > 0)
    {
        int nodeIndex = stack[--stackPtr];
        BVHNode node = _BVHNodes[nodeIndex];
        
        if (node.triangleIndex_A >= 0)
        {
            // Leaf node, check triangles
            int triangleIndices[4] = {node.triangleIndex_A, node.triangleIndex_B, node.triangleIndex_C, node.triangleIndex_D};
            for (int i = 0; i < 4; i++)
            {
                if (triangleIndices[i] >= 0)
                {
                    Triangle tri = TRIANGLES[triangleIndices[i]];
                    bool hit;
                    HitInfo hitInfo = RayTriangleIntersection(ray, tri, triangleIndices[i], hit);
                    if (hit)
                    {
                        intersectionCount++;
                    }
                }
            }
        }
        else
        {
            // Internal node, traverse children
            if (node.leftChild >= 0) stack[stackPtr++] = node.leftChild;
            if (node.rightChild >= 0) stack[stackPtr++] = node.rightChild;
        }
    }
    
    return (intersectionCount % 2) == 1;
}
        


   

#endif