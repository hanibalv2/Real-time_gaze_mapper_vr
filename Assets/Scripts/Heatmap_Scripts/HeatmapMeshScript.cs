using System;
using System.Collections;
using UnityEngine;


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]

public class HeatmapMeshScript : MonoBehaviour
{
    // editor settings
    public Transform playerDummy;
    public Transform playerVR;
    private Transform player;

    // Color
    public Gradient colorGradient;
    private Color[] colors;

    //mash
    public Vector3[] newVertices;
    public int[] newTriangles;

    //new mesh structur
    public int size_x = 128;
    private int size_z;
    private int msize_x;
    private int msize_z;
    public float tileSize = 0.03125f;

    //Default Ground
    public GameObject defaultGround;

    //Minimap Camera
    public Camera minimapCamera;

    //compute shader heatmap
    public ComputeShader shader;
    private ComputeBuffer vertbuffer;
    private ComputeBuffer weightsbuffer;
    private ComputeBuffer outputbuffer;

    // "small comptueshader color gradient": buffer and switcher
        //private ComputeBuffer[] colorsbuffer;
        //private int computingBuffer = 0;

    // Tracking
    int sumAddedPosToTracking = 0;
    private Vector3 playerPos;
    bool heatmapCalculationDone = true;

    // UI 
    public bool trackingEnabled;
    public float smoothvalue = 0.25f;
    public MinimapHeatmapScript minimapScript;

    //Keyboard Controller
    public DefaultGroundScript defaultGroundScript;

    void Start()
    {
        // Dummy used or VR
        if (!playerVR.parent.gameObject.activeSelf)
        {
            player = playerDummy;
        }
        else
        {
            player = playerVR;
        }

        size_z = size_x;
        msize_x = size_x + 1;
        msize_z = size_z + 1;

        GenerateMeshGridFromScratch();

        // start tracking
        InvokeRepeating("TrackPositionForVertices", 0.0f, 0.1f);

        SetupBufferAndShader();
        SetupDefaultGround();
        SetupMinimapCamera();
    }



    private void SetupBufferAndShader()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        int meshVerticesLength = mesh.vertices.Length;

        // set static values
        shader.SetInt("_size", size_x);
        shader.SetInt("_msize", msize_x);
        shader.SetInt("_arraylength", meshVerticesLength);

        // init buffer
        vertbuffer = new ComputeBuffer(meshVerticesLength, 12);
        vertbuffer.SetData(mesh.vertices);
        weightsbuffer = new ComputeBuffer(meshVerticesLength, 4);
        weightsbuffer.SetData(initweightbuffer());
        outputbuffer = new ComputeBuffer(meshVerticesLength, 4);
        outputbuffer.SetData(initweightbuffer());

        //"small comptueshader color gradient": init buffer
        /*
        colors = new Color[meshVerticesLength];
        colorsbuffer = new ComputeBuffer[] {
                                            new ComputeBuffer(colors.Length, 16),
                                            new ComputeBuffer(colors.Length, 16)
                                           };
           
        // set buffer for Shader
         Shader.SetGlobalBuffer("_hmColors", colorsbuffer[1 - computingBuffer]);
        */
    }

    private void SetupDefaultGround()
    {
        defaultGround.GetComponent<MeshRenderer>().enabled = false;
        defaultGround.transform.localScale = new Vector3(size_x * tileSize, size_x * tileSize, size_x * tileSize);
        defaultGround.transform.localPosition = new Vector3(size_x * tileSize / 2, 0, size_x * tileSize / 2);
    }

    private void SetupMinimapCamera()
    {
        Vector3 cameraTransform = minimapCamera.transform.localPosition;
        minimapCamera.transform.localPosition = new Vector3(size_x * tileSize / 2, cameraTransform.y, size_x * tileSize / 2);
    }
    // buffer filler
    private float[] initweightbuffer()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        float[] fillarray = new float[mesh.vertices.Length];
        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            fillarray[i] = 0;
        }
        return fillarray;
    }


    // UI
    public void switchTrackingEnable()
    {
        trackingEnabled = !trackingEnabled;
        if (minimapScript != null)
        {
            minimapScript.Init();
        }
        else
        {
            Debug.LogWarning("No Heatmap MinimapScript attached");
        }
    }
    public void SetSmooth(float value)
    {
        smoothvalue = value;
    }

    private void Update()
    {
        // calcualte the heatmap
        if (heatmapCalculationDone)
        {
            heatmapCalculationDone = false;
            StartCoroutine("drawHeatmapWithKDE");
        }

        // Keyboard Tool Controller 
        if (Input.GetKeyDown(KeyCode.V))
        {
            switchTrackingEnable();
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (smoothvalue <= 1.0f)
            {
                smoothvalue = smoothvalue + 0.05f;
               
            }
        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            if (smoothvalue > 0.01f)
            {
                smoothvalue = smoothvalue - 0.05f;
            }
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            GetComponent<MeshRenderer>().enabled= !GetComponent<MeshRenderer>().enabled;
            defaultGroundScript.switchVisibility();
        }
    }

    // Increment position weight in GPU buffer
    void TrackPositionForVertices()
    {
        if (trackingEnabled)
        {
            playerPos = player.transform.position;

            // set scaled positon
            int pos_x_index = Mathf.RoundToInt(playerPos.x / tileSize);
            int pos_z_index = Mathf.RoundToInt(playerPos.z / tileSize);

            // check if numbers inside grid (numbers positive (1+1=2) and in range?)
            if (Mathf.Sign(pos_x_index) + Mathf.Sign(pos_z_index) == 2 && pos_x_index <= size_x && pos_z_index <= size_z)
            {
                //get vertex index number by reverse calculation -> see: generateMeshGridFromScratch()
                int vertindex = pos_z_index * (size_x + 1) + pos_x_index;

                // Increment position weight in GPU buffer
                shader.SetInt("_vertindex", vertindex);
                int kernelID = shader.FindKernel("IncVerWeight");
                shader.SetBuffer(kernelID, "_weights", weightsbuffer);
                shader.Dispatch(kernelID, 1, 1, 1);

                sumAddedPosToTracking++;
            }
            else
            {
                Debug.Log("Out of bounds");
            }
        }
    }

    // generate Mesh Grid
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

        int[] triangles = new int[numTris * 3];

        // generate vertex grid
        int x, z;
        for (z = 0; z < vsize_z; z++)
        {
            for (x = 0; x < vsize_x; x++)
            {
                vertices[z * vsize_x + x] = new Vector3((float)x * tileSize, 0, (float)z * tileSize);
                normals[z * vsize_x + x] = Vector3.up;
            }
        }

        // build triangles
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

        //assigne mesh to components
        MeshFilter mesh_filter = GetComponent<MeshFilter>();
        mesh_filter.mesh = mesh;

    }

    // color the vertices
    IEnumerator drawHeatmapWithKDE()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

        // computeshader
        int kernelIndex = shader.FindKernel("CSMain");

        // reset static values (safety)  
        shader.SetInt("_size", size_x);
        shader.SetInt("_msize", msize_x);
        shader.SetInt("_arraylength", vertices.Length);

        // set change values
        shader.SetFloat("_smooth", smoothvalue);
        shader.SetFloat("_summax", sumAddedPosToTracking);

        // set buffers
        shader.SetBuffer(kernelIndex, "_vertices", vertbuffer);
        shader.SetBuffer(kernelIndex, "_weights", weightsbuffer);

        // "color gradient": set computeshader output buffer
        shader.SetBuffer(kernelIndex, "_output", outputbuffer);

        // "small comptueshader color gradient": buffer 
            //shader.SetBuffer(kernelIndex, "_colors", colorsbuffer[computingBuffer]);

        // start GPU calculation
        shader.Dispatch(kernelIndex, (size_x / 32) + 1, (size_z / 32) + 1, 1);

        // "color gradient": update Shader with computeshader results
        Shader.SetGlobalBuffer("_hmWeights", outputbuffer);

        // "small comptueshader color gradient":update Shader with computeshader results
            //Shader.SetGlobalBuffer("_hmColors", colorsbuffer[computingBuffer]);
            //computingBuffer = 1 - computingBuffer;

        // set buffer for Shader
        Shader.SetGlobalColor("_botColor", colorGradient.colorKeys[0].color);
        Shader.SetGlobalColor("_midColor", colorGradient.colorKeys[1].color);
        Shader.SetGlobalColor("_topColor", colorGradient.colorKeys[2].color);


        if (Input.GetKeyDown(KeyCode.E))
        {
            float[] foobar = new float[vertices.Length];
            outputbuffer.GetData(foobar);

            Debug.Log("e down" + sumAddedPosToTracking);
            for (int i = 0; i < foobar.Length; i++)
            {
                Debug.Log(foobar[i] + "" + vertices[i]);
                yield return null;
            }
        }

        heatmapCalculationDone = true;
    }

    // buffers need to be released at the end of the program
    private void OnDestroy()
    {
        vertbuffer.Release();
        weightsbuffer.Release();
        outputbuffer.Release();

        //"small comptueshader color gradient": buffer need to be released 
            //colorsbuffer[0].Release();
            //colorsbuffer[1].Release();
    }
}