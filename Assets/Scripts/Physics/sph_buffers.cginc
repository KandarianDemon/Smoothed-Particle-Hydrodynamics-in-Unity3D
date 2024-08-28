#ifndef SPH_BUFFERS_INCLUDED
#define SPH_BUFFERS_INCLUDED



RWStructuredBuffer<Particle> PARTICLES;
RWStructuredBuffer<Particle> MEMBRANE;

RWStructuredBuffer<Vertex> VERTICES;
RWStructuredBuffer<Triangle> TRIANGLES;



RWStructuredBuffer<Connection> CONNECTIONS;

RWStructuredBuffer<int> GRID_TRACKER;
RWStructuredBuffer<int> GRID;

RWStructuredBuffer<int> CELLCOUNT;
RWStructuredBuffer<int> CELLTRACKER;
RWStructuredBuffer<int> PARTICLEMAP;

RWStructuredBuffer<int> NEIGHBORCELLS;

RWStructuredBuffer<uint2> SPATIALHASH;

RWStructuredBuffer<float3> stats;

StructuredBuffer<float3> vPARTICLES;

#endif
