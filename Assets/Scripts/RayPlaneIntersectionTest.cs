using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayPlaneIntersectionTest : MonoBehaviour
{

    // Outline:

    // RayCast from camera.

    // Find intersection of ray with Quad.
    // Loop over Quad triangles.
    //




    public Camera cam;
    public GameObject quad;
    GameObject intersectionPoint;

    MeshFilter filter;
    Vector3[] verts;
    int[] tris;

    
    void OnDrawGizmos()
    {

        if(cam == null)
        { 
            return;
        }

        
        
    }

    // Start is called before the first frame update
    void Start()
    {
        filter = quad.GetComponent<MeshFilter>();

        verts = new Vector3[filter.mesh.vertices.Length];
        tris = new int[filter.mesh.triangles.Length];
        intersectionPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        intersectionPoint.name = "pointer";
        intersectionPoint.transform.localScale *= 0.4f;
        intersectionPoint.SetActive(false);


        verts = filter.mesh.vertices;
        tris = filter.mesh.triangles;

    }

    // Update is called once per frame
    void Update()
    {

        // Grab screenpoint, convert to worldpoint.
        float t = 1000.0f;

        Vector3 mousePosition = Input.mousePosition;

        Ray ray = cam.ScreenPointToRay(mousePosition);
        Color col = Color.black;

        for(int i = 0; i< tris.Length;i+=3)
        {
            Debug.Log($"HELLO! {i}");
            int a,b,c;
            a= tris[i];
            b = tris[i+1];
            c = tris[i+2];

            Vector3 A,B,C;
            A = quad.transform.TransformPoint(verts[a]);
            B = quad.transform.TransformPoint(verts[b]);
            C = quad.transform.TransformPoint(verts[c]);

            Vector3 normal = Vector3.Cross(A,B);
            float denom = Vector3.Dot(normal,ray.direction);

            Debug.Log($"This is the normal {normal} this is the direction { ray.direction} denom {denom} A {A} B{B}");

            if(Mathf.Abs(denom)<0.00001f)
            {
                Debug.Log($"GOODBYE! {denom}");
                continue;
            }

            float temp_t = Vector3.Dot(ray.direction,normal)/denom;

            //check if point is inside the triangle

            

            if(temp_t < t)
            {
                t = temp_t;
            }

            // check if point is inside triangle.

            // angle 1/cos( u dot v/ mag u time mag v)
            Vector3 Q = ray.origin + ray.direction*t;

            float angle_abq = 1/Mathf.Cos(Vector3.Dot(B-Q, A-Q)/(B-Q).magnitude*(A-Q).magnitude);
            float angle_acq = 1/Mathf.Cos(Vector3.Dot(C-Q, A-Q)/(C-Q).magnitude*(A-Q).magnitude);
            float angle_bcq = 1/Mathf.Cos(Vector3.Dot(B-Q, C-Q)/(B-Q).magnitude*(C-Q).magnitude);

            Debug.Log($"abq {Mathf.Rad2Deg*(angle_abq)} acq {Mathf.Rad2Deg*angle_acq} bcq {Mathf.Rad2Deg*angle_bcq} sum {Mathf.Rad2Deg*(angle_acq + angle_bcq + angle_abq)}");

        }



        Vector3 pointerPos = ray.origin + t*ray.direction;

        intersectionPoint.transform.position = pointerPos;
        intersectionPoint.SetActive(true);




        Debug.DrawRay(ray.origin,ray.direction*t, Color.red);
        

        Debug.Log($"mouse position is at {mousePosition}");
        
    }


}
