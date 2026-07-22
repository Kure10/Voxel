using System;
using System.Collections;
using System.Collections.Generic;
using Character;
using UnityEngine;

namespace VoxelWorld
{
    public class World : MonoBehaviour
    {
        [Header("Chunk Settings")]
        public int MapSizeInChunks = 6;
        public int ChunkSize = 16;
        public int ChunkHeight = 100;
        public GameObject ChunkPrefab;

        [Header("Player")]
        public PlayerSpawner PlayerSpawner;
        [Header("Terrain Generation")]
        public TerrainGenerator TerrainGenerator;

        [Header("World Rules (height bands, water)")]
        public WorldRules WorldRules;

        [Header("Seed")]
        [Tooltip("If checked, a new random seed is picked every time you press Generate.")]
        public bool UseRandomSeed = true;
        [Tooltip("Current seed. Untick 'Use Random Seed' and keep this value to reproduce this exact terrain.")]
        public int Seed;

        private Vector2Int _worldOffset;
        Dictionary<Vector3Int, ChunkData> _chunkDataDictionary = new Dictionary<Vector3Int, ChunkData>();
        Dictionary<Vector3Int, ChunkRenderer> _chunkDictionary = new Dictionary<Vector3Int, ChunkRenderer>();

        public void GenerateWorld()
        {
            if (UseRandomSeed)
                Seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

            System.Random seededRandom = new System.Random(Seed);
            _worldOffset = new Vector2Int(seededRandom.Next(-100000, 100000), seededRandom.Next(-100000, 100000));

            _chunkDataDictionary.Clear();
            foreach (ChunkRenderer chunk in _chunkDictionary.Values)
            {
                Destroy(chunk.gameObject);
            }

            _chunkDictionary.Clear();

            for (int x = 0; x < MapSizeInChunks; x++)
            {
                for (int z = 0; z < MapSizeInChunks; z++)
                {
                    ChunkData data = new ChunkData(ChunkSize, ChunkHeight, this,
                        new Vector3Int(x * ChunkSize, 0, z * ChunkSize));
                    GenerateVoxels(data);
                    _chunkDataDictionary.Add(data.WorldPosition, data);
                }
            }

            foreach (ChunkData data in _chunkDataDictionary.Values)
            {
                MeshData meshData = Chunk.GetChunkMeshData(data);
                GameObject chunkObject = Instantiate(ChunkPrefab, data.WorldPosition, Quaternion.identity);
                ChunkRenderer chunkRenderer = chunkObject.GetComponent<ChunkRenderer>();
                _chunkDictionary.Add(data.WorldPosition, chunkRenderer);
                chunkRenderer.InitializeChunk(data);
                chunkRenderer.UpdateChunk(meshData);
            }
            
            PlayerSpawner.SpawnPlayer();
        }

        private void GenerateVoxels(ChunkData data)
        {
            for (int x = 0; x < data.ChunkSize; x++)
            {
                for (int z = 0; z < data.ChunkSize; z++)
                {
                    int groundPosition = TerrainGenerator.GetSurfaceHeight(data.WorldPosition.x + x,
                        data.WorldPosition.z + z,
                        ChunkHeight, _worldOffset);

                    for (int y = 0; y < ChunkHeight; y++)
                    {
                        BlockType voxelType = BlockType.Gray;
                        if (y > groundPosition)
                        {
                            voxelType = y < WorldRules.WaterLevel ? BlockType.Water : BlockType.Air;
                        }
                        else
                        {
                            voxelType = WorldRules.GetSolidBlockType(y);
                        }

                        Chunk.SetBlock(data, new Vector3Int(x, y, z), voxelType);
                    }
                }
            }
        }

        internal BlockType GetBlockFromChunkCoordinates(ChunkData chunkData, int x, int y, int z)
        {
            Vector3Int pos = Chunk.ChunkPositionFromBlockCoords(this, x, y, z);
            ChunkData containerChunk = null;

            _chunkDataDictionary.TryGetValue(pos, out containerChunk);

            if (containerChunk == null)
                return BlockType.Nothing;
            Vector3Int blockInCHunkCoordinates =
                Chunk.GetBlockInChunkCoordinates(containerChunk, new Vector3Int(x, y, z));
            return Chunk.GetBlockFromChunkCoordinates(containerChunk, blockInCHunkCoordinates);
        }
        
        public int GetSurfaceHeight(int worldX, int worldZ)
        {
            return TerrainGenerator.GetSurfaceHeight(worldX, worldZ, ChunkHeight, _worldOffset);
        }
    }
}