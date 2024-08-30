using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Simulation
{
    public enum ObjectType
    {
        None, Primitive, Mesh

    }

    public enum AddConstraints
    {
        Yes, No
    }
    public enum FluidSimulationType
    {
        SPH,PCISPH,IISPH,SimpleCollisions
    }

    public enum BoundaryHandling
    {
        RepulsionForces, LennardJones, BoundaryParticles, StencilBoundary, GhostParticles
    }

    public enum SimulationObjectType 
    {
        Boundary, Fluid, Immersible
    }
}