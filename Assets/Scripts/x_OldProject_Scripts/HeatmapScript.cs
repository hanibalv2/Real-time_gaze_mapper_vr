using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeatmapScript : MonoBehaviour
{

    public GameObject player;

    // position holder // very very bad idea
    List<Vector3> playerPos = new List<Vector3>();
    // position var
    private Vector3 currentPos;
    private Vector3 pixelPos;
    // frame counter
    private int frames;
    // texture
    private Texture2D texture;
    private int[,] positionMatrix;


    private void Awake()
    {
        frames = 0;
    }

    void Start()
    {
        playerPos.Add(player.transform.position);
        texture = new Texture2D(500, 500);
        GetComponent<Renderer>().material.mainTexture = texture;
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                Color color = ((x & y) != 0 ? Color.red : Color.yellow);
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
    }

    // Update is called once per frame

    void Update()
    {
        frames++;
        currentPos = player.transform.position;
        playerPos.Add(currentPos);

        if (frames % 120 == 0) {
            UpdateHeatmap();
            frames = 0;
        }



    }
    
    void UpdateHeatmap()
    {   
        Debug.Log((int) currentPos.x + " " + (int)currentPos.y);
        // GetComponent<Renderer>().material.mainTexture = texture;
        // Color color = Color.cyan;
        //  texture.SetPixel((int)currentPos.x ,(int)currentPos.y,color);
        //  texture.Apply();

    }
}
