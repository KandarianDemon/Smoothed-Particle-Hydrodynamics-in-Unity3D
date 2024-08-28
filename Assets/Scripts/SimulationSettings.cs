using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;





[CreateAssetMenu(fileName = "SimulationSettings", menuName = "SPH/SimulationSettings", order = 0)]
public class SimulationSettings : ScriptableObject 
{

    public int numberOfParticles;

    
    public float particleRadius;
    public float smoothingRadius;

    public float timestep;

    public float stiffness;
    
}