using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VectortoolUIScript : MonoBehaviour {

    public Text text;
    public Slider slider;
    int value;

	// Update is called once per frame
	void Update () {
        value=(int)(slider.value *100.0f);
        text.text = "Threshold: " + value + "%";
	}
}
