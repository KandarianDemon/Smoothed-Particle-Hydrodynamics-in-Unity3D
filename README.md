#  Smoothed Particle Hydrodynamics 

> Water can flow or it can crash. Be water, my friend - Bruce Lee

## Virtual _C. Elegans_ models 
<p align = "justify">This repository is part of a project aiming to build an interactive biophysical simulation of _C. elegans_ and its locomotion behaviour.

As locomotive behaviour requires sensing the environment as well as interior states of the worms body we're trying to combine a simplified biophysical model of the worms body, e.g. a mass-spring-model with a particle-based simulation of its surrounding environment.

One simple observable locomotive behaviour in relation to the viscosity of the environment is forward and backwards locomotion. In viscous environments, like agar, the characteristic crawling motion pattern can be observed, whereas in less viscous environments, like water, swimming motions are displayed.

Both can be distinguished through the wavelengths of the body curvature traversing the body, as well as the beating frequency.

<img src="/images/crawling_loop0001-0025.gif">
<img src="/images/swimming_loop0001-0025.gif">


 </p>

  ## Applications of SPH

 <p align="justify"> Originally developed to simulate processes in Astrophysics in the 1970s it gained popularity in computer graphics as a means to simulate the behaviour of fluids. It has been especially popular in the film, vfx and video game industries, as it enables the simulation of fluid behaviour in real-time.
 
 If you've seen <i>The Return of the King</i> you may remember Gollum drowning in a sea of lava after following the One ring to its doom. That lava simulation is based on the SPH algorithm.

As of 2024 new approaches exist of course, many based on SPH but with improvements to the original algorithm, leading to more accurate and stable simulation results. However, to get into interactive fluid simulations we thought it a good idea to start with the basics to get familiar with some of the concepts. </p>

 ## A brief introduction to SPH

 > I'm singing in the rain. Just singing in the rain. What a glorious feeling. I'm happy again - Gene Kelly
 <p align="justify"> In <i>Smoothed Particle Hydrodynamics</i> a fluid volume is discretized in a set of particles. These particles carry a <i>reference density, velocity, pressure </i> and <i>position</i>. 

 In each timestep of the simulation, each particles position is updated, by calculating estimating the density at the particles location and thus deriving the pressure. Then pressure forces and viscous forces acting on the particle are used to calculate the change in velocity, which then determines the new position at the next timestep. </p>

 <img src="images/simulation_simplified.PNG"/>

 So, for an SPH simulation as you can find it here, one needs two things:

 <ol>
 <li> A domain or grid</li>
 <li> A bunch of particles</li>
 </ol>
<div style="padding: 1rem; margin:1rem">
 <img src="/images/grid_comp.png"/>
 </div>

 The <i>domain</i> keeps the particles bounded and is divided into a set of grid cells. This is important to reduce the number of particles the algorithm has to compare during density estimation. In order to keep the particles within the domain the position of a particle is compared to the boundary dimensions, so for instance, if a particles' x-dimensions exceeds the x-dimension of the boundary, the particles x-position is reset, the velocity in that direction reversed and a damping factor applied, so that the particle loses energy upon bouncing off the wall.

 The <i>density</i> is measured by iterating over all the neighbouring particles within a Smoothing Radius (equal to the grid cell size) and summing up their local densities multiplied with a weighting factor depending on the distance to the particle of interest.
 
 For density estimation as well as the calculation of the pressure and viscous forces different <i> Smoothing Kernels</i> or their derivatives are applied. 
 
 One advantage of particle-based fluid simulation methods is, that they allow for parallel processing on the GPU. So instead of processing each particle in sequence (and you can get quite high numbers of particles in your simulation), you can process them in parallel simulataneously.

 Not surprisingly is the GPU or <i>Graphics Processing Unit</i> an integral part to computer graphics and is responsible for most of the things you see on your screen. If you want to see a green orb on screen, you take your geometric information (vertices and edges), describing the sphere and then you pass it to a <i>Shader</i>-script, describing <i>how</i> the Sphere should look like. Shaders most commonly use the GPU, where each pixel on the screen is processed in parallel.

 In Unity we can use that to move our particle simulation to the GPU as well, a so-called <i>Compute Shader</i>
 </p>


 ## Results thus far

 <p align="justify"> Currently we managed a basic interactive implementation of the SPH algorithm in Unity3D using ComputeShaders and GPU processing. It is still a little buggy, yet the domain can be moved, rotated, squished or made larger with the fluid behaving accordingly.
 
 In the following images you can see it in action. The animations you see are not pre-rendered but captured in real-time on a Windows PC and a Nvidia GeForce RTX 2080 GPU</p>
 
 <p float="left">
 <img src="/images/sph_1-ezgif.com-video-to-gif-converter.gif"/>
 <img src="/images/sph_2-ezgif.com-video-to-gif-converter.gif"/>
 <img src="/images/sph_3-ezgif.com-video-to-gif-converter.gif"/>
 </p>

 ## Further Reading
 
 SPH in astrophysics, R.A. Gingold, J.J Monaghan

 <b>R. A. Gingold, J. J. Monaghan, Smoothed particle hydrodynamics: theory and application to non-spherical stars, Monthly Notices of the Royal Astronomical Society, Volume 181, Issue 3, December 1977, Pages 375–389, https://doi.org/10.1093/mnras/181.3.375</b>

 Paper on SPH in interactive applications by Matthias Müller:

 <b>Matthias Müller, David Charypar, and Markus Gross. 2003. Particle-based fluid simulation for interactive applications. In Proceedings of the 2003 ACM SIGGRAPH/Eurographics symposium on Computer animation (SCA '03). Eurographics Association, Goslar, DEU, 154–159.</b>

## Other ressources

In case you're interested in computer graphics and simulations, Matthias Müller hosts a series of short video lectures on the principles and implementation of various simulation techniques.

>https://matthias-research.github.io/pages/tenMinutePhysics/index.html

Also, heres a pretty cool video by Sebastian Lague on the simulation of fluids. Here he deals with the implementation of SPH as well.

> https://www.youtube.com/watch?v=rSKMYc1CQHE




 
