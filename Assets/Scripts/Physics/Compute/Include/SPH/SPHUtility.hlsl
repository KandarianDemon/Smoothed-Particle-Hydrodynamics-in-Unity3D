
#include "./structs.cginc"
static const int triangles[36] =


{ 
        
                            0,1,2,
                            3,0,2,
                            
                            4,5,6,
                            7,4,6,
                            
                            0,1,5,
                            4,0,5,
                            
                            3,2,6,
                            7,3,6,
                            
                            0,3,7,
                            4,0,7,
                            
                            1,2,6,
                            5,1,6};

struct Cube{

    float3 verts[8];
};




int3 IntCoord(float3 position)
{
    int x = floor(position.x);
    int y = floor(position.y);
    int z = floor(position.z);
  
    return int3(x,y,z);
}


bool CheckIntersection( float3 l, float3 l0, out float3 intersectionPoint, float3 HALF_BOUNDSIZE)
{
    // needs start point of line, point on the plane, and normal of the plane and direction of line

    // float3 cornerPoints[8] {float3(-HALF_BOUNDSIZE.x,-HALF_BOUNDSIZE.y,HALF_BOUNDSIZE.z) , float3(-HALF_BOUNDSIZE.x,HALF_BOUNDSIZE.y,HALF_BOUNDSIZE.z), float3(HALF_BOUNDSIZE.x,-HALF_BOUNDSIZE.y,HALF_BOUNDSIZE.z),float3(HALF_BOUNDSIZE.x,HALF_BOUNDSIZE.y,HALF_BOUNDSIZE.z),
    //                         float3(-HALF_BOUNDSIZE.x,-HALF_BOUNDSIZE.y,-HALF_BOUNDSIZE.z) , float3(-HALF_BOUNDSIZE.x,HALF_BOUNDSIZE.y,-HALF_BOUNDSIZE.z), float3(HALF_BOUNDSIZE.x,-HALF_BOUNDSIZE.y,-HALF_BOUNDSIZE.z),float3(-HALF_BOUNDSIZE.x,-HALF_BOUNDSIZE.y,-HALF_BOUNDSIZE.z)};

                          
  

        // if(dot(l,n) != 0 ){

        // float d = dot((p-l0),n)/dot(l,n);

        // intersectionPoint = l0 + d*l;
        // }
    



    return false;

   

}

Cube GetCube(float3 dimensions)
{
    Cube cube  = (Cube) 0;

    cube.verts[0] = -dimensions;
    cube.verts[1]  = float3(dimensions.x, -dimensions.y,-dimensions.z);
    cube.verts[2]  = float3(dimensions.x,dimensions.y,-dimensions.z);
    cube.verts[3]  = float3(-dimensions.x,dimensions.y,-dimensions.z);

    cube.verts[4]  = dimensions;
    cube.verts[5]  = float3(dimensions.x, -dimensions.y,dimensions.z);
    cube.verts[6]  = float3(dimensions.x,dimensions.y,dimensions.z);
    cube.verts[7]  = float3(-dimensions.x,dimensions.y,dimensions.z);

    return cube;
}






bool GetIntersectionPoint(float3 P, float3 D, float3 Q, float3 N, out float3 intersectionPoint)
{
    // Calculate the dot product of the line direction and the plane normal
    float denom = dot(D, N);
    

    // Check if the line is parallel to the plane
   
    if (denom < 0.0001)
    {
        return false;
    }
   
    // Calculate the parameter along the line where the intersection occurs
    float t = -dot( N,P-Q) / denom;

    



    // Calculate the intersection point
    intersectionPoint = P + t*D;

    

    return true;

    
}

float3 CalculateBoundaryForces(float3 position,float3 HALF_BOUNDSIZE)
{
    float3 boundaryForce = float3(0, 0, 0);

    // Apply forces near the boundaries to keep particles inside the domain
    float boundaryDamping = 0.1f;

    if (position.x < -HALF_BOUNDSIZE.x)
        boundaryForce.x += boundaryDamping * (-HALF_BOUNDSIZE.x - sign(position.x));
   

    if (position.y < -HALF_BOUNDSIZE.y) boundaryForce.y+= boundaryDamping * (-HALF_BOUNDSIZE.y - position.y);
  

    if (position.z < -HALF_BOUNDSIZE.z)
        boundaryForce.z += boundaryDamping * (-HALF_BOUNDSIZE.z - position.z);
  

    return boundaryForce;
}


float3 NormalizePosition(float3 position, float3 worldPos)
{

    return position + worldPos;
}


int Hash(float3 position, float numberOfItems)
{
    int3 cellID = IntCoord(position);

    uint hash = (cellID.x * 92837111) ^ (cellID.y * 689287499) ^ (cellID.z * 2839923481);
    int h = abs(hash) % numberOfItems;

    return h;
}

int3 GetDimensions(float3 bounds, float spacing)
{
    int3 dims = IntCoord(bounds/spacing);

    return dims;
}

int GridCellID(float3 position, int3 dimensions)
{


    int3 cellID = IntCoord(position);

     int u = cellID.x;
     int v = cellID.y;
     int w = cellID.z;

    if(u<0) u = 0;
    if(u > dimensions.x) u = dimensions.x;

    if(v<0) v = 0;
    if(v > dimensions.y-1) v = dimensions.y;

    if(w<0) w = 0;
    if(w > dimensions.z) w = dimensions.z;

    //return (floor(cellID.x * dimensions.y + cellID.y)*dimensions.z + cellID.z);
    return (floor(u * (dimensions.y) *(dimensions.z) + floor(v*(dimensions.z)) + w));


}

int GetCellHash(float3 position,float3 boundSize,float smoothingRadius,int numCells)
{

    // Function takes the original Position!
    // position         : Original Position as stored for the Particle
    // bound size       : The dimensions of the bounding box
    // smoothingRadius  : Smoothing radius or cell size
    // number of cells  : Nuber of Cells 


    // Normalize the position in relation to the bounding box and the cell size.
    float3 norm_pos = NormalizePosition(position,boundSize)/smoothingRadius;

    
    //Compute the Hash value for the residing cell

    //First get cell coordinates
    int xi = floor(norm_pos.x);
    int yi = floor(norm_pos.y);
    int zi = floor(norm_pos.z);


    // use cell coordinates to obtain Hash value
    uint hash = (xi * 92837111) ^ (yi * 689287499) ^ (zi * 2839923481);
    int h = abs(hash) % (numCells);

    return h;



}

NeighborIndexData GetNeighborCellIndices(float3 normalizedPosition, int3 dimensions, float radius, int numCells)
{   

    int index = 0;          //counter to access the indices in the 1D-array
    
    NeighborIndexData data; // struct containing the index array
    int indices[27];



    int3 fakePos = normalizedPosition*10000;
    int step = radius*10000;

     for(int x = fakePos.x-step; x <= fakePos.x+step ;x+=step)
    {
        for(int y = fakePos.y - step; y <= fakePos.y+step ;y+=step)
        {
             for(int z = fakePos.z - step; z <= fakePos.z+step ;z+=step)
                {
                
                    
                    float3 rePos = float3(x,y,z)/10000.0f;

                    int3 cellID = IntCoord(rePos/radius);
                //     if(i == 26) break;
                    int u = cellID.x;
                    int v = cellID.y;
                    int w = cellID.z;

                    u = (sign(u) == -1) ? 0:min(u,dimensions.x);
                    v = (sign(v) == -1) ? 0:min(v,dimensions.y);
                    w = (sign(w) == -1) ? 0:min(w,dimensions.z);


                    uint hash = (cellID.x * 92837111) ^ (cellID.y * 689287499) ^ (cellID.z * 2839923481);
                    int h = abs(hash) % numCells;
                    
                     indices[index] = h;
                    
                     //indices[i] = (floor(u * (dimensions.y) *(dimensions.z)) + floor(v*(dimensions.z)) + w);
                    index++;

                    if(index >= 27) {
                        data.indices = indices;
                        return data;}


                }
        
        }
    }

       
    
    data.indices = indices;
    return data;
}









