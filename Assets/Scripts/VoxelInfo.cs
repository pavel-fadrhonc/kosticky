using UnityEngine;

namespace DefaultNamespace
{
    public class VoxelInfo
    {
        public Vector3 worldPos;
        public Biome biome;
        public bool isEnclosed; // has all neighbours therefore is not generated
        public int vertexStartIndex = -1; // not all voxels are part of mesh
    }
}