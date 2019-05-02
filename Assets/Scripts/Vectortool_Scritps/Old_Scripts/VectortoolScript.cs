using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

public class VectortoolScript : MonoBehaviour
{

    public Camera headCamera;
    public static Dictionary<BoxColliderBehaviour, int> hasMapColliderBoxes = new Dictionary<BoxColliderBehaviour, int>();
    public int shootedRays = 1;
    public int maxcubes = 7;
    public List<int> favcube;
    // scatterplot
    public Transform scatterplot;
    bool switchV = false;
    // Use this for initialization
    void Start()
    {
        FillScatterplotWithCollider();
        for(int i=0;i< maxcubes; i++)
        {
            favcube.Add(1);
        }
        InvokeRepeating("TrackColliderinfo", 0.0f, 0.5f);
    }

    // some shared functions
    public static float NormalizeMinMax(float value, float min, float max)
    {
        return (value - min) / (max - min);
    }

    public static float NormalizeLog(float value)
    {
        return Mathf.Log10(value);
    }

    public int getTriggerSum()
    {
        return hasMapColliderBoxes.Values.Sum();
    }

    public float getAverage()
    {
        return (float)hasMapColliderBoxes.Values.Average();
    }

    public float getValueBorder()
    {
        return shootedRays / 10;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // DEBUG show hash entries
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Key R Pressed:" + " show Raycast hashmap");
            Debug.Log("Sum : " + getTriggerSum());
            Debug.Log("Average : " + getAverage());
            Debug.Log("Reys : " + shootedRays);
            Debug.Log("Count Keys : " + hasMapColliderBoxes.Keys.Count());
            foreach (KeyValuePair<BoxColliderBehaviour, int> kvp in hasMapColliderBoxes)
            {   
                if(kvp.Value>0)
                Debug.Log(System.String.Format("Key = {0}, Value = {1}", kvp.Key.transform.position, kvp.Value));
            }
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            switchV = !switchV;
            Debug.Log("switchV " + switchV);
        }
            // DEBUG show view direction in editor
            //Vector3 viewdirection = headCamera.transform.forward;
            // Debug.DrawRay(headCamera.transform.position, viewdirection, Color.black, 2f, false);

            // CODE

            //first hit
            /*
            RaycastHit hit;
            Ray ray = headCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            if (Physics.Raycast(ray, out hit, 10.0f))
            {
                //Debug.Log("hit");
                hit.transform.SendMessage("HitByRay");
            }
            */

        }

    void TrackColliderinfo()
    {
        Ray ray = headCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit[] allhits = Physics.RaycastAll(ray, 10.0f);
        shootedRays++;
        if (switchV)
        {
            for (int i = 0; i < allhits.Length; i++)
            {
                //Debug.Log("hit");
                allhits[i].transform.SendMessage("HitByRay");
                try
                {
                    allhits[i].collider.gameObject.GetComponent<BoxColliderBehaviour>().hitByRay();
                }
                catch (NullReferenceException e)
                {
                    Debug.Log("nope");
                }
            }
        }
    }


    // Filled the given object with Boxes
    private void FillScatterplotWithCollider()
    {   
        // maxboxes plus one for scale
        int maxBoxCollider = 4;


        // generate prefab
        GameObject TransparentBoxColliderCubePrefab = Instantiate(Resources.Load("Prefabs/TransparentBoxColliderCubePrefab2") as GameObject) as GameObject;
        TransparentBoxColliderCubePrefab.name = "scatterplotColliderWedge";
        
        // modify collider
        // makes sure collider is trigger
        BoxCollider boxcollider = TransparentBoxColliderCubePrefab.GetComponent<BoxCollider>();
        boxcollider.isTrigger = true;

        // positioning of the box
        for (int x = 0; x <= maxBoxCollider; x++)
        {
            for (int y = 0; y <= maxBoxCollider; y++)
            {
                for (int z = 0; z <= maxBoxCollider; z++)
                {
                    // set brush position
                    // (-0.5), because transform center is cube center
                    Vector3 brushPos = new Vector3(
                                                    NormalizeMinMax((float)x, 0.0f, (float)maxBoxCollider) - 0.5f,
                                                    NormalizeMinMax((float)y, 0.0f, (float)maxBoxCollider) - 0.5f,
                                                    NormalizeMinMax((float)z, 0.0f, (float)maxBoxCollider) - 0.5f
                                                );
                    // init copy and make boxcollider part of the scatterplot
                    GameObject BoxColliderBrush = Instantiate(TransparentBoxColliderCubePrefab) as GameObject;
                    BoxColliderBrush.transform.parent = scatterplot.transform;
                    BoxColliderBrush.transform.localScale = new Vector3(1.0f / maxBoxCollider, 1.0f / maxBoxCollider, 1.0f / maxBoxCollider);
                    BoxColliderBrush.transform.localPosition = brushPos;
                }
            }
        }

        // Delet prefab to prefent overlapping in scene
        Destroy(TransparentBoxColliderCubePrefab);
    }
}
