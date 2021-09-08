using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    public class Sector
    {
        private Chunk[,] _chunks;

        public Chunk[,] Chunks => _chunks;
        
        public Vector3 WorldPos { get; private set; }
        
        public Bounds Bounds { get; private set; }

        public string SectorInfo
        {
            set
            {
                for (int chunkX = 0; chunkX < Chunks.GetLength(0); chunkX++)
                {
                    for (int chunkZ = 0; chunkZ < Chunks.GetLength(1); chunkZ++)
                    {
                        var chunk = Chunks[chunkX, chunkZ];

                        chunk.SectorInfo = value;
                    }
                }           
            }
        }

        public void Generate(Vector3 worldOffset, Vector2 noiseOffset)
        {
            var gameCon = Locator.Instance.GameSettings;
            var sectorSize = gameCon.SectorSize;
            var chunkSize = gameCon.ChunkSize;
            var voxelSize = gameCon.VoxelSize;
            var noiseScale = gameCon.NoiseScale;
            var worldHeight = gameCon.WorldHeight;

            var chunkCountX = Mathf.CeilToInt(sectorSize / (float) chunkSize);
            var chunkCountZ = Mathf.CeilToInt(sectorSize / (float) chunkSize);

            _chunks = new Chunk[chunkCountX, chunkCountZ];
            WorldPos = worldOffset;
            
            // generate chunks
            for (int x = 0; x < sectorSize; x++)
            {
                for (int z = 0; z < sectorSize; z++)
                {
                    var xPos = x * voxelSize + worldOffset.x;
                    var zPos = z * voxelSize + worldOffset.z;
                    
                    var heightNorm = Mathf.PerlinNoise((xPos / sectorSize) * noiseScale.x + noiseOffset.x, 
                        (zPos / sectorSize) * noiseScale.z + noiseOffset.y);
                    
                    var yIndex = (int) Mathf.Floor(heightNorm * noiseScale.y * worldHeight);
                    
                    int chunkIdxX = Mathf.FloorToInt(x / (float) chunkSize);
                    int chunkIdxZ = Mathf.FloorToInt(z / (float) chunkSize);

                    if (Chunks[chunkIdxX, chunkIdxZ] == null)
                    {
                        var chunkWSPosX = chunkIdxX * chunkSize * voxelSize + worldOffset.x;
                        var chunkWSPosZ = chunkIdxZ * chunkSize * voxelSize + worldOffset.z;
                        
                        Chunks[chunkIdxX, chunkIdxZ] = new Chunk(new int[chunkSize,chunkSize], 
                            new Vector3(chunkWSPosX, 0, chunkWSPosZ));
                    }

                    var chunk = Chunks[chunkIdxX, chunkIdxZ];

                    var chunkXPos = x - (chunkIdxX * chunkSize);
                    var chunkZPos = z - (chunkIdxZ * chunkSize);

                    chunk.voxelHeights[chunkXPos, chunkZPos] = yIndex;
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

            
            // generate chunk geometry
            for (int chunkX = 0; chunkX < Chunks.GetLength(0); chunkX++)
            {
                for (int chunkZ = 0; chunkZ < Chunks.GetLength(1); chunkZ++)
                {
                    var chunk = Chunks[chunkX, chunkZ];
                    
                    chunk.Generate();
                    
                    Bounds.Encapsulate(chunk.Bounds);
                }
            } 
            
            var sectorSizeWS = sectorSize * voxelSize;
            Bounds = new Bounds(WorldPos 
                                + sectorSizeWS * 0.5f * Vector3.right 
                                + sectorSizeWS * 0.5f * Vector3.forward, 
                new Vector3(sectorSizeWS, worldHeight, sectorSizeWS));
        }
    }
}