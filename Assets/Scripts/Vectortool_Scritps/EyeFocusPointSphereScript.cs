using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * TEST skript to simulate the focus point of the user
 */

public class EyeFocusPointSphereScript : MonoBehaviour
{

    public Transform playerDummy;
    public Transform playerVR;
    private Transform _player;

    public float distance = 0.2f;
    public float sphereCastRadius = 1.0f;
    public float sphereCastDistance = 10.0f;
    public VectortoolAbstractScript VAS;
    public List<VectortoolAbstractScriptAdvance> VASAList;
    public bool useAdvancedScript = true;
    public LayerMask layerMask;
    // Use this for initialization

    public bool switchGaze = false;

    public int maxCollisionsPerFrame = 10;
    void Start()
    {
        if (!playerVR.parent.gameObject.activeSelf)
        {
            _player = playerDummy;
            //player.GetChild(0).tag = "MainCamera";
            _player.tag = "MainCamera";
            Debug.Log("EyeFocusPointSphereScript use player :'Dummy' ");
            playerVR.tag = "Untagged";
            playerVR.parent.tag = "Untagged";
        }
        else
        {
            _player = playerVR;
            _player.tag = "MainCamera";
            Debug.Log("EyeFocusPointSphereScript use player : 'VR' ");
        }

        //VAS = GameObject.Find("Vectortool_Abstracthandler").GetComponent<VectortoolAbstractScript>();
        PupilData.calculateMovingAverage = false; // necessary?
    }
    void OnEnable()
    {
        //necessary?
        if (PupilTools.IsConnected)
        {
            //PupilGazeTracker.Instance.StartVisualizingGaze();
            PupilGazeTracker.Instance.StartVisualizingGaze();
            PupilGazeTracker.Instance.StopVisualizingGaze();
            Debug.Log("gaze subscribed");
            PupilTools.SubscribeTo("gaze");
        }
    }
    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.U))
        {
            switchGaze = !switchGaze;
            Debug.Log("connect:" + PupilTools.IsConnected + ", gaze: " + PupilTools.IsGazing + ", pos :" + PupilData._2D.GazePosition);

            if (PupilTools.IsConnected && switchGaze)
            {
                PupilGazeTracker.Instance.StartVisualizingGaze();
            }
            else if (!switchGaze)
            {
                PupilGazeTracker.Instance.StopVisualizingGaze();
            }
        }
        Ray ray;
        if (PupilTools.IsConnected)
        {
            //transform.position = PupilData._3D.GazePosition;
            //Debug.Log(PupilData._3D.GazePosition);
            //Debug.Log(PupilData._2D.GazePosition);
            Vector2 gazePointCenter = PupilData._2D.GazePosition;
            Vector3 viewportPoint = new Vector3(gazePointCenter.x, gazePointCenter.y, 1f);
            transform.position = viewportPoint + transform.forward * distance;


            ray = Camera.main.ViewportPointToRay(viewportPoint);
            Debug.DrawRay(ray.origin, ray.direction, Color.red);


        }
        else
        {
            //Ray ray = Camera.main. postion + ImagePosition. forward 
            ray = new Ray(
                Camera.main.transform.position,
                Camera.main.transform.forward);
            Debug.DrawLine(ray.origin, ray.origin + ray.direction * sphereCastDistance, Color.green);
        }

        RaycastHit[] hit = Physics.SphereCastAll(ray, sphereCastRadius, sphereCastDistance, layerMask);
        if (hit.Length > 0)
        {

            for (int i = 0; i < hit.Length && i < maxCollisionsPerFrame; i++)
            {
                AlternativOnTriggerStay(hit[i].collider);
            }

        }

        // transform.position = player.position + transform.forward * distance;
        //transform.localRotation = player.rotation;


        if (Input.GetKeyDown(KeyCode.R))
        {
            GetComponent<MeshRenderer>().enabled = !GetComponent<MeshRenderer>().enabled;
        }

    }

    private void AlternativOnTriggerStay(Collider other)
    {
        //Vector3 center = other.gameObject.transform.position;
        int id = other.GetComponent<ColliderBoxScript>().Id;
        if (useAdvancedScript)
        {
            var position = _player.position;
            foreach (var VASA in VASAList)
            {
                VASA.UpdateCollision(id, position, other.ClosestPoint(position));
            }

        }
        else
        {
            //sVAS.UpdateCollision(center);
        }

    }
}
