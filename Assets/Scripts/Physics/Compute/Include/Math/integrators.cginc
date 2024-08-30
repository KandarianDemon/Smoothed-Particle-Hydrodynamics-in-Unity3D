#ifndef INTEGRATORS_INCLUDED
#define INTEGRATORS_INCLUDED

void EulerIntegration(float3 og_position,inout float3 position, inout float3 velocity,float3 pressureforce, float timestep)
{
    

     velocity = velocity +(pressureforce + GRAVITY/1.0f)*timestep ;
     position = og_position + velocity * timestep;

}

void VerletIntegration(float3 old_position, inout float3 position, inout float3 velocity, float3 pressureforce, float timestep)
{
    float3 acceleration = pressureforce + GRAVITY;
    float3 new_position = 2 * position - old_position + acceleration * timestep * timestep;
    
    velocity = (new_position - old_position) / (2 * timestep);
    
   
    position = new_position;
}

void SemiImplicitEuler(inout float3 position, inout float3 velocity, float3 pressureforce, float timestep)
{
    float3 acceleration = pressureforce + GRAVITY;
    
    // Update velocity first (semi-implicit)
    velocity += acceleration * timestep;
    
    // Then update position
    position += velocity * timestep;
}

void LeapfrogIntegrator(inout float3 position, inout float3 velocity, float3 pressureforce, float timestep)
{
    float3 acceleration = pressureforce + GRAVITY;
    
    // Update position using current velocity
    position += velocity * timestep + 0.5f * acceleration * timestep * timestep;
    
    // Update velocity
    velocity += acceleration * timestep;
}

#endif