


#  Smoothed Particle Hydrodynamics 

> Water can flow or it can crash. Be water, my friend - Bruce Lee

<p align = "justify">This repository is part of a project aiming to build an interactive biophysical simulation of <i>C. Elegans</i> and its locomotion behaviour. And especially, it's a great sandbox to unite a passion for neurobiology, biophysics, computer graphics and film :-). <br>

Here we toyed around with a lagrangian, particle-based simulation technique for fluids, which at some point should represent the liquid environment a virtual worm or some other virtual parasite inhabits.

Groups and Institutions involved include the Biomedical physics group of Prof. Dr. Kollmannsberger at the HHU Düsseldorf and the department of microscopy at the faculty of biology of the JMU Würzburg led by Prof.Dr. Christian Stigloher



## Table of Contents

<div align="center">
<ol >
<b>
<li>C. Elegans Connectomics</li>
<li> The Matrix for Worms
<li>Applications of Smoothed Particle Hydrodynamics
<li>A brief introduction to SPH
<li>Implementation in Unity3D
<li>Results
<li> Further reading and sources
<li> Further resources
<li> Disclaimer
</b>
</ol>
</div>

## C. Elegans connectomics

<div align="justify">
<i>C. Elegans</i>, measuring a mere 1mm in length, resides in the microscopic world of decaying fruit or soil. Biologists find its 959 somatic cells intriguing, particularly its 300 neurons comprising the nervous system and 95 body wall muscles governing locomotion.

Despite its modest nervous system compared to humans, <i>C. elegans</i> exhibits diverse behaviors, thanks to its mapped neural connections, available since 1986 (White et al, 1986). Now, with advanced computing power, we're exploring the possibility of simulating this system to understand real nervous systems better. Thus we're asking: Can we simulate this tiny system? What can it teach us about real nervous systems? And can it aid experimentalists in predicting and exploring new hypotheses?

While previous researchers have tackled these questions, today's tech landscape broadens the scope. If you're interested in collaborating for open science, feel free to reach out :-)
</div><br>

<div align="center">
<img src="/images/title_loop0001-0800-ezgif.com-video-to-gif-converter (1).gif">
</div><br>

## The Matrix for Worms

<div align="justify">

As locomotive behaviour requires sensing the environment as well as interior states of the worms body we're trying to combine a simplified biophysical model of the worms body, e.g. a mass-spring-model with a particle-based simulation of its surrounding environment.

In different environments, like agar or water, we observe distinct locomotion patterns. Agar yields the classic crawling motion, while water prompts swimming motions. We can differentiate them by analyzing body curvature wavelengths and beating frequencies.

<div align="justify">
    <div float="left">
        <img src="/images/crawling_loop0001-0025.gif"/>
        <p>Crawling locomotion</p>
    </div>
    <div float="left">
        <img src="/images/swimming_loop0001-0025.gif"/>
        <p> Swimming locomotion </p>
    </div>
</div>

 

</div>

One goal of an connectome-base integrated biophysical model would be to study if such a model is capable of sensing changes in environment, eg. the viscosity and adapt behaviour accordingly and, if it does so, how close it resembles the behaviour of the actual animal.

## Applications of SPH

 <div align="justify">

Originally developed in the 1970s to model processes in astrophysics, the Smoothed Particle Hydrodynamics (SPH) algorithm found its way into computer graphics, particularly in simulating fluid behavior. It gained significant traction in industries like film, visual effects (VFX), and video games, enabling real-time fluid simulations.

For instance, in <i>The Return of the King</i>, you might recall the dramatic scene where Gollum meets his demise in a sea of lava—a stunning visual brought to life using the SPH algorithm.

Fast forward to 2024, and while new approaches have emerged, many still build upon SPH, enhancing its original capabilities for more precise and stable results. Other methods like Lattice Boltzmann methods, Eulerian fluid simulation, and particle-based methods such as Position-Based Fluids (PBF) and Material Point Method (MPM) offer different strengths, like efficiency for complex flows or handling fluid-solid interactions. Yet, SPH remains popular due to its versatility, particularly in real-time applications and its ability to capture complex fluid behaviors with relatively straightforward implementations. But sometimes, it's best to start with the basics, not just for mastering concepts but also because, well, it's just plain fun. 

 ## A brief introduction to SPH


 <p align="justify"> 
In Smoothed Particle Hydrodynamics (SPH), we break down a fluid volume into a collection of particles, each with its own set of properties like reference density, velocity, pressure, and position.

During each simulation timestep, we update the position of each particle. First, we estimate the density at each particle's location to derive the pressure. Then, we calculate the pressure and viscous forces acting on the particle to determine the change in velocity. Finally, this velocity change determines the particle's new position for the next timestep.
 
 </p>

<div align="center">
</br>
 <img src="images/simulation_simplified.PNG"/>
 
 </div>
</br>
<div align="justify">
 For an SPH simulation, you need:

<div align="center">

<ol>
</br>
<li>A domain or grid.
<li>A bunch of particles.
</ol>
</div> <br>


Once you have these, you can perform the following steps: <br>
<br>
<div align="center">
<ol>

<li>Hashing
<li>Neighborhood
<li>Density estimation
<li>Pressure estimation
<li>Computation of forces
<li>Integration
</ol>
</div>
<br>

The domain keeps particles bounded and is divided into grid cells, reducing the number of comparisons during density estimation (steps 1 and 2).

To keep particles within the domain, their positions are compared to boundary dimensions. If a particle exceeds these dimensions, its position is reset, velocity reversed, and a damping factor applied to lose energy upon bouncing off the wall.

Density is measured by summing neighboring particles' densities within a smoothing radius (equal to the grid cell size), weighted by distance.

Different smoothing kernels are applied for density estimation, pressure calculation, and viscous forces (see Müller et al, 2003).

During the final integration we use eulers' method, as its implementation is very straight forward, especially for biologists turning towards computer science. Other integration schemes however, may yield numerically more stable results (e.g. Verlet-integration etc) and we'll implement these in the future as well.

Particle-based methods like SPH enable parallel processing on the GPU, speeding up simulations by processing particles simultaneously. GPUs, integral to computer graphics, handle rendering tasks efficiently, utilizing shaders for pixel processing. In Unity, we can use a "Compute Shader" to assign different data (instead of pixels) to a single thread and thus process big data, such as particles, in parallel.

In Unity, this parallel processing is facilitated by Compute Shaders, allowing SPH simulations to run on the GPU.
 </p>

 </div>

 ## Implementation in Unity3D

  <div align="justify">

  For this implementation we require a few ingredients, namely:
  <ul>
  
 
  <li> Particle system (simulation parameters, initialization of particles and SPH settings, Initialization and dispatch of ComputeShader kernels)
  <li> Domain (transform of the simulation space, rendering the domain boundaries using GL-Lines)
   
  
  <li> Compute Shader (SPH algorithm, in parallel for each particle, GPU)
  <li> Shaders (Billboard shader to render particles as circles facing the camera, particle shader and material to render the particles on screen)
  </ul>
  </div>

  The SPH-Algorithm itself is performed by a ComputeShader, where each particle in a particle system is assigned a thread on the GPU. Each step in the procedure has its own shader kernel, which must be dispatched during the game loop on the CPU side.

  To do so, we need a CPU-side particle system, that lets the user configure the number of particles, physical properties and sets up the required buffers for the GPU and connects them to the SPH compute shader.

  Last but not least a domain is required. We use the GL-Lines library to draw the box on screen. The domains transform properties (e.g. scale, rotation and translation) is then connected to the compute shader and updated every time the user adjusts the transform. Therefore enabling the simulation to react in real-time to changes to the domain.

  In order to actually SEE our particles doing something a shader and a material is required, which is applied to the object holding the particle-system component. I will skip the details for now, but we basically take the particle buffer and during each render step in the <b>OnRender()</b>-method the <i>Graphics</i>-class is used to render all the particles as sprites at their corresponding locations.

  



 ## Results

 <div align="justify">

 <p align="justify"> Currently we got a basic interactive implementation of the SPH algorithm to run in Unity3D using ComputeShaders and GPU processing. It is still a little buggy, yet the domain can be moved, rotated, squished or made larger with the fluid behaving accordingly.
 
 In the following images you can see it in action. The animations you see are not pre-rendered but captured in real-time on a Windows PC and a Nvidia GeForce RTX 2080 GPU</p><br>
 
 <p float="left" align="center">
 <img src="/images/sph_1-ezgif.com-video-to-gif-converter.gif"/>
 <img src="/images/sph_2-ezgif.com-video-to-gif-converter.gif"/>
 <img src="/images/sph_3-ezgif.com-video-to-gif-converter.gif"/>
 </p> <br>
 
 <p> In future, we'd like to add physical bodies interacting with the fluid, namely a mass-spring-representation of a worms' body, like in previous works by Palyanov et. al (2012), but adapted to an interactive environment </p>
 </div>

 ## Further Reading
 
 SPH in astrophysics, R.A. Gingold, J.J Monaghan

 <b>R. A. Gingold, J. J. Monaghan, Smoothed particle hydrodynamics: theory and application to non-spherical stars, Monthly Notices of the Royal Astronomical Society, Volume 181, Issue 3, December 1977, Pages 375–389, https://doi.org/10.1093/mnras/181.3.375</b>

 Paper on SPH in interactive applications by Matthias Müller:

 <b>Matthias Müller, David Charypar, and Markus Gross. 2003. Particle-based fluid simulation for interactive applications. In Proceedings of the 2003 ACM SIGGRAPH/Eurographics symposium on Computer animation (SCA '03). Eurographics Association, Goslar, DEU, 154–159.</b>

 <i>Sibernetic</i> a particle-based simulation engine for biophysical simulation of <i>C. Elegans</i> locomotion. Based on the PCISPH method, but not in an interactive Setting.

  <b>Palyanov A, Khayrulin S, Larson SD, Dibert A. Towards a virtual C. elegans: a framework for simulation and visualization of the neuromuscular system in a 3D physical environment. In Silico Biol. 2011-2012;11(3-4):137-47. doi: 10.3233/ISB-2012-0445. PMID: 22935967.</b>

## Other ressources

In case you're interested in computer graphics and simulations, Matthias Müller hosts a series of short video lectures on the principles and implementation of various simulation techniques.

>https://matthias-research.github.io/pages/tenMinutePhysics/index.html

Also, heres a pretty cool video by Sebastian Lague on the simulation of fluids. Here he deals with the implementation of SPH as well.

> https://www.youtube.com/watch?v=rSKMYc1CQHE

## Disclaimer

<div align="justify">
<p>The texts published here were drafted by the author, but adapted and made more concise using ChatGPT.</p>
</div>






 
