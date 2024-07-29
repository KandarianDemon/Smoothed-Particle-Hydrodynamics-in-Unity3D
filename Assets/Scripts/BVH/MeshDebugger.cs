using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MeshDebugger : MonoBehaviour
{


    [Range(0.001f,0.05f)]
    public float radius;
    MeshFilter filter;
    Mesh mesh;
    Vector3[] vertices;
    Bounds bounds;
    // Start is called before the first frame update
    void Start()
    {
        filter = GetComponent<MeshFilter>();
        mesh = filter.sharedMesh;
        vertices = mesh.vertices;
        
    }

    void OnDrawGizmos(){

        if(vertices== null) return;
        Vector3 scale = gameObject.transform.localScale;
        mesh.RecalculateBounds();
        bounds = GetComponent<MeshRenderer>().bounds;
        
        for(int i = 0; i<vertices.Length;i++)
        {

            Vector3 adjScale = new Vector3(vertices[i].x * scale.x, vertices[i].y*scale.y,vertices[i].z * scale.z);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(gameObject.transform.TransformPoint(vertices[i]),radius);
        }

        Gizmos.DrawWireCube(bounds.center, bounds.size);
            }

    // Update is called once per frame
    void Update()
    {
        
    }
}
