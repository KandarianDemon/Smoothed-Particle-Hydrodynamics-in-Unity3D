using UnityEngine;
using System.Collections;
using System.Collections.Generic;



public class ParticleWorld : MonoBehaviour {


        // Manages the Scene To Particle Translation.
        // Manages Particle Data.


        // Scene Organisation                        : Maybe make that its own class. SceneManager -> ParticleWorld -> Simulator.
        // |_______DOMAIN                            : Provides the simulation Space and acts as parent.
        //              |_______ Fluid1              : Carries Fluid Settings                           Object Settings only affect Particle settings like type and physical properties.
        //              |_______ Fluid2              : Carries different FluidSettings                      
        //              |_______ Object1             : Object Settings -Rigid, Soft, fixed, moveable.   
        //              |_______ Worm1               : Contractible Matter                              

        // SceneManager: Basically a List of Objects. Add/Remove Objects. Transform can be accessed through the object itself.



        //  ParticleWorld : Store number of particles per Object, startIndex in particleArray. Objects can be accessed and manipulated in the particledata[];
        //  Voxelize each Object. Keep Volume, Fixed Grid:
        //                Bounds to grid. Check each cell if inside mesh.
        //                Cellsize = smoothingradius or precomputed equilibrium distance.
        //  Voxelizer returns array of Particles, maybe in local space: Conversion to domain-space/ world space might be required.
        //  
        //  




}