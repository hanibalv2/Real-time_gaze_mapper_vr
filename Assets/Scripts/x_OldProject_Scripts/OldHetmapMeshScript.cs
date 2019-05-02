using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class OldHeatmapMeshScript : MonoBehaviour
{
    public GameObject player;

    private Dictionary<Vector2, int> hashmap = new Dictionary<Vector2, int>();
    private Vector2 arrayPos;
    private Vector3 playerPos;

    private int frame = 0;
    private int counter = 0;
    //mash
    public Vector3[] newVertices;
    public Vector2[] newUV;
    public int[] newTriangles;

    public GameObject datapointPrefab;
    public Material datapointMaterial;

    private List<GameObject> currentMeshes = new List<GameObject>();

    //new mesh structur
    public int size_x = 64;
    public int size_z = 64;
    public float tileSize = 0.0625f;


    void Start()
    {
        if (datapointPrefab == null)
        {
            datapointPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            datapointPrefab.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);
        }
        if (datapointMaterial == null)
        {
            datapointMaterial = datapointPrefab.GetComponent<Renderer>().material;
            //set prefab shader 
            datapointMaterial.shader = (Shader.Find("Particles/Additive"));
        }

        playerPos = player.transform.position;
        arrayPos = new Vector2(playerPos.x, playerPos.z);
        //arrayPos = new Vector2(playerPos.x, playerPos.z);
        hashmap.Add(arrayPos, 1);

        //mesh
        //GenerateMeshGrid();
        GenerateMeshGridFromScratch();
        BuildTexture();
    }

    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("Key H Pressed");
            foreach (KeyValuePair<Vector2, int> kvp in hashmap)
            {
                Debug.Log(System.String.Format("Key = {0}, Value = {1}", kvp.Key, kvp.Value));
            }

        }
        if (frame % 10 == 0)
        {
            playerPos = player.transform.position;
            //arrayPos = new Vector2((int)playerPos.x, (int)playerPos.z);
            //arrayPos = new Vector2(Mathf.Floor(((playerPos.x)) *64f) / 64f, Mathf.Floor(((playerPos.z)) * 64f) / 64f);
            arrayPos = new Vector2(Mathf.Floor(playerPos.x / size_x * 10000), Mathf.Floor(playerPos.z / size_z * 10000));
            Debug.Log(arrayPos.x + " " + playerPos.x / size_x);
            if (hashmap.ContainsKey(arrayPos))
            {
                hashmap[arrayPos] += 1;
                Debug.Log("counter here " + arrayPos + " : " + hashmap[arrayPos] + ", counted entries : " + hashmap.Count + " frame: " + frame);
            }
            else
            {
                hashmap.Add(arrayPos, 1);
                //Debug.Log("New hashmap key add");
            }

            if (frame >= 300 && hashmap.Count >= 5)
            {
                //heatmapUpdate();
                ReBuildTexture();
                frame = 0;
            }
        }

        frame++;
    }

    private void heatmapUpdate()
    {
        // DEBUG GO
        counter++;
        Debug.Log("Update Heatmap :" + counter + " heatmap Counter: " + hashmap.Count);

        visualizeData(HashmapToVector3(hashmap));

        // DEBUG END

        /* IDictionaryEnumerator enumerator = hashmap.GetEnumerator();
         List<Vector2> KeyList = new List<Vector2>();

         while (enumerator.MoveNext())
         {
             KeyList.Add((Vector2)(enumerator.Key));
         }
         Vector2 [] Keyoutput = KeyList.ToArray();
         Debug.Log("Keyout lenght: "+Keyoutput.Length);
         for (int i = 0; i < positions.Length; i++) {
             positions[i] = new Vector4( Keyoutput[i].x,Keyoutput[i].y , 0, 0);
         }
         */

    }

    // COPY KLAU SCATTERPLOT GO
    public void visualizeData(Vector3[] data)
    {
        GameObject dataPointBrush = Instantiate(datapointPrefab);     // Instantiate the prefab only once in the beginning as this takes a little while.
        dataPointBrush.transform.parent = gameObject.transform;
        List<CombineInstance> combine = new List<CombineInstance>();    // list to combine submeshes

        MeshFilter dataPointMeshFilter = dataPointBrush.GetComponent<MeshFilter>();

        //clear all submeshes (prevent overlaying)
        foreach (GameObject cm in currentMeshes)
        {
            Destroy(cm);
        }

        int vertexCount = 0;

        for (int index = 0; index < data.Length; index++)
        {
            dataPointBrush.transform.localPosition = new Vector3(data[index].x, data[index].y, data[index].z);// Put brush at correct position. // todo ask why substract -0.5f

            if (vertexCount + dataPointMeshFilter.mesh.vertexCount > 65000 || index == data.Length - 1)
            {
                GameObject submesh = new GameObject();                                         // Create the gameobject for the combined Mesh
                submesh.AddComponent<MeshFilter>();
                submesh.AddComponent<MeshRenderer>();
                submesh.GetComponent<Renderer>().sharedMaterial = datapointMaterial;
                submesh.GetComponent<Renderer>().materials[0] = datapointMaterial;
                submesh.name = "meshChunk";

                submesh.layer = 10; // layer for minimap

                submesh.transform.parent = transform;                                             // Put the new mesh in the correct position in your scene
                Mesh CombinedMesh = new Mesh();
                CombinedMesh.CombineMeshes(combine.ToArray(), true, true);
                submesh.GetComponent<MeshFilter>().mesh = CombinedMesh;                             // add submesh to the scene
                combine = new List<CombineInstance>();
                vertexCount = 0;
                currentMeshes.Add(submesh);       // Array that saves all the combined meshes. Useful to delete them later.
            }

            vertexCount += dataPointMeshFilter.sharedMesh.vertexCount;
            CombineInstance combinInstance = new CombineInstance();
            combinInstance.mesh = dataPointMeshFilter.sharedMesh;
            combinInstance.transform = dataPointMeshFilter.transform.localToWorldMatrix;
            combine.Add(combinInstance);        // aadd brush to the submesh
        }
        DestroyImmediate(dataPointBrush);            // Destroy the brush.
    }

    public Vector3[] HashmapToVector3(Dictionary<Vector2, int> datamap)
    {
        Vector3[] generatedVector3 = new Vector3[datamap.Count];
        var datamapAllKeys = datamap.Keys.ToArray();
        for (int i = 0; i < datamapAllKeys.Length; i++)
        {
            generatedVector3[i] = new Vector3(datamapAllKeys[i].x, 0, datamapAllKeys[i].y);
        }
        return generatedVector3;
    }

    private void GenerateMeshGrid()
    {
        GameObject dataPointBrush = Instantiate(datapointPrefab);     // Instantiate the prefab only once in the beginning as this takes a little while.
        dataPointBrush.transform.parent = gameObject.transform;
        List<CombineInstance> combine = new List<CombineInstance>();    // list to combine submeshes

        MeshFilter dataPointMeshFilter = dataPointBrush.GetComponent<MeshFilter>();
        int vertexCount = 0;
        Debug.Log("Generate Grid");
        for (float indexX = -1.0f; indexX < 1.0f; indexX = indexX + 0.01f)
        {
            for (float indexY = -1.0f; indexY < 1.0f; indexY = indexY + 0.01f)
            {
                dataPointBrush.transform.localPosition = new Vector3(indexX, 0, indexY);// Put brush at correct position. // todo ask why substract -0.5f

                if (vertexCount + dataPointMeshFilter.mesh.vertexCount > 65000 || indexY >= 0.99f)
                {
                    GameObject submesh = new GameObject();                                         // Create the gameobject for the combined Mesh
                    submesh.AddComponent<MeshFilter>();
                    submesh.AddComponent<MeshRenderer>();
                    submesh.GetComponent<Renderer>().sharedMaterial = datapointMaterial;
                    submesh.GetComponent<Renderer>().materials[0] = datapointMaterial;
                    submesh.name = "gridMesh";

                    submesh.transform.parent = transform;                                             // Put the new mesh in the correct position in your scene
                    Mesh CombinedMesh = new Mesh();
                    CombinedMesh.CombineMeshes(combine.ToArray(), true, true);
                    submesh.GetComponent<MeshFilter>().mesh = CombinedMesh;                             // add submesh to the scene
                    combine = new List<CombineInstance>();
                    vertexCount = 0;
                }

                vertexCount += dataPointMeshFilter.sharedMesh.vertexCount;
                CombineInstance combinInstance = new CombineInstance();

                //try addcolor //work
                Mesh mesh = dataPointMeshFilter.GetComponent<MeshFilter>().mesh;
                Vector3[] vertices = mesh.vertices;
                // create new colors array where the colors will be created.
                Color[] colors = new Color[vertices.Length];

                for (int i = 0; i < vertices.Length; i++)
                {
                    if (indexX > 0.0f)
                        colors[i] = Color.Lerp(Color.red, Color.green, vertices[i].y);
                    else
                        colors[i] = Color.Lerp(Color.black, Color.magenta, vertices[i].y);
                }
                // assign the array of colors to the Mesh.


                mesh.colors = colors;
                combinInstance.mesh = dataPointMeshFilter.sharedMesh;
                combinInstance.transform = dataPointMeshFilter.transform.localToWorldMatrix;
                combine.Add(combinInstance);        // aadd brush to the submesh
            }
        }
        DestroyImmediate(dataPointBrush);
    }
    // COPY KLAU SCATTERPLOT STOP

    private void GenerateMeshGridFromScratch()
    {

        Debug.Log("Generate Grid From Scratch");

        int numTiles = size_x * size_z;
        int numTris = numTiles * 2;

        int vsize_x = size_x + 1;
        int vsize_z = size_z + 1;
        int numVerts = vsize_x * vsize_z;

        // generate mesh data
        Vector3[] vertices = new Vector3[numVerts];
        Vector3[] normals = new Vector3[numVerts];
        Vector2[] uv = new Vector2[numVerts];

        int[] triangles = new int[numTris * 3];

        //do the math

        int x, z;
        for (z = 0; z < vsize_z; z++)
        {
            for (x = 0; x < vsize_x; x++)
            {
                vertices[z * vsize_x + x] = new Vector3((float)x * tileSize, z * (1 / 32), (float)z * tileSize);
                normals[z * vsize_x + x] = Vector3.up;
                //strech to whole mesh
                uv[z * vsize_x + x] = new Vector2((float)x / size_x, (float)z / size_z);
                //reuse to fit in tiles
                //uv[z * vsize_x + x] = new Vector2(vertices[z * vsize_x + x].x, vertices[z * vsize_x + x].z);
            }
        }

        for (z = 0; z < size_z; z++)
        {
            for (x = 0; x < size_x; x++)
            {
                int squareIndex = z * size_x + x;
                int triOffset = squareIndex * 6;
                triangles[triOffset + 0] = z * vsize_x + x + 0;
                triangles[triOffset + 1] = z * vsize_x + x + vsize_x + 0;
                triangles[triOffset + 2] = z * vsize_x + x + vsize_x + 1;

                triangles[triOffset + 3] = z * vsize_x + x + 0;
                triangles[triOffset + 4] = z * vsize_x + x + vsize_x + 1;
                triangles[triOffset + 5] = z * vsize_x + x + 1;
            }
        }

        //create a new Mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.uv = uv;

        //assigne mesh to components
        MeshFilter mesh_filter = GetComponent<MeshFilter>();
        MeshRenderer mesh_renderer = GetComponent<MeshRenderer>();
        MeshCollider mesh_collider = GetComponent<MeshCollider>();

        mesh_filter.mesh = mesh;
        mesh_collider.sharedMesh = mesh;
    }

    public void BuildTexture()
    {
        Debug.Log("Build Texture init");
        int texWidth = 64;
        int texHeight = 64;
        Texture2D texture = new Texture2D(texWidth, texHeight);



        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                Color color = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
                texture.SetPixel(x, y, color);
            }
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();
        MeshRenderer mesh_Renderer = GetComponent<MeshRenderer>();
        mesh_Renderer.sharedMaterials[0].mainTexture = texture;
    }
    public void ReBuildTexture()
    {
        Debug.Log("reBuild Texture");
        int texWidth = 64;
        int texHeight = 64;
        Texture2D texture = new Texture2D(texWidth, texHeight);

        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                if (hashmap.ContainsKey(new Vector2(Mathf.Floor(((float)x) * 64f) / 64f, Mathf.Floor(((float)y) * 64f) / 64f)))
                {
                    texture.SetPixel(x, y, Color.red);
                    Debug.Log("red at: x=" + (Mathf.Round((float)x)) + " , y=" + (Mathf.Round((float)y)));
                }
                else
                {
                    texture.SetPixel(x, y, Color.blue);
                }
            }
        }
        texture.filterMode = FilterMode.Point;
        texture.Apply();
        MeshRenderer mesh_Renderer = GetComponent<MeshRenderer>();
        mesh_Renderer.materials[0].mainTexture = texture;
    }

    public void TrailPath()
    {
        MeshCollider mesh_collider = GetComponent<MeshCollider>();
    }
}
