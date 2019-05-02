using System;
using MarchingCubesStruct;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine.Serialization;



public class VectortoolAbstractScriptAdvance : MonoBehaviour
{

    private Vector3 _measuredSize;
    private Vector3 _measuredCenter;
    private Vector3[,,] _posMatrix;
    private Dictionary<int, int> _sphereColliderMap = new Dictionary<int, int>();
    private int[] _sphereColliderArray;
    private List<int> _sphereColliderList = new List<int>();

    //marching cube
    private List<CubeStruct> _cubeStructList = new List<CubeStruct>();
    private Voxel[] _voxels;

    // shader
    [FormerlySerializedAs("m_material")] public Material mMaterial;
    [FormerlySerializedAs("m_Highlight")] public Material mHighlight;
    private Shader _shaderDefault;
    private Shader _shader0;
    private Shader _shader1;
    private Shader _shader2;
    private Shader _shader3;


    private List<GameObject> _meshes = new List<GameObject>();
    private int[] _allactivatableVoxels;
    public float threshold;

    public bool isRecording;

    private int _dynamicMin = 0;
    private int _dynamicMax = 0;

    private MeshCollider _meshCollider;
    [FormerlySerializedAs("m_LayerMask")] public LayerMask mLayerMask;
    private int _xDimensionCounter = 0, _yDimensionCounter = 0, _zDimensionCounter = 0;
    private bool _colorswitch = true;
    public float cubesize = 0.8f;

    public bool voxelGlobalState = true;

    // complex mesh settings
    public bool combineChildren = false;
    public LayerMask roomLayerMask;
    private bool _meshCoroutineRunning = false;


    private void Start()
    {
        threshold = 0.0f;

        // Set Meshcollider  
        _meshCollider = transform.GetComponent<MeshCollider>();

        //if children exist, combine them to parent mesh 
        if (combineChildren)
        {
            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
            CombineInstance[] combine = new CombineInstance[meshFilters.Length];
            int i = 0;
            while (i < meshFilters.Length)
            {
                combine[i].mesh = meshFilters[i].sharedMesh;
                combine[i].transform = transform.worldToLocalMatrix * meshFilters[i].transform.localToWorldMatrix;
                //meshFilters[i].gameObject.SetActive(false);
                i++;
            }
            transform.GetComponent<MeshFilter>().mesh = new Mesh();
            transform.GetComponent<MeshFilter>().mesh.CombineMeshes(combine);
            //transform.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Diffuse"));
            _meshCollider.sharedMesh = transform.GetComponent<MeshFilter>().mesh;
            //transform.gameObject.SetActive(true);
        }


        _measuredSize = MeasureSizeOfGameObject();
        _measuredCenter = MeasureCenterOfGameObject();

        Debug.Log("size: " + _measuredSize + " center :" + _measuredCenter + " max:" + GetComponent<MeshCollider>().bounds.max + " min:" + GetComponent<MeshCollider>().bounds.min);
        FillScatterplotWithCollider();
        
        // meshcollider is disabled for performance reasons, it is no longer necessary.
        //meshCollider.enabled = false;

        GenerateMarchingCubeGrid();

        _cubeStructsArray = _cubeStructList.ToArray();
        _vertsVec = new Vector3[_cubeStructsArray.Length][];
        _triindicesVec = new int[_cubeStructsArray.Length][];

        RemoveUnnecessaryComponents();
        _allactivatableVoxels = GetAllactivatableVoxels();

        _shaderDefault = Shader.Find("Standard");
        _shader0 = Shader.Find("SuperSystems/Wireframe");
        _shader1 = Shader.Find("SuperSystems/Wireframe-Shaded-Unlit");
        _shader2 = Shader.Find("SuperSystems/Wireframe-Transparent");
        _shader3 = Shader.Find("SuperSystems/Wireframe-Transparent-Culled");

    }

    private void Update()
    {

        UpdateActiveVoxels();
        PerformMarchingCubeAlgorithm();

        // Keyboard Tool Controller 
        if (Input.GetKeyDown(KeyCode.F))
        {
            isRecording = !isRecording;
            Debug.Log("Recording :" + isRecording);
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (threshold <= 1.0f)
            {
                threshold = threshold + 0.1f;
            }
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (threshold > 0.01f)
            {
                threshold = threshold - 0.1f;
            }
        }
        if (Input.GetKeyDown(KeyCode.J))
        {
            HideShowMeshes();
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            //switch between viewspace
            voxelGlobalState = !voxelGlobalState;
            if (voxelGlobalState)
                //RenderSettings.ambientIntensity = 1.0f;
                Debug.Log("Show: Viewed");
            else
                //RenderSettings.ambientIntensity = 0.1f;
                Debug.Log("Show: Missed");
        }
        if (Input.GetKeyDown(KeyCode.F5))
        {

            //_sphereColliderMap.Keys.ToList().ForEach(x => _sphereColliderMap[x] = 0);
            _dynamicMax = 0;
        }
    }

    // Update hashmap (remote)
    //public void UpdateCollision(Vector3 center, Vector3 player, Vector3 closestPointOnCollider)
    public void UpdateCollision(int id, Vector3 player, Vector3 closestPointOnCollider)
    {
        Debug.DrawRay(closestPointOnCollider, player - closestPointOnCollider, Color.magenta);

        if (isRecording && !Physics.Linecast(player, closestPointOnCollider, roomLayerMask))
        {
            
            if (_sphereColliderMap.ContainsKey(id))
            {
                _sphereColliderMap[id] += 1;
                //Debug.Log("Update:" + center + " to " + sphereColliderMap[center]);
                if (_sphereColliderMap[id] > _dynamicMax)
                {
                    _dynamicMax = _sphereColliderMap[id];
                }
            }
            else
            {
                Debug.Log("ERROR: " + id + " is not part of map");
            }
           
            /*_sphereColliderArray[id] += 1;
            if(_sphereColliderArray[id] > _dynamicMax)
            {
                _dynamicMax = _sphereColliderArray[id];
            }*/

        }
    }

    // Measure size and Center of GameObject
    private Vector3 MeasureSizeOfGameObject()
    {
        return GetComponent<MeshCollider>().bounds.size;
    }
    private Vector3 MeasureCenterOfGameObject()
    {
        return GetComponent<MeshCollider>().bounds.center;
    }

    #region User_Interface_Functions
    public void StartStopRecording()
    {
        isRecording = !isRecording;
    }
    public void HideShowMeshes()
    {
        foreach (GameObject mesh in _meshes)
        {
            bool vis = mesh.GetComponent<MeshRenderer>().isVisible;
            mesh.SetActive(!vis);
        }
    }
    public void HideShowMeshes(bool setVisibility)
    {
        foreach (GameObject mesh in _meshes)
        {
            mesh.SetActive(setVisibility);
        }
    }
    public void SetThreshold(float value)
    {
        threshold = value;
    }
    public void SwitchShader(int option)
    {
        Debug.Log("shader option: " + option);
        switch (option)
        {
            case 0:
                mMaterial.shader = _shader0;
                break;
            case 1:
                mMaterial.shader = _shader1;
                break;
            case 2:
                mMaterial.shader = _shader2;
                break;
            case 3:
                mMaterial.shader = _shader3;
                break;
            default:
                mMaterial.shader = _shaderDefault;
                break;
        }
    }
    #endregion

    #region Debug_Functions
    /*
    private void DEBUGgrowCubes(int min, int max, float threshold)
    {

        GameObject[] gameObjects = GameObject.FindGameObjectsWithTag("simpleSphere");

        for (var i = 0; i < gameObjects.Length; i++)
        {
            Destroy(gameObjects[i]);
        }

        GameObject simpleSphere = Instantiate(Resources.Load("Prefabs/SimpleSphere") as GameObject) as GameObject;
        foreach (KeyValuePair<Vector3, int> entry in sphereColliderMap)
        {
            if (entry.Value == 0)
                continue;
            if ((NormalizeMinMax(entry.Value, min, max) < threshold))
                continue;
            GameObject Brush = Instantiate(simpleSphere) as GameObject;
            Brush.transform.parent = scatterplot.transform;
            Brush.transform.localScale = new Vector3((NormalizeMinMax(entry.Value, min, max)) * 0.25f, (NormalizeMinMax(entry.Value, min, max)) * 0.25f, (NormalizeMinMax(entry.Value, min, max)) * 0.25f);
            Brush.transform.position = entry.Key;
            Brush.tag = "simpleSphere";
        }
        Destroy(simpleSphere);
    }
    */

    private void DEBUG_OnDrawGizmos()
    {
        /*
        int k = 0;
        foreach (CubeStruct newcube in cubeStructList)
        {
            if (k == 0 || k == 7)
            {
                Vector3[] debuggiz = newcube.EdgeToEdgeVertices;
                for (int i = 0; i < debuggiz.Length; i++)
                {
                    switch (i)
                    {
                        case 0:
                            Gizmos.color = Color.red;
                            break;
                        case 1:
                            Gizmos.color = Color.green;
                            break;
                        case 2:
                            Gizmos.color = Color.blue;
                            break;
                        case 3:
                            Gizmos.color = Color.black;
                            break;
                        case 4:
                            Gizmos.color = Color.red;
                            break;
                        case 5:
                            Gizmos.color = Color.green;
                            break;
                        case 6:
                            Gizmos.color = Color.blue;
                            break;
                        case 7:
                            Gizmos.color = Color.black;
                            break;
                        case 8:
                            Gizmos.color = Color.red;
                            break;
                        case 9:
                            Gizmos.color = Color.green;
                            break;
                        case 10:
                            Gizmos.color = Color.green;
                            break;
                        case 11:
                            Gizmos.color = Color.black;
                            break;
                        default:
                            Gizmos.color = Color.yellow;
                            break;
                    }
                    Gizmos.DrawSphere(debuggiz[i], 0.1f);
                }
            }
            k++;
        }
        */
        int zi = 0;
        foreach (CubeStruct cube in _cubeStructList)
        {
            switch (cube.EdgeVoxels[0].voxelNumber % 8)
            {
                case 0:
                    Gizmos.color = Color.red;
                    break;
                case 1:
                    Gizmos.color = Color.green;
                    break;
                case 2:
                    Gizmos.color = Color.blue;
                    break;
                case 3:
                    Gizmos.color = Color.cyan;
                    break;

                case 4:
                    Gizmos.color = Color.white;
                    break;
                case 5:
                    Gizmos.color = Color.magenta;
                    break;
                case 6:
                    Gizmos.color = Color.gray;
                    break;
                case 7:
                    Gizmos.color = Color.black;
                    break;
                default:
                    Gizmos.color = Color.yellow;
                    break;
            }
            //Gizmos.color = Color.white;
            Gizmos.DrawSphere(cube.EdgeVoxels[0].position, 0.05f);
            zi++;
        }
    }
    private void DebugDrawCubeLines(Voxel a, Voxel b, Voxel c, Voxel d, Voxel e, Voxel f, Voxel g, Voxel h)
    {
        //if (colorswitch)
        //{
        Debug.DrawLine(a.position, b.position, Color.green, 1000.0f);
        Debug.DrawLine(b.position, c.position, Color.green, 1000.0f);
        Debug.DrawLine(c.position, d.position, Color.green, 1000.0f);
        Debug.DrawLine(d.position, a.position, Color.green, 1000.0f);

        Debug.DrawLine(e.position, f.position, Color.blue, 1000.0f);
        Debug.DrawLine(f.position, g.position, Color.blue, 1000.0f);
        Debug.DrawLine(g.position, h.position, Color.blue, 1000.0f);
        Debug.DrawLine(h.position, e.position, Color.blue, 1000.0f);

        Debug.DrawLine(a.position, e.position, Color.white, 1000.0f);
        Debug.DrawLine(b.position, f.position, Color.white, 1000.0f);
        Debug.DrawLine(c.position, g.position, Color.white, 1000.0f);
        Debug.DrawLine(d.position, h.position, Color.white, 1000.0f);
        _colorswitch = !_colorswitch;
        //}
        //else
        //{
        //    colorswitch = !colorswitch;
        //}
    }
    #endregion

    #region Preproprocess_Steps
    // The Scatterplot needs Collider for Collision detection
    private void FillScatterplotWithCollider()
    {
        // generate prefab
        GameObject transparentBoxColliderCubePrefab = Instantiate(Resources.Load("Prefabs/TransparentBoxColliderCubePrefab") as GameObject);
        transparentBoxColliderCubePrefab.name = "TransparentBoxColliderCubePrefab";

        // modify collider
        // makes sure collider is  trigger
        BoxCollider boxCollider = transparentBoxColliderCubePrefab.GetComponent<BoxCollider>();
        boxCollider.isTrigger = true;

        float colliderVolume = _measuredSize.x * _measuredSize.y * _measuredSize.z;
        float cubeFiller = colliderVolume / colliderVolume * cubesize;
        colliderVolume = Mathf.Round(colliderVolume);

        Bounds bounds = GetComponent<MeshCollider>().bounds;
        int posMatrixX = (int)Mathf.Ceil(_measuredSize.x / cubeFiller);
        int posMatrixY = (int)Mathf.Ceil(_measuredSize.y / cubeFiller) * posMatrixX;
        int posMatrixZ = (int)Mathf.Ceil(_measuredSize.z / cubeFiller) * posMatrixY;
        string s = $"X{posMatrixX}, Y{posMatrixY}, Z{posMatrixZ}"; 
        Debug.Log(" The mighty string: "+s);
        _posMatrix = new Vector3[posMatrixX, posMatrixY, posMatrixZ];

        int loopIndex = 0;
        for (float x = 0; x <= _measuredSize.x; x = x + cubeFiller)
        {
            _yDimensionCounter = 0;
            for (float y = 0; y <= _measuredSize.y; y = y + cubeFiller)
            {
                _zDimensionCounter = 0;
                for (float z = 0; z <= _measuredSize.z; z = z + cubeFiller)
                {
                    // set brush position
                    Vector3 brushPos = new Vector3(/*
                                                    NormalizeMinMax(x, measuredCenter.x - measuredCenter.x / 2, measuredCenter.x + measuredCenter.x / 2) ,
                                                    NormalizeMinMax(y, measuredCenter.y - measuredCenter.y / 2, measuredCenter.x + measuredCenter.x / 2) ,
                                                    NormalizeMinMax(z, measuredCenter.z - measuredCenter.z / 2, measuredCenter.x + measuredCenter.x / 2) 
                                                    */
                                                   /*
                                                   Mathf.LerpUnclamped(NormalizeMinMax(x, 0, measuredSize.x), bounds.min.x, bounds.max.x),
                                                   Mathf.LerpUnclamped(NormalizeMinMax(y, 0, measuredSize.y), bounds.min.y, bounds.max.y),
                                                   Mathf.LerpUnclamped(NormalizeMinMax(z, 0, measuredSize.z), bounds.min.z, bounds.max.z)
                                                   */
                                                     bounds.min.x + x,
                                                     bounds.min.y + y,
                                                     bounds.min.z + z
                                                );
                    // init copy and make boxcollider part of the room
                    GameObject boxColliderBrush = Instantiate(transparentBoxColliderCubePrefab, transform, true);
                    boxColliderBrush.transform.localScale = new Vector3(cubeFiller, cubeFiller, cubeFiller);
                    boxColliderBrush.transform.position = brushPos;
                    boxColliderBrush.GetComponent<ColliderBoxScript>().Id = loopIndex;

                    /*  TODO: FIX FOR THIS // <<<<<-<<<<<-<<<<<-<<<<<-<<<<<-<<<<<-<<<<<-<<<<<-<<<<<-<<<<<-<<<<<- REACTIVATE
                     * Remove outer collider trigger
                     * this prevents unintentional clipping  
                     * 
                     * LATE COMMENT (also for the todo): I HAVE NO IDEA WHAT THIS MEAN..... BUT IT WORKS. 
                     */
                    if ((x == 0 || x == 1.0f / _measuredSize.x) || (y == 0 || y == 1.0f / _measuredSize.y) || (z == 0 || z == 1.0f / _measuredSize.z))
                    {
                        //BoxColliderBrush.GetComponent<BoxCollider>().isTrigger = false;

                        /*
                         *  MISSING FUNCTION
                         * 
                         */
                    }

                    // Check which areas collide with the prefab. Destroy the brush if it doesn't intersect with the room. 
                    bool keepBrush = false;
                    Collider[] hitColliders = Physics.OverlapBox(boxColliderBrush.transform.position, boxColliderBrush.transform.localScale / 2, Quaternion.identity, mLayerMask) ?? throw new ArgumentNullException("Physics.OverlapBox(boxColliderBrush.transform.position, boxColliderBrush.transform.localScale / 2, Quaternion.identity, mLayerMask)");
                    foreach (Collider collider in hitColliders)
                    {
                        if (_meshCollider == collider)
                        {
                            keepBrush = true;
                            break;
                        }
                    }

                    if (keepBrush)
                    {
                        // add collider box to matrix for marching cube algorithm
                        //todo multi dict ,no vector3
                        
                        
                        //_sphereColliderMap.Add(boxColliderBrush.transform.position, 0);
                        _sphereColliderMap.Add(loopIndex, 0);
                       
                        //_sphereColliderList.Add(loopIndex);
                        
                    }
                    else
                    {
                        //Destroy(BoxColliderBrush);
                        boxColliderBrush.GetComponent<BoxCollider>().isTrigger = false;
                        boxColliderBrush.GetComponent<BoxCollider>().enabled = false;
                        boxColliderBrush.SetActive(false);
                    }

                    _posMatrix[_xDimensionCounter, _yDimensionCounter, _zDimensionCounter] = boxColliderBrush.transform.position;
                    _zDimensionCounter++;
                    loopIndex++;
                }
                _yDimensionCounter++;
            }
            _xDimensionCounter++;
        }
        Debug.Log("x " + _xDimensionCounter + " y " + _yDimensionCounter + " z " + _zDimensionCounter);
        // Delete prefab to prevent overlapping in scene
        Destroy(transparentBoxColliderCubePrefab);
        
       /* Debug.Log("Count : "+_sphereColliderList.Count);       
        int row = _sphereColliderList.Count;
        int[] temp = _sphereColliderList.ToArray();
        for (int i = 0; i < temp.Length; i++)
        {
            temp[i] = 0;
        }

        _sphereColliderArray = temp;
*/
    }
    
    // Create the Grid for the Marching cube algorithm
    private void GenerateMarchingCubeGrid()
    {
        // Generate voxels
        int voxelIndexCounter = 0;
        _voxels = new Voxel[_xDimensionCounter * _yDimensionCounter * _zDimensionCounter];

        for (int x = 0; x < _xDimensionCounter; x++)
        {
            for (int y = 0; y < _yDimensionCounter; y++)
            {
                for (int z = 0; z < _zDimensionCounter; z++)
                {
                    //Debug.Log("index:"+ voxelIndexCounter + "x:"+x+" y:"+y+" z:"+z+" ="+posMatrix[x,y,z]);
                    _voxels[voxelIndexCounter] = new Voxel(voxelIndexCounter, false, _posMatrix[x, y, z], false);
                    voxelIndexCounter++;
                }
            }
        }

        // Generate cubes
        /* 
         *  create a cube (dimension: voxel.x+1 voxel.y+1 voxel.z+1) from each voxel.
         *  skip the first and last voxel of a row( "i" mod "boxSideSize"),
         *  skip the first and last ("boxSideSize") voxels of a  colum,
         *  and  the first and last voxels of the cube layer( "i" < ("voxelIndexCounter" - "boxSideSize" * "boxSideSize")) => the complete last row*colum.
         */
        /*
         * x_Dimension_counter == ROW
         * y_Dimension_counter == COLUMN
         * z_Dimension_counter == DEPTH
         */
        Debug.Log(voxelIndexCounter);
        int columnCounter = 1;
        bool jumper = false;
        for (int i = 0; i < (voxelIndexCounter - _yDimensionCounter * _zDimensionCounter); i++)
        {
            // if last last element in current COLUM is reached, jump to next COLUMN in current ROW.
            if ((i + 1) % _zDimensionCounter == 0)
            {
                jumper = true;
                columnCounter++;

                // if next COLUM is the last COLUM in current ROW skip it and jump to next ROW. 
                // ("i" is set to last Element in current ROW and get incremented at the end of the loop).
                if (columnCounter == _yDimensionCounter)
                {
                    columnCounter = 1;
                    i = i + _zDimensionCounter;
                }
            }
            // if last ROW is reached do nothing || do nothing if jumper == true .
            //if (i >= (x_Dimension_counter - 1) * y_Dimension_counter * z_Dimension_counter || jumper || i >= i+ z_Dimension_counter * y_Dimension_counter + z_Dimension_counter+1)
            if (jumper)
            {
                jumper = false;
            }
            else
            {
                /*
               Debug.LogFormat("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7},",
                   i,
                   i + 1,
                   i + z_Dimension_counter + 1,
                   i + z_Dimension_counter,
                   i + z_Dimension_counter * y_Dimension_counter,
                   i + z_Dimension_counter * y_Dimension_counter + 1,
                   i + z_Dimension_counter * y_Dimension_counter + z_Dimension_counter + 1,
                   i + z_Dimension_counter * y_Dimension_counter + z_Dimension_counter );
                */
                // create cube 
                TriangulateCube(
                _voxels[i],
                _voxels[i + 1],
                _voxels[i + _zDimensionCounter + 1],
                _voxels[i + _zDimensionCounter],
                _voxels[i + _zDimensionCounter * _yDimensionCounter],
                _voxels[i + _zDimensionCounter * _yDimensionCounter + 1],
                _voxels[i + _zDimensionCounter * _yDimensionCounter + _zDimensionCounter + 1],
                _voxels[i + _zDimensionCounter * _yDimensionCounter + _zDimensionCounter]
                );
            }
        }
    }

    // create a cube and add to list  
    private void TriangulateCube(Voxel a, Voxel b, Voxel c, Voxel d, Voxel e, Voxel f, Voxel g, Voxel h)
    {
        //DebugDrawCubeLines(a, b, c, d, e, f, g, h);
        CubeStruct newcube = new CubeStruct(new Voxel[] { a, b, c, d, e, f, g, h });
        _cubeStructList.Add(newcube);
    }

    private int[] GetAllactivatableVoxels()
    {
        List<int> g = new List<int>();
        for (int i = 0; i < _voxels.Length; i++)
        {
            if (_sphereColliderMap.ContainsKey(i))
            {
                g.Add(i);
            }
        }
        return g.ToArray();
    }

    private void RemoveUnnecessaryComponents()
    {
        foreach (Transform prefabCube in transform)
        {
            if (!prefabCube.gameObject.activeSelf)
            {
                Destroy(prefabCube.gameObject);
            }
        }
    }
    #endregion

    // update active voxels
    private void UpdateActiveVoxels()
    {
        int max = _dynamicMax;  //int max = sphereColliderMap.Values.Max();
        int min = 0;//sphereColliderMap.Values.Min();

        //DEBUG growCubes(min,max,threshold);

        // activate voxel 
                                                                foreach (int voxelIndexPos in _allactivatableVoxels)
                                                                {
                                                                    if (NormalizeMinMax(_sphereColliderMap[voxelIndexPos], min, max) > threshold)
                                                                    {
                                                                        _voxels[voxelIndexPos].state = voxelGlobalState;
                                                                    }
                                                                    else
                                                                    {
                                                                        _voxels[voxelIndexPos].state = !voxelGlobalState;
                                                                    }
                                                                }

       /* for (int i = 0; i < _sphereColliderArray.Length; i++)
        {
            if (NormalizeMinMax(_sphereColliderMap[i],min,max) > threshold)
            {
                _voxels[i].state = voxelGlobalState;
            }
            else
            {
                _voxels[i].state = !voxelGlobalState;      
            }
        }*/
    }

    private CubeStruct[] _cubeStructsArray;
    private Vector3[][] _vertsVec;
    private int[][] _triindicesVec;
    private int[] _thistrianglesVec;

    private void PerformMarchingCubeAlgorithm()
    {
        // build triangels from cube list
        //cubeStructsArray = cubeStructList.ToArray();
        //vertsVec = new Vector3[cubeStructsArray.Length][];
        //triindicesVec = new int[cubeStructsArray.Length][];

        // Run over each cube
        for (int c = 0; c < _cubeStructsArray.Length; c++)
        {
            _thistrianglesVec = new int[15] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
            int flagIndex = 0;

            // get active voxels
            for (int i = 0; i < 8; i++)
            {
                if (_cubeStructsArray[c].EdgeVoxels[i].state)
                {
                    flagIndex |= 1 << i;
                }
            }

            // set triangles based on active voxels
            // there can only be 5 triangles per cube
            int ttvIndex = 0;
            for (int i = 0; i < 5; i++)
            {
                if (TriangleConnectionTable[flagIndex][3 * i] < 0)
                {
                    break;
                }

                for (int j = 0; j < 3; j++)
                {
                    int indexPos = TriangleConnectionTable[flagIndex][3 * i + j];
                    _thistrianglesVec[ttvIndex] = indexPos;
                    ttvIndex++;
                }
            }
            // add cube vertices
            _triindicesVec[c] = _thistrianglesVec;
            _vertsVec[c] = _cubeStructsArray[c].EdgeToEdgeVertices;
        }

        if (!_meshCoroutineRunning)
        {
            StartCoroutine(UpdateMeshes(_vertsVec, _triindicesVec));
        }
        //UpdateMeshes(vertsVec, triindicesVec);
    }
    
    /*
     *  arrays are set, now generate meshes 
     * 
     */
    private IEnumerator UpdateMeshes(Vector3[][] vertsArray, int[][] triindicesArray)
    //private void UpdateMeshes(Vector3[][] vertsArray, int[][] triindicesArray)
    {
        _meshCoroutineRunning = true;
        int maxVertsPerMesh = 30000; //must be divisible by 3, ie 3 verts == 1 triangle
        //int maxVertsPerMesh = 15000; //must be divisible by 3, ie 3 verts == 1 triangle
        //int maxVertsPerMesh = 7500; //must be divisible by 3, ie 3 verts == 1 triangle
        int vertsArrayLength = vertsArray.Length;
        int numMeshes = (vertsArrayLength * 12) / maxVertsPerMesh + 1;

        // generate mesh

        int vertsArrayIndex = 0;
        for (int i = 0; i < numMeshes; i++)
        {
            Vector3[] vertices = new Vector3[maxVertsPerMesh];
            int[] triangles = new int[maxVertsPerMesh * 3];
            Color[] voxColor = new Color[vertices.Length];

            // mesh max vertices is limited
            /*
             * j = vertex counter per mesh
             * d = index of vertex array (need to be past between meshes) 
             * g = triangle index counter  per mesh
             */

            int v = 0;
            for (int j = 0, d = vertsArrayIndex, g = 0; (j < maxVertsPerMesh && d < vertsArrayLength); d++, g++)
            {
                // perform marching cube algorithm  "actually"
                // perform current cube "d"
                // hack "i already have all verts in an array, i iterate over the mesh, so if there are no vertes(cubes) left, do nothing"
                if (d < vertsArrayLength)
                {
                    //add current cube vertices (the lines not the edges)
                    for (int k = 0; k < 12; k++)
                    {
                        vertices[j] = vertsArray[d][k];

                        /*
                         * use for colored triangles
                         * /
                        float lerpvalue = 0;
                        Vector3[] cubevoxels = cubeStructsArray[d].getVoxelPosFromEdgeToEdgeVertices(k);                      
                        lerpvalue = NormalizeMinMax((float)sphereColliderMap[cubevoxels[0]] + (float)sphereColliderMap[cubevoxels[1]], min, max);
                        voxColor[j] = Color.Lerp(Color.black,Color.white,lerpvalue);
                        //voxColor[j] = UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
                        */
                        j++;
                    }
                    // add all triangles of current cube, if there are some
                    int triintriindicesArrayAtDLength = triindicesArray[d].Length;
                    if (triintriindicesArrayAtDLength != 0)
                    {
                        int offset = v;
                        for (int t = 0; t < triintriindicesArrayAtDLength; t++)
                        {
                            if (triindicesArray[d][t] == -1)
                            {
                                break;
                            }
                            triangles[t + offset] = triindicesArray[d][t] + (g * 12);
                            v++;
                        }
                    }
                }

                if (j >= maxVertsPerMesh)
                {
                    vertsArrayIndex = d;
                }
            }

            Mesh mesh;
            GameObject go;
            if (i == _meshes.Count)
            {
                mesh = new Mesh();
                mesh.vertices = vertices;
                mesh.triangles = triangles;
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                mesh.colors = voxColor;

                go = new GameObject("Mesh");
                go.transform.parent = transform;
                go.AddComponent<MeshFilter>();
                go.AddComponent<MeshRenderer>();
                if (voxelGlobalState)
                {
                    go.GetComponent<Renderer>().material = mMaterial;
                }
                else
                {
                    go.GetComponent<Renderer>().material = mHighlight;
                }
                go.GetComponent<MeshFilter>().mesh = mesh;

                _meshes.Add(go);
            }
            else
            {
                mesh = _meshes.ElementAt(i).GetComponent<MeshFilter>().mesh;
                mesh.vertices = vertices;
                mesh.triangles = triangles;
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                mesh.colors = voxColor;

                go = _meshes.ElementAt(i);
                go.transform.parent = transform;
                if (voxelGlobalState)
                {
                    go.GetComponent<Renderer>().material = mMaterial;
                }
                else
                {
                    go.GetComponent<Renderer>().material = mHighlight;
                }

                go.GetComponent<MeshFilter>().mesh = mesh;
            }

            yield return null;
        }
        _meshCoroutineRunning = false;
    }

    // static functions 
    private static float NormalizeMinMax(float value, float min, float max)
    {
        return (value - min) / (max - min);
    }

    // DEBUG all possible bit combinations 
    private static readonly int[] CubeEdgeFlags = new int[]
    {
        0x000, 0x109, 0x203, 0x30a, 0x406, 0x50f, 0x605, 0x70c, 0x80c, 0x905, 0xa0f, 0xb06, 0xc0a, 0xd03, 0xe09, 0xf00,
        0x190, 0x099, 0x393, 0x29a, 0x596, 0x49f, 0x795, 0x69c, 0x99c, 0x895, 0xb9f, 0xa96, 0xd9a, 0xc93, 0xf99, 0xe90,
        0x230, 0x339, 0x033, 0x13a, 0x636, 0x73f, 0x435, 0x53c, 0xa3c, 0xb35, 0x83f, 0x936, 0xe3a, 0xf33, 0xc39, 0xd30,
        0x3a0, 0x2a9, 0x1a3, 0x0aa, 0x7a6, 0x6af, 0x5a5, 0x4ac, 0xbac, 0xaa5, 0x9af, 0x8a6, 0xfaa, 0xea3, 0xda9, 0xca0,
        0x460, 0x569, 0x663, 0x76a, 0x066, 0x16f, 0x265, 0x36c, 0xc6c, 0xd65, 0xe6f, 0xf66, 0x86a, 0x963, 0xa69, 0xb60,
        0x5f0, 0x4f9, 0x7f3, 0x6fa, 0x1f6, 0x0ff, 0x3f5, 0x2fc, 0xdfc, 0xcf5, 0xfff, 0xef6, 0x9fa, 0x8f3, 0xbf9, 0xaf0,
        0x650, 0x759, 0x453, 0x55a, 0x256, 0x35f, 0x055, 0x15c, 0xe5c, 0xf55, 0xc5f, 0xd56, 0xa5a, 0xb53, 0x859, 0x950,
        0x7c0, 0x6c9, 0x5c3, 0x4ca, 0x3c6, 0x2cf, 0x1c5, 0x0cc, 0xfcc, 0xec5, 0xdcf, 0xcc6, 0xbca, 0xac3, 0x9c9, 0x8c0,
        0x8c0, 0x9c9, 0xac3, 0xbca, 0xcc6, 0xdcf, 0xec5, 0xfcc, 0x0cc, 0x1c5, 0x2cf, 0x3c6, 0x4ca, 0x5c3, 0x6c9, 0x7c0,
        0x950, 0x859, 0xb53, 0xa5a, 0xd56, 0xc5f, 0xf55, 0xe5c, 0x15c, 0x055, 0x35f, 0x256, 0x55a, 0x453, 0x759, 0x650,
        0xaf0, 0xbf9, 0x8f3, 0x9fa, 0xef6, 0xfff, 0xcf5, 0xdfc, 0x2fc, 0x3f5, 0x0ff, 0x1f6, 0x6fa, 0x7f3, 0x4f9, 0x5f0,
        0xb60, 0xa69, 0x963, 0x86a, 0xf66, 0xe6f, 0xd65, 0xc6c, 0x36c, 0x265, 0x16f, 0x066, 0x76a, 0x663, 0x569, 0x460,
        0xca0, 0xda9, 0xea3, 0xfaa, 0x8a6, 0x9af, 0xaa5, 0xbac, 0x4ac, 0x5a5, 0x6af, 0x7a6, 0x0aa, 0x1a3, 0x2a9, 0x3a0,
        0xd30, 0xc39, 0xf33, 0xe3a, 0x936, 0x83f, 0xb35, 0xa3c, 0x53c, 0x435, 0x73f, 0x636, 0x13a, 0x033, 0x339, 0x230,
        0xe90, 0xf99, 0xc93, 0xd9a, 0xa96, 0xb9f, 0x895, 0x99c, 0x69c, 0x795, 0x49f, 0x596, 0x29a, 0x393, 0x099, 0x190,
        0xf00, 0xe09, 0xd03, 0xc0a, 0xb06, 0xa0f, 0x905, 0x80c, 0x70c, 0x605, 0x50f, 0x406, 0x30a, 0x203, 0x109, 0x000
    };

    // all triangle cases for a cube
    private static readonly int[][] TriangleConnectionTable =
    {
        new int []{-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{0, 1, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{1, 8, 3, 9, 8, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{0, 8, 3, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{9, 2, 10, 0, 2, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{2, 8, 3, 2, 10, 8, 10, 9, 8, -1, -1, -1, -1, -1, -1, -1},
        new int []{3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{0, 11, 2, 8, 11, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{1, 9, 0, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{1, 11, 2, 1, 9, 11, 9, 8, 11, -1, -1, -1, -1, -1, -1, -1},
        new int []{3, 10, 1, 11, 10, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{0, 10, 1, 0, 8, 10, 8, 11, 10, -1, -1, -1, -1, -1, -1, -1},
        new int []{3, 9, 0, 3, 11, 9, 11, 10, 9, -1, -1, -1, -1, -1, -1, -1},
        new int []{9, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{4, 3, 0, 7, 3, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{0, 1, 9, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{4, 1, 9, 4, 7, 1, 7, 3, 1, -1, -1, -1, -1, -1, -1, -1},
        new int []{1, 2, 10, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{3, 4, 7, 3, 0, 4, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1},
        new int []{9, 2, 10, 9, 0, 2, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1},
        new int []{2, 10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4, -1, -1, -1, -1},
        new int []{8, 4, 7, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{11, 4, 7, 11, 2, 4, 2, 0, 4, -1, -1, -1, -1, -1, -1, -1},
        new int []{9, 0, 1, 8, 4, 7, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1},
        new int []{4, 7, 11, 9, 4, 11, 9, 11, 2, 9, 2, 1, -1, -1, -1, -1},
        new int []{3, 10, 1, 3, 11, 10, 7, 8, 4, -1, -1, -1, -1, -1, -1, -1},
        new int []{1, 11, 10, 1, 4, 11, 1, 0, 4, 7, 11, 4, -1, -1, -1, -1},
        new int []{4, 7, 8, 9, 0, 11, 9, 11, 10, 11, 0, 3, -1, -1, -1, -1},
        new int []{4, 7, 11, 4, 11, 9, 9, 11, 10, -1, -1, -1, -1, -1, -1, -1},
        new int []{9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{9, 5, 4, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{0, 5, 4, 1, 5, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{8, 5, 4, 8, 3, 5, 3, 1, 5, -1, -1, -1, -1, -1, -1, -1},
        new int []{1, 2, 10, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{3, 0, 8, 1, 2, 10, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1},
        new int []{5, 2, 10, 5, 4, 2, 4, 0, 2, -1, -1, -1, -1, -1, -1, -1},
        new int []{2, 10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8, -1, -1, -1, -1},
        new int []{9, 5, 4, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{0, 11, 2, 0, 8, 11, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1},
        new int []{0, 5, 4, 0, 1, 5, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1},
        new int []{2, 1, 5, 2, 5, 8, 2, 8, 11, 4, 8, 5, -1, -1, -1, -1},
        new int []{10, 3, 11, 10, 1, 3, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1},
        new int []{4, 9, 5, 0, 8, 1, 8, 10, 1, 8, 11, 10, -1, -1, -1, -1},
        new int []{5, 4, 0, 5, 0, 11, 5, 11, 10, 11, 0, 3, -1, -1, -1, -1},
        new int []{5, 4, 8, 5, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1},
        new int []{9, 7, 8, 5, 7, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{9, 3, 0, 9, 5, 3, 5, 7, 3, -1, -1, -1, -1, -1, -1, -1},
        new int []{0, 7, 8, 0, 1, 7, 1, 5, 7, -1, -1, -1, -1, -1, -1, -1},
        new int []{1, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{9, 7, 8, 9, 5, 7, 10, 1, 2, -1, -1, -1, -1, -1, -1, -1},
        new int []{10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3, -1, -1, -1, -1},
        new int []{8, 0, 2, 8, 2, 5, 8, 5, 7, 10, 5, 2, -1, -1, -1, -1},
        new int []{2, 10, 5, 2, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1},
        new int []{7, 9, 5, 7, 8, 9, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1},
        new int []{9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 11, -1, -1, -1, -1},
        new int []{2, 3, 11, 0, 1, 8, 1, 7, 8, 1, 5, 7, -1, -1, -1, -1},
        new int []{11, 2, 1, 11, 1, 7, 7, 1, 5, -1, -1, -1, -1, -1, -1, -1},
        new int []{9, 5, 8, 8, 5, 7, 10, 1, 3, 10, 3, 11, -1, -1, -1, -1},
        new int []{5, 7, 0, 5, 0, 9, 7, 11, 0, 1, 0, 10, 11, 10, 0, -1},
        new int []{11, 10, 0, 11, 0, 3, 10, 5, 0, 8, 0, 7, 5, 7, 0, -1},
        new int []{11, 10, 5, 7, 11, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{0, 8, 3, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{9, 0, 1, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{1, 8, 3, 1, 9, 8, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1},
        new int []{1, 6, 5, 2, 6, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{1, 6, 5, 1, 2, 6, 3, 0, 8, -1, -1, -1, -1, -1, -1, -1},
        new int []{9, 6, 5, 9, 0, 6, 0, 2, 6, -1, -1, -1, -1, -1, -1, -1},
        new int []{5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8, -1, -1, -1, -1},
        new int []{2, 3, 11, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{11, 0, 8, 11, 2, 0, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1},
        new int []{0, 1, 9, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1},
        new int []{5, 10, 6, 1, 9, 2, 9, 11, 2, 9, 8, 11, -1, -1, -1, -1},
        new int []{6, 3, 11, 6, 5, 3, 5, 1, 3, -1, -1, -1, -1, -1, -1, -1},
        new int []{0, 8, 11, 0, 11, 5, 0, 5, 1, 5, 11, 6, -1, -1, -1, -1},
        new int []{3, 11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9, -1, -1, -1, -1},
        new int []{6, 5, 9, 6, 9, 11, 11, 9, 8, -1, -1, -1, -1, -1, -1, -1},
        new int []{5, 10, 6, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{4, 3, 0, 4, 7, 3, 6, 5, 10, -1, -1, -1, -1, -1, -1, -1},
        new int []{1, 9, 0, 5, 10, 6, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1},
        new int []{10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4, -1, -1, -1, -1},
        new int []{6, 1, 2, 6, 5, 1, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1},
        new int []{1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7, -1, -1, -1, -1},
        new int []{8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6, -1, -1, -1, -1},
        new int []{7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9, -1},
        new int []{3, 11, 2, 7, 8, 4, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1},
        new int []{5, 10, 6, 4, 7, 2, 4, 2, 0, 2, 7, 11, -1, -1, -1, -1},
        new int []{0, 1, 9, 4, 7, 8, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1},
        new int []{9, 2, 1, 9, 11, 2, 9, 4, 11, 7, 11, 4, 5, 10, 6, -1},
        new int []{8, 4, 7, 3, 11, 5, 3, 5, 1, 5, 11, 6, -1, -1, -1, -1},
        new int []{5, 1, 11, 5, 11, 6, 1, 0, 11, 7, 11, 4, 0, 4, 11, -1},
        new int []{0, 5, 9, 0, 6, 5, 0, 3, 6, 11, 6, 3, 8, 4, 7, -1},
        new int []{6, 5, 9, 6, 9, 11, 4, 7, 9, 7, 11, 9, -1, -1, -1, -1},
        new int []{10, 4, 9, 6, 4, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{4, 10, 6, 4, 9, 10, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1},
        new int []{10, 0, 1, 10, 6, 0, 6, 4, 0, -1, -1, -1, -1, -1, -1, -1},
        new int []{8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1, 10, -1, -1, -1, -1},
        new int []{1, 4, 9, 1, 2, 4, 2, 6, 4, -1, -1, -1, -1, -1, -1, -1},
        new int []{3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4, -1, -1, -1, -1},
        new int []{0, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{8, 3, 2, 8, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1},
        new int []{10, 4, 9, 10, 6, 4, 11, 2, 3, -1, -1, -1, -1, -1, -1, -1},
        new int []{0, 8, 2, 2, 8, 11, 4, 9, 10, 4, 10, 6, -1, -1, -1, -1},
        new int []{3, 11, 2, 0, 1, 6, 0, 6, 4, 6, 1, 10, -1, -1, -1, -1},
        new int []{6, 4, 1, 6, 1, 10, 4, 8, 1, 2, 1, 11, 8, 11, 1, -1},
        new int []{9, 6, 4, 9, 3, 6, 9, 1, 3, 11, 6, 3, -1, -1, -1, -1},
        new int []{8, 11, 1, 8, 1, 0, 11, 6, 1, 9, 1, 4, 6, 4, 1, -1},
        new int []{3, 11, 6, 3, 6, 0, 0, 6, 4, -1, -1, -1, -1, -1, -1, -1},
        new int []{6, 4, 8, 11, 6, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{7, 10, 6, 7, 8, 10, 8, 9, 10, -1, -1, -1, -1, -1, -1, -1},
        new int []{0, 7, 3, 0, 10, 7, 0, 9, 10, 6, 7, 10, -1, -1, -1, -1},
        new int []{10, 6, 7, 1, 10, 7, 1, 7, 8, 1, 8, 0, -1, -1, -1, -1},
        new int []{10, 6, 7, 10, 7, 1, 1, 7, 3, -1, -1, -1, -1, -1, -1, -1},
        new int []{1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7, -1, -1, -1, -1},
        new int []{2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9, -1},
        new int []{7, 8, 0, 7, 0, 6, 6, 0, 2, -1, -1, -1, -1, -1, -1, -1},
        new int []{7, 3, 2, 6, 7, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{2, 3, 11, 10, 6, 8, 10, 8, 9, 8, 6, 7, -1, -1, -1, -1},
        new int []{2, 0, 7, 2, 7, 11, 0, 9, 7, 6, 7, 10, 9, 10, 7, -1},
        new int []{1, 8, 0, 1, 7, 8, 1, 10, 7, 6, 7, 10, 2, 3, 11, -1},
        new int []{11, 2, 1, 11, 1, 7, 10, 6, 1, 6, 7, 1, -1, -1, -1, -1},
        new int []{8, 9, 6, 8, 6, 7, 9, 1, 6, 11, 6, 3, 1, 3, 6, -1},
        new int []{0, 9, 1, 11, 6, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{7, 8, 0, 7, 0, 6, 3, 11, 0, 11, 6, 0, -1, -1, -1, -1},
        new int []{7, 11, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{3, 0, 8, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{0, 1, 9, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{8, 1, 9, 8, 3, 1, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1},
        new int []{10, 1, 2, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{1, 2, 10, 3, 0, 8, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1},
        new int []{2, 9, 0, 2, 10, 9, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1},
        new int []{6, 11, 7, 2, 10, 3, 10, 8, 3, 10, 9, 8, -1, -1, -1, -1},
        new int []{7, 2, 3, 6, 2, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{7, 0, 8, 7, 6, 0, 6, 2, 0, -1, -1, -1, -1, -1, -1, -1},
        new int []{2, 7, 6, 2, 3, 7, 0, 1, 9, -1, -1, -1, -1, -1, -1, -1},
        new int []{1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6, -1, -1, -1, -1},
        new int []{10, 7, 6, 10, 1, 7, 1, 3, 7, -1, -1, -1, -1, -1, -1, -1},
        new int []{10, 7, 6, 1, 7, 10, 1, 8, 7, 1, 0, 8, -1, -1, -1, -1},
        new int []{0, 3, 7, 0, 7, 10, 0, 10, 9, 6, 10, 7, -1, -1, -1, -1},
        new int []{7, 6, 10, 7, 10, 8, 8, 10, 9, -1, -1, -1, -1, -1, -1, -1},
        new int []{6, 8, 4, 11, 8, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{3, 6, 11, 3, 0, 6, 0, 4, 6, -1, -1, -1, -1, -1, -1, -1},
        new int []{8, 6, 11, 8, 4, 6, 9, 0, 1, -1, -1, -1, -1, -1, -1, -1},
        new int []{9, 4, 6, 9, 6, 3, 9, 3, 1, 11, 3, 6, -1, -1, -1, -1},
        new int []{6, 8, 4, 6, 11, 8, 2, 10, 1, -1, -1, -1, -1, -1, -1, -1},
        new int []{1, 2, 10, 3, 0, 11, 0, 6, 11, 0, 4, 6, -1, -1, -1, -1},
        new int []{4, 11, 8, 4, 6, 11, 0, 2, 9, 2, 10, 9, -1, -1, -1, -1},
        new int []{10, 9, 3, 10, 3, 2, 9, 4, 3, 11, 3, 6, 4, 6, 3, -1},
        new int []{8, 2, 3, 8, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1},
        new int []{0, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8, -1, -1, -1, -1},
        new int []{1, 9, 4, 1, 4, 2, 2, 4, 6, -1, -1, -1, -1, -1, -1, -1},
        new int []{8, 1, 3, 8, 6, 1, 8, 4, 6, 6, 10, 1, -1, -1, -1, -1},
        new int []{10, 1, 0, 10, 0, 6, 6, 0, 4, -1, -1, -1, -1, -1, -1, -1},
        new int []{4, 6, 3, 4, 3, 8, 6, 10, 3, 0, 3, 9, 10, 9, 3, -1},
        new int []{10, 9, 4, 6, 10, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{4, 9, 5, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{0, 8, 3, 4, 9, 5, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1},
        new int []{5, 0, 1, 5, 4, 0, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1},
        new int []{11, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5, -1, -1, -1, -1},
        new int []{9, 5, 4, 10, 1, 2, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1},
        new int []{6, 11, 7, 1, 2, 10, 0, 8, 3, 4, 9, 5, -1, -1, -1, -1},
        new int []{7, 6, 11, 5, 4, 10, 4, 2, 10, 4, 0, 2, -1, -1, -1, -1},
        new int []{3, 4, 8, 3, 5, 4, 3, 2, 5, 10, 5, 2, 11, 7, 6, -1},
        new int []{7, 2, 3, 7, 6, 2, 5, 4, 9, -1, -1, -1, -1, -1, -1, -1},
        new int []{9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7, -1, -1, -1, -1},
        new int []{3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0, -1, -1, -1, -1},
        new int []{6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8, -1},
        new int []{9, 5, 4, 10, 1, 6, 1, 7, 6, 1, 3, 7, -1, -1, -1, -1},
        new int []{1, 6, 10, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4, -1},
        new int []{4, 0, 10, 4, 10, 5, 0, 3, 10, 6, 10, 7, 3, 7, 10, -1},
        new int []{7, 6, 10, 7, 10, 8, 5, 4, 10, 4, 8, 10, -1, -1, -1, -1},
        new int []{6, 9, 5, 6, 11, 9, 11, 8, 9, -1, -1, -1, -1, -1, -1, -1},
        new int []{3, 6, 11, 0, 6, 3, 0, 5, 6, 0, 9, 5, -1, -1, -1, -1},
        new int []{0, 11, 8, 0, 5, 11, 0, 1, 5, 5, 6, 11, -1, -1, -1, -1},
        new int []{6, 11, 3, 6, 3, 5, 5, 3, 1, -1, -1, -1, -1, -1, -1, -1},
        new int []{1, 2, 10, 9, 5, 11, 9, 11, 8, 11, 5, 6, -1, -1, -1, -1},
        new int []{0, 11, 3, 0, 6, 11, 0, 9, 6, 5, 6, 9, 1, 2, 10, -1},
        new int []{11, 8, 5, 11, 5, 6, 8, 0, 5, 10, 5, 2, 0, 2, 5, -1},
        new int []{6, 11, 3, 6, 3, 5, 2, 10, 3, 10, 5, 3, -1, -1, -1, -1},
        new int []{5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2, -1, -1, -1, -1},
        new int []{9, 5, 6, 9, 6, 0, 0, 6, 2, -1, -1, -1, -1, -1, -1, -1},
        new int []{1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8, -1},
        new int []{1, 5, 6, 2, 1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{1, 3, 6, 1, 6, 10, 3, 8, 6, 5, 6, 9, 8, 9, 6, -1},
        new int []{10, 1, 0, 10, 0, 6, 9, 5, 0, 5, 6, 0, -1, -1, -1, -1},
        new int []{0, 3, 8, 5, 6, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{10, 5, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{11, 5, 10, 7, 5, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{11, 5, 10, 11, 7, 5, 8, 3, 0, -1, -1, -1, -1, -1, -1, -1},
        new int []{5, 11, 7, 5, 10, 11, 1, 9, 0, -1, -1, -1, -1, -1, -1, -1},
        new int []{10, 7, 5, 10, 11, 7, 9, 8, 1, 8, 3, 1, -1, -1, -1, -1},
        new int []{11, 1, 2, 11, 7, 1, 7, 5, 1, -1, -1, -1, -1, -1, -1, -1},
        new int []{0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2, 11, -1, -1, -1, -1},
        new int []{9, 7, 5, 9, 2, 7, 9, 0, 2, 2, 11, 7, -1, -1, -1, -1},
        new int []{7, 5, 2, 7, 2, 11, 5, 9, 2, 3, 2, 8, 9, 8, 2, -1},
        new int []{2, 5, 10, 2, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1},
        new int []{8, 2, 0, 8, 5, 2, 8, 7, 5, 10, 2, 5, -1, -1, -1, -1},
        new int []{9, 0, 1, 5, 10, 3, 5, 3, 7, 3, 10, 2, -1, -1, -1, -1},
        new int []{9, 8, 2, 9, 2, 1, 8, 7, 2, 10, 2, 5, 7, 5, 2, -1},
        new int []{1, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{0, 8, 7, 0, 7, 1, 1, 7, 5, -1, -1, -1, -1, -1, -1, -1},
        new int []{9, 0, 3, 9, 3, 5, 5, 3, 7, -1, -1, -1, -1, -1, -1, -1},
        new int []{9, 8, 7, 5, 9, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{5, 8, 4, 5, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1},
        new int []{5, 0, 4, 5, 11, 0, 5, 10, 11, 11, 3, 0, -1, -1, -1, -1},
        new int []{0, 1, 9, 8, 4, 10, 8, 10, 11, 10, 4, 5, -1, -1, -1, -1},
        new int []{10, 11, 4, 10, 4, 5, 11, 3, 4, 9, 4, 1, 3, 1, 4, -1},
        new int []{2, 5, 1, 2, 8, 5, 2, 11, 8, 4, 5, 8, -1, -1, -1, -1},
        new int []{0, 4, 11, 0, 11, 3, 4, 5, 11, 2, 11, 1, 5, 1, 11, -1},
        new int []{0, 2, 5, 0, 5, 9, 2, 11, 5, 4, 5, 8, 11, 8, 5, -1},
        new int []{9, 4, 5, 2, 11, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{2, 5, 10, 3, 5, 2, 3, 4, 5, 3, 8, 4, -1, -1, -1, -1},
        new int []{5, 10, 2, 5, 2, 4, 4, 2, 0, -1, -1, -1, -1, -1, -1, -1},
        new int []{3, 10, 2, 3, 5, 10, 3, 8, 5, 4, 5, 8, 0, 1, 9, -1},
        new int []{5, 10, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2, -1, -1, -1, -1},
        new int []{8, 4, 5, 8, 5, 3, 3, 5, 1, -1, -1, -1, -1, -1, -1, -1},
        new int []{0, 4, 5, 1, 0, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5, -1, -1, -1, -1},
        new int []{9, 4, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{4, 11, 7, 4, 9, 11, 9, 10, 11, -1, -1, -1, -1, -1, -1, -1},
        new int []{0, 8, 3, 4, 9, 7, 9, 11, 7, 9, 10, 11, -1, -1, -1, -1},
        new int []{1, 10, 11, 1, 11, 4, 1, 4, 0, 7, 4, 11, -1, -1, -1, -1},
        new int []{3, 1, 4, 3, 4, 8, 1, 10, 4, 7, 4, 11, 10, 11, 4, -1},
        new int []{4, 11, 7, 9, 11, 4, 9, 2, 11, 9, 1, 2, -1, -1, -1, -1},
        new int []{9, 7, 4, 9, 11, 7, 9, 1, 11, 2, 11, 1, 0, 8, 3, -1},
        new int []{11, 7, 4, 11, 4, 2, 2, 4, 0, -1, -1, -1, -1, -1, -1, -1},
        new int []{11, 7, 4, 11, 4, 2, 8, 3, 4, 3, 2, 4, -1, -1, -1, -1},
        new int []{2, 9, 10, 2, 7, 9, 2, 3, 7, 7, 4, 9, -1, -1, -1, -1},
        new int []{9, 10, 7, 9, 7, 4, 10, 2, 7, 8, 7, 0, 2, 0, 7, -1},
        new int []{3, 7, 10, 3, 10, 2, 7, 4, 10, 1, 10, 0, 4, 0, 10, -1},
        new int []{1, 10, 2, 8, 7, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{4, 9, 1, 4, 1, 7, 7, 1, 3, -1, -1, -1, -1, -1, -1, -1},
        new int []{4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1, -1, -1, -1, -1},
        new int []{4, 0, 3, 7, 4, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{4, 8, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{9, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{3, 0, 9, 3, 9, 11, 11, 9, 10, -1, -1, -1, -1, -1, -1, -1},
        new int []{0, 1, 10, 0, 10, 8, 8, 10, 11, -1, -1, -1, -1, -1, -1, -1},
        new int []{3, 1, 10, 11, 3, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{1, 2, 11, 1, 11, 9, 9, 11, 8, -1, -1, -1, -1, -1, -1, -1},
        new int []{3, 0, 9, 3, 9, 11, 1, 2, 9, 2, 11, 9, -1, -1, -1, -1},
        new int []{0, 2, 11, 8, 0, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{3, 2, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{2, 3, 8, 2, 8, 10, 10, 8, 9, -1, -1, -1, -1, -1, -1, -1},
        new int []{9, 10, 2, 0, 9, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{2, 3, 8, 2, 8, 10, 0, 1, 8, 1, 10, 8, -1, -1, -1, -1},
        new int []{1, 10, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{1, 3, 8, 9, 1, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{0, 9, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{0, 3, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        new int []{-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}
    };
}