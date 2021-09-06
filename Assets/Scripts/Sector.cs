using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    public class Sector
    {
        private Chunk[,] _chunks;

        public Chunk[,] Chunks => _chunks;

        public void Generate(Vector2 noisePos, Vector2 worldOffset)
        {
            var gameCon = Locator.Instance.GameConstants;
            var worldSize = gameCon.WorldSize;
            var chunkSize = gameCon.ChunkSize;
            var voxelSize = gameCon.VoxelSize;
            var noiseScale = gameCon.NoiseScale;
            var voxelMaterial = gameCon.VoxelMaterial;

            var chunkCountX = Mathf.CeilToInt(worldSize.x / (float) chunkSize);
            var chunkCountZ = Mathf.CeilToInt(worldSize.z / (float) chunkSize);

            _chunks = new Chunk[chunkCountX, chunkCountZ];
            
            // generate chunks
            for (int x = 0; x < worldSize.x; x++)
            {
                for (int z = 0; z < worldSize.z; z++)
                {
                    var xPos = x * voxelSize + worldOffset.x;
                    var zPos = z * voxelSize + worldOffset.y;
                    
                    var heightNorm = Mathf.PerlinNoise((xPos / worldSize.x) * noiseScale.x, (zPos / worldSize.z) * noiseScale.z);
                    
                    var yIndex = (int) Mathf.Floor(heightNorm * noiseScale.y * worldSize.y);

                    //voxels[x, z] = yIndex;

                    var yPos = yIndex * voxelSize;

                    int chunkIdxX = Mathf.FloorToInt(x / (float) chunkSize);
                    int chunkIdxZ = Mathf.FloorToInt(z / (float) chunkSize);

                    if (Chunks[chunkIdxX, chunkIdxZ] == null)
                    {
                        var chunkWSPosX = chunkIdxX * chunkSize * voxelSize + worldOffset.x;
                        var chunkWSPosZ = chunkIdxZ * chunkSize * voxelSize + worldOffset.y;
                        
                        Chunks[chunkIdxX, chunkIdxZ] = new Chunk(new int[chunkSize,chunkSize], 
                            new Vector2(chunkWSPosX, chunkWSPosZ),
                            worldOffset);
                    }

                    var chunk = Chunks[chunkIdxX, chunkIdxZ];

                    var chunkXPos = x - (chunkIdxX * chunkSize);
                    var chunkZPos = z - (chunkIdxZ * chunkSize);

                    chunk.voxels[chunkXPos, chunkZPos] = yIndex;
                }
            }  
            
            // set chunk neighbours
            for (int chunkX = 0; chunkX < chunkCountX; chunkX++)
            {
                for (int chunkZ = 0; chunkZ < chunkCountZ; chunkZ++)
                {
                    var chunkNeighbours = new ChunkNeighbours();
                    if (chunkX > 0)
                    {
                        chunkNeighbours.left = _chunks[chunkX - 1, chunkZ];
                    }
                    if (chunkX < chunkCountX - 1)
                    {
                        chunkNeighbours.right = _chunks[chunkX + 1, chunkZ];
                    }
                    if (chunkZ > 0)
                    {
                        chunkNeighbours.back = _chunks[chunkX, chunkZ - 1];
                    }
                    if (chunkZ < chunkCountZ - 1)
                    {
                        chunkNeighbours.front = _chunks[chunkX, chunkZ + 1];
                    }
                    
                    var chunk = _chunks[chunkX, chunkZ];
                    chunk.SetNeighbours(chunkNeighbours);
                }
            }
            
            
            for (int chunkX = 0; chunkX < Chunks.GetLength(0); chunkX++)
            {
                for (int chunkZ = 0; chunkZ < Chunks.GetLength(1); chunkZ++)
                {
                    var chunk = Chunks[chunkX, chunkZ];
                    
                    chunk.Generate();
                    
                    // var chunkBaseIdxX = chunkSize * chunkX;
                    // var chunkBaseIdxZ = chunkSize * chunkZ;
                    //
                    // // build chunk mesh
                    // // iterate from yIndexBottom to yIndexTop for every x,z in chunk
                    // // find neighbour cells
                    // // for y below or equal lowest neighbour y do not generate anything
                    // // if neighbour y < voxel y, then generate appropriate side and add it to voxel mesh
                    //
                    // var chunkWSPosX = chunkX * chunkSize * voxelSize + worldOffset.x;
                    // var chunkWSPosZ = chunkZ * chunkSize * voxelSize + worldOffset.y;
                    // var chunkWSPosY = 0f;
                    //
                    // var chunkGo = new GameObject($"Chunk:({chunkX}, {chunkZ}))");
                    // chunkGo.transform.position = new Vector3(chunkWSPosX, chunkWSPosY, chunkWSPosZ);
                    // var chunkMeshRenderer = chunkGo.AddComponent<MeshRenderer>();
                    // var chunkMeshFilter = chunkGo.AddComponent<MeshFilter>();
                    //
                    // var chunkMesh = new Mesh();
                    //
                    // var vertices = new List<Vector3>();
                    // var voxelUvs = new List<Vector2>() ;
                    // var triangles = new List<int>();
                    //
                    // for (int voxelX = 0; voxelX < chunkSize; voxelX++)
                    // {
                    //     for (int voxelZ = 0; voxelZ < chunkSize; voxelZ++)
                    //     {
                    //         var absVoxelX = chunkBaseIdxX + voxelX;
                    //         var absVoxelZ = chunkBaseIdxZ + voxelZ;
                    //         var yval = chunk.voxels[voxelX, voxelZ];
                    //
                    //         // draw voxel
                    //         var xPos = absVoxelX * voxelSize;
                    //         var zPos = absVoxelZ * voxelSize;
                    //
                    //         var yPos = yval * voxelSize;
                    //
                    //         for (int yIndex = 0; yIndex < yval; yIndex++)
                    //         {
                    //             // if at least one neighbour voxel is missing, we are drawing this
                    //             
                    //             
                    //         }
                    //
                    //         // build voxel mesh
                    //         for (int i = 0; i < 6; i++)
                    //         {
                    //             for (int j = 0; j < 6; j++)
                    //             {
                    //                 var vertexIndex = VoxelConsts.faces[i, j];
                    //                 var vertexPosLocal = VoxelConsts.vertexPos[vertexIndex] * voxelSize;
                    //
                    //                 var chunkVoxelXPos = voxelX * voxelSize;
                    //                 var chunkVoxelZPos = voxelZ * voxelSize;
                    //
                    //                 vertices.Add(new Vector3(
                    //                     chunkVoxelXPos + vertexPosLocal.x,
                    //                     yPos + vertexPosLocal.y,
                    //                     chunkVoxelZPos + vertexPosLocal.z));
                    //                 
                    //                 //triangles[i * 6 + j] = vertices.Count - 1;
                    //                 
                    //                 triangles.Add(vertices.Count - 1);
                    //                 voxelUvs.Add(VoxelConsts.uvs[j]);
                    //             }
                    //         }
                    //     }
                    // }
                    //
                    // chunkMesh.SetVertices(vertices);
                    // chunkMesh.SetTriangles(triangles, 0);
                    // chunkMesh.SetUVs(0, voxelUvs);
                    //
                    // chunkMesh.RecalculateNormals();
                    //
                    // chunkMeshFilter.mesh = chunkMesh;
                    // chunkMeshRenderer.material = voxelMaterial;                                 
                    
                }
            }            
        }
    }
}