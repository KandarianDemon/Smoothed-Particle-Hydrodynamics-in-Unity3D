using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using UnityEditor;
using System.Linq;
using TMPro;
using System.Data;
using UnityEngine.UIElements;

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

    public float threadNum = 512.0f;


    [Header("Debugging")]

    public bool debugParticles;
    
    [Range(0,3)]
    public int visMode;
    
    [Range(500,10000)]
    public float maxDensity;

    [Range(10,200)]
    public float maxVelocity;
     [Range(10,10000)]
    public float maxPressure;


    [Header("Object Settings")]
    public AddConstraints constraints;
    public PrimitiveType type;
    public ObjectType oType;
    public GameObject mesh;


    

    #endregion

    #region SPH Settings
    [Header("SPH Settings")]
    [Tooltip("This adjusts the size of the grid cells and is the radius for the smoothing kernel")]
    [Range(0.01f,5f)]
    public float smoothingRadius;
    public float particleRadius;
    [Range(0.001f,100000000000)]
    public float stiffness;
    [Range(7.0f,80.0f)]
    public float dynamicViscosity;
    
    [Tooltip("The Epsilon value for the Lennerd-Jones-Force repelling particles from the domain walls")]
    [Range(0.001f,1000.0f)]
    public float lj_epsilon;

    int maxParticles;


    #endregion

    #region ComputeShaderAndBuffers
    [Header("Compute Shader")]
    public ComputeShader computeShader;
    public ComputeBuffer PARTICLES;
    public Particle[] particles;

    public ComputeBuffer quad;

    public ComputeBuffer stats;
    public float[] _stats;
    

    public ComputeBuffer CELLCOUNT;
    public int[] cellCount;


    public ComputeBuffer TRIANGLES;
    public ComputeBuffer VERTICES;
   

    public ComputeBuffer PARTICLE_MAP;
    public int[] particleMap;

    public ComputeBuffer CELLTRACKER;
    public int[] cellTracker;

   
    public ComputeBuffer BBOX_TRIANGLES;

    public ComputeBuffer CONNECTIONS;
    public Connection[] cons;

    public int[] gridTracker; // Tracks number of particles within gridCell
    public int[] grid;        // stores indices of particles within Cell

    public ComputeBuffer vPARTICLES;



    // for rendering the particles procedurally

    bool allBuffersSet = false;

    // init kernel, emit kernel, update kernel
    #endregion

    #region Rendering

    [Header("Rendering and Bounding Box")]
    public Material renderMaterial;

    public Material memMat;
     public MeshRenderer renderer;
    public MeshFilter filter;
    ComputeBuffer argsBuffer;
    uint[] args = new uint[5] {0,0,0,0,0};
    public Mesh particleMesh;



    [Header("Membrane and other objects")]
    
    public ComputeBuffer membraneVerts;

    // particle billboard shader
    // quad for Graphics.DrawProceduralNow
    

    #endregion

    #region lifecycle_methods
    void Awake()
    {
        renderer = gameObject.GetComponent<MeshRenderer>();
        filter =   gameObject.GetComponent<MeshFilter>();
        

        // GameObject membraneObject = new GameObject("Membrane");
        // MeshFilter memFilter = membraneObject.gameObject.AddComponent<MeshFilter>();
        // MeshRenderer memRenderer = membraneObject.gameObject.AddComponent<MeshRenderer>();


        // Mesh mesh = Generate_Membrane(5,10);
        // mesh.RecalculateNormals();
        // memFilter.mesh = mesh;

        if(oType == ObjectType.Primitive){

       
        
        GameObject cube = GameObject.CreatePrimitive(type);
        cube.transform.localScale = cube.transform.localScale*2.5f;
        cube.name = "Cube";
        cube.transform.position = cube.transform.position - new Vector3(0,cube.transform.localScale.y,0);
        BVHComponent bvhComponent = cube.AddComponent<BVHComponent>();
        bvhComponent.computeShader = this.computeShader;
        cube.tag ="Simulation Object";

        // Build compute buffers for BVH. or let the buffers be build on the BVH component.

        MeshFilter cubeFilter = cube.GetComponent<MeshFilter>();
        MeshRenderer cubeRenderer = cube.GetComponent<MeshRenderer>();
        cubeFilter.mesh.RecalculateNormals();


        //membraneObject.transform.SetParent(GameObject.Find("GameObject").transform);
        cube.transform.SetParent(GameObject.Find("GameObject").transform);


        membraneVerts = new ComputeBuffer(cubeFilter.mesh.vertices.Length, sizeof(float)*3);

        // get world position

        Vector3[] worldVerts = new Vector3[cubeFilter.mesh.vertices.Length];

        for(int i = 0; i < worldVerts.Length;i++)
        {
            worldVerts[i] = gameObject.transform.TransformPoint(cubeFilter.mesh.vertices[i]);
        }
        

        membraneVerts.SetData(worldVerts);
        //memRenderer.material = memMat;

        cubeRenderer.material = memMat;
        memMat.SetBuffer("vertices", membraneVerts);

         }

        else if(oType == ObjectType.Mesh) {

            GameObject custom = GameObject.Find("dragon_remesh");
            custom.transform.localScale = custom.transform.localScale*2.0f;
            BVHComponent bvhComponent = custom.GetComponent<BVHComponent>();
            bvhComponent.computeShader = this.computeShader;
            custom.tag = "Simulation Object";


            MeshFilter customFilter = custom.GetComponent<MeshFilter>();
            MeshRenderer customRenderer = custom.GetComponent<MeshRenderer>();

            customFilter.mesh.RecalculateNormals();

            custom.transform.SetParent(GameObject.Find("GameObject").transform);

            membraneVerts = new ComputeBuffer(customFilter.mesh.vertices.Length, sizeof(float)*3);
            Vector3[] worldVerts = new Vector3[customFilter.mesh.vertices.Length];

             for(int i = 0; i < worldVerts.Length;i++)
            {
            worldVerts[i] = gameObject.transform.TransformPoint(customFilter.mesh.vertices[i]);
            }

             membraneVerts.SetData(worldVerts);
        //memRenderer.material = memMat;

            customRenderer.material = memMat;
            memMat.SetBuffer("vertices", membraneVerts);


            Debug.Log(" DRAGON HAS SPAWNED!");

            

        }

        else if(oType == ObjectType.None)
        {

        }
        


        

       
        InitializeParticles();
        CreateBuffers();
        InitShader();





        

       
    }

     void FixedUpdate()
    {

        Debug.Log($" Number of boundary particles { (((this.transform.localScale.x + smoothingRadius) * (this.transform.localScale.y + smoothingRadius) * (this.transform.localScale.z + smoothingRadius)) - (this.transform.localScale.x * this.transform.localScale.y * this.transform.localScale.z))/(4/3 * Mathf.PI * particleRadius*particleRadius*particleRadius) + particles.Length}");


        if(runSimulation)
        {

            stats.GetData(_stats);
            UpdateGUI(_stats[0],_stats[1],_stats[2]);
            computeShader.SetInt("LOWESTCELL", numberOfParticles*10);
            computeShader.SetInt("HIGHESTCELL", (int)-1);
            computeShader.SetFloat("SMOOTHING_RADIUS",smoothingRadius);
            computeShader.SetFloat("STIFFNESS", stiffness);
            computeShader.SetFloat("VISCOSITY", dynamicViscosity);
            computeShader.SetFloat("maxPressure", maxPressure);
            computeShader.SetFloat("DT",Time.deltaTime);
            computeShader.SetMatrix("localToWorld", transform.localToWorldMatrix);
            computeShader.SetMatrix("worldToLocal", transform.worldToLocalMatrix);

            computeShader.SetFloat("EPSILON", lj_epsilon); 
            

            computeShader.SetVector("HALF_BOUNDSIZE", domain.GetComponent<GLLines>().GetDimensions()/2);


             Vector3 numberOfGridCells = domain.GetComponent<GLLines>().GetDimensions()/smoothingRadius;

            int xDim = Mathf.CeilToInt(numberOfGridCells[0]);
            int yDim = Mathf.CeilToInt(numberOfGridCells[1]);
            int zDim = Mathf.CeilToInt(numberOfGridCells[2]);


            
            // ============ INVESTIGATE: MOST LIKELY CULPRIT FOR GC ======================
            // ============ performance gets worse with more particles 
            //cellCount = new int[xDim*yDim*zDim];
            // cellCount = new int[particles.Length];
            // cellTracker = new int[cellCount.Length];
          

            // if(CELLCOUNT != null) CELLCOUNT.Release();
            // if(CELLTRACKER != null) CELLTRACKER.Release();

            // CELLTRACKER = new ComputeBuffer(cellTracker.Length, sizeof(int));
            // CELLTRACKER.SetData(cellTracker);

            // CELLCOUNT = new ComputeBuffer(cellCount.Length, sizeof(int));
            // CELLCOUNT.SetData(cellCount);

            
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

    public void OnDestroy()
    {
        ReleaseBuffers();
    }

  
    void OnDrawGizmos()
    {

        // GameObject mem = GameObject.Find("Membrane");

        // Mesh m = mem.GetComponent<MeshFilter>().mesh;

        // for(int i = 0; i< m.vertices.Length;i++)
        // {
        //     Gizmos.color = Color.red;
        //     Gizmos.DrawSphere(m.vertices[i], 0.2f);
        //     Handles.Label(m.vertices[i] + new Vector3(0,1,0), i.ToString());
        // }
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

    void UpdateGUI(float vMax, float fMax, float pMax)
    {
        TMP_Text text = GameObject.Find("ParticleNumber").GetComponent<TMP_Text>();
        text.SetText(particles.Length.ToString());

        TMP_Text text1 = GameObject.Find("vMax").GetComponent<TMP_Text>();
        text1.SetText(vMax.ToString());

        TMP_Text text2 = GameObject.Find("fMax").GetComponent<TMP_Text>();
        text2.SetText(fMax.ToString());

        TMP_Text text3 = GameObject.Find("pMax").GetComponent<TMP_Text>();
        text3.SetText(pMax.ToString());
    }

    #endregion

    #region SetUp

    void InitializeParticles()
    {
        
        int particlesPerAxis = Mathf.CeilToInt(Mathf.Pow(numberOfParticles,1f/3f));
        int initParticlesKernel = computeShader.FindKernel("InitParticles");

        int groupsX = Mathf.Max(Mathf.CeilToInt(numberOfParticles/512.0f),1);

        // grab verts of membrane and turn them into a particle Array. Then merge fluid particles and membrane particles.
        // Then tell the init kernel which indices to skip (because here are the membrane vertices)

        int n = GameObject.FindGameObjectsWithTag("Simulation Object").Length;
        GameObject membrane = (GameObject.FindGameObjectsWithTag("Simulation Object").Length == 1) ? null:GameObject.FindGameObjectsWithTag("Simulation Object")[n-1];
        
        if(membrane != null)
        {
            Debug.Log("Membrane name " + membrane.name);
       

        Vector3[] worldVerts = new Vector3[membrane.GetComponent<MeshFilter>().mesh.vertices.Length];

        for(int i = 0; i<worldVerts.Length;i++)
        {
            worldVerts[i] = membrane.transform.TransformPoint(membrane.GetComponent<MeshFilter>().mesh.vertices[i]);
        }
        Vector3[] verts =Convert_Vertices_To_Local( worldVerts, gameObject.transform);

        Particle[] memParticles = new Particle[verts.Length];
        for(int i = 0; i<memParticles.Length;i++){

            memParticles[i].position = worldVerts[i];
            memParticles[i]._static = 1;
            memParticles[i].type = (constraints == AddConstraints.Yes) ? 2 : 1;

        }


        Triangle[] tris = Set_Triangles(membrane.GetComponent<MeshFilter>().mesh);
        Vertex[] vertexes = Set_Vertices(membrane.GetComponent<MeshFilter>().mesh);

        TRIANGLES = new ComputeBuffer(tris.Length, Marshal.SizeOf(typeof(Triangle)));
        VERTICES = new ComputeBuffer(vertexes.Length, Marshal.SizeOf(typeof(Vertex)));

        TRIANGLES.SetData(tris);
        VERTICES.SetData(vertexes);





        int UpdateKernel = computeShader.FindKernel("Update");
        computeShader.SetBuffer(UpdateKernel,"TRIANGLES", TRIANGLES);
        computeShader.SetBuffer(UpdateKernel,"VERTICES", VERTICES);
        

       

        // Add the corresponding buffers
        // dont forget to adjust the particle index number of the vertex!



        particles = new Particle[numberOfParticles + memParticles.Length];

        for(int i = 0; i<memParticles.Length;i++)
        {
            particles[i] = memParticles[i];
        }



         }

         else{
            particles = new Particle[numberOfParticles];

            Triangle[] tris = new Triangle[1];
            tris[0].a = -1;
            tris[0].b = -1;
            tris[0].c = -1;

            

            TRIANGLES = new ComputeBuffer(tris.Length, Marshal.SizeOf(typeof(Triangle)));
           
            TRIANGLES.SetData(tris);

              int UpdateKernel = computeShader.FindKernel("Update");
              computeShader.SetBuffer(UpdateKernel,"TRIANGLES", TRIANGLES);
            


         }

        PARTICLES = new ComputeBuffer(particles.Length, Marshal.SizeOf(typeof(Particle)));

        Debug.Log($" {particles.Length} particles occupy a space of {particles.Length} x {Marshal.SizeOf(typeof(Particle))} = {particles.Length * Marshal.SizeOf(typeof(Particle))}");

        PARTICLES.SetData(particles);

        computeShader.SetVector("HALF_BOUNDSIZE", domain.GetComponent<GLLines>().GetDimensions()/2);
        computeShader.SetBuffer(initParticlesKernel, "PARTICLES", PARTICLES);
        computeShader.SetFloat( "PARTICLE_RADIUS", particleRadius);
        computeShader.Dispatch(initParticlesKernel,groupsX,1,1);

        // cellCount = new int[particles.Length];
        // cellTracker = new int[cellCount.Length];

        //  CELLTRACKER = new ComputeBuffer(cellTracker.Length, sizeof(int));
        //     CELLTRACKER.SetData(cellTracker);

        //     CELLCOUNT = new ComputeBuffer(cellCount.Length, sizeof(int));
        //     CELLCOUNT.SetData(cellCount);
      


    }

    void InitializeConstraints()
    {

        // if mesh or primitive
        if (oType == ObjectType.None || constraints == AddConstraints.No) return;

        Mesh mesh = transform.GetChild(0).GetComponent<MeshFilter>().mesh;
        List<SPHConstraint> sph_cons = new List<SPHConstraint>();

        for (int i = 0; i < mesh.triangles.Length; i+=3)
        {   
            sph_cons.Add(new SPHConstraint(i,i+1,1));
            sph_cons.Add(new SPHConstraint(i+1,i+2,1));
            sph_cons.Add(new SPHConstraint(i+2,i,1));
        }



        SPHConstraint[] sph_cons_array = sph_cons.ToArray();

        ComputeBuffer CONSTRAINTS = new ComputeBuffer(sph_cons_array.Length, Marshal.SizeOf(typeof(SPHConstraint)));

        

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

    public void Run_Pause(){
        runSimulation = !runSimulation;
    }

    public void ChangeViewMode(int number)
    {

        if(number <= 6){

        
        visMode = number;
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
        if(membraneVerts!= null) membraneVerts.Release();
    }

    void CreateVirtualParticleBuffer()
    {

        int ppA         = Mathf.RoundToInt(smoothingRadius/particleRadius); // number of particles placed along one axis
        int array_size  = ppA*ppA*ppA; //number of particles inside the array

        Debug.Log($" Number of virtual particles in the stencil {array_size}");

        vPARTICLES = new ComputeBuffer(array_size,sizeof(float)*3);
        Vector3[] v_Particles = new Vector3[array_size];

        for(int x = 0; x<ppA;x++)
        {
            for(int y = 0; y<ppA;y++)
            {
                for(int z = 0; z<ppA;z++)
                {

                    int array_index = x*ppA*ppA+y*ppA + z;

                    v_Particles[array_index] = new Vector3(x,y,z)*particleRadius;

                }

            }
        }

        vPARTICLES.SetData(v_Particles);

        computeShader.SetBuffer(computeShader.FindKernel("ComputeDensity"),"vPARTICLES", vPARTICLES);
        computeShader.SetBuffer(computeShader.FindKernel("ComputeForces"),"vPARTICLES", vPARTICLES);
        computeShader.SetInt("v_ParticleNumber",array_size);
    



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

        // debug buffer for debugging force,velocity,pressure

            _stats = new float[3]{0,0,0};
            stats = new ComputeBuffer(1,sizeof(float)*3);
            computeShader.SetBuffer(computeShader.FindKernel("Update"),"stats",stats);

    
        

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

        CreateVirtualParticleBuffer();
        
        
    }

    #endregion

    #region dispatchKernels

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
       
      
        
        int groupsX = Mathf.Max(Mathf.CeilToInt(numberOfParticles/threadNum),1);

      
        computeShader.Dispatch(UpdateKernel,groupsX,1,1);

       

       
       



    }

    

    void DispatchClearGrid()
    {
        int ClearGridKernel = computeShader.FindKernel("ClearGrid");

       

        CELLCOUNT.SetData(cellCount);
        CELLTRACKER.SetData(cellTracker);

       
            computeShader.SetBuffer(ClearGridKernel, "CELLCOUNT",  CELLCOUNT);
            computeShader.SetBuffer(ClearGridKernel, "CELLTRACKER",  CELLTRACKER);
           

        
          
        
        
        int groupsX = Mathf.Max(Mathf.CeilToInt(cellCount.Length/threadNum),1);
        
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
        int groupsX = Mathf.Max(Mathf.CeilToInt(particles.Length/64.0f),1);
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
        int groupsX = Mathf.Max(Mathf.CeilToInt(cellCount.Length/threadNum),1);
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
        
        
        int groupsX = Mathf.Max(Mathf.CeilToInt(particles.Length/threadNum),1);
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
        
       
        int groupsX = Mathf.Max(Mathf.CeilToInt(particles.Length/threadNum),1);
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

        int groupsX = Mathf.Max(Mathf.CeilToInt(particles.Length/threadNum),1);

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

        int groupsX = Mathf.Max(Mathf.CeilToInt(particleMap.Length/64.0f),1);

        computeShader.Dispatch(pressureKernel, groupsX,1,1);
    }

    void DispatchCons()
    {
         int ConnectionKernel = computeShader.FindKernel("ConstraintSolver");
        computeShader.SetBuffer(ConnectionKernel, "CONNECTIONS", CONNECTIONS);
        int groupsX = Mathf.Max(Mathf.CeilToInt(cons.Length/threadNum),1);

        
        computeShader.Dispatch(ConnectionKernel,groupsX,1,1);
    }
    #endregion

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


   
  #region rendering_functions
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


    public void ChangeParticleRenderSize(float stepSize)
    {
        float size = renderMaterial.GetFloat("_SizeMul");
        renderMaterial.SetFloat("_SizeMul", size + stepSize);

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

        float et = Time.time;
       
    }

    
    #endregion

    int Hash(Vector3 position, float numberOfItems)
{
    int xi = Mathf.FloorToInt(position.x);
    int yi = Mathf.FloorToInt(position.y);
    int zi = Mathf.FloorToInt(position.z);

    long hash = (xi * 92837111) ^ (yi * 689287499) ^ (zi * 2839923481);
    int h = Mathf.RoundToInt(Mathf.Abs(hash) % numberOfItems);

    return h;
}

    #region mesh_generation
    Mesh Generate_Membrane(float h, int resolution){

        // x,y resolution of box
        // membrane should be fixed to the box
        Mesh m = new Mesh();

        Vector3 dims = domain.transform.localScale;
        Vector3 origin = domain.transform.position - dims/2;

        Vector3 gridDims = dims/resolution;

        int xDims = Mathf.RoundToInt(gridDims.x);
        int yDims = Mathf.RoundToInt(gridDims.y);

        float x_step = dims.x/xDims;
        float y_step = dims.y/yDims;

        Debug.Log($"Dimensions {xDims} {yDims} {gridDims} {dims} {resolution}");


        List<Vector3> verts = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        

        for(int x = 0; x <= resolution;x++){
            for(int y = 0;y<=resolution;y++)
            {
                verts.Add(origin + new Vector3(x*gridDims.x,UnityEngine.Random.Range(0.8f*h,1.2f*h),y*gridDims.y));
            }
        }

        // generate tris

        int yy = 0;


     

        
        for(int y = 0; y < resolution;y++)
        {

            for(int x = 0; x< resolution;x++)
            {

          

                //tri 1:
                int a = y*(resolution+1) + x;
                int b = y*(resolution+1) + x + 1;
                int c = (y+1)*(resolution+1) + x + 1;
                int d = (y+1)*(resolution+1) + x; //!!!!!!!!!! equals b for some reason.v v

                tris.Add(a);
                tris.Add(b);
                tris.Add(c);

           

                tris.Add(a);
                tris.Add(c);
                tris.Add(d);
                

                Debug.Log($"A: {a} B:{b} C:{c} D:{d} X: {x} Y:{y}");


                 
                 }



           
        }
        
        foreach(var p in tris)
        {Debug.Log(p);}
        m.vertices = verts.ToArray();
        m.triangles= tris.ToArray();


        return m;
    }

    Vector3[] Convert_Vertices_To_Local(Vector3[] verts, Transform transform)
    {
        Vector3[] converted = new Vector3[verts.Length];

        for(int i = 0; i< verts.Length;i++)
        {

            converted[i] = transform.InverseTransformPoint(verts[i]);
        }

        return converted;

    }

    Triangle[] Set_Triangles(Mesh mesh)
    {
        List<Triangle> tris = new List<Triangle>();
        Vector3[] verts = mesh.vertices;
        int[] triangles = mesh.triangles;

       

        for(int i = 0; i<triangles.Length;i+=3)
        {

            //Debug.Log($" number of triangles {triangles.Length/3} number of normals {mesh.normals.Length} current triangle {i}");
            Triangle angle = new Triangle();
            angle.a = triangles[i];
            angle.b = triangles[i+1];
            angle.c = triangles[i+2];

            angle.center = (verts[angle.a] + verts[angle.b] + verts[angle.c])/3;
            angle.normal = -1*Vector3.Cross(verts[angle.c]-verts[angle.a],verts[angle.b] - verts[angle.a]);
            //if (i % 2 == 0.0f) angle.normal *= -1; 
            tris.Add(angle);
        }

        Triangle[] triArray = tris.ToArray();

        return triArray;
    }

    Vertex[] Set_Vertices(Mesh mesh)
    {
        Vertex[] vertices = new Vertex[mesh.vertices.Length];

        int tricount = 0;
        for(int i = 0; i<mesh.triangles.Length;i+=3){
            
        
            int[] indices = new int[] {mesh.triangles[i], mesh.triangles[i+1],mesh.triangles[i+2]};

            for(int j = 0; j <= 2; j++)
            {
                int idx = indices[j];
                
                //Debug.Log($" vertices Length {vertices.Length} idx is {idx} i is {i}");
               

                int n = vertices[idx].numberOfTris;

                switch(n){

                    default:
                    vertices[idx].a = tricount;
                    break;

                    case 1:
                    vertices[idx].b = tricount;
                    break;

                    case 2:
                    vertices[idx].c = tricount;
                    break;

                    case 3:
                    vertices[idx].d = tricount;
                    break;

                    case 4:
                    vertices[idx].e = tricount;
                    break;

                    case 5:
                    vertices[idx].f = tricount;
                    break;
                }

                
                vertices[idx].pIndex = idx;
                vertices[idx].numberOfTris +=1;
            }
            Debug.Log("============================================================");
            tricount++;

        }

        return vertices;




    }
    #endregion


        

}

#region structs

public struct Particle
{

    public Vector3  color;              // Color of the particle for debugging
    public Vector3  position;           // Particle position
    public Vector3  velocity;           // Particle Velocity
    public Vector3  offset;             // Forces applied to the particle
    public Vector3 predictedPosition;   // Predicted Position
   

    
    public float pressure;              // Pressure at particle location
    public float density;               // current density at the particle location
    public float radius;
    public float mass;

    public int hash;
    public int index;
    public int _static;                 // is the particle static

    public int type;                    // 0 - Boundary, 1 - Fluid, 2 - Elastic
   

   

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

public struct SPHConstraint
{
    public int a;
    public int b;

    public float distance;

    public SPHConstraint(int a, int b, float distance)
    {
        this.a = a;
        this.b = b;
        this.distance = distance;
    }

}


public struct Vertex{
    
    public int a,b,c,d,e,f,g;
    public int numberOfTris;

    public int pIndex;
}

public struct Triangle{

    public int a,b,c,hash;  //indices of particles forming the triangle

    public Vector3 center; //center position of the triangle;
    public Vector3 normal; // normal direction of the triangle;

}

#endregion


