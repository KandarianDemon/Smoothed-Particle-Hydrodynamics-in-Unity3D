using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Simulation;

[CreateAssetMenu(fileName = "RenderSettings", menuName = "SPH/RenderSettings", order = 0)]
    public class RenderSettings : Settings
    {

        public SimulationObjectType objType;           //0 - BOUNDARY    1-FLUID    2-BODY
        public float density;       // Density of the object
        public float viscosity;     // Viscosity of the object
        public Color color;         // Color of the particles
        public bool isStatic;       // wether the particles are static or not.
    }
