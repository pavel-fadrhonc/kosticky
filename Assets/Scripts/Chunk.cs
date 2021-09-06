using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DefaultNamespace
{
    public class Chunk
    {
        // x,z indices relative to position of chunks - height values
        public int[,] voxels;
        
        private readonly Vector2 _chunkWsPos;
        private readonly Vector2 _worldOffset;
        private ChunkNeighbours _neighbours;
        private int _chunkSize;
        private float _voxelSize;
        private Material _material;

        private GameObject _gameObject;
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;
        private Mesh _mesh;

        private MeshCollider _meshCollider;
        
        private List<Vector3> _vertices = new List<Vector3>();
        private List<Vector2> _voxelUvs = new List<Vector2>();
        private List<int> _triangles = new List<int>();

        public Chunk(int[,] voxels, Vector2 chunkWSPos, Vector2 worldOffset)
        {
            this.voxels = voxels;
            _chunkWsPos = chunkWSPos;
            _worldOffset = worldOffset;

            var gameCon = Locator.Instance.GameConstants;

            _chunkSize = gameCon.ChunkSize;
            _voxelSize = gameCon.VoxelSize;
            _material = gameCon.VoxelMaterial;
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

            for (int voxelX = 0; voxelX < _chunkSize; voxelX++)
            {
                for (int voxelZ = 0; voxelZ < _chunkSize; voxelZ++)
                {
                    // draw the volume if some of the neighbours are missing
                    var yval = voxels[voxelX, voxelZ];

                    for (int yIndex = 0; yIndex < yval; yIndex++)
                    {
                        // if at least one neighbour voxel is missing, we are drawing this
                        var neighbourMissing = false;

                        int rightN;
                        if (voxelX == _chunkSize - 1)
                        {
                            if (_neighbours.right == null)
                                rightN = 0;
                            else
                            {
                                rightN = _neighbours.right.voxels[0, voxelZ];
                            }
                        }
                        else
                        {
                            rightN = voxels[voxelX + 1, voxelZ];
                        }

                        int leftN;
                        if (voxelX == 0)
                        { 
                            if (_neighbours.left == null)
                                leftN = 0;
                            else
                            {
                                leftN = _neighbours.left.voxels[_chunkSize - 1, voxelZ];
                            }
                        }
                        else
                        {
                            leftN = voxels[voxelX - 1, voxelZ];
                        }

                        int frontN;
                        if (voxelZ == _chunkSize - 1)
                        {
                            if (_neighbours.front == null)
                                frontN = 0;
                            else
                            {
                                frontN = _neighbours.front.voxels[voxelX, 0];
                            }
                        }
                        else
                        {
                            frontN = voxels[voxelX, voxelZ + 1];
                        }

                        int backN;
                        if (voxelZ == 0)
                        {
                            if (_neighbours.back == null)
                                backN = 0;
                            else
                            {
                                backN = _neighbours.back.voxels[voxelX, _chunkSize - 1];
                            }
                        }
                        else
                        {
                            backN = voxels[voxelX, voxelZ - 1];
                        }

                        neighbourMissing = backN < yIndex ||
                                           frontN < yIndex ||
                                           rightN < yIndex ||
                                           leftN < yIndex;

                        if (neighbourMissing)
                            DrawVoxel(voxelX, yIndex, voxelZ);

                    }
                    
                    // always draw the surface
                    DrawVoxel(voxelX, yval, voxelZ);
                }
            }

            _mesh.SetVertices(_vertices);
            _mesh.SetTriangles(_triangles, 0);
            _mesh.SetUVs(0, _voxelUvs);
            
            _mesh.RecalculateNormals();

            _meshFilter.mesh = _mesh;
            _meshRenderer.material = _material;

            _meshCollider.sharedMesh = _mesh;
            _meshCollider.convex = true;
            
            _vertices.Clear();
            _voxelUvs.Clear();
            _triangles.Clear();
        }

        private void DrawVoxel(int voxelXIdx, int voxelYIdx, int voxelZIdx)
        {
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
                            
                    //triangles[i * 6 + j] = vertices.Count - 1;
                            
                    _triangles.Add(_vertices.Count - 1);
                    _voxelUvs.Add(VoxelConsts.uvs[j]);
                }
            }            
        }

        private void Init()
        {
            if (_gameObject != null)
                return;
            
            _gameObject = new GameObject($"Chunk:())");
            _gameObject.transform.position = new Vector3(_chunkWsPos.x, 0, _chunkWsPos.y);
            _meshRenderer = _gameObject.AddComponent<MeshRenderer>();
            _meshFilter = _gameObject.AddComponent<MeshFilter>();
            _meshCollider = _gameObject.AddComponent<MeshCollider>();
            _mesh = new Mesh();
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