#ifndef SPH_KERNELS_INCLUDED
#define SPH_KERNELS_INCLUDED

float SpikeyKernel(float r, float h, float pi)
{

  

        if( r>= 0 && r<= h)
        {
          return 15/(pi * (h*h*h*h*h*h))
                  * ((h- r)*(h - r)*(h - r));

                  
        }
        else{
          return 0;
        }
        
    

  

}



    

       // Doyub Kim page 130
float SpikeyKernelFirstDerivative(float r, float h, float pi)
  { 
    
       if(r <= h)
       {
        float scale = 45/(pow(h,6)*pi);
        float v = h-r;
        return -v*v*scale;
       }

       return 0;
    
      

      
  }


 float SpikeyKernelLaplacian(float distance,float cellSize,float radius,float pi)
{

  if(distance < cellSize)
    {
        float x = 1.0f - distance/radius;

        return 90.0f /(pi*(radius*radius*radius*radius*radius) * x);
    }

    else{
        return 0;
    }

}

float StdKernel(float distanceSquared, float r, float pi)
{
    // Doyub Kim
    float x = r - distanceSquared / (r*r);
    return 315.f / ( 64.f * pi * (r*r*r) ) * x * x * x;
}


 float WPoly6Kernel(float r,float h,float pi)
{

  


  if(r < h)
  {
    float x = (315/(64*pi*pow(abs(h),9)));
     return  x * ((h*h - r*r)*(h*h - r*r)*(h*h - r*r));
  }

  return 0;
          
           
          

          
          
   
            
         

         

}

 float WPoly6Kernel_Bindel2011(float r,float h,float pi)
{

  float x = 4/(pi*(h*h*h*h*h*h*h*h));
          if(r>= 0 && r <= h )
          {
            return  x *
                    ((h*h - r*r)*(h*h - r*r)*(h*h - r*r));
          }

          else{
            return x*0;
          }
   
            
         

         

}


 float ViscosityKernel(float r,float h,float pi)
{
  
  if(r >= 0 && r <= h)
    {
        return (15.0f/2*pi*(h*h*h)) * (
            (-(r*r*r)/(2*h*h*h)) 
             +(r*r)/ (h*h)
             + (h/2*r)
             -1);
    }
    else{
        return 0;
    }

}


 float ViscosityKernelLaplacian(float r, float h)
{
  
    if (r <= h)
    {
        float h3 = h * h * h;
        float h6 = h3 * h3;
        return 0.01f*(45.0f / (3.14159265359f * h6) * (h - r));
    }
    return 0.0f;

}

#endif