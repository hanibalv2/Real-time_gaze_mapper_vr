using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
 * The Object a cube is made from
 * 
 */

namespace MarchingCubesStruct
{
    public struct CubeStruct
    {
        public Vector3[] EdgeToEdgeVertices { get; set; }
        public Voxel[] EdgeVoxels { get; set; }

        public CubeStruct(Voxel[] edges)
        {
            EdgeToEdgeVertices = new Vector3[12];
            EdgeVoxels = new Voxel[8];
            EdgeVoxels = edges;
            InterpolateEdgeVertices();
        }

        /*
         * interpolate the points between each neighbour edges.
         */
        private void InterpolateEdgeVertices()
        {
            for (int i = 0; i < EdgeToEdgeVertices.Length; i++)
            {
                if (i == 0 || i == 1 || i == 2 || i == 4 || i == 5 || i == 6)
                {
                    EdgeToEdgeVertices[i] = Vector3.Lerp(EdgeVoxels[i].position, EdgeVoxels[i + 1].position, 0.5f);
                }
                else if (i == 3)
                {
                    EdgeToEdgeVertices[i] = Vector3.Lerp(EdgeVoxels[3].position, EdgeVoxels[0].position, 0.5f);
                }
                else if (i == 7)
                {
                    EdgeToEdgeVertices[i] = Vector3.Lerp(EdgeVoxels[7].position, EdgeVoxels[4].position, 0.5f);
                }
                else if (i == 8 || i == 9 || i == 10 || i == 11)
                {
                    EdgeToEdgeVertices[i] = Vector3.Lerp(EdgeVoxels[i - 8].position, EdgeVoxels[i - 4].position, 0.5f);
                }
            }
        }

        public Vector3[] GetVoxelPosFromEdgeToEdgeVertices(int vertexindex)
        {
            Vector3[] voxels = new Vector3[2];
            switch (vertexindex)
            {
                case 0:
                    voxels = new Vector3[] { EdgeVoxels[0].position, EdgeVoxels[1].position };
                    break;
                case 1:
                    voxels = new Vector3[] { EdgeVoxels[1].position, EdgeVoxels[2].position };
                    break;
                case 2:
                    voxels = new Vector3[] { EdgeVoxels[2].position, EdgeVoxels[3].position };
                    break;
                case 3:
                    voxels = new Vector3[] { EdgeVoxels[3].position, EdgeVoxels[0].position };
                    break;
                case 4:
                    voxels = new Vector3[] { EdgeVoxels[4].position, EdgeVoxels[5].position };
                    break;
                case 5:
                    voxels = new Vector3[] { EdgeVoxels[5].position, EdgeVoxels[6].position };
                    break;
                case 6:
                    voxels = new Vector3[] { EdgeVoxels[6].position, EdgeVoxels[7].position };
                    break;
                case 7:
                    voxels = new Vector3[] { EdgeVoxels[7].position, EdgeVoxels[4].position };
                    break;
                case 8:
                    voxels = new Vector3[] { EdgeVoxels[0].position, EdgeVoxels[4].position };
                    break;
                case 9:
                    voxels = new Vector3[] { EdgeVoxels[1].position, EdgeVoxels[5].position };
                    break;
                case 10:
                    voxels = new Vector3[] { EdgeVoxels[2].position, EdgeVoxels[6].position };
                    break;
                case 11:
                    voxels = new Vector3[] { EdgeVoxels[3].position, EdgeVoxels[7].position };
                    break;
                default: break;
            }
            return voxels;
        }
    }
}