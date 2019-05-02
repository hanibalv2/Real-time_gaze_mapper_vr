using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;
using Valve.VR;

public class PlayerVrController : MonoBehaviour
{
    private Valve.VR.EVRButtonId triggerbutton = Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger;
    private Valve.VR.EVRButtonId touchpad = Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad;
    private Player player = null;
    private int showGridIndex;

    public List<VectortoolAbstractScriptAdvance> VASAList;
    private void Start()
    {
        HideShowMesh(false);
        showGridIndex = 0;
        player = Valve.VR.InteractionSystem.Player.instance;
        if (player == null)
        {
            Debug.LogError("Teleport: No Player instance found in map.");
            Destroy(this.gameObject);
            return;
        }

        //trackedObject = GetComponent<SteamVR_TrackedObject>();
    }

    // Update is called once per frame
    void Update()
    {
        player = Valve.VR.InteractionSystem.Player.instance;
        foreach (Hand hand in player.hands)
        {
            if (hand.controller != null && hand.startingHandType == Hand.HandType.Left )
            {
                if (hand.controller.GetPressDown(triggerbutton))
                {
                    HideShowMesh(true);
                    Debug.Log("hide false");
                }
                if (hand.controller.GetPressUp(triggerbutton))
                {
                    HideShowMesh(false);
                    Debug.Log("hide false");
                }

                /*
                if (hand.controller.GetPress(touchpad))  // Is any DPad button pressed?
                {
                    var touchpadAxis = hand.controller.GetAxis(touchpad);
                    const float threshold = 0.3f;
                    
                    if (touchpadAxis.y > (1.0f - threshold)) { return dPadButtonId == EVRButtonId.k_EButton_DPad_Up; }
                    else if (touchpadAxis.y < threshold) { return dPadButtonId == EVRButtonId.k_EButton_DPad_Down; }
                    else if (touchpadAxis.x > (1.0f - threshold)) { return dPadButtonId == EVRButtonId.k_EButton_DPad_Right; }
                    else if (touchpadAxis.x < threshold) { return dPadButtonId == EVRButtonId.k_EButton_DPad_Left; }
                }
                */
            }
        }
    }

    private void HideShowMesh(bool setVisibility)
    {
        foreach (var VASA in VASAList)
        {
            VASA.HideShowMeshes(setVisibility);
        }
    }
}
