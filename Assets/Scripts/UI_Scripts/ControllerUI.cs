using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ControllerUI : MonoBehaviour {

    public GameObject controllerLeft;
    public Canvas tablet;

    private SteamVR_TrackedObject trackedObjLeft;
    private SteamVR_Controller.Device deviceLeft;
    private bool leftTriggerHold = false;
    private int controllerLeft_index = -1;

    void Awake()
    {
        if (controllerLeft == null)
        {
            controllerLeft = GameObject.Find("Controller (left)");
        }
        try
        {
            trackedObjLeft = controllerLeft.GetComponent<SteamVR_TrackedObject>();
        }
        catch (ExitGUIException e)
        {
            // STEAM VR probably not running
            Debug.Log("SteamVR error");
        }
    }
        // Use this for initialization
        void Start () {
        transform.SetParent(controllerLeft.transform);
        transform.localPosition = new Vector3(0.2f, 0,0);
        transform.eulerAngles = new Vector3(90,0,0);
        transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
	}
	
	// Update is called once per frame
	void Update () {
        //tablet.transform.LookAt(Camera.main.transform);


        if (leftTriggerHold)
        {
            tablet.transform.rotation = Quaternion.LookRotation(tablet.transform.position - Camera.main.transform.position);
        }
        
	}
    private void FixedUpdate()
    {
        controllerLeft_index = (int)trackedObjLeft.index;
        deviceLeft = SteamVR_Controller.Input(controllerLeft_index);
        if (deviceLeft.GetPressUp(SteamVR_Controller.ButtonMask.Trigger))
        {
            leftTriggerHold = false;
        }
        if (deviceLeft.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
        {
            leftTriggerHold = true;
        }
        
    }
}
