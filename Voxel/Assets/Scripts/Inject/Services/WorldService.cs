using System.Collections.Generic;
using UnityEngine;

namespace VoxelWorld
{
    public class WorldService : IService
    {
        [Inject] private WorldRules _worldRules;

        private readonly Dictionary<Vector3Int, ChunkData> _chunkDataDictionary = new();
        private readonly Dictionary<Vector3Int, ChunkRenderer> _chunkDictionary = new();

        private TerrainGenerator _terrainGenerator;
        private Vector2Int _worldOffset;

        public int ChunkSize => _worldRules.ChunkSize;
        public int ChunkHeight => _worldRules.ChunkHeight;

        public void Init() { }
        public void Destroy() { }

        public void Configure(TerrainGenerator terrainGenerator)
        {
            _terrainGenerator = terrainGenerator;
        }

        public void SetWorldOffset(Vector2Int worldOffset)
        {
            _worldOffset = worldOffset;
        }

        public void ClearWorld()
        {
            foreach (ChunkRenderer chunk in _chunkDictionary.Values)
                Object.Destroy(chunk.gameObject);

            _chunkDataDictionary.Clear();
            _chunkDictionary.Clear();
        }

        public void RegisterChunkData(ChunkData data)
        {
            _chunkDataDictionary[data.WorldPosition] = data;
        }

        public void RegisterChunkRenderer(Vector3Int worldPosition, ChunkRenderer renderer)
        {
            _chunkDictionary[worldPosition] = renderer;
        }

        public IEnumerable<ChunkData> AllChunkData => _chunkDataDictionary.Values;

        public Vector3Int ChunkPositionFromBlockCoords(int x, int y, int z)
        {
            return new Vector3Int
            {
                x = Mathf.FloorToInt(x / (float)ChunkSize) * ChunkSize,
                y = Mathf.FloorToInt(y / (float)ChunkHeight) * ChunkHeight,
                z = Mathf.FloorToInt(z / (float)ChunkSize) * ChunkSize
            };
        }

        public BlockType GetBlockFromChunkCoordinates(int worldX, int worldY, int worldZ)
        {
            Vector3Int chunkPos = ChunkPositionFromBlockCoords(worldX, worldY, worldZ);

            if (!_chunkDataDictionary.TryGetValue(chunkPos, out ChunkData containerChunk))
                return BlockType.Nothing;

            Vector3Int localCoords = Chunk.GetBlockInChunkCoordinates(containerChunk, new Vector3Int(worldX, worldY, worldZ));
            return Chunk.GetBlockFromChunkCoordinates(containerChunk, localCoords);
        }

        public BlockType GetBlockAtWorldPosition(Vector3Int worldPos)
        {
            return GetBlockFromChunkCoordinates(worldPos.x, worldPos.y, worldPos.z);
        }

        public bool TrySetBlockAtWorldPosition(Vector3Int worldPos, BlockType blockType)
        {
            Vector3Int chunkPos = ChunkPositionFromBlockCoords(worldPos.x, worldPos.y, worldPos.z);
            if (!_chunkDataDictionary.TryGetValue(chunkPos, out ChunkData chunkData))
                return false;

            Vector3Int localPos = Chunk.GetBlockInChunkCoordinates(chunkData, worldPos);
            Chunk.SetBlock(chunkData, localPos, blockType);
            chunkData.ModifiedByThePlayer = true;

            if (_chunkDictionary.TryGetValue(chunkPos, out ChunkRenderer renderer))
                renderer.UpdateChunk(Chunk.GetChunkMeshData(chunkData));

            return true;
        }

        public int GetSurfaceHeight(int worldX, int worldZ)
        {
            return _terrainGenerator.GetSurfaceHeight(worldX, worldZ, _worldRules.MaxTerrainHeight, ChunkHeight, _worldOffset);
        }

        public int AddBlockDamage(Vector3Int worldPos)
        {
            Vector3Int chunkPos = ChunkPositionFromBlockCoords(worldPos.x, worldPos.y, worldPos.z);
            if (!_chunkDataDictionary.TryGetValue(chunkPos, out ChunkData chunkData))
                return 0;

            Vector3Int localPos = Chunk.GetBlockInChunkCoordinates(chunkData, worldPos);
            return Chunk.AddBlockDamage(chunkData, localPos);
        }

        public void ClearBlockDamage(Vector3Int worldPos)
        {
            Vector3Int chunkPos = ChunkPositionFromBlockCoords(worldPos.x, worldPos.y, worldPos.z);
            if (!_chunkDataDictionary.TryGetValue(chunkPos, out ChunkData chunkData))
                return;

            Vector3Int localPos = Chunk.GetBlockInChunkCoordinates(chunkData, worldPos);
            Chunk.ClearBlockDamage(chunkData, localPos);
        }
    }
}