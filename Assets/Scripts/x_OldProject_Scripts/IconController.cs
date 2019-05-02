using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IconController : MonoBehaviour {

    public GameObject user;

    private Vector3 offset;
    private Quaternion rotation;

    void Start()
    {
    }

    void LateUpdate()
    {
        transform.position = new Vector3(user.transform.position.x,2,user.transform.position.z);
        transform.eulerAngles = new Vector3(90,user.transform.eulerAngles.y, 0);
     
    }
}
