using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HeatmapMeshScript))]
public class HeatmapMouse : MonoBehaviour {

    HeatmapMeshScript _heatmapMeshScript;
    Vector3 currentTileCoord;

    public Transform selectionCube;

	// Use this for initialization
	void Start () {
        _heatmapMeshScript = GetComponent<HeatmapMeshScript>();
	}
	
	// Update is called once per frame
	void noUpdate () {
        Ray ray = Camera.current.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitinfo;
        if (GetComponent<Collider>().Raycast(ray,out hitinfo, Mathf.Infinity))
        {
            Debug.Log(hitinfo.point);
//            float x = (hitinfo.point.x / _heatmapMeshScript.tileSize);
//            float z = (hitinfo.point.z / _heatmapMeshScript.tileSize);

            float x = (hitinfo.point.x);
            float z = (hitinfo.point.z);

            currentTileCoord.x = x;
            currentTileCoord.z = z;
            selectionCube.transform.position = currentTileCoord;

            /*
           int texWidth = 10;
           int texHeight = 10;

           MeshRenderer mesh_Renderer = GetComponent<MeshRenderer>();
           Texture2D texture = mesh_Renderer.materials[0].GetTexture();

           Texture2D texture = new Texture2D(texWidth, texHeight);

                   texture.SetPixel((int)x, (int)z, Color.red);

           texture.Apply();
           */
        }

    }
}
