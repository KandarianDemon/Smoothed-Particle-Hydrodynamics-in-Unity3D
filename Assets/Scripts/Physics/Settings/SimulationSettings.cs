using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Simulation;

 [CreateAssetMenu(fileName = "SimulationSettings", menuName = "SPH/SimulationSettings", order = 0)]
    public class SimulationSettings : Settings
    {

        public FluidSimulationType simulationType;
        public ComputeShader compute;
        public BoundaryHandling boundaryHandlingType;
        public int numberOfParticles;

        
        public float particleRadius;
        public float smoothingRadius;
        public int maxNumberOfNeighbors;
        public float stiffness;

        public float timestep;

        public void LinkPropertiesToShader()
        {
            if(compute== null)
            {
            throw new System.Exception(" Compute Shader is Not Set on SimulationSettings!");
            
            }
            Debug.Log("Linked");
            compute.SetFloat("PARTICLE_RADIUS", particleRadius);
            compute.SetFloat("SMOOTHING_RADIUS", smoothingRadius);
            compute.SetInt("MAX_NEIGHBORS", maxNumberOfNeighbors);
            compute.SetFloat("STIFFNESS", stiffness);
            compute.SetFloat("DT", timestep);
        }

        void OnValidate()
        {
            LinkPropertiesToShader();
        }
        

        

    }