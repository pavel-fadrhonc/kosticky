using System;
using System.Collections.Generic;
using DefaultNamespace.Utils;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace DefaultNamespace
{
    public class WorldManager : UnityEngine.MonoBehaviour
    {
        private Sector _currentSector;
        private Vector2Int _currentSectorIdx;
        private Sector _middleSectorInCluster;
        private Vector2Int _middleSectorInClusterIdx;
        
        private int _sectorSpan; // how many sectors in row or column are next to the active one
        private int _sectorAbsoluteMiddleIdx; // index into center and starting sector in terms of all possible sectors
        private float _sectorGenerateTresholdWS;

        private int _chunkSize;  // how many voxels are in the chunk (chunks are squared)
        private float _voxelSize; 
        private Vector3 _noiseScale; 
        private float _sectorSize;
        private Vector2 _noiseOffsetSpan = new Vector2(5,20);
        private Vector2 _noiseOffset;

        private const int SECTOR_CLUSTER_SIZE = 3;
        private const int MAX_SECTOR_ROWS_COLUMNS = 100;

        private SectorWithChanges[,] _sectors = new SectorWithChanges[MAX_SECTOR_ROWS_COLUMNS, MAX_SECTOR_ROWS_COLUMNS];

        private float _chunkSizeWS; // how large is chunk in meters
        private float _sectorSizeWS;

        private UserChanges _userChanges = new UserChanges();

        private List<Biome> _userBiomes;
        
        private void Start()
        {
            var gameCon = Locator.Instance.GameSettings;

            _userBiomes = new List<Biome>(gameCon.UserBiomes);
            _sectorSize = gameCon.SectorSize;
            _chunkSize = gameCon.ChunkSize;
            _voxelSize = gameCon.VoxelSize;
            _noiseScale = gameCon.NoiseScale;

            _chunkSizeWS = _chunkSize * _voxelSize;
            _sectorSizeWS = _sectorSize * _voxelSize;
            _sectorGenerateTresholdWS = gameCon.SectorPosGenerateTreshold * _sectorSizeWS;

            _noiseOffset = new Vector2(Random.Range(_noiseOffsetSpan.x, _noiseOffsetSpan.y),
                Random.Range(_noiseOffsetSpan.x, _noiseOffsetSpan.y));
            
            _sectorSpan = Mathf.FloorToInt(SECTOR_CLUSTER_SIZE * 0.5f);
            var startIdx = Mathf.FloorToInt(MAX_SECTOR_ROWS_COLUMNS * 0.5f) - _sectorSpan;
            var endIdx = startIdx + SECTOR_CLUSTER_SIZE;
            _sectorAbsoluteMiddleIdx = startIdx + _sectorSpan;
            
            _currentSectorIdx = new Vector2Int(_sectorAbsoluteMiddleIdx, _sectorAbsoluteMiddleIdx);
            _middleSectorInClusterIdx = _currentSectorIdx;
            
            _userChanges.Init();
            if (PlayerPrefs.HasKey(Constants.LOAD_SAVE_FILE_FLAG_NAME))
            {
                _userChanges.Load();

                _noiseOffset =
                    new Vector2(PlayerPrefs.GetFloat(Constants.SAVED_NOISE_OFFSET_X_KEY,_noiseOffset.x),
                        PlayerPrefs.GetFloat(Constants.SAVED_NOISE_OFFSET_Y_KEY, _noiseOffset.y));
                PlayerPrefs.DeleteKey(Constants.LOAD_SAVE_FILE_FLAG_NAME);
            }

            // load changes from save file
            for (int x = 0; x < MAX_SECTOR_ROWS_COLUMNS; x++)
            {
                for (int y = 0; y < MAX_SECTOR_ROWS_COLUMNS; y++)
                {
                    var sectorWithChange = new SectorWithChanges();
                    _sectors[x, y] = sectorWithChange;

                    Vector3 sectorWP = new Vector3((x - _sectorAbsoluteMiddleIdx) * _sectorSizeWS, 0,
                        (y - _sectorAbsoluteMiddleIdx) * _sectorSizeWS);

                    foreach (var addChange in _userChanges.AddChanges)
                    {
                        if (addChange.WorldPos.x > sectorWP.x &&
                            addChange.WorldPos.x < sectorWP.x + _sectorSizeWS &&
                            addChange.WorldPos.z > sectorWP.z &&
                            addChange.WorldPos.z < sectorWP.z + _sectorSizeWS)
                        {
                            sectorWithChange.addChanged.Add(addChange);
                        }
                    }
                    
                    foreach (var removeChange in _userChanges.RemoveChanges)
                    {
                        if (removeChange.WorldPos.x > sectorWP.x &&
                            removeChange.WorldPos.x < sectorWP.x + _sectorSizeWS &&
                            removeChange.WorldPos.z > sectorWP.z &&
                            removeChange.WorldPos.z < sectorWP.z + _sectorSizeWS)
                        {
                            sectorWithChange.removeChanges.Add(removeChange);
                        }
                    }                    
                }
            }

            // generate cluster            
            for (int sectorX = startIdx; sectorX < endIdx; sectorX++)
            {
                for (int sectorY = startIdx; sectorY < endIdx; sectorY++)
                {
                    GenerateNewSector(new Vector2Int(sectorX, sectorY));
                }
            }

            _currentSector = _sectors[_currentSectorIdx.x, _currentSectorIdx.y].sector;
            _middleSectorInCluster = _currentSector;
            
            // load changes into cluster
            for (int sectorX = startIdx; sectorX < endIdx; sectorX++)
            {
                for (int sectorY = startIdx; sectorY < endIdx; sectorY++)
                {
                    LoadChangesForSector(new Vector2Int(sectorX, sectorY));
                }
            }

            Locator.Instance.CharacterController.transform.position = new Vector3(_sectorSizeWS * 0.5f,
                Locator.Instance.GameSettings.WorldHeight, _sectorSizeWS * 0.5f);

            // for (int chunkX = 0; chunkX < _currentSector.Chunks.GetLength(0); chunkX++)
            // {
            //     for (int chunkZ = 0; chunkZ < _currentSector.Chunks.GetLength(1); chunkZ++)
            //     {
            //         var chunk = _currentSector.Chunks[chunkX, chunkZ];
            //         
            //         // var chunkBaseIdxX = chunkSize * chunkX;
            //         // var chunkBaseIdxZ = chunkSize * chunkZ;
            //     }
            // }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;
            
        }

        private void Update()
        {
            var charWP = Locator.Instance.CharacterController.transform.position;
            
            var curSec = SectorAtWorldPos(charWP);
            if (curSec != _currentSector)
            {
                int curSecXIdx = _currentSectorIdx.x;
                int curSecYIdx = _currentSectorIdx.y;

                if (curSec.WorldPos.x < _currentSector.WorldPos.x)
                    curSecXIdx--;
                else if (curSec.WorldPos.x >= _currentSector.WorldPos.x + _sectorSizeWS)
                    curSecXIdx++;
                if (curSec.WorldPos.z < _currentSector.WorldPos.z)
                    curSecYIdx--;
                else if (curSec.WorldPos.z >= _currentSector.WorldPos.z + _sectorSizeWS)
                    curSecYIdx++;

                _currentSectorIdx = new Vector2Int(curSecXIdx, curSecYIdx);
                _currentSector = curSec;
            }

            if (_currentSector != _middleSectorInCluster)
            {
                Action righLeftChangesAction = null;
                Action frontBackChangesAction = null;
                if (charWP.x < _middleSectorInCluster.WorldPos.x - _sectorGenerateTresholdWS)
                { // left
                    ForEachClusterIn(EClusterPartType.Column, -_sectorSpan - 1, (x, y) => GenerateNewSector(new Vector2Int(x, y)));
                    righLeftChangesAction = () => { ForEachClusterIn(EClusterPartType.Column, -_sectorSpan, (x, y) => LoadChangesForSector(new Vector2Int(x, y))); };
                    ForEachClusterIn(EClusterPartType.Column, _sectorSpan, (x, y) => DisposeSectorAtIndex(new Vector2Int(x, y)));
                }
                else if (charWP.x >= _middleSectorInCluster.WorldPos.x + _sectorSizeWS + _sectorGenerateTresholdWS)
                { // right
                    ForEachClusterIn(EClusterPartType.Column, _sectorSpan + 1, (x, y) => GenerateNewSector(new Vector2Int(x, y)));
                    righLeftChangesAction = () => { ForEachClusterIn(EClusterPartType.Column, _sectorSpan, (x, y) => LoadChangesForSector(new Vector2Int(x, y))); };
                    ForEachClusterIn(EClusterPartType.Column, -_sectorSpan, (x, y) => DisposeSectorAtIndex(new Vector2Int(x, y)));
                }

                if (charWP.z < _middleSectorInCluster.WorldPos.z - _sectorGenerateTresholdWS)
                { // back
                    ForEachClusterIn(EClusterPartType.Row, -_sectorSpan - 1, (x, y) => GenerateNewSector(new Vector2Int(x, y)));
                    frontBackChangesAction = () => { ForEachClusterIn(EClusterPartType.Row, -_sectorSpan, (x, y) => LoadChangesForSector(new Vector2Int(x, y))); };
                    ForEachClusterIn(EClusterPartType.Row, _sectorSpan, (x, y) => DisposeSectorAtIndex(new Vector2Int(x, y)));
                }
                else if (charWP.z >= _middleSectorInCluster.WorldPos.z + _sectorSizeWS + _sectorGenerateTresholdWS)
                { // front
                    ForEachClusterIn(EClusterPartType.Row, _sectorSpan + 1, (x, y) => GenerateNewSector(new Vector2Int(x, y)));
                    frontBackChangesAction = () => { ForEachClusterIn(EClusterPartType.Row, _sectorSpan, (x, y) => LoadChangesForSector(new Vector2Int(x, y))); };
                    ForEachClusterIn(EClusterPartType.Row, -_sectorSpan, (x, y) => DisposeSectorAtIndex(new Vector2Int(x, y)));
                }
                 
                _middleSectorInClusterIdx = new Vector2Int(_currentSectorIdx.x, _currentSectorIdx.y);
                _middleSectorInCluster = _sectors[_middleSectorInClusterIdx.x, _middleSectorInClusterIdx.y].sector;
                
                righLeftChangesAction?.Invoke();
                frontBackChangesAction?.Invoke(); 
            }
            
            for (int chunkX = 0; chunkX < _currentSector.Chunks.GetLength(0); chunkX++)
            {
                for (int chunkZ = 0; chunkZ < _currentSector.Chunks.GetLength(1); chunkZ++)
                {
                    var chunk = _currentSector.Chunks[chunkX, chunkZ];
                    
                    DebugDraw.DrawBounds(chunk.Bounds);
                }
            }
        }

        public enum EClusterPartType
        {
            Row,
            Column
        }

        private void ForEachClusterIn(EClusterPartType partType, int offset, Action<int, int> sectorIdxAction)
        {
            for (int i = 0; i < SECTOR_CLUSTER_SIZE; i++)
            {
                int sectorX;
                int sectorY;

                if (partType == EClusterPartType.Column)
                {
                    sectorX = _middleSectorInClusterIdx.x + offset;
                    sectorY = _middleSectorInClusterIdx.y - _sectorSpan + i;
                }
                else
                {
                    sectorX = _middleSectorInClusterIdx.x - _sectorSpan + i;
                    sectorY = _middleSectorInClusterIdx.y + offset;
                }
                    
                sectorIdxAction?.Invoke(sectorX, sectorY);
            }   
        }

        private void DisposeSectorAtIndex(Vector2Int index)
        {
            _sectors[index.x, index.y].sector.Dispose();
            _sectors[index.x, index.y].sector = null;
        }
        
        /// <summary>
        /// Sectors index in terms of two dimensional array of all sectors
        /// </summary>
        /// <param name="sectorIndex"></param>
        private void GenerateNewSector(Vector2Int sectorIndex)
        {
            var sector = new Sector();
            
            Vector3 sectorOffset = new Vector3((sectorIndex.x - _sectorAbsoluteMiddleIdx) * _sectorSizeWS, 0,
                (sectorIndex.y - _sectorAbsoluteMiddleIdx) * _sectorSizeWS);
            sector.Generate(sectorOffset, _noiseOffset);
            sector.SectorInfo = $"Sector:({sectorIndex.x},{sectorIndex.y})";
            _sectors[sectorIndex.x, sectorIndex.y].sector = sector;                 
        }

        private void LoadChangesForSector(Vector2Int sectorIdx)
        {
            var addChanges = _sectors[sectorIdx.x, sectorIdx.y].addChanged;
            foreach (var addChange in addChanges)
            {
                AddVoxelToWorldPos(addChange.WorldPos, _userBiomes[addChange.biomeIdx], false);
            }
            
            var removeChanges = _sectors[sectorIdx.x, sectorIdx.y].removeChanges;
            foreach (var removeChange in removeChanges)
            {
                RemoveVoxelOnWorldPos(removeChange.WorldPos, false);
            }
        }

        /// <summary>
        /// In the current sector
        /// </summary>
        /// <param name="worldPos"></param>
        /// <returns></returns>
        public float GeVoxelWSHeightAtWorldPos(Vector3 worldPos)
        {
            var voxelYIndex = GetVoxelYIndexAtWorldPos(worldPos);

            return voxelYIndex * _voxelSize + _voxelSize;
        }

        public VoxelInfo GetVoxelAtWorldPos(Vector3 worldPos)
        {
            Sector sector;
            var chunk = GetChunkAtWorldPos(worldPos, out sector);
            return chunk.GetVoxelAtWorldPos(worldPos);
        }

        public float GetVoxelYIndexAtWorldPos(Vector3 worldPos)
        {
            Sector sector;
            var chunk = GetChunkAtWorldPos(worldPos, out sector);

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
                    var sect = _sectors[sectorX, sectorY].sector;
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
                    else if (chunk.Bounds.Contains(ray.origin))
                    {
                        _intersectionChunks.Add(new IntersectionChunkInfo()
                        {
                            chunk = chunk,
                            rayDistance = 0f
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

        public void AddVoxelToWorldPos(Vector3 worldPos, Biome biome, bool record = true)
        {
            Sector sector;
            var chunk = GetChunkAtWorldPos(worldPos, out sector);
            chunk.AddVoxelAtWorldPos(worldPos, biome);

            if (record)
            {
                var sectorIdx = GetSectorIdx(sector);
                _sectors[sectorIdx.x, sectorIdx.y].addChanged.Add(new UserChanges.ChangeAdd()
                {
                    biomeIdx = _userBiomes.IndexOf(biome),
                    WorldPos = worldPos
                });
            }
        }

        public void RemoveVoxelOnWorldPos(Vector3 worldPos, bool record = true)
        {
            Sector sector;
            var chunk = GetChunkAtWorldPos(worldPos, out sector);
            chunk.RemoveVoxelAtWorldPos(worldPos);

            if (record)
            {
                var sectorIdx = GetSectorIdx(sector);
                _sectors[sectorIdx.x, sectorIdx.y].removeChanges.Add(new UserChanges.ChangeRemove()
                {
                    WorldPos = worldPos
                });
            }
        }
        
        public void SaveWorld() // :recyclate
        {
            _userChanges.Reset();

            for (int x = 0; x < MAX_SECTOR_ROWS_COLUMNS; x++)
            {
                for (int y = 0; y < MAX_SECTOR_ROWS_COLUMNS; y++)
                {
                    var sectorCh = _sectors[x, y];

                    foreach (var addChange in sectorCh.addChanged)
                    {
                        _userChanges.RecordAddChange(addChange.WorldPos, addChange.biomeIdx);
                    }
                    
                    foreach (var removeChange in sectorCh.removeChanges)
                    {
                        _userChanges.RecordRemoveChange(removeChange.WorldPos);
                    }
                }
            }
            
            _userChanges.Save();
            
            PlayerPrefs.SetFloat(Constants.SAVED_NOISE_OFFSET_X_KEY, _noiseOffset.x);
            PlayerPrefs.SetFloat(Constants.SAVED_NOISE_OFFSET_Y_KEY, _noiseOffset.y);
        }

        public void LoadSavedWorld()
        {
            PlayerPrefs.SetInt(Constants.LOAD_SAVE_FILE_FLAG_NAME, 1);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        
        private Chunk GetChunkAtWorldPos(Vector3 worldPos, out Sector chunkSector)
        {
            // get sector at world pos
            Sector sector = SectorAtWorldPos(worldPos);
            chunkSector = sector;
                
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
            for (int sectorX = _middleSectorInClusterIdx.x - _sectorSpan; sectorX <= _middleSectorInClusterIdx.x + _sectorSpan; sectorX++)
            {
                for (int sectorY = _middleSectorInClusterIdx.y - _sectorSpan; sectorY <= _middleSectorInClusterIdx.y + _sectorSpan; sectorY++)
                {
                    var sect = _sectors[sectorX, sectorY].sector;

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
        
        private Vector2Int GetSectorIdx(Sector sector)
        {
            for (int sectorX = _middleSectorInClusterIdx.x - _sectorSpan;
                sectorX <= _middleSectorInClusterIdx.x + _sectorSpan;
                sectorX++)
            {
                for (int sectorY = _middleSectorInClusterIdx.y - _sectorSpan;
                    sectorY <= _middleSectorInClusterIdx.y + _sectorSpan;
                    sectorY++)
                {
                    if (_sectors[sectorX, sectorY].sector == sector)
                        return new Vector2Int(sectorX, sectorY);

                }
            }
            
            return Vector2Int.zero;
        }
        
    }
}