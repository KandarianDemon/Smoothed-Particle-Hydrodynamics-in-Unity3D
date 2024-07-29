using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayCaster : MonoBehaviour
{

    [Range(1,100)]
    public float rayLength;
    Ray ray;

    public GameObject[] objects;
    


    
    


    // Start is called before the first frame update
    void OnDrawGizmos(){
        
        if(objects == null) return;
        foreach(GameObject obj in objects){


       
        float hitLength = Hit(obj.GetComponent<MeshRenderer>().bounds);
       
        float length = (hitLength < rayLength) ? hitLength:rayLength;
        
        
        
        Gizmos.color =(hitLength < rayLength) ? Color.green:Color.red;

        if(hitLength<rayLength){

            Gizmos.DrawSphere(gameObject.transform.position+ gameObject.transform.TransformDirection(Vector3.forward)*length,0.1f);
        }
        Gizmos.DrawRay(gameObject.transform.position, gameObject.transform.TransformDirection(Vector3.forward)*length);
         }
    }


    public float Hit(Bounds bounds)


    {   

        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        
        Vector3 origin = gameObject.transform.position;
        Vector3 direction = gameObject.transform.TransformDirection(Vector3.forward);
        

        Vector3 tlow = DivideVectors(min - origin,direction);
        Vector3 tHigh = DivideVectors(max - origin,direction);

        Vector3 t1 = Vector3.Min(tlow,tHigh);
        Vector3 t2 = Vector3.Max(tlow,tHigh);

        float dstFar = Mathf.Min(Mathf.Min(t2.x,t2.y),t2.z);
        float dstNear = Mathf.Max(Mathf.Max(t1.x,t1.y),t1.z);

        bool hit = dstFar >= dstNear && dstFar > 0;
        return hit ? dstNear : 1000000.0f;


    }

    public Vector3 DivideVectors(Vector3 a, Vector3 b){

        b.x = (b.x != 0) ? b.x: 0.00001f;
        b.y = (b.y != 0) ? b.y: 0.00001f;
        b.z = (b.z != 0) ? b.z: 0.00001f;

        return new Vector3(a.x/b.x, a.y/b.y,a.z/b.z);
    }
}

