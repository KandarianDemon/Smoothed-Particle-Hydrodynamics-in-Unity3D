#ifndef SPH_SETTINGS_INCLUDED
#define SPH_SETTINGS_INCLUDED

float pi;
int NUMTHREADS = 1024;

float3 GRAVITY;

float3 HALF_BOUNDSIZE;

int NUMBER_OF_PARTICLES;
float PARTICLE_RADIUS;

int CELLOFINTEREST = 0;

float SMOOTHING_RADIUS;

int NUMBEROFCELLS;

int LOWESTCELL;
int HIGHESTCELL;

float DT;

const float4x4 localToWorld;
const float4x4 worldToLocal;


float STIFFNESS;
float VISCOSITY;

//Time stepping

float vMax;
float fMax;

float maxPressure;

#endif