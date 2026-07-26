using System.Collections.Concurrent;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VoxelWorld
{
    public class WorldService : IService
    {
        [Inject] private WorldRules _worldRules;

        private readonly ConcurrentDictionary<Vector3Int, ChunkData> _chunkDataDictionary = new();
        private readonly Dictionary<Vector3Int, ChunkRenderer> _chunkDictionary = new();
        private readonly HashSet<Vector3Int> _loadingChunks = new();

        private TerrainGenerator _terrainGenerator;
        private Vector2Int _worldOffset;
        private ChunkPool _chunkPool;
        public int ChunkSize => _worldRules.ChunkSize;
        public int ChunkHeight => _worldRules.ChunkHeight;
        public IEnumerable<Vector3Int> LoadedChunkPositions => _chunkDictionary.Keys;

        public void Init() { }
        public void Destroy() { }
        
        public void Configure(TerrainGenerator terrainGenerator)
        {
            _terrainGenerator = terrainGenerator;

            if (_chunkPool == null)
            {
                int viewDiameter = _worldRules.ViewDistanceInChunks * 2 + 1;
                _chunkPool = new ChunkPool(_worldRules.ChunkPrefab, viewDiameter * viewDiameter);
            }
        }

        public void SetWorldOffset(Vector2Int worldOffset) => _worldOffset = worldOffset;

        public void ClearWorld()
        {
            foreach (ChunkRenderer chunk in _chunkDictionary.Values)
                _chunkPool.Return(chunk);

            _chunkDataDictionary.Clear();
            _chunkDictionary.Clear();
            _loadingChunks.Clear();
        }

        public Vector3Int ChunkPositionFromBlockCoords(int x, int y, int z)
        {
            return new Vector3Int
            {
                x = Mathf.FloorToInt(x / (float)ChunkSize) * ChunkSize,
                y = Mathf.FloorToInt(y / (float)ChunkHeight) * ChunkHeight,
                z = Mathf.FloorToInt(z / (float)ChunkSize) * ChunkSize
            };
        }

        // Chunk INDEX (not world position) — e.g. (1,0,2), not (16,0,32)
        public Vector3Int WorldPositionToChunkIndex(Vector3 worldPos)
        {
            return new Vector3Int(Mathf.FloorToInt(worldPos.x / ChunkSize), 0, Mathf.FloorToInt(worldPos.z / ChunkSize));
        }

        public async UniTask LoadChunkAsync(Vector3Int chunkPos)
        {
            if (_chunkDictionary.ContainsKey(chunkPos) || _loadingChunks.Contains(chunkPos))
                return;

            _loadingChunks.Add(chunkPos);

            ChunkData data = _chunkDataDictionary.TryGetValue(chunkPos, out ChunkData existing) ? existing : null;

            MeshData meshData = await UniTask.RunOnThreadPool(() =>
            {
                if (data == null)
                {
                    data = new ChunkData(ChunkSize, ChunkHeight, this, chunkPos);
                    GenerateVoxels(data);
                    _chunkDataDictionary[chunkPos] = data;
                }

                return Chunk.GetChunkMeshData(data);
            });

            await UniTask.SwitchToMainThread();

            ChunkRenderer chunkRenderer = _chunkPool.Get(chunkPos);
            chunkRenderer.InitializeChunk(data);
            chunkRenderer.UpdateChunk(meshData);
            _chunkDictionary[chunkPos] = chunkRenderer;

            RefreshLoadedNeighbors(chunkPos); // fixes boundary faces on already-loaded neighbors

            _loadingChunks.Remove(chunkPos);
        }

        public void UnloadChunk(Vector3Int chunkPos)
        {
            if (_chunkDictionary.TryGetValue(chunkPos, out ChunkRenderer renderer))
            {
                _chunkPool.Return(renderer);
                _chunkDictionary.Remove(chunkPos);
            }

            if (_chunkDataDictionary.TryGetValue(chunkPos, out ChunkData data) && !data.ModifiedByThePlayer)
                _chunkDataDictionary.TryRemove(chunkPos, out _);
        }

        private void GenerateVoxels(ChunkData data)
        {
            for (int x = 0; x < data.ChunkSize; x++)
            {
                for (int z = 0; z < data.ChunkSize; z++)
                {
                    int groundPosition = _terrainGenerator.GetSurfaceHeight(
                        data.WorldPosition.x + x, data.WorldPosition.z + z,
                        _worldRules.MaxTerrainHeight, ChunkHeight, _worldOffset);

                    for (int y = 0; y < data.ChunkHeight; y++)
                    {
                        BlockType voxelType;
                        if (y > groundPosition)
                            voxelType = y < _worldRules.WaterLevel ? BlockType.Water : BlockType.Air;
                        else
                            voxelType = _worldRules.GetSolidBlockType(y);

                        Chunk.SetBlock(data, new Vector3Int(x, y, z), voxelType);
                    }
                }
            }
        }

        public BlockType GetBlockFromChunkCoordinates(int worldX, int worldY, int worldZ)
        {
            Vector3Int chunkPos = ChunkPositionFromBlockCoords(worldX, worldY, worldZ);

            if (!_chunkDataDictionary.TryGetValue(chunkPos, out ChunkData containerChunk))
                return BlockType.Nothing;

            Vector3Int localCoords = Chunk.GetBlockInChunkCoordinates(containerChunk, new Vector3Int(worldX, worldY, worldZ));
            return Chunk.GetBlockFromChunkCoordinates(containerChunk, localCoords);
        }

        public BlockType GetBlockAtWorldPosition(Vector3Int worldPos) =>
            GetBlockFromChunkCoordinates(worldPos.x, worldPos.y, worldPos.z);

        public bool TrySetBlockAtWorldPosition(Vector3Int worldPos, BlockType blockType)
        {
            Vector3Int chunkPos = ChunkPositionFromBlockCoords(worldPos.x, worldPos.y, worldPos.z);
            if (!_chunkDataDictionary.TryGetValue(chunkPos, out ChunkData chunkData))
                return false;

            Vector3Int localPos = Chunk.GetBlockInChunkCoordinates(chunkData, worldPos);
            Chunk.SetBlock(chunkData, localPos, blockType);
            chunkData.ModifiedByThePlayer = true;

            RefreshChunkMesh(chunkPos);
            RefreshBoundaryNeighbors(chunkPos, localPos);

            return true;
        }

        private void RefreshChunkMesh(Vector3Int chunkPos)
        {
            if (_chunkDataDictionary.TryGetValue(chunkPos, out ChunkData chunkData) &&
                _chunkDictionary.TryGetValue(chunkPos, out ChunkRenderer renderer))
            {
                renderer.UpdateChunk(Chunk.GetChunkMeshData(chunkData));
            }
        }
        
        private async UniTask RefreshChunkMeshAsync(Vector3Int chunkPos)
        {
            if (!_chunkDataDictionary.TryGetValue(chunkPos, out ChunkData chunkData) ||
                !_chunkDictionary.ContainsKey(chunkPos))
                return;

            MeshData meshData = await UniTask.RunOnThreadPool(() => Chunk.GetChunkMeshData(chunkData));
            await UniTask.SwitchToMainThread();

            // Chunk may have unloaded while this was building in the background — re-check before applying.
            if (_chunkDictionary.TryGetValue(chunkPos, out ChunkRenderer renderer))
                renderer.UpdateChunk(meshData);
        }

        private void RefreshBoundaryNeighbors(Vector3Int chunkPos, Vector3Int localPos)
        {
            if (localPos.x == 0) RefreshChunkMesh(chunkPos + new Vector3Int(-ChunkSize, 0, 0));
            else if (localPos.x == ChunkSize - 1) RefreshChunkMesh(chunkPos + new Vector3Int(ChunkSize, 0, 0));

            if (localPos.z == 0) RefreshChunkMesh(chunkPos + new Vector3Int(0, 0, -ChunkSize));
            else if (localPos.z == ChunkSize - 1) RefreshChunkMesh(chunkPos + new Vector3Int(0, 0, ChunkSize));
        }

        private void RefreshLoadedNeighbors(Vector3Int chunkPos)
        {
            RefreshChunkMeshAsync(chunkPos + new Vector3Int(-ChunkSize, 0, 0)).Forget();
            RefreshChunkMeshAsync(chunkPos + new Vector3Int(ChunkSize, 0, 0)).Forget();
            RefreshChunkMeshAsync(chunkPos + new Vector3Int(0, 0, -ChunkSize)).Forget();
            RefreshChunkMeshAsync(chunkPos + new Vector3Int(0, 0, ChunkSize)).Forget();
        }

        public int GetSurfaceHeight(int worldX, int worldZ) =>
            _terrainGenerator.GetSurfaceHeight(worldX, worldZ, _worldRules.MaxTerrainHeight, ChunkHeight, _worldOffset);

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