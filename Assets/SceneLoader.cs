using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public PupilManager PM;
    
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            PM.StartCoroutine(PM.UnloadCurrentScene());
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            PM.currentSceneIndex = 0;
            PM.StartCoroutine(PM.LoadCurrentScene());
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            PM.currentSceneIndex = 1;
            PM.StartCoroutine(PM.LoadCurrentScene());
        }
        if (Input.GetKeyDown(KeyCode.F4))
        {
            PM.currentSceneIndex = 2;
            PM.StartCoroutine(PM.LoadCurrentScene());
        }
    }
}
