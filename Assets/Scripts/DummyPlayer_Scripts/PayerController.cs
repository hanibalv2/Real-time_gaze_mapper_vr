using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PayerController : MonoBehaviour {

    public float movementSpeed = 10;
    private float posy = 1;
    private float horizontalY = 0;
    // public float turningSpeed = 60;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("Screenshot taken");
            ScreenCapture.CaptureScreenshot("Screenshot_" + string.Format("text-{0:yyyy-MM-dd_hh-mm-ss-tt}.PNG",
            System.DateTime.Now));
        }

        float horizontalZ = Input.GetAxis("Horizontal") * movementSpeed * Time.deltaTime;
        transform.Translate(horizontalZ,0, 0);
        
        float vertical = Input.GetAxis("Vertical") * movementSpeed * Time.deltaTime;
        transform.Translate(0, 0, vertical);

        float horizontalY = 0;
        if (Input.GetKey(KeyCode.LeftShift))
            horizontalY= horizontalY + 0.01f * movementSpeed;
        if (Input.GetKey(KeyCode.Space))
            horizontalY = horizontalY - 0.01f * movementSpeed;
        //transform.Translate(0,horizontalY, 0);
        posy += horizontalY;
        transform.position =new  Vector3(transform.position.x, posy, transform.position.z);
    }

}
