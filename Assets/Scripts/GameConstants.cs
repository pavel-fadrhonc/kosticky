using UnityEngine;

namespace DefaultNamespace
{
    [CreateAssetMenu(fileName = "GameConstants", menuName = "GridWorld/GameConstants")]
    public class GameConstants : ScriptableObject
    {
        [SerializeField] private int _chunkSize;

        public int ChunkSize
        {
            get => this._chunkSize;
        }

        [SerializeField] private Vector3Int _worldSize;

        public Vector3Int WorldSize
        {
            get => this._worldSize;
        }

        [SerializeField] private float _voxelSize;

        public float VoxelSize
        {
            get => this._voxelSize;
        }

        [SerializeField] private Vector3 _noiseScale;

        public Vector3 NoiseScale
        {
            get => this._noiseScale;
        }

        [SerializeField] private Material _voxelMaterial;

        public Material VoxelMaterial
        {
            get => this._voxelMaterial;
        }

        
    }
}