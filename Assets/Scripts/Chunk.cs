using System.Collections.Generic;
using DefaultNamespace.Utils;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace DefaultNamespace
{
    public class Chunk
    {
        public Vector3 WorldPos => _chunkWsPos;

        public Bounds Bounds => _bounds;

        private string _sectorInfo;
        public string SectorInfo
        {
            private get => _sectorInfo;
            set
            {
                _sectorInfo = value;
                _gameObject.name = ToString();
            }
        }

        private readonly float BIOME_UV_SCALE;
        private readonly float CHUNK_SIZE_WS;

        private const float ON_VERTEX_SIDE_THETA = 0.0001f;
        private const int VERTICES_IN_VOXEL = 36;
        
        // x,z indices relative to position of chunks - height values
        public int[,] voxelHeights;
        
        private VoxelInfo[,,] _voxels;
        private Bounds _bounds;

        private readonly Vector3 _chunkWsPos;
        private ChunkNeighbours _neighbours;
        private int _chunkSize;
        private float _voxelSize;
        private int _worldHeight;
        private int _precisionIterationCount;
        private Material _material;

        private GameObject _gameObject;
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;
        private Mesh _mesh;

        private MeshCollider _meshCollider;
        
        private List<Vector3> _vertices = new List<Vector3>();
        private List<Vector2> _voxelUvs = new List<Vector2>();
        private List<int> _triangles = new List<int>();

        private BiomeManager _biomeManager;

        private static int _chunkIdx;

        private int _thisChunkIdx;

        public Chunk(int[,] voxelHeights, Vector3 chunkWSPos)
        {
            this.voxelHeights = voxelHeights;
            _chunkWsPos = chunkWSPos;

            var gameSettings = Locator.Instance.GameSettings;

            _chunkSize = gameSettings.ChunkSize;
            _voxelSize = gameSettings.VoxelSize;
            _worldHeight = gameSettings.WorldHeight;
            _material = gameSettings.VoxelMaterial;

            BIOME_UV_SCALE = gameSettings.BiomeUVSize;
            CHUNK_SIZE_WS = gameSettings.ChunkSizeWS;

            _precisionIterationCount = gameSettings.RaycastPrecisionIterationCount;

            _biomeManager = Locator.Instance.BiomeManager;

            _voxels = new VoxelInfo[voxelHeights.GetLength(0), _worldHeight, voxelHeights.GetLength(1)];

            _thisChunkIdx = _chunkIdx++;
        }

        public bool GetRayHitInfo(Ray ray, out VoxelHitInfo hitInfo)
        {
            float startPointDist;
            bool hit = Bounds.IntersectRay(ray, out startPointDist);
            if (!hit)
            {
                hitInfo = default;
                return false;
            }

            startPointDist *= 1.001f; // offset for rounding into chunk

            Vector3 previousCheckPoint = Vector3.zero;
            Vector3 nextCheckPoint = Vector3.zero;
            int checkIdx = 0;
            VoxelInfo voxel = null;
            bool isCheckPointInside = true;
            while (voxel == null)
            {
                previousCheckPoint = nextCheckPoint;
                nextCheckPoint = ray.origin + ray.direction * (startPointDist + checkIdx * _voxelSize);
                
                //DebugDraw.Sphere(nextCheckPoint, 0.1f);
                
                isCheckPointInside = Bounds.Contains(nextCheckPoint);
                if (!isCheckPointInside)
                    break;
                
                voxel = GetVoxelAtWorldPos(nextCheckPoint);
                checkIdx++;
            }

            if (voxel == null)
            {
                hitInfo = default;
                return false;                
            }

            var firstVoxelHit = voxel;
            if (previousCheckPoint != Vector3.zero)
            {
                // do couple iterations of splitting the range into half for increased precision
                float increment = _voxelSize * 0.5f;
                float precisionDistance = increment;
                for (int i = 0; i < _precisionIterationCount; i++)
                {
                    nextCheckPoint = previousCheckPoint + ray.direction * precisionDistance;
                    //DebugDraw.Sphere(nextCheckPoint, 0.1f);
                    voxel = GetVoxelAtWorldPos(nextCheckPoint);
                    increment *= 0.5f;
                    if (voxel != null)
                    {
                        precisionDistance -= increment;
                    }
                    else
                    {
                        precisionDistance += increment;
                    }
                }
            }
            if (voxel == null && firstVoxelHit != null)
            {
                voxel = firstVoxelHit;
            }


            var voxelBounds = new Bounds(voxel.worldPos + Vector3.up * (_voxelSize * 0.5f) + Vector3.right * (_voxelSize * 0.5f) + Vector3.forward * (_voxelSize * 0.5f),
                new Vector3(_voxelSize,_voxelSize,_voxelSize));
            
            //DebugDraw.DrawBounds(voxelBounds);

            float voxelRayDist;
            var voxelHit = voxelBounds.IntersectRay(ray, out voxelRayDist);
            if (!voxelHit)
            {
                Assert.IsTrue(voxelHit, "Voxel not hit. At this point this should not happen.");
            }
            
            var voxelHitPoint = ray.origin + ray.direction * voxelRayDist;
            var voxelHitLocal = voxelHitPoint - voxel.worldPos;
            Vector3 normal = Vector3.zero;

            //Locator.Instance.DebugToken.transform.position = voxelHitPoint;
            
            if (Mathf.Abs(voxelHitLocal.x) < ON_VERTEX_SIDE_THETA)
            {
                normal = Vector3.left;
            }
            else if (Mathf.Abs(voxelHitLocal.x - _voxelSize) < ON_VERTEX_SIDE_THETA)
            {
                normal = Vector3.right;
            }
            else if (Mathf.Abs(voxelHitLocal.y) < ON_VERTEX_SIDE_THETA)
            {
                normal = Vector3.down;;
            }
            else if (Mathf.Abs(voxelHitLocal.y - _voxelSize) < ON_VERTEX_SIDE_THETA)
            {
                normal = Vector3.up;
            }
            else if (Mathf.Abs(voxelHitLocal.z) < ON_VERTEX_SIDE_THETA)
            {
                normal = Vector3.back;;
            }
            else if (Mathf.Abs(voxelHitLocal.z - _voxelSize) < ON_VERTEX_SIDE_THETA)
            {
                normal = Vector3.forward;
            }
            else
            {
                Debug.LogError("Cannot properly determine vertex intersection normal");
            }
            
            hitInfo = new VoxelHitInfo()
            {
                voxelInfo = voxel,
                normal = normal
            };

            return true;
        }

        public VoxelInfo GetVoxelAtWorldPos(Vector3 worldPos)
        {
            var voxelIdx = WorldPosToVoxelIdx(worldPos);

            return VoxelInfoAtVoxelIdx(voxelIdx);
        }

        public void AddVoxelAtWorldPos(Vector3 worldPos, Biome biome)
        {
            var voxelIdx = WorldPosToVoxelIdx(worldPos);
            
            DrawVoxel(voxelIdx.x, voxelIdx.y, voxelIdx.z, biome);
            
            _voxels[voxelIdx.x, voxelIdx.y, voxelIdx.z] = new VoxelInfo()
            {
                worldPos = VoxelIdxToWorldPos(new Vector3Int(voxelIdx.x, voxelIdx.y, voxelIdx.z)),
                biome = biome,
                isEnclosed = false
            };

            if (voxelHeights[voxelIdx.x, voxelIdx.z] < voxelIdx.y)
                voxelHeights[voxelIdx.x, voxelIdx.z] = voxelIdx.y;
            
            ApplyMeshArrays();
        }

        public void RemoveVoxelAtWorldPos(Vector3 worldPos)
        {
            var voxelIdx = WorldPosToVoxelIdx(worldPos);

            if (voxelIdx.y == 0)
                return;

            var voxelInfo = _voxels[voxelIdx.x, voxelIdx.y, voxelIdx.z];
            if (voxelInfo == null)
                return;
            
            var vertexStartIndex = voxelInfo.vertexStartIndex;

            if (vertexStartIndex > -1)
            {
                _vertices.RemoveRange(vertexStartIndex, VERTICES_IN_VOXEL);
                _triangles.RemoveRange(vertexStartIndex, VERTICES_IN_VOXEL);
                
                for (int i = vertexStartIndex; i < _triangles.Count; i++)
                {
                    _triangles[i] -= VERTICES_IN_VOXEL;
                }
                
                _voxelUvs.RemoveRange(vertexStartIndex, VERTICES_IN_VOXEL);
            }

            // adjust starting index records in all voxels after removed voxel
            for (int xIndex = 0; xIndex < _chunkSize; xIndex++)
            {
                for (int zIndex = 0; zIndex < _chunkSize; zIndex++)
                {
                    var yHeight = voxelHeights[xIndex, zIndex];
                    for (int yIndex = 0; yIndex <= yHeight; yIndex++)
                    {
                        var voxel = _voxels[xIndex, yIndex, zIndex];

                        if (voxel != null && voxel.vertexStartIndex > voxelInfo.vertexStartIndex)
                            voxel.vertexStartIndex -= VERTICES_IN_VOXEL;
                    }
                }
            }
            
            _voxels[voxelIdx.x, voxelIdx.y, voxelIdx.z] = null;
            
            // figure out new max height on the voxel XZ position
            if (voxelIdx.y == voxelHeights[voxelIdx.x, voxelIdx.z])
            {
                for (int yIndex = 0; yIndex < voxelIdx.y; yIndex++)
                {
                    var voxel = _voxels[voxelIdx.x, yIndex, voxelIdx.z];
                    if (voxel != null)
                        voxelHeights[voxelIdx.x, voxelIdx.z] = yIndex;
                }    
            }
            
            // look at all neighbours and if some were enclosed, draw them
            VoxelInfo neighbourVoxelInfo = null;
            
            // LEFT
            if (voxelIdx.x == 0)
            {
                DrawNeighbourChunkVertex(_neighbours.left, new Vector3Int(_chunkSize - 1, voxelIdx.y, voxelIdx.z));
            }
            else
            {
                DrawNeighbourVertex(new Vector3Int(voxelIdx.x - 1, voxelIdx.y, voxelIdx.z));
            }

            // RIGHT
            if (voxelIdx.x == _chunkSize - 1)
            {
                DrawNeighbourChunkVertex(_neighbours.right, new Vector3Int(0, voxelIdx.y, voxelIdx.z));
            }
            else
            {
                DrawNeighbourVertex(new Vector3Int(voxelIdx.x + 1, voxelIdx.y, voxelIdx.z));
            }
            
            // BACK
            if (voxelIdx.z == 0)
            {
                DrawNeighbourChunkVertex(_neighbours.back, new Vector3Int(voxelIdx.x, voxelIdx.y, _chunkSize - 1));
            }
            else
            {
                DrawNeighbourVertex(new Vector3Int(voxelIdx.x, voxelIdx.y, voxelIdx.z - 1));
            }      
            
            // FORWARD
            if (voxelIdx.z == _chunkSize - 1)
            {
                DrawNeighbourChunkVertex(_neighbours.front, new Vector3Int(voxelIdx.x, voxelIdx.y, 0));
            }
            else
            {
                DrawNeighbourVertex(new Vector3Int(voxelIdx.x, voxelIdx.y, voxelIdx.z + 1));
            }   
            
            // DOWN
            DrawNeighbourVertex(new Vector3Int(voxelIdx.x, voxelIdx.y - 1, voxelIdx.z));
            
            // UP
            if (voxelIdx.y < _worldHeight - 1)
                DrawNeighbourVertex(new Vector3Int(voxelIdx.x, voxelIdx.y + 1, voxelIdx.z));
            
            ApplyMeshArrays();
        }

        private void DrawNeighbourVertex(Vector3Int neighbourVertexIndex)
        {
            var neighbourVoxelInfo = _voxels[neighbourVertexIndex.x, neighbourVertexIndex.y, neighbourVertexIndex.z];

            if (neighbourVoxelInfo != null && neighbourVoxelInfo.isEnclosed)
            {
                var startIndex = DrawVoxel(neighbourVertexIndex.x, neighbourVertexIndex.y, neighbourVertexIndex.z, neighbourVoxelInfo.biome);
                neighbourVoxelInfo.vertexStartIndex = startIndex;
                neighbourVoxelInfo.isEnclosed = false;
            }
        }
        
        private void DrawNeighbourChunkVertex(Chunk neighbourChunk, Vector3Int neighbourVertexIndex)
        {
            if (neighbourChunk != null)
            {
                var neighbourVoxelInfo = neighbourChunk._voxels[neighbourVertexIndex.x, neighbourVertexIndex.y, neighbourVertexIndex.z];

                if (neighbourVoxelInfo != null && neighbourVoxelInfo.isEnclosed)
                {
                    var startingIndex = neighbourChunk.DrawVoxel(neighbourVertexIndex.x, neighbourVertexIndex.y, neighbourVertexIndex.z, neighbourVoxelInfo.biome);
                    neighbourVoxelInfo.isEnclosed = false;
                    neighbourVoxelInfo.vertexStartIndex = startingIndex;
                    neighbourChunk.ApplyMeshArrays();
                }
            }            
        }
        
        public void SetNeighbours(ChunkNeighbours neighbours)
        {
            _neighbours = neighbours;
        }

        public void Generate()
        {
            // build chunk mesh
            // iterate from yIndexBottom to yIndexTop for every x,z in chunk
            // find neighbour cells
            // for y below or equal lowest neighbour y do not generate anything
            // if neighbour y < voxel y, then generate appropriate side and add it to voxel mesh
            Init();
            
            int vertexStartIndex = 0;

            for (int voxelX = 0; voxelX < _chunkSize; voxelX++)
            {
                for (int voxelZ = 0; voxelZ < _chunkSize; voxelZ++)
                {
                    // draw the volume if some of the neighbours are missing
                    var yval = voxelHeights[voxelX, voxelZ];

                    float heightNorm;
                    Biome biome;
                    for (int yIndex = 0; yIndex < yval; yIndex++)
                    {
                        vertexStartIndex = 0;
                        
                        // if at least one neighbour voxel is missing, we are drawing this
                        var neighbourMissing = false;

                        int rightN;
                        if (voxelX == _chunkSize - 1)
                        {
                            if (_neighbours.right == null)
                                rightN = 0;
                            else
                            {
                                rightN = _neighbours.right.voxelHeights[0, voxelZ];
                            }
                        }
                        else
                        {
                            rightN = voxelHeights[voxelX + 1, voxelZ];
                        }

                        int leftN;
                        if (voxelX == 0)
                        { 
                            if (_neighbours.left == null)
                                leftN = 0;
                            else
                            {
                                leftN = _neighbours.left.voxelHeights[_chunkSize - 1, voxelZ];
                            }
                        }
                        else
                        {
                            leftN = voxelHeights[voxelX - 1, voxelZ];
                        }

                        int frontN;
                        if (voxelZ == _chunkSize - 1)
                        {
                            if (_neighbours.front == null)
                                frontN = 0;
                            else
                            {
                                frontN = _neighbours.front.voxelHeights[voxelX, 0];
                            }
                        }
                        else
                        {
                            frontN = voxelHeights[voxelX, voxelZ + 1];
                        }

                        int backN;
                        if (voxelZ == 0)
                        {
                            if (_neighbours.back == null)
                                backN = 0;
                            else
                            {
                                backN = _neighbours.back.voxelHeights[voxelX, _chunkSize - 1];
                            }
                        }
                        else
                        {
                            backN = voxelHeights[voxelX, voxelZ - 1];
                        }

                        neighbourMissing = backN < yIndex ||
                                           frontN < yIndex ||
                                           rightN < yIndex ||
                                           leftN < yIndex;

                        heightNorm = yIndex / (float) _worldHeight;
                        biome = _biomeManager.GetBiomeByNormHeight(heightNorm);

                        
                        if (neighbourMissing)
                            vertexStartIndex = DrawVoxel(voxelX, yIndex, voxelZ, biome);
                        
                        _voxels[voxelX, yIndex, voxelZ] = new VoxelInfo()
                        {
                            worldPos = VoxelIdxToWorldPos(new Vector3Int(voxelX, yIndex, voxelZ)),
                            biome = biome,
                            isEnclosed = !neighbourMissing,
                            vertexStartIndex = vertexStartIndex
                        };
                    }
                    
                    heightNorm = yval / (float) _worldHeight;
                    biome = _biomeManager.GetBiomeByNormHeight(heightNorm);
                    
                    // always draw the surface
                    vertexStartIndex = DrawVoxel(voxelX, yval, voxelZ, biome);
                    
                    _voxels[voxelX, yval, voxelZ] = new VoxelInfo()
                    {
                        worldPos = VoxelIdxToWorldPos(new Vector3Int(voxelX, yval, voxelZ)),
                        biome = biome,
                        isEnclosed = false,
                        vertexStartIndex = vertexStartIndex
                    };
                }
            }

            ApplyMeshArrays();
            
            _meshRenderer.material = _material;
        }

        /// <summary>
        /// Bases on index of voxel in chunk returns world position of voxel
        /// Does not perform safety checks
        /// </summary>
        public Vector3 VoxelIdxToWorldPos(Vector3Int voxelIdx)
        {
            var voxelWorldX = _chunkWsPos.x + voxelIdx.x * _voxelSize;
            var voxelWorldY = voxelIdx.y * _voxelSize;
            var voxelWorldZ = _chunkWsPos.z + voxelIdx.z * _voxelSize;

            return new Vector3(voxelWorldX, voxelWorldY, voxelWorldZ);
        }

        public Vector3Int WorldPosToVoxelIdx(Vector3 worldPos)
        {
            var voxelChunkPoX = worldPos.x - WorldPos.x;
            var voxelXIdx = Mathf.FloorToInt(voxelChunkPoX / _voxelSize);
            
            var voxelChunkPosZ = worldPos.z - WorldPos.z;
            var voxelZIdx = Mathf.FloorToInt(voxelChunkPosZ / _voxelSize);

            var voxelYIdx = Mathf.FloorToInt(worldPos.y / _voxelSize);

            return new Vector3Int(voxelXIdx, voxelYIdx, voxelZIdx);
        }
        
        /// <summary>
        /// Returns VoxelInfo base on voxel chunk idx.
        /// Does not perform safety checks
        /// </summary>
        /// <param name="voxelIdx"></param>
        /// <returns></returns>
        public VoxelInfo VoxelInfoAtVoxelIdx(Vector3Int voxelIdx)
        {
            return _voxels[voxelIdx.x, voxelIdx.y, voxelIdx.z];
        }

        private void ApplyMeshArrays()
        {
            _mesh.Clear();
            
            _mesh.SetVertices(_vertices);
            _mesh.SetTriangles(_triangles, 0);
            _mesh.SetUVs(0, _voxelUvs);
            
            _mesh.RecalculateNormals();

            _meshFilter.mesh = _mesh;
            _bounds = _meshRenderer.bounds;
        }

        private int DrawVoxel(int voxelXIdx, int voxelYIdx, int voxelZIdx, Biome biome)
        {
            int startIndex = _vertices.Count;
            
            // build voxel mesh
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    var vertexIndex = VoxelConsts.faces[i, j];
                    var vertexPosLocal = VoxelConsts.vertexPos[vertexIndex] * _voxelSize;

                    var chunkVoxelXPos = voxelXIdx * _voxelSize;
                    var chunkVoxelYPos = voxelYIdx * _voxelSize;
                    var chunkVoxelZPos = voxelZIdx * _voxelSize;

                    _vertices.Add(new Vector3(
                        chunkVoxelXPos + vertexPosLocal.x,
                        chunkVoxelYPos + vertexPosLocal.y,
                        chunkVoxelZPos + vertexPosLocal.z));
                            
                    _triangles.Add(_vertices.Count - 1);
                    _voxelUvs.Add(biome.uvs + VoxelConsts.uvs[j] * BIOME_UV_SCALE);
                }
            }

            return startIndex;
        }

        private void Init()
        {
            if (_gameObject != null)
                return;
            
            _gameObject = new GameObject(ToString());
            _gameObject.transform.position = _chunkWsPos;
            _meshRenderer = _gameObject.AddComponent<MeshRenderer>();
            _meshFilter = _gameObject.AddComponent<MeshFilter>();
            _mesh = new Mesh();
            
            _vertices.Clear();
            _voxelUvs.Clear();
            _triangles.Clear();
        }

        public override string ToString()
        {
            return $"Chunk {_thisChunkIdx}, {SectorInfo}";
        }
    }

    public class ChunkNeighbours
    {
        public Chunk front;
        public Chunk back;
        public Chunk left;
        public Chunk right;
    }
}