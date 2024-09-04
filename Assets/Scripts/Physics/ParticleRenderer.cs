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

using PhysicsComputeShaderUtility;

namespace Simulation
{

    
    public class ParticleRenderer : MonoBehaviour 
    {

        public Material renderMaterial;
        MeshRenderer renderer;
        MeshFilter filter;

        ComputeBuffer argsBuffer;
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        public Mesh particleMesh;



        public Awake(){
            
        }




        
    }


}