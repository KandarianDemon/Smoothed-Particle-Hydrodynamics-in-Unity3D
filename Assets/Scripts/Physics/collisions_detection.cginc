#ifndef COLLISIONS_DETECTION_INCLUDED
#define COLLISIONS_DETECTION_INCLUDED

#include "sph_buffers.cginc"
#include "sph_settings.cginc"

void ResolveCollisions(float3 position, float3 velocity,uint particleIndex)
{
	// Transform position/velocity to the local space of the bounding box (scale not included)
	float3 posLocal = mul(worldToLocal, float4(position, 1)).xyz;
	float3 velocityLocal = mul(worldToLocal, float4(velocity, 0)).xyz;

	// Calculate distance from box on each axis (negative values are inside box)
	float3 halfSize = 0.05;
    halfSize = mul(worldToLocal,float4(halfSize,1)).xyz;

	const float3 edgeDst = (HALF_BOUNDSIZE - PARTICLE_RADIUS+0.001f) - abs(position);

    float collisionDamping = 0.45f;
    float mass = ((4*pow(PARTICLE_RADIUS,3)*pi)/(NUMBER_OF_PARTICLES*3))*1000;

	// Resolve collisions
	if (edgeDst.x <= 0)
	{
		position.x = HALF_BOUNDSIZE.x * sign(position.x) + sign(position.x)*-1*PARTICLE_RADIUS ;
        
		velocity.x = -1 * collisionDamping;
	}
	if (edgeDst.y <= 0)
	{
		position.y = HALF_BOUNDSIZE.y * sign(position.y) + sign(position.y)*-1*PARTICLE_RADIUS;
		velocity.y *= -1 * collisionDamping;
	}
	if (edgeDst.z <= 0)
	{
		position.z = HALF_BOUNDSIZE.z * sign(position.z) + sign(position.z)*-1*PARTICLE_RADIUS;
		velocity.z *= -1 * collisionDamping;
	}

    

	// Transform resolved position/velocity back to world space
	PARTICLES[particleIndex].position = position;
	PARTICLES[particleIndex].velocity = velocity;

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

    return origin - Q;

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
       float3 R = reflect(velocity, -tri_normal);
       
       // To compute the reset position we need to find the length of vector contactPoint-resetPoint.
       // Find Orthogonal projection of r*n on v by applying: w = (dot(u,v)/ length(v)^2) * v;
       // v is the vector to be projected on
       // u is the vector to project
       
       float3 vector_v = PARTICLES[id.x].position + closest_t*length(velocity);
       float ism = 1/pow(length(vector_v),2);
       float3 projVector= dot(PARTICLE_RADIUS*TRIANGLES[collisionTriIndex].normal,vector_v) * ism + vector_v;

    //    position = PARTICLES[id.x].position + vector_v - projVector;
       
       //position = PARTICLES[id.x].position - tri_normal*PARTICLE_RADIUS;

       position = PARTICLES[id.x].position - tri_normal*PARTICLE_RADIUS*2.0f;





       velocity = R*0.12f;
       
        
       
        
        
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


    void ResolveBVHCollisions(uint3 id, RWStructuredBuffer<Particle> PARTICLES, float3 velocity, float3 position)
    {
            float hitDistance;
            int hitTriIndex;

            float3 direction = position - PARTICLES[id.x].position;

            

            
            
            if(TraverseBVH_LeafNodeCollision(PARTICLES[id.x].position, normalize(velocity),PARTICLES, hitDistance,hitTriIndex)) 
            {

                // if triangle is hit -> Reflect Vector!.
                // contact point
                // float3 contactPoint = PARTICLES[id.x].position + (hitDistance-PARTICLE_RADIUS) * normalize(velocity);
                // float3 normal = _Triangles[hitTriIndex].normal;
                // position = contactPoint;
                // velocity = reflect(velocity,-normal);
                PARTICLES[id.x].color = float3(1,0,0);
                

                
            

                // add proximity check


            }
            else{
                PARTICLES[id.x].color = float3(0,0,0);
            }
            
    }

#endif