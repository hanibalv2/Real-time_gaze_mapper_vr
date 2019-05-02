using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefaultGroundScript : MonoBehaviour
{
    private bool isEnable = false;
    private void Start()
    {
        transform.GetComponent<MeshRenderer>().enabled = true;
        transform.GetComponent<MeshRenderer>().enabled = false;
    }
    public void switchVisibility()
    {
        isEnable = !isEnable;
        transform.GetComponent<MeshRenderer>().enabled = isEnable;
    }
}
