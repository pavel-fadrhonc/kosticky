using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace DefaultNamespace
{
    [CreateAssetMenu(fileName = nameof(GameSettings), menuName = "GridWorld/GameSettings")]
    public class GameSettings : ScriptableObject
    {
        [SerializeField] private List<Biome> _generatedBiomes;
        public IReadOnlyList<Biome> GeneratedBiomes
        {
            get => this._generatedBiomes;
        }

        [SerializeField] private List<Biome> _userBiomes;
        public IReadOnlyList<Biome> UserBiomes
        {
            get => this._userBiomes;
        }
        
        [SerializeField] private float _biomeUVSize;
        public float BiomeUVSize
        {
            get => this._biomeUVSize;
        }
        
        /// <summary>
        /// In voxels
        /// </summary>
        [SerializeField] private int _worldHeight;
        public int WorldHeight
        {
            get => this._worldHeight;
        }
        
        /// <summary>
        /// In voxels
        /// </summary>
        [SerializeField] private int _chunkSize;
        public int ChunkSize
        {
            get => this._chunkSize;
        }

        /// <summary>
        /// In Voxels
        /// </summary>
        [SerializeField] private int sectorSize;
        public int SectorSize
        {
            get => this.sectorSize;
        }

        /// <summary>
        /// In meters
        /// </summary>
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

        private float _chunkSizeWS;
        /// <summary>
        /// // how large is chunk in meters
        /// </summary>
        public float ChunkSizeWS => _chunkSizeWS;

        [Tooltip("How many precision iterations is made when raycasting the world")]
        [SerializeField] private int _raycastPrecisionIterationCount;
        public int RaycastPrecisionIterationCount
        {
            get => this._raycastPrecisionIterationCount;
        }

        private void OnEnable()
        {
            _chunkSizeWS = _chunkSize * _voxelSize;
        }
    }
}