using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BacktrackingPath : MonoBehaviour {

    public GameObject player;
    
    // prefab 
    private GameObject bread;

    // position holder
    List<Vector3> playerPos = new List<Vector3>();
    // position var
    private Vector3 referencePos, lastPos, currentPos;


    void Start() {
        referencePos    = player.transform.position;
        lastPos         = player.transform.position;
        playerPos.Add(player.transform.position);

        //prefab point
        bread = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bread.transform.position += new Vector3(0.0f, -0.4999f, 0.0f);
        bread.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
    }    
	
	void Update () {
        
        //save current Pos
        currentPos = player.transform.position;
        playerPos.Add(currentPos);

        //print players pos if he travel 
        if (DistanceFarEnough(currentPos, referencePos))
        {
            // drop breadcrumb (marker)
            Debug.Log("lay breadcrumb" + currentPos);
            GameObject breadcrumb = Instantiate(bread); 
            
            //breadcrumb.transform.Translate(currentPos);
            breadcrumb.transform.position = new Vector3(currentPos.x, 0.01f, currentPos.z);

            // connect last marker with the current one
            LineRenderer lineRenderer = breadcrumb.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));
            //lineRenderer.alignment = LineAlignment.Local;
            lineRenderer.startColor = Color.red;
            lineRenderer.endColor = Color.red;
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;

            lineRenderer.SetPosition(0, SetPositionToGround(referencePos));
            lineRenderer.SetPosition(1, SetPositionToGround(currentPos));

            // remember last marker
            referencePos = currentPos;
        }

        //remember last player position
        lastPos = currentPos;
    }

    private bool DistanceFarEnough(Vector3 a, Vector3 b)
    {
        float distance = Vector3.Distance(a, b);
        if(distance >= 1.0f)
        {
            return true;
        }
        return false;
    }

    private Vector3 SetPositionToGround(Vector3 a)
    {
        return new Vector3(a.x, 0.01f, a.z);
    }
}
