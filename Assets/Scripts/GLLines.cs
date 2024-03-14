using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class GLLines : MonoBehaviour
{
   // When added to an object, draws colored rays from the
	// transform position.
    
	public Vector3 dimensions;

	static Material lineMaterial;
    public readonly int[] bbox_triangles = { 
        
                            0,1,2,
                            3,2,0,
                            
                            4,5,6,
                            7,6,4,
                            
                            0,1,5,
                            4,5,0,
                            
                            3,2,6,
                            7,6,3,
                            
                            0,3,7,
                            4,7,0,
                            
                            1,2,6,
                            5,6,1};
   

    Vector3[] cornerPoints;

    Vector3 gravityPointer;
    public float size;

    public /// <summary>
    /// Callback to draw gizmos that are pickable and always drawn.
    /// </summary>
    void OnDrawGizmos()
    {
        

       Vector3 cell = this.transform.position/size;
       
       uint xi = (uint) Mathf.FloorToInt(cell.x);
       uint yi = (uint) Mathf.FloorToInt(cell.y);
       uint zi = (uint) Mathf.FloorToInt(cell.z);


       uint h = ((xi)*9283711) ^ ((yi) * 689287499) ^ ((zi) *283923401) ;
       float idx = Mathf.Abs(h) % 3000;

        Handles.Label(transform.position + new Vector3(0,transform.localScale.y + 3,0)," Object is located in grid Cell " + idx);
         
         Gizmos.color = Color.blue;
         Gizmos.DrawWireCube(new Vector3(xi,yi,zi), new Vector3(size,size,size));
                
      
        //Gizmos.DrawWireSphere(transform.position, size);
       //Gizmos.DrawLine(transform.position, new Vector3(xi,yi,zi) * size);
       

        



        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + new Vector3 (0,-9.81f,0),.5f);

        
        
        Gizmos.DrawLine(transform.position,transform.TransformDirection(new Vector3(0,-9.81f* transform.localScale.y,0) ));
        Gizmos.color = Color.magenta;

        Vector3 localDir = transform.InverseTransformDirection( new Vector3(0,-9.81f,0) );
         Gizmos.DrawLine(transform.position,transform.position + transform.TransformPoint(localDir) );
        
        //Gizmos.DrawSphere(transform.InverseTransformPoint(new Vector3(0,-9.81f + transform.localScale.y,0) ),.5f);

    }
	static void CreateLineMaterial ()
	{
		if (!lineMaterial)
		{
			// Unity has a built-in shader that is useful for drawing
			// simple colored things.
			Shader shader = Shader.Find ("Hidden/Internal-Colored");
			lineMaterial = new Material (shader);
			lineMaterial.hideFlags = HideFlags.HideAndDontSave;
			// Turn on alpha blending
			lineMaterial.SetInt ("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			lineMaterial.SetInt ("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			// Turn backface culling off
			lineMaterial.SetInt ("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
			// Turn off depth writes
			lineMaterial.SetInt ("_ZWrite", 0);
		}
	}

	// Will be called after all regular rendering is done
	public void OnRenderObject ()
	{
        dimensions = transform.localScale;
		Draw();
		
	}

    public Vector3 GetDimensions()
    {


        
        return this.transform.localScale;
    }

   

    void Draw()
    {

        CreateLineMaterial ();
		// Apply the line material
		lineMaterial.SetPass (0);

		GL.PushMatrix ();
		// Set transformation matrix for drawing to
		// match our transform
		//GL.MultMatrix (transform.localToWorldMatrix);

		// Draw lines
		GL.Begin (GL.LINES);

        Quaternion rotation = transform.rotation;

        Vector3 v1 =  new Vector3(-.5f, -.5f, -.5f);
        Vector3 v2 = v1 + new Vector3(1,0,0);
        Vector3 v3 = v1 + new Vector3(1,1,0);
        Vector3 v4 = v1 + new Vector3(0,1,0);

        Vector3 v5 = v1 + new Vector3(0,0,1);
        Vector3 v6 = v5 + new Vector3(1,0,0);
        Vector3 v7 = v5 + new Vector3(1,1,0);
        Vector3 v8 = v5 + new Vector3(0,1,0); 



        if(cornerPoints == null)
        {
            cornerPoints = new Vector3[] {v1,v2,v3,v4,v5,v6,v7,v8};
        }


        Vector3[] worldPos = new Vector3 []
        {
            transform.TransformPoint(cornerPoints[0]),
            transform.TransformPoint(cornerPoints[1]),
            transform.TransformPoint(cornerPoints[2]),
            transform.TransformPoint(cornerPoints[3]),

            transform.TransformPoint(cornerPoints[4]),
            transform.TransformPoint(cornerPoints[5]),
            transform.TransformPoint(cornerPoints[6]),
            transform.TransformPoint(cornerPoints[7]),
        };


        for(int i = 0; i < worldPos.Length; i++)
        {

            
            if(i == 3 || i == 7)
            {

           
            GL.Vertex3(worldPos[i].x,worldPos[i].y,worldPos[i].z);
            GL.Vertex3(worldPos[i-3].x,worldPos[i-3].y,worldPos[i-3].z);
            }

            else 
            {
            
          
            GL.Vertex3(worldPos[i].x,worldPos[i].y,worldPos[i].z);
            GL.Vertex3(worldPos[i+1].x,worldPos[i+1].y,worldPos[i+1].z);
            }
            
        }






         for(int i = 0; i < 4; i++)
        {   

           
            GL.Vertex3(worldPos[i].x,worldPos[i].y,worldPos[i].z);
            GL.Vertex3(worldPos[i+4].x,worldPos[i+4].y,worldPos[i+4].z);


            

            

        }



		GL.End ();
        GL.PopMatrix ();

    }


    // Add handles to the faces, distance between mouseposition and screenposition of the handle determines which face is to be moved

    // determine direction, face direction.
}
