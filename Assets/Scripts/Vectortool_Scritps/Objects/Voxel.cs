using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * The voxel object each cube has
 * the voxel number is not used at the moment.
 */
namespace MarchingCubesStruct
{
    public class Voxel
    {
        public bool state;
        public Vector3 position;
        public int voxelNumber;
        public bool isMarkedAsHighlight;

        public Voxel(int num,bool vis, Vector3 pos, bool marked)
        {
            state = vis;
            position = pos;
            voxelNumber = num;
            isMarkedAsHighlight = marked;
        }
    }
}