using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using UnityEditor;

[RequireComponent(typeof(MeshRenderer),typeof(MeshFilter))]
public class ParticleSystem : MonoBehaviour
{
    

    #region ParticleSettings

    public float lifeTime;
    public float mass;
    public int numberOfParticles;
    public Vector3 initialVelocity;

    public bool runSimulation = false;
    public int cellOfInterest;
    #endregion

    #region DomainSettings

    public GameObject domain;


    [Header("Dispatch Kernels")]
    public bool disp_init;
    public bool disp_clearGrid;
    public bool disp_gridupdate;
    public bool disp_density;
    public bool disp_forces;
    public bool disp_partialSums;
    public bool disp_mapParticles;
    public bool disp_neighborSearch;

    public bool disp_pressure;


    [Header("Debugging")]

    public bool debugParticles;
    
    [Range(0,2)]
    public int visMode;
    
    [Range(500,10000)]
    public float maxDensity;

    [Range(10,200)]
    public float maxVelocity;
     [Range(10,20000)]
    public float maxPressure;


   




    #endregion

    #region SPH Settings
    [Header("SPH Settings")]
    [Range(0.01f,5f)]
    public float smoothingRadius;
    [Range(0.001f,10000000000)]
    public float stiffness;
    public float dynamicViscosity;
    public float particleRadius;

    int maxParticles;


    #endregion

    #region ComputeShader
    [Header("Compute Shader")]
    public ComputeShader computeShader;
    public ComputeBuffer PARTICLES;
    public Particle[] particles;

    public ComputeBuffer quad;


    

    public ComputeBuffer CELLCOUNT;
    public int[] cellCount;

   

    public ComputeBuffer PARTICLE_MAP;
    public int[] particleMap;

    public ComputeBuffer CELLTRACKER;
    public int[] cellTracker;

   
    public ComputeBuffer BBOX_TRIANGLES;

    public ComputeBuffer CONNECTIONS;
    public Connection[] cons;

    public int[] gridTracker; // Tracks number of particles within gridCell
    public int[] grid;        // stores indices of particles within Cell


    // for rendering the particles procedurally

    bool allBuffersSet = false;

    // init kernel, emit kernel, update kernel
    #endregion

    #region Rendering

    [Header("Rendering and Bounding Box")]
    public Material renderMaterial;
     public MeshRenderer renderer;
    public MeshFilter filter;
    ComputeBuffer argsBuffer;
    uint[] args = new uint[5] {0,0,0,0,0};
    public Mesh particleMesh;

    // particle billboard shader
    // quad for Graphics.DrawProceduralNow
    

    #endregion

    void Awake()
    {
         renderer = gameObject.GetComponent<MeshRenderer>();
        filter =   gameObject.GetComponent<MeshFilter>();

       
        InitializeParticles();
        CreateBuffers();
        InitShader();

       
    }

  
    void OnDrawGizmos()
    {

        // Vector3 dimDom = domain.GetComponent<GLLines>().GetDimensions();



        // Vector3 cellsPerAxis = dimDom/(dimDom*smoothingRadius);
        // Vector3 size =  dimDom * smoothingRadius;

        //  for(int x = 0; x<Mathf.FloorToInt(cellsPerAxis.x);x++)
        // {
        //     for (int y = 0; y < Mathf.FloorToInt(cellsPerAxis.y);y++)
        //     {
        //         for(int z = 0; z < Mathf.FloorToInt(cellsPerAxis.z);z++)
        //         {
        //             Vector3 pos = domain.transform.position - dimDom/2;
        //             pos += new Vector3(x,y,z) * smoothingRadius + size/2;

        //             Gizmos.DrawWireCube(pos, size/2);
        //         }
        //     }
        // }

         Vector3 dims = transform.localScale/smoothingRadius;
          Handles.Label(this.transform.position + new Vector3(0,11,0),  " number of cells " + (Mathf.FloorToInt(dims.x) * Mathf.FloorToInt(dims.y) * Mathf.FloorToInt(dims.z)).ToString());
       
       

       Vector3 cubePos = transform.position - transform.localScale/2 + new Vector3(smoothingRadius/2,smoothingRadius/2,smoothingRadius/2);
       Gizmos.DrawWireCube(cubePos, new Vector3(smoothingRadius,smoothingRadius,smoothingRadius));

        

       

        
    }

    void InitializeParticles()
    {
        
        int particlesPerAxis = Mathf.CeilToInt(Mathf.Pow(numberOfParticles,1f/3f));
        int initParticlesKernel = computeShader.FindKernel("InitParticles");

        int groupsX = Mathf.Max(Mathf.CeilToInt(numberOfParticles/512.0f),1);

      
        


        particles = new Particle[numberOfParticles];

        PARTICLES = new ComputeBuffer(particles.Length, Marshal.SizeOf(typeof(Particle)));
        PARTICLES.SetData(particles);

        computeShader.SetVector("HALF_BOUNDSIZE", domain.GetComponent<GLLines>().GetDimensions()/2);
        computeShader.SetBuffer(initParticlesKernel, "PARTICLES", PARTICLES);
        computeShader.SetFloat( "PARTICLE_RADIUS", particleRadius);
        computeShader.Dispatch(initParticlesKernel,groupsX,1,1);
      


    }

    Connection[] MakeConnections(Particle[] particles)
    {
        Connection[] cons = new Connection[particles.Length-1];

        for(int i = 0; i < particles.Length-1; i++)
        {
            cons[i] = new Connection(i,i+1);
        }

        return cons;
    }


    void FixedUpdate()
    {
        if(runSimulation)
        {
            
            computeShader.SetInt("LOWESTCELL", numberOfParticles*10);
            computeShader.SetInt("HIGHESTCELL", (int)-1);
            computeShader.SetFloat("SMOOTHING_RADIUS",smoothingRadius);
            computeShader.SetFloat("STIFFNESS", stiffness);
            computeShader.SetFloat("VISCOSITY", dynamicViscosity);
            computeShader.SetFloat("maxPressure", maxPressure);
            computeShader.SetFloat("DT",Time.deltaTime);
            computeShader.SetMatrix("localToWorld", transform.localToWorldMatrix);
            computeShader.SetMatrix("worldToLocal", transform.worldToLocalMatrix);
            

            computeShader.SetVector("HALF_BOUNDSIZE", domain.GetComponent<GLLines>().GetDimensions()/2);


             Vector3 numberOfGridCells = domain.GetComponent<GLLines>().GetDimensions()/smoothingRadius;

            int xDim = Mathf.CeilToInt(numberOfGridCells[0]);
            int yDim = Mathf.CeilToInt(numberOfGridCells[1]);
            int zDim = Mathf.CeilToInt(numberOfGridCells[2]);


            

            //cellCount = new int[xDim*yDim*zDim];
            cellCount = new int[particles.Length];
            cellTracker = new int[cellCount.Length];
          

            if(CELLCOUNT != null) CELLCOUNT.Release();
            if(CELLTRACKER != null) CELLTRACKER.Release();

            CELLTRACKER = new ComputeBuffer(cellTracker.Length, sizeof(int));
            CELLTRACKER.SetData(cellTracker);

            CELLCOUNT = new ComputeBuffer(cellCount.Length, sizeof(int));
            CELLCOUNT.SetData(cellCount);

            
            if(disp_clearGrid)      DispatchClearGrid();
            if(disp_gridupdate)     DispatchGridUpdate();
            if(disp_partialSums)    DispatchPartialSums();
            if(disp_mapParticles)   DispatchMapParticles();
            //if(disp_neighborSearch) DispatchNeighborSearch();
            if(disp_density)        DispatchDensity();
            if(disp_forces)        DispatchComputeForces();
            if(disp_init) 
            
            
            {

                for(int i = 0; i<1; i++)
                {
                    DispatchInit();
                }
                
            }          
                
                
            if(disp_pressure)       DispatchPressure();


        }

     

        
    }

    void CreateBuffers()
    {
       
        quad = new ComputeBuffer(6, sizeof(float) * 3);

        quad.SetData(

            new[] {

            new Vector3(-0.5f,0.5f),
            new Vector3(0.5f,0.5f),
            new Vector3(0.5f,-0.5f),

            new Vector3(-0.5f,-0.5f),
            new Vector3(0.5f,-0.5f),
            new Vector3(-0.5f,0.5f)
            
            
            
            }
        );

    
        

        // if(numberOfParticles < 100)
        // {

        //     cons = MakeConnections(particles);
        //     CONNECTIONS = new ComputeBuffer(cons.Length, Marshal.SizeOf(typeof(Connection)));
        //     CONNECTIONS.SetData(cons);
        // }

        Vector3 numberOfGridCells = domain.GetComponent<GLLines>().GetDimensions()/smoothingRadius;

        int xDim = Mathf.CeilToInt(numberOfGridCells[0]);
        int yDim = Mathf.CeilToInt(numberOfGridCells[1]);
        int zDim = Mathf.CeilToInt(numberOfGridCells[2]);


        

        //cellCount = new int[xDim*yDim*zDim];
        cellCount = new int[particles.Length];
        cellTracker = new int[cellCount.Length];

        int pMapLength = particles.Length+1;
        particleMap = new int[pMapLength];
        Array.Fill(particleMap,0);

        computeShader.SetInt("NUMBEROFCELLS", cellCount.Length);
        

        


        CELLCOUNT = new ComputeBuffer(cellCount.Length, sizeof(int));
        CELLCOUNT.SetData(cellCount);

      

        PARTICLE_MAP = new ComputeBuffer(particleMap.Length, sizeof(int));
        PARTICLE_MAP.SetData(particleMap);

        CELLTRACKER = new ComputeBuffer(cellTracker.Length,sizeof(int));
        CELLTRACKER.SetData(cellTracker);


        
        

        

       
        computeShader.SetFloat("pi", Mathf.PI);
        
        
    }

    void DispatchInit()
    {
       

        int UpdateKernel = computeShader.FindKernel("Update");
       

        //CreateBuffers();

        float pVolume = 4/3*Mathf.PI*Mathf.Pow(particleRadius,3);

        float cellVolume = pVolume*40;

        //float smoothingRadius= Mathf.Pow(cellVolume,1/3);
        
        computeShader.SetFloat("SMOOTHING_RADIUS", smoothingRadius);
        computeShader.SetInt("NUMTHREADS", 512);
        computeShader.SetInt("NUMBER_OF_PARTICLES", numberOfParticles);
        

        Vector3 grav = domain.transform.InverseTransformDirection(new Vector3(0,-9.81f,0));

        computeShader.SetVector("GRAVITY", grav);

        // computeShader.SetVector("HALF_BOUNDSIZE", domain.GetComponent<GLLines>().GetDimensions()/2);
       
        computeShader.SetBuffer(UpdateKernel,"PARTICLES", PARTICLES);
       
      
        
        int groupsX = Mathf.Max(Mathf.CeilToInt(numberOfParticles/512.0f),1);

      
        computeShader.Dispatch(UpdateKernel,groupsX,1,1);

       

       
       



    }

    

    void DispatchClearGrid()
    {
        int ClearGridKernel = computeShader.FindKernel("ClearGrid");

       

        CELLCOUNT.SetData(cellCount);
        CELLTRACKER.SetData(cellTracker);

       
            computeShader.SetBuffer(ClearGridKernel, "CELLCOUNT",  CELLCOUNT);
            computeShader.SetBuffer(ClearGridKernel, "CELLTRACKER",  CELLTRACKER);
           

        
          
        
        
        int groupsX = Mathf.Max(Mathf.CeilToInt(cellCount.Length/512.0f),1);
        
        computeShader.Dispatch(ClearGridKernel,groupsX,1,1);

       


    }


    

    void DispatchGridUpdate()
    {
       
        int GridUpdate= computeShader.FindKernel("GridUpdate");

        computeShader.SetInt("CELLOFINTEREST", cellOfInterest);
        computeShader.SetFloat("SMOOTHING_RADIUS", smoothingRadius);
       

       
        computeShader.SetBuffer(GridUpdate,"CELLCOUNT", CELLCOUNT);
        //computeShader.SetBuffer(GridUpdate,"GRID", GRID);
        computeShader.SetBuffer(GridUpdate,"PARTICLES", PARTICLES);
       
        
    //    CELLCOUNT.GetData(cellCount);
        // Debug.Log(" ======== CELLCOUNT BEFORE GRID UPDATE ======== ");
        // for(int i = 0; i< cellCount.Length;i++)
        // {
        //     Debug.Log($"cell {i} contains {cellCount[i]} particles ");
        // }
        int groupsX = Mathf.Max(Mathf.CeilToInt(particles.Length/512.0f),1);
        computeShader.Dispatch(GridUpdate,groupsX,1,1);
        // Debug.Log(" ====== Grid Update ========= ");
        // Debug.Log($"group size is {groupsX} number of cells is { cellCount.Length}");
         
        
        // Debug.Log(" ======== CELLCOUNT ======== ");
        // for(int i = 0; i< cellCount.Length;i++)
        // {
        //     Debug.Log($"cell {i} contains {cellCount[i]} particles ");
        // }

    
    }

    void DispatchPartialSums()
    {
        int PartialSumsKernel = computeShader.FindKernel("PartialSums");
        if(allBuffersSet == false) computeShader.SetBuffer(PartialSumsKernel, "CELLCOUNT", CELLCOUNT);
        int groupsX = Mathf.Max(Mathf.CeilToInt(cellCount.Length/512.0f),1);
        //computeShader.Dispatch(PartialSumsKernel,groupsX,1,1);
         
        CELLCOUNT.GetData(cellCount);
        

        int start = 0;
        for(int j = 0; j<cellCount.Length;j++)
        {   
            //Debug.Log($"Original cellcount {cellCount[j] } j is {j}");
            start += cellCount[j];
            cellCount[j] = start;
            //Debug.Log($"groupsize is {groupsX} j is {j} count is {start} cellCount in cell {j} is {cellCount[j]}");
        }

        CELLCOUNT.SetData(cellCount);


        // Debug.Log(" ======== PARTIALSUM ======== ");
        // for(int i = 0; i< cellCount.Length;i++)
        // {
        //     Debug.Log($" partial sum of cell {i} is {cellCount[i]} ");
        // }
    }

    void DispatchMapParticles()
    {
        int MapParticlesKernel = computeShader.FindKernel("MapParticles");
        
        CELLTRACKER.SetData(cellTracker);
       
       
        computeShader.SetBuffer(MapParticlesKernel, "CELLCOUNT", CELLCOUNT);
        computeShader.SetBuffer(MapParticlesKernel, "PARTICLEMAP", PARTICLE_MAP);
        computeShader.SetBuffer(MapParticlesKernel,"PARTICLES", PARTICLES);
        computeShader.SetBuffer(MapParticlesKernel, "CELLTRACKER", CELLTRACKER);
        
        
        int groupsX = Mathf.Max(Mathf.CeilToInt(particles.Length/512.0f),1);
        //PARTICLES.GetData(particles);
        //CELLCOUNT.GetData(cellCount);

        // Debug.Log(" ====== CELL COUNTS PARTIAL SUMS ========");
        // for(int i = 0; i < cellCount.Length;i++)
        // {
        //    Debug.Log($" {i} :: {cellCount[i]}");

        // }
        // for(int i = 0; i<particles.Length; i++)
        // {
        //     Debug.Log($"Particle {i} index is {particles[i].index}");
        // }
        computeShader.Dispatch(MapParticlesKernel,groupsX,1,1);

        // foreach(var p in particles)
        // {
        //     int h = p.hash;
        //     int i = Array.IndexOf(particles,p);

        //     if(cellCount[h] == 0) continue;
        //     int index = cellCount[h]-1;

        //     //Debug.Log($" Index is {index} hash is {h}");
        //     particleMap[index] = i; 


        // }

        // foreach(var p in particleMap)
        // {
        //     Debug.Log($"{Array.IndexOf(particleMap,p)} :: {p}");
        // }


        // PARTICLE_MAP.GetData(particleMap);
        // CELLTRACKER.GetData(cellTracker);
        // CELLCOUNT.GetData(cellCount);

      

        // Debug.Log($"Cell {cellOfInterest} contains {cellTracker[cellOfInterest]} || {cellCount[cellOfInterest]} particles particle map index of particle {cellOfInterest} is {particles[cellOfInterest].index} hash is {particles[cellOfInterest].hash}");
        // Debug.Log(" ============ PARTICLE MAP ============");
        // for(int i = 0; i<particleMap.Length-1;i++)
        // {

           
        //     Debug.Log($" index of particle is {i} cell is {particleMap[i]} index in particlemap is {particles[particleMap[i]].hash}");
        // }
        
        

        
    
    }

    // void DispatchNeighborSearch()
    // {
    //     int neighborSearchKernel = computeShader.FindKernel("NeighborSearch");
    //     if(allBuffersSet == false)
    //     {

        
       
    //     computeShader.SetBuffer(neighborSearchKernel , "PARTICLES", PARTICLES);
    //     }
    //     int groupsX = Mathf.Max(Mathf.CeilToInt(particles.Length/64.0f),1);
    //     computeShader.Dispatch(neighborSearchKernel,groupsX,1,1);

    // }

    void DispatchDensity()
    {
        int densityKernel= computeShader.FindKernel("ComputeDensity");

      
       
        
        //computeShader.SetBuffer(densityKernel, "NEIGHBORCELLS", NEIGHBORCELLS);
        computeShader.SetBuffer(densityKernel, "CELLCOUNT", CELLCOUNT);
        computeShader.SetBuffer(densityKernel, "PARTICLEMAP", PARTICLE_MAP);
        computeShader.SetBuffer(densityKernel,  "PARTICLES", PARTICLES);
        computeShader.SetBuffer(densityKernel, "CELLTRACKER", CELLTRACKER);
        
       
        int groupsX = Mathf.Max(Mathf.CeilToInt(particles.Length/512.0f),1);
         float startTime = Time.time;
        computeShader.Dispatch(densityKernel,groupsX,1,1);
        
        
        
        // float randDist = UnityEngine.Random.Range(0.0f, smoothingRadius);
        // float sum = particles[0].mass*WPoly6Kernel(randDist,smoothingRadius,Mathf.PI);

        // Debug.Log($"Test density at {randDist} is {sum}");

        //PARTICLES.GetData(particles);
        int idx = Mathf.Min(cellOfInterest,particles.Length);
        // // // foreach(var p in particles)
        // // // {
        // // //     Debug.Log($"density is {p.density} mass is {p.mass}");
        // // // }
       

        
    }

    void DispatchComputeForces()
    {
        int computeForcesKernel = computeShader.FindKernel("ComputeForces");

        computeShader.SetBuffer(computeForcesKernel, "CELLCOUNT", CELLCOUNT);
        computeShader.SetBuffer(computeForcesKernel, "PARTICLEMAP", PARTICLE_MAP);
        computeShader.SetBuffer(computeForcesKernel, "PARTICLES", PARTICLES);
        computeShader.SetBuffer(computeForcesKernel, "CELLTRACKER", CELLTRACKER);

        int groupsX = Mathf.Max(Mathf.CeilToInt(particles.Length/512.0f),1);

        computeShader.Dispatch(computeForcesKernel, groupsX,1,1);

          //PARTICLES.GetData(particles);
        // // foreach(var p in particles)
        // // {
        // //     Debug.Log($"density is {p.density} mass is {p.mass}");
        // // }
        //Debug.Log($" Density of particle {cellOfInterest} is {particles[cellOfInterest].density} its pressure is {particles[cellOfInterest].pressure}  normal offset {particles[cellOfInterest].offset} map index {particles[cellOfInterest].index} hash {particles[cellOfInterest].hash}");

    }

    void DispatchPressure()
    {   

        int pressureKernel = computeShader.FindKernel("ComputePressure");
        computeShader.SetBuffer(pressureKernel, "PARTICLEMAP", PARTICLE_MAP);
        computeShader.SetBuffer(pressureKernel, "PARTICLES",PARTICLES);
         computeShader.SetBuffer(pressureKernel, "CELLCOUNT",CELLCOUNT);
          computeShader.SetBuffer(pressureKernel, "CELLTRACKER",CELLTRACKER);

        int groupsX = Mathf.Max(Mathf.CeilToInt(particleMap.Length/512.0f),1);

        computeShader.Dispatch(pressureKernel, groupsX,1,1);
    }

    void DispatchCons()
    {
         int ConnectionKernel = computeShader.FindKernel("ConnectionKernel");
        computeShader.SetBuffer(ConnectionKernel, "CONNECTIONS", CONNECTIONS);
        int groupsX = Mathf.Max(Mathf.CeilToInt(cons.Length/512.0f),1);

        
        computeShader.Dispatch(ConnectionKernel,groupsX,1,1);
    }

     float WPoly6Kernel(float r,float h,float pi)
        {

        float x = (315/(64*pi*(h*h*h*h*h*h*h*h*h)));
                if(r>= 0 && r <= h )
                {
                    return  x *
                            ((h*h - r*r)*(h*h - r*r)*(h*h - r*r));
                }

                else{
                    return x*0;
                }
        
                    
                

                

        }


    void ReleaseBuffers()
    {
        if(PARTICLES != null) PARTICLES.Release();
        if(quad != null) quad.Release();
        if(CONNECTIONS != null) CONNECTIONS.Release();
        if(PARTICLE_MAP!= null) PARTICLE_MAP.Release();
        if(CELLCOUNT!= null) CELLCOUNT.Release();
        if(CELLTRACKER!= null) CELLTRACKER.Release();
    }

  //SetUpBillboardShader
    public void InitShader()
    {
            renderMaterial.SetBuffer("particles", PARTICLES);
            renderMaterial.SetBuffer("quad", quad);
            renderMaterial.SetFloat("max_dist ", domain.GetComponent<GLLines>().GetDimensions().x);


            if(particleMesh != null)

            {
            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            int instanceCount = particles.Length;

            // Indirect args
      

             
            
            args[0] = (uint)0;
            args[1] = (uint)instanceCount;
            args[2] = (uint)particleMesh.GetIndexStart(0);
            args[3] = (uint)particleMesh.GetBaseVertex(0);
            args[4] = (uint) 0;
         
       
            argsBuffer.SetData(args);
            }
    }
  //SetUpComputeShader
    public void OnRenderObject()
    {
          
            renderMaterial.SetVector("worldPosTransform", domain.transform.position);
            renderMaterial.SetVector("dimensions",domain.GetComponent<GLLines>().GetDimensions());
            renderMaterial.SetMatrix("l2w", domain.transform.localToWorldMatrix);
            renderMaterial.SetFloat("numberOfCells", cellCount.Length);
            renderMaterial.SetPass(0);
            renderMaterial.SetInt("cellOfInterest", cellOfInterest);
            renderMaterial.SetInt("visMode", visMode);
            renderMaterial.SetFloat("maxVelocity",maxVelocity);
            renderMaterial.SetFloat("maxDensity",maxDensity);
            renderMaterial.SetFloat("maxPressure",maxPressure);

            
            
            if(particleMesh == null) {Graphics.DrawProceduralNow(MeshTopology.Quads,6,numberOfParticles);}

            else{
            
            Bounds bounds = new Bounds(Vector3.zero,Vector3.one * 1000000.0f);

            args[0] = (uint)0;
            args[1] = (uint)particles.Length;
            args[2] = (uint)particleMesh.GetIndexStart(0);
            args[3] = (uint)particleMesh.GetBaseVertex(0);
            args[4] = (uint) 0;
         
       
            argsBuffer.SetData(args);
           
            Graphics.DrawMeshInstancedIndirect(particleMesh,0,renderMaterial,bounds, argsBuffer);}

            // if(PARTICLES != null)
            // {
            //     PARTICLES.GetData(particles);
            // }
    
    
    }

    public void OnDestroy()
    {
        ReleaseBuffers();
    }

    int Hash(Vector3 position, float numberOfItems)
{
    int xi = Mathf.FloorToInt(position.x);
    int yi = Mathf.FloorToInt(position.y);
    int zi = Mathf.FloorToInt(position.z);

    long hash = (xi * 92837111) ^ (yi * 689287499) ^ (zi * 2839923481);
    int h = Mathf.RoundToInt(Mathf.Abs(hash) % numberOfItems);

    return h;
}

}

public struct Particle
{

    public Vector3  color;
    public Vector3  position;
    public Vector3  velocity;
    public Vector3  offset;
    public Vector3 predictedPosition;
   

    
    public float pressure;
    public float density;
    public float radius;
    public float mass;

    public int hash;
    public int index;
   

   

    //Aditional Density, pressure etc



}



public struct Uint2
{
    uint a;
    uint b;

    public Uint2(uint a,uint b)
    {
        this.a = a;
        this.b = b;
    }
}


public struct Connection
{
    public int p1;
    public int p2;


    public Connection(int a, int b)
    {
        this.p1 = a;
        this.p2 = b;
    }

    // just a distance constraint, if points are farther or closer than a certain value, position will be adjusted
}
