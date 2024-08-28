using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Simulator : MonoBehaviour
{

    ParticleData        data;
    ParticleData initialState;
    [SerializeField]
    public SimulationSettings  simulationSettings;   //ScriptableObject
   

    #region LifeCycle
    // ============= Inititalize Simulation =================
    
    // take ParticleData -> CreateBuffers, SetUPComputeShader
       void Start()
       {
        
        InitializeSimulation(data, simulationSettings);   // Function needs to return some flags, to determine wether setup was
                                                // successfull.
         
       }

       void Update()
       {

        SimulationStep(data);                   //Steps the simulation forward.

       }


       void OnEnable() {
    
       }

       private void OnDisable() 
       {
        
       }

       private void OnDestroy() 
       {
        //Remove all buffers
       }
       #endregion

    #region memberFunctions

       void InitializeSimulation(ParticleData data, SimulationSettings settings)
       {
        
        // CreateBuffers
        // LinkBuffers to kernels
        // link properties to kernels and compute shader


        throw new System.NotImplementedException();
       }

       void ResetSimulation()
       {

        // Store initial Particle Conditions somewhere;
        data = initialState;

        // RecalculateBuffers();
        throw new System.NotImplementedException();

       }

       void SimulationStep(ParticleData data)
       {


        // Dispatch the kernels
        // provide compute helper functionality.
        // array with kernel handles. Store the handles in the right order, loop over the array to dispatch the kernels.

        // 
        throw new System.NotImplementedException();
       }

       void SaveParticleDataToFile(ParticleData data)
       {

        // basically stores all the particle data for each timestep.
        // Can be used for replay or statistics later on.
        // maybe only get relevant subset of the data
        throw new System.NotImplementedException();
       }

       #endregion


    // ============= Advance Simulation =====================

    // SimulationStep();

}

public struct ParticleData
{
    Vector3[]   position;                       // Holds the position of the particles
    Vector3[]   velocity;                       // Holds the velocity of the particles

    Vector3[]   predictedPositions;             // predictedPosition for density estimation.
    Vector3[]   forces;                         // Forces applied to that particle
    Vector3[]   color;                          // Color of the particle. USeful for debugging

    float[]      density;                       // Current density at particle location
    float[]      refDensity;                    // reference density of the fluid or body
    float[]      pressure;                      // estimated pressure at particle location
    int[]       _static;                        // is the particle immovable. (actually can be replaced with type)
    int[]       type;                           // type. 0 = BOUNDARY, 1 = FLUID, 2 = ELASTIC

    int[]       hash;                           // grid cell hash of the particle

    int[]       index;                          // index of the particle in the sorted particle Map


}

public struct InitializationFlags
{

    // Track wether things happened during initialization;


}