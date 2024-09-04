#ifndef SPH_GPU_BUFFERS_INCLUDED
#define SPH_GPU_BUFFERS_INCLUDED


// Particle Data Buffers

RWStructuredBuffer<float3> POSITIONS;
RWStructuredBuffer<float3> VELOCITIES;
RWStructuredBuffer<float3> PREDICTEDPOSITIONS;
RWStructuredBuffer<float3> FORCES;
RWStructuredBuffer<float3> COLORS;

RWStructuredBuffer<float> DENSITIES;
RWStructuredBuffer<float> REFDENSITIES;
RWStructuredBuffer<float> PRESSURES;
RWStructuredBuffer<float> MASSES;


RWStructuredBuffer<int>   _STATIC;
RWStructuredBuffer<int>   TYPES;
RWStructuredBuffer<int>   HASHES;
RWStructuredBuffer<int>   INDICES;



// NeighborSearch Buffers

// RWStructuredBuffer<int> CELLCOUNT:register(u2);
// RWStructuredBuffer<int> CELLTRACKER:register(u3);
// RWStructuredBuffer<int> PARTICLEMAP:register(u4);



// Constraints Buffers


#endif