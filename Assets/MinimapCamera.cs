using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MinimapCamera : MonoBehaviour
{

    public Camera _heatmapCamera;

    // resolution  
    // 4k = 3840 x 2160, 1080p = 1920 x 1080
    public int captureWidth = 1920;
    public int captureHeight = 1080;

    // private vars for screenshot
    private Rect rect;
    private RenderTexture renderTexture;
    private Texture2D screenShot;
    private int counter = 0; // image #

    void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            SaveHeatmapAsPNG();
        }
    }

    public void SaveHeatmapAsPNG()
    {
        if (renderTexture == null)
        {
            // creates off-screen render texture that can rendered into
            rect = new Rect(0, 0, captureWidth, captureHeight);
            renderTexture = new RenderTexture(captureWidth, captureHeight, 24);
            screenShot = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
        }
        // get main heatmap camera and manually render scene into renderTexture
        _heatmapCamera.targetTexture = renderTexture;
        _heatmapCamera.Render();

        // read pixels will read from the currently active render texture so make our offscreen 
        // render texture active and then read the pixels
        RenderTexture.active = renderTexture;
        screenShot.ReadPixels(rect, 0, 0);

        // reset active camera texture and render texture
        _heatmapCamera.targetTexture = null;
        RenderTexture.active = null;

        byte[] fileData = null;
        fileData = screenShot.EncodeToPNG();

        new System.Threading.Thread(() =>
        {
            // create file and write optional header with image bytes
            string filename = "Screenshot_Heatmap_" + string.Format("text-{0:yyyy-MM-dd_hh-mm-ss-tt}.PNG", System.DateTime.Now);
            var f = System.IO.File.Create(filename);
            f.Write(fileData, 0, fileData.Length);
            f.Close();
            Debug.Log(string.Format("Wrote screenshot {0} of size {1}", filename, fileData.Length));
        }).Start();

    }
}