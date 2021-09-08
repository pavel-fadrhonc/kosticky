using System;
using System.Collections.Generic;
using DefaultNamespace.Utils;
using UnityEngine;

namespace DefaultNamespace
{
    public class WorldManager : UnityEngine.MonoBehaviour
    {
        private Sector _currentSector;
        private Vector2Int _currentSectorIdx;
        private int _sectorSpan; // how many sectors in row or column are next to the active one
        private int _sectorMiddleIdx; // index into center and starting sector

        private int _chunkSize;  // how many voxels are in the chunk (chunks are squared)
        private float _voxelSize; 
        private Vector3 _noiseScale; 
        private float _sectorSize; 

        private const int SECTOR_ROW_COLUMN_SIZE = 3;
        private const int MAX_SECTOR_ROWS_COLUMNS = 100;

        private Sector[,] _sectors = new Sector[MAX_SECTOR_ROWS_COLUMNS, MAX_SECTOR_ROWS_COLUMNS];

        private float _chunkSizeWS; // how large is chunk in meters
        private float _sectorSizeWS;
        
        private void Start()
        {
            var gameCon = Locator.Instance.GameSettings;
            
            _sectorSize = gameCon.SectorSize;
            _chunkSize = gameCon.ChunkSize;
            _voxelSize = gameCon.VoxelSize;
            _noiseScale = gameCon.NoiseScale;

            _chunkSizeWS = _chunkSize * _voxelSize;
            _sectorSizeWS = _sectorSize * _voxelSize;

            _sectorSpan = Mathf.FloorToInt(SECTOR_ROW_COLUMN_SIZE * 0.5f);
            var startIdx = Mathf.FloorToInt(MAX_SECTOR_ROWS_COLUMNS * 0.5f) - _sectorSpan;
            var endIdx = startIdx + SECTOR_ROW_COLUMN_SIZE;
            var middleIdx = startIdx + _sectorSpan;

            for (int sectorX = startIdx; sectorX < endIdx; sectorX++)
            {
                for (int sectorY = startIdx; sectorY < endIdx; sectorY++)
                {
                    var sector = new Sector();
                    Vector3 sectorOffset = new Vector3((sectorX - middleIdx) * _sectorSizeWS, 0,
                        (sectorY - middleIdx) * _sectorSizeWS);
                    sector.Generate(sectorOffset, new Vector2(5,5));
                    sector.SectorInfo = $"Sector:({sectorX},{sectorY})";
                    _sectors[sectorX, sectorY] = sector;
                }
            }

            _currentSectorIdx = new Vector2Int(middleIdx, middleIdx);
            _currentSector = _sectors[_currentSectorIdx.x, _currentSectorIdx.y];

            Locator.Instance.CharacterController.transform.position = new Vector3(_sectorSizeWS * 0.5f,
                Locator.Instance.GameSettings.WorldHeight, _sectorSizeWS * 0.5f);
            //
            // _currentSector = new Sector();
            // _currentSector.Generate(Vector2.zero, Vector2.zero);

            for (int chunkX = 0; chunkX < _currentSector.Chunks.GetLength(0); chunkX++)
            {
                for (int chunkZ = 0; chunkZ < _currentSector.Chunks.GetLength(1); chunkZ++)
                {
                    var chunk = _currentSector.Chunks[chunkX, chunkZ];
                    
                    // var chunkBaseIdxX = chunkSize * chunkX;
                    // var chunkBaseIdxZ = chunkSize * chunkZ;
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;
            
        }

        private void Update()
        {
            for (int chunkX = 0; chunkX < _currentSector.Chunks.GetLength(0); chunkX++)
            {
                for (int chunkZ = 0; chunkZ < _currentSector.Chunks.GetLength(1); chunkZ++)
                {
                    var chunk = _currentSector.Chunks[chunkX, chunkZ];
                    
                    DebugDraw.DrawBounds(chunk.Bounds);
                }
            }
        }

        /// <summary>
        /// In the current sector
        /// </summary>
        /// <param name="worldPos"></param>
        /// <returns></returns>
        public float GeVoxelHeightAtWorldPos(Vector3 worldPos)
        {
            var voxelYIndex = GetVoxelYIndexAtWorldPos(worldPos);

            return voxelYIndex * _voxelSize + _voxelSize;
        }

        public VoxelInfo GetVoxelAtWorldPos(Vector3 worldPos)
        {
            var chunk = GetChunkAtWorldPos(worldPos);
            return chunk.GetVoxelAtWorldPos(worldPos);
        }

        public float GetVoxelYIndexAtWorldPos(Vector3 worldPos)
        {
            var chunk = GetChunkAtWorldPos(worldPos);

            if (chunk == null)
                return 0f;

            var voxelPosX = Mathf.Repeat(worldPos.x, _chunkSizeWS);
            var voxelPosZ = Mathf.Repeat(worldPos.z, _chunkSizeWS);

            var voxelIdxX = Mathf.FloorToInt(voxelPosX / _voxelSize);
            var voxelIdxZ = Mathf.FloorToInt(voxelPosZ / _voxelSize);

            return chunk.voxelHeights[voxelIdxX, voxelIdxZ];            
        }

        private struct IntersectionChunkInfo
        {
            public Chunk chunk;
            public float rayDistance;
        }

        private struct IntersectionSectorInfo
        {
            public Sector sector;
            public float rayDistance;
        }
        
        private List<IntersectionChunkInfo> _intersectionChunks = new List<IntersectionChunkInfo>();
        private List<IntersectionSectorInfo> _intersectionSectors = new List<IntersectionSectorInfo>();
        public bool GetVoxelRayIntersection(Ray ray, out VoxelHitInfo voxelHitInfo, float maxDistance = float.MaxValue)
        {
            // first figure out which sectors are we hitting with the ray
            _intersectionSectors.Clear();
            for (int sectorX = _currentSectorIdx.x - _sectorSpan; sectorX <= _currentSectorIdx.x + _sectorSpan; sectorX++)
            {
                for (int sectorY = _currentSectorIdx.y - _sectorSpan; sectorY <= _currentSectorIdx.y + _sectorSpan; sectorY++)
                {
                    float distance;
                    var sect = _sectors[sectorX, sectorY];
                    if (sect.Bounds.IntersectRay(ray, out distance) && distance < maxDistance)
                    {
                        _intersectionSectors.Add(new IntersectionSectorInfo()
                        {
                            rayDistance = distance,
                            sector = sect
                        });
                    }
                }                
            }
            _intersectionSectors.Sort((s1, s2) => (int) Mathf.Sign(s1.rayDistance - s1.rayDistance));
            _intersectionSectors.Insert(0, new IntersectionSectorInfo() {sector = _currentSector});

            foreach (var intersectionSectorInfo in _intersectionSectors)
            {
                _intersectionChunks.Clear();
                var sector = intersectionSectorInfo.sector;
            
                for (var index0 = 0; index0 < sector.Chunks.GetLength(0); index0++)
                for (var index1 = 0; index1 < sector.Chunks.GetLength(1); index1++)
                {
                    var chunk = sector.Chunks[index0, index1];
                
                    float distance;
                    if (chunk.Bounds.IntersectRay(ray, out distance) && distance < maxDistance)
                    {
                        _intersectionChunks.Add(new IntersectionChunkInfo()
                        {
                            chunk = chunk,
                            rayDistance = distance
                        });
                    }
                }
            
                _intersectionChunks.Sort((ch1, ch2) => (int) Mathf.Sign(ch1.rayDistance - ch2.rayDistance));

                foreach (var intersectionChunk in _intersectionChunks)
                {
                    var hit = intersectionChunk.chunk.GetRayHitInfo(ray, out voxelHitInfo);
                
                    if (hit )
                    {
                        bool distanceCheck = true;
                        if (maxDistance < float.MaxValue)
                            distanceCheck = (voxelHitInfo.voxelInfo.worldPos - ray.origin).magnitude < maxDistance;
                    
                        if (distanceCheck)
                            return true;
                    }
                }                
            }

            voxelHitInfo = default;
            return false;
        }

        public void AddVoxelToWorldPos(Vector3 worldPos, Biome biome)
        {
            var chunk = GetChunkAtWorldPos(worldPos);
            chunk.AddVoxelAtWorldPos(worldPos, biome);
        }

        public void RemoveVoxelOnWorldPos(Vector3 worldPos)
        {
            var chunk = GetChunkAtWorldPos(worldPos);
            chunk.RemoveVoxelAtWorldPos(worldPos);
        }

        private Chunk GetChunkAtWorldPos(Vector3 worldPos)
        {
            // get sector at world pos
            Sector sector = SectorAtWorldPos(worldPos);
                
            var sectorPosition = worldPos - sector.WorldPos;
            
            var chunkX = Mathf.FloorToInt(sectorPosition.x / _chunkSizeWS);

            if (chunkX > sector.Chunks.GetLength(0) - 1)
                return null;

            var chunkZ = Mathf.FloorToInt(sectorPosition.z / _chunkSizeWS);

            if (chunkZ > sector.Chunks.GetLength(1) - 1)
                return null;

            return sector.Chunks[chunkX, chunkZ];
        }

        private Sector SectorAtWorldPos(Vector3 worldPos)
        {
            Sector sector = null;
            for (int sectorX = _currentSectorIdx.x - _sectorSpan; sectorX <= _currentSectorIdx.x + _sectorSpan; sectorX++)
            {
                for (int sectorY = _currentSectorIdx.y - _sectorSpan; sectorY <= _currentSectorIdx.y + _sectorSpan; sectorY++)
                {
                    var sect = _sectors[sectorX, sectorY];

                    if (worldPos.x > sect.WorldPos.x &&
                        worldPos.x < sect.WorldPos.x + _sectorSizeWS &&
                        worldPos.z > sect.WorldPos.z &&
                        worldPos.z < sect.WorldPos.z + _sectorSizeWS)
                    {
                        sector = sect;
                    }
                }                
            }

            return sector;
        }
        
        
    }
}