using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
public class MinimapHeatmapScript : MonoBehaviour {

    public Text text;
    public RawImage rawimage;
    bool initbool;

    private void Start()
    {
        initbool = false;
    }

    // Use this for initialization
    public void Init () {
        if (initbool == false)
        {
            text.gameObject.SetActive(false);
            rawimage.gameObject.SetActive(true);
            initbool = true;
        }
	}
}
