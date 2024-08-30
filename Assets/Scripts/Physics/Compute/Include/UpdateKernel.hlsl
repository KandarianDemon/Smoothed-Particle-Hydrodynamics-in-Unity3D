#ifndef UPDATEKERNEL_INCLUDED
#define UPDATEKERNEL_INCLUDED

#pragma target 5.0
#pragma kernel Update


[numthreads(512,1,1)]
void Update (uint3 id : SV_DispatchThreadID)
{

    if(PARTICLES[id.x]._static ==1)
    {
        return;
    }
    // TODO: insert actual code here!
    if(id.x >= NUMBER_OF_PARTICLES) return; 
    
    
   
    float3 position = PARTICLES[id.x].position;
     float timestep = 0.001f;

    float3 velocity = PARTICLES[id.x].velocity;
   
    // ********************** Adaptive Time Stepping ********************************
    //if(length(PARTICLES[id.x].velocity) > vMax) vMax = length(PARTICLES[id.x].velocity);
    //   float cfl = 0.004*(SMOOTHING_RADIUS/sqrt(STIFFNESS));
    //   float dTF = 0.00025*sqrt(SMOOTHING_RADIUS/fMax);
    // float timestep = min(cfl,dTF);
    // timestep = min(DT/2.0f,timestep);
    
   
   
    float3 pressureForce =  (PARTICLES[id.x].offset)*timestep;

   
     

     for(int i = 0; i<10; i++)
     {

        //VerletIntegration(PARTICLES[id.x].position,position,velocity,pressureForce,timestep);
        //velocity = velocity +pressureForce + GRAVITY/1.0f*timestep ;
        //  position = PARTICLES[id.x].position + velocity * timestep;

        //EulerIntegration(PARTICLES[id.x].position,position, velocity,pressureForce,timestep);
        //VerletIntegration(PARTICLES[id.x].position,position,velocity,pressureForce,timestep);
         SemiImplicitEuler(position,velocity,pressureForce,timestep);
    

     
     }
     
     
     //SemiImplicitEuler(position,velocity,pressureForce,timestep*10.0f);
     //velocity += AddBoundaryRepulsion(position,1000.0f,PARTICLE_RADIUS*2.0f,PARTICLES[id.x].mass)*timestep;
     //EulerIntegration(PARTICLES[id.x].position,position, velocity,pressureForce,timestep*10.0f);
     //LeapfrogIntegrator(position, velocity,pressureForce,timestep*10.0f);
     //VerletIntegration(PARTICLES[id.x].position,position,velocity,pressureForce,timestep*50.0f);

     
     stats[0] = vMax;
     stats[1] = fMax;
     stats[2] = maxPressure;
    

     int iterations = 0;
     //MembraneCollisions_Felix(id,position,velocity,timestep);
     while(iterations < 4)
     {

  
        float travelDistance = length(position - PARTICLES[id.x].position);

        Ray ray = ConstructRay(position, normalize(velocity), travelDistance );
        HitInfo info = GetHitInfo();
        //bool hit = ResolveMeshCollisions(ray, info);
        bool hit = BruteForceCollisions(id,PARTICLES,velocity,position,timestep*10.0f,info);

        int nodeIndex_root;
        int nodeIndex_tip;


     

        bool hit_root   = CheckBVHCollisions(ray,nodeIndex_root);
        bool hit_tip    = CheckBVHCollisions(InvertRay(ray),nodeIndex_tip);

    

        PARTICLES[id.x].color = (hit) ? float3(1,0,0):float3(0,0,0);

        if(hit_root && !hit_tip)  PARTICLES[id.x].color = float3(1,1,0);
        if(hit_tip && !hit_root)   PARTICLES[id.x].color = float3(0,1,1);
        if(hit_tip && hit_root)   PARTICLES[id.x].color = float3(0,1,0);


    
        bool  exceedsRadius     = travelDistance >= 2*PARTICLE_RADIUS;
        PARTICLES[id.x].color = (hit) ? float3(1,1,0):float3(0,0,0);


        if(hit)
        {

            //bool collision = BVHGeometryCollisions(id, PARTICLES,nodeIndex_root,velocity,position,timestep,info);
            
            if(hit)
            {
                //PARTICLES[id.x].color = float3(0,0,1);

                float dot_v_n = dot(normalize(velocity),info.normal);
                bool isInside =  dot_v_n > 0.0f;
                velocity = (!isInside) ? reflect(velocity,info.normal)*0.9f:velocity;

                
                velocity = reflect(velocity,info.normal)*0.55f;
                position = info.hitPoint + info.normal*1.5f* PARTICLE_RADIUS;



            }
        }

        iterations++;
        
        }

    


     ResolveCollisions(position,velocity,id.x);
    
    //PARTICLES[id.x].color = float3(PARTICLES[id.x].type/100,0,1- (PARTICLES[id.x].type/100));
     
   

   
    
}

#endif