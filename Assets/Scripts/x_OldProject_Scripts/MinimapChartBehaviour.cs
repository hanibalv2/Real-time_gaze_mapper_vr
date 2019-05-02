using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinimapChartBehaviour : MonoBehaviour
{

    // cubemap
    public GameObject controllerLeft;
    public GameObject controllerRight;

    private SteamVR_TrackedObject trackedObjLeft;
    private SteamVR_TrackedObject trackedObjRight;
    private SteamVR_Controller.Device deviceLeft;
    private SteamVR_Controller.Device deviceRight;

    void Awake()
    {
        if (controllerLeft == null)
        {
            controllerLeft = GameObject.Find("Controller (left)");
        }
        if (controllerRight == null)
        {
            controllerRight = GameObject.Find("Controller (right)");
        }
        try
        {
            trackedObjLeft = controllerLeft.GetComponent<SteamVR_TrackedObject>();
            trackedObjRight = controllerRight.GetComponent<SteamVR_TrackedObject>();
        }
        catch (System.Exception e)
        {
            // STEAM VR probably not running
            Debug.Log("SteamVR error");
        }
    }

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
       // deviceLeft = SteamVR_Controller.Input((int)trackedObjLeft.index);
       // deviceRight = SteamVR_Controller.Input((int)trackedObjRight.index);
    }
}
