using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    public class WorldManager : UnityEngine.MonoBehaviour
    {
        public float debugSphereRadius = 0.1f;
        public Color debugSphereColor = Color.blue;

        public int[,] voxels;

        private Sector _sector;
        
        private void Start()
        {
            var gameCon = Locator.Instance.GameConstants;
            
            var worldSize = gameCon.WorldSize;
            var chunkSize = gameCon.ChunkSize;
            var voxelSize = gameCon.VoxelSize;
            var noiseScale = gameCon.NoiseScale;
            var voxelMaterial = gameCon.VoxelMaterial;

            _sector = new Sector();
            _sector.Generate(Vector2.zero, Vector2.zero);

            for (int chunkX = 0; chunkX < _sector.Chunks.GetLength(0); chunkX++)
            {
                for (int chunkZ = 0; chunkZ < _sector.Chunks.GetLength(1); chunkZ++)
                {
                    var chunk = _sector.Chunks[chunkX, chunkZ];
                    
                    var chunkBaseIdxX = chunkSize * chunkX;
                    var chunkBaseIdxZ = chunkSize * chunkZ;
                    
                    // build chunk mesh
                    // iterate from yIndexBottom to yIndexTop for every x,z in chunk
                    // find neighbour cells
                    // for y below or equal lowest neighbour y do not generate anything
                    // if neighbour y < voxel y, then generate appropriate side and add it to voxel mesh
                    
                    // var chunkGo = new GameObject($"Chunk:({chunkX}, {chunkZ}))");
                    // chunkGo.transform.position = new Vector3(xPos, yPos, zPos);
                    // chunkGo.transform.localScale = new Vector3(voxelSize, voxelSize, voxelSize);
                    // var voxelMeshRenderer = voxelGo.AddComponent<MeshRenderer>();
                    // var voxelMeshFilter = voxelGo.AddComponent<MeshFilter>();
                    //
                    // var voxelMesh = new Mesh();
                    //
                    // var vertices = new List<Vector3>();
                    // var voxelUvs = new List<Vector2>() ;                    

                    // for (int voxelX = 0; voxelX < chunkSize; voxelX++)
                    // {
                    //     for (int voxelZ = 0; voxelZ < chunkSize; voxelZ++)
                    //     {
                    //         var absVoxelX = chunkBaseIdxX + voxelX;
                    //         var absVoxelZ = chunkBaseIdxZ + voxelZ;
                    //         var yIndex = chunk.voxels[voxelX, voxelZ];
                    //
                    //         // draw voxel
                    //         var xPos = absVoxelX * voxelSize;
                    //         var zPos = absVoxelZ * voxelSize;
                    //
                    //         var yPos = yIndex * voxelSize;
                    //
                    //         // build voxel mesh
                    //         var voxelGo = new GameObject($"Voxel_ABS:({absVoxelX},{yIndex},{absVoxelZ})_CH:({chunkX}, {chunkZ})_LOC:({voxelX}, {voxelZ}))");
                    //         voxelGo.transform.position = new Vector3(xPos, yPos, zPos);
                    //         voxelGo.transform.localScale = new Vector3(voxelSize, voxelSize, voxelSize);
                    //         var voxelMeshRenderer = voxelGo.AddComponent<MeshRenderer>();
                    //         var voxelMeshFilter = voxelGo.AddComponent<MeshFilter>();
                    //
                    //         var voxelMesh = new Mesh();
                    //
                    //         var vertices = new List<Vector3>();
                    //         var voxelUvs = new List<Vector2>() ;
                    //
                    //         int[] triangles = new int[VoxelConsts.faces.Length * 6];
                    //         for (int i = 0; i < 6; i++)
                    //         {
                    //             for (int j = 0; j < 6; j++)
                    //             {
                    //                 var vertexIndex = VoxelConsts.faces[i, j];
                    //                 vertices.Add(VoxelConsts.vertexPos[vertexIndex]);
                    //                 triangles[i * 6 + j] = vertices.Count - 1;
                    //                 voxelUvs.Add(VoxelConsts.uvs[j]);
                    //             }
                    //         }
                    //
                    //         voxelMesh.SetVertices(vertices);
                    //         voxelMesh.SetTriangles(triangles, 0);
                    //         voxelMesh.SetUVs(0, voxelUvs);
                    //
                    //         voxelMesh.RecalculateNormals();
                    //
                    //         voxelMeshFilter.mesh = voxelMesh;
                    //         voxelMeshRenderer.material = voxelMaterial;                                 
                    //     }
                    // }


                }
            }
            
            return;
            
            voxels = new int[worldSize.x, worldSize.z];
            
            for (int x = 0; x < worldSize.x; x++)
            {
                for (int z = 0; z < worldSize.z; z++)
                {
                    var xPos = x * voxelSize;
                    var zPos = z * voxelSize;

                    var heightNorm = Mathf.PerlinNoise(xPos / worldSize.x * noiseScale.x,
                        zPos / worldSize.z * noiseScale.z);
                    
                    var yIndex = (int) Mathf.Floor(heightNorm * worldSize.y);

                    voxels[x, z] = yIndex;

                    var yPos = yIndex * voxelSize;
                    
                    // build voxel mesh
                    var voxelGo = new GameObject($"Voxel_{x}_{yIndex}_{z}");
                    voxelGo.transform.position = new Vector3(xPos, yPos, zPos);
                    var voxelMeshRenderer = voxelGo.AddComponent<MeshRenderer>();
                    var voxelMeshFilter = voxelGo.AddComponent<MeshFilter>();

                    var voxelMesh = new Mesh();

                    var vertices = new List<Vector3>();
                    var voxelUvs = new List<Vector2>() ;
                    // for (int i = 0; i < vertices.Length; i++)
                    // {
                    //     vertices[i] = VoxelConsts.vertexPos[i];
                    // }
                    // voxelMesh.SetVertices(vertices);

                    int[] triangles = new int[VoxelConsts.faces.Length * 6];
                    for (int i = 0; i < 6; i++)
                    {
                        for (int j = 0; j < 6; j++)
                        {
                            var vertexIndex = VoxelConsts.faces[i, j];
                            vertices.Add(VoxelConsts.vertexPos[vertexIndex]);
                            triangles[i * 6 + j] = vertices.Count - 1;
                            voxelUvs.Add(VoxelConsts.uvs[j]);
                        }
                    }

                    voxelMesh.SetVertices(vertices);
                    voxelMesh.SetTriangles(triangles, 0);
                    voxelMesh.SetUVs(0, voxelUvs);

                    voxelMesh.RecalculateNormals();

                    voxelMeshFilter.mesh = voxelMesh;
                    voxelMeshRenderer.material = voxelMaterial;
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;
            
            // for (int x = 0; x < worldSize.x; x++)
            // {
            //     for (int z = 0; z < worldSize.z; z++)
            //     {
            //         var xPos = x * voxelSize;
            //         var zPos = z * voxelSize;
            //         var yPos = voxels[x, z] * voxelSize;
            //
            //         Gizmos.DrawSphere(new Vector3(xPos, yPos, zPos), debugSphereRadius);
            //         
            //         
            //     }
            // }
            
            
        }
    }
}