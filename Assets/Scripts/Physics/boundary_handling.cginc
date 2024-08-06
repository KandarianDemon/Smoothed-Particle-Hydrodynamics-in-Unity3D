#ifndef BOUNDARY_HANDLING_INCLUDED



#define BOUNDARY_HANDLING_INCLUDED

#include "sph_settings.cginc"
#include "structs.cginc"


int Get_MaxVirtualParticles(float radius, float h){

    float box_volume = pow(2*h,2)*h;
    float particle_volume = 4/3*PI*pow(radius,3);

    return round(box_volume/particle_volume);


}

int GenerateVirtualParticles(inout Particle virtualParticles[150], float h, float radius, Particle particle, float3 normal, float distance)
{
    float spacing = 2 * h / radius; // Correctly calculate spacing based on radius
    

    int count = 0;

    // Iterate over a cube around the particle, considering only half of it in the direction of overlap
    for (int x = -h; x <= h; x += spacing)
    {
        for (int y = -h; y <= h; y += spacing)
        {
            for (int z = -h; z <= h; z += spacing)
            {
                float3 offset = float3(x, y, z); // Use Vector3 for offsets

                float3 projectedPosition = offset - dot(offset, normal) * normal; // Ensure correct projection

                // Check if the projected position is within the bounds and overlaps with the boundary
                if (length(projectedPosition - particle.position) <= h && dot(offset, normal) > distance)
                {
                    virtualParticles[count].position = particle.position + projectedPosition;
                    virtualParticles[count].pressure = 1000.0f;
                    count++;

                    // Stop generating virtual particles once the limit is reached
                    if (count >= 150)
                    {
                        return count;
                    }
                        
                }
            }
        }
    }

    return count; // Return the final count of generated virtual particles
}


void EstimateVirtualDensity()
{

}


#endif