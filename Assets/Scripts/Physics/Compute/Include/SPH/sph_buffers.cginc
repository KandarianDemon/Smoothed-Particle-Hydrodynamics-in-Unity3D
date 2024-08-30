#ifndef SPH_BUFFERS_INCLUDED
#define SPH_BUFFERS_INCLUDED



RWStructuredBuffer<Particle> PARTICLES: register(u0);
RWStructuredBuffer<Particle> MEMBRANE;

RWStructuredBuffer<Vertex> VERTICES;
RWStructuredBuffer<Triangle> TRIANGLES:register(u1);



RWStructuredBuffer<Connection> CONNECTIONS;


RWStructuredBuffer<int> CELLCOUNT:register(u2);
RWStructuredBuffer<int> CELLTRACKER:register(u3);
RWStructuredBuffer<int> PARTICLEMAP:register(u4);

RWStructuredBuffer<int> NEIGHBORCELLS;

RWStructuredBuffer<uint2> SPATIALHASH;

RWStructuredBuffer<float3> stats;

StructuredBuffer<float3> vPARTICLES:register(t0);

#endif
