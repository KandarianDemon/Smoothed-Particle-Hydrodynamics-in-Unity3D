using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PhysicsComputeShaderUtility;
using System.Linq;
using System.Text.RegularExpressions;

namespace Simulation{


public class Simulator : MonoBehaviour
{

    ParticleData        data;
    ParticleData initialState;
    [SerializeField]
    public SimulationSettings  simulationSettings;   //ScriptableObject
    public ComputeShaderData<ParticleData> computeData;
    // Collision Data
    // Physics data

        [SerializeField]
        public  string[] bufferNames;               // Contains the Names of all buffers created from a data object

        [SerializeField] 
        public string[] kernelNames;                // Contains the names of all the kernels on a compute shader
        Dictionary<string, ComputeBuffer> buffers;  // pairs the buffers and the buffer name
        Dictionary<string,string[]> kernelBufferMap;     // assigns each kernel handle an array of buffers.
        
   

    #region LifeCycle
    // ============= Inititalize Simulation =================
    
    // take ParticleData -> CreateBuffers, SetUPComputeShader
       void Awake()
       {




            InitializeSimulation();
            
            
         
       }

       void Update()
       {

        //SimulationStep(data);                   //Steps the simulation forward.

       }

       void OnRender()
       {

       }


       void OnEnable() {
    
       }

       private void OnDisable() 
       {
        
       }

       private void OnDestroy() 
       {
            DisposeBuffers();
       }

       void DisposeBuffers()
       {
        // Loop over buffers. Dispose all of them if they arent null

        if(buffers!= null)
        {
            foreach(var e in buffers){

                if(e.Value != null)
                {
                        e.Value.Dispose();

                }
            }
        }
        
       }
       #endregion

    #region memberFunctions

       void InitializeSimulation()
       {
        
            // Init Particle Data
            data = new ParticleData(500000);
            computeData = new ComputeShaderData<ParticleData>(simulationSettings.compute, data);



            // CreateBuffers and SetData
            buffers = computeData.buffers;
            bufferNames = computeData.bufferNames;

            // Get kernels and assign buffers
            kernelNames = computeData.kernelNames;
            kernelBufferMap = ComputeHelper.GenerateBufferKernelMap(simulationSettings.compute);


            // Assign properties.

            // Only do this, when the old Particle System isnt active.

            if(GetComponent<ParticleSystem>().enabled == false)
            {
            
            simulationSettings.LinkPropertiesToShader();
            computeData.LinkBuffersToKernels(simulationSettings.compute);
            InitializeParticles(computeData, simulationSettings.compute);
            }
            
       


        
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

            
            ComputeHelper.DispatchKernels(simulationSettings.compute, kernelNames);
       }

       void SaveParticleDataToFile(ParticleData data)
       {

        // basically stores all the particle data for each timestep.
        // Can be used for replay or statistics later on.
        // maybe only get relevant subset of the data
        throw new System.NotImplementedException();
       }

       void InitializeParticles(ComputeShaderData<ParticleData> computeData,ComputeShader cs)
       {

            //Check if the buffers are null
            if (computeData == null || cs == null) return;

            int InitParticlesKernel = cs.FindKernel("InitParticles");
            Vector3Int groupSize = ComputeHelper.GetThreadGroupSize(cs, InitParticlesKernel);



            cs.Dispatch(InitParticlesKernel, groupSize.x, groupSize.y, groupSize.z);
            


       }

       #endregion


    // ============= Advance Simulation =====================

    // SimulationStep();

}

public struct ParticleData
{
    public Vector3[]   positions;                       // Holds the position of the particles
    public Vector3[]   velocities;                       // Holds the velocity of the particles

    public Vector3[]   predictedPositions;             // predictedPosition for density estimation.
    public Vector3[]   forces;                         // Forces applied to that particle
    public Vector3[]   colors;
    public Particle[] particles;                        // Color of the particle. USeful for debugging !!!! REMOVE REMOVE REMOVE REMOVE

    public float[]      densities;                       // Current density at particle location
    public float[]      refDensities;                    // reference density of the fluid or body
    public float[]      pressures;                      // estimated pressure at particle location
        public float[] masses;
    public int[]       _static;                        // is the particle immovable. (actually can be replaced with type)
    public int[]       types;                           // type. 0 = BOUNDARY, 1 = FLUID, 2 = ELASTIC

    public int[]       hashes;                           // grid cell hash of the particle

    public int[]       indices;                          // index of the particle in the sorted particle Map


    public ParticleData(int numberOfEntries)
    {

            positions = new Vector3[numberOfEntries];
            velocities = new Vector3[numberOfEntries];
            predictedPositions = new Vector3[numberOfEntries];
            forces = new Vector3[numberOfEntries];
            colors = new Vector3[numberOfEntries];
            particles = new Particle[numberOfEntries];

            densities = new float[numberOfEntries];
            refDensities = new float[numberOfEntries];
            masses = new float[numberOfEntries];
            pressures = new float[numberOfEntries];
            _static = new int[numberOfEntries];
            types = new int[numberOfEntries];
            hashes = new int[numberOfEntries];
            indices = new int[numberOfEntries];

    }

}

public struct InitializationFlags
{

    // Track wether things happened during initialization;


}
}