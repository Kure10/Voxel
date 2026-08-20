using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace VoxelWorld
{
    public class WorldService : IService
    {
        [Inject] private WorldRules _worldRules;

        private readonly ConcurrentDictionary<Vector3Int, BlockType[]> _pendingSavedChunks = new();

        private readonly ConcurrentDictionary<Vector3Int, ChunkData> _chunkDataDictionary = new();
        private readonly Dictionary<Vector3Int, ChunkRenderer> _chunkDictionary = new();
        private readonly HashSet<Vector3Int> _loadingChunks = new();

        private TerrainGenerator _terrainGenerator;
        private Vector2Int _worldOffset;
        private ChunkPool _chunkPool;

        public int CurrentSeed { get; private set; }
        public int ChunkSize => _worldRules.ChunkSize;
        public int ChunkHeight => _worldRules.ChunkHeight;
        public IEnumerable<Vector3Int> LoadedChunkPositions => _chunkDictionary.Keys;

        public void Init() { }
        public void Destroy() { }

        public void SetCurrentSeed(int seed) => CurrentSeed = seed;
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

            if (data == null)
            {
                if (_pendingSavedChunks.TryRemove(chunkPos, out BlockType[] savedBlocks))
                {
                    // Restoring a chunk from a save file — reuse the saved block array, mark as modified
                    // so it's never silently discarded/regenerated later.
                    data = new ChunkData(ChunkSize, ChunkHeight, this, chunkPos)
                    {
                        Blocks = savedBlocks,
                        ModifiedByThePlayer = true
                    };
                }
                else
                {
                    // No save data for this position — generate fresh from noise, via a Burst job.
                    data = new ChunkData(ChunkSize, ChunkHeight, this, chunkPos);
                    await GenerateVoxelsAsync(data);
                }

                _chunkDataDictionary[chunkPos] = data;
            }

            // Meshing still goes through the .NET thread pool for now — it depends on
            // neighbouring chunks' data (via Chunk.GetBlockFromChunkCoordinates), which
            // makes it a bigger job to Burst-ify. That's the next step.
            MeshData meshData = await UniTask.RunOnThreadPool(() => Chunk.GetChunkMeshData(data));

            await UniTask.SwitchToMainThread();

            ChunkRenderer chunkRenderer = _chunkPool.Get(chunkPos);
            chunkRenderer.InitializeChunk(data);
            chunkRenderer.UpdateChunk(meshData);
            _chunkDictionary[chunkPos] = chunkRenderer;

            RefreshLoadedNeighbors(chunkPos);

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

        /// <summary>
        /// Fills data.Blocks using a Burst-compiled, multi-threaded job instead of a
        /// plain C# nested loop. Runs from the main thread: scheduling a job is cheap,
        /// the actual work happens on Unity's job worker threads while we asynchronously
        /// wait via UniTask (no frame is blocked).
        /// </summary>
        private async UniTask GenerateVoxelsAsync(ChunkData data)
        {
            int voxelCount = data.ChunkSize * data.ChunkHeight * data.ChunkSize;

            // Allocator.Persistent (not TempJob!) because we're awaiting across frames —
            // TempJob native arrays must be disposed within ~4 frames or the safety system
            // throws. With many chunks streaming in at once, that window isn't guaranteed.
            // We dispose this manually right below, once the job is done.
            var blocks = new NativeArray<BlockType>(voxelCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            var job = new GenerateVoxelsJob
            {
                ChunkSize = data.ChunkSize,
                ChunkHeight = data.ChunkHeight,

                WorldPositionX = data.WorldPosition.x,
                WorldPositionZ = data.WorldPosition.z,
                WorldOffsetX = _worldOffset.x,
                WorldOffsetY = _worldOffset.y,

                NoiseZoom = _terrainGenerator.NoiseSettings.NoiseZoom,
                Octaves = _terrainGenerator.NoiseSettings.Octaves,
                Persistance = _terrainGenerator.NoiseSettings.Persistance,
                RedistributionModifier = _terrainGenerator.NoiseSettings.RedistributionModifier,
                Exponent = _terrainGenerator.NoiseSettings.Exponent,

                MaxTerrainHeight = _worldRules.MaxTerrainHeight,
                WaterLevel = _worldRules.WaterLevel,
                GrayLevelsAboveWater = _worldRules.GrayLevelsAboveWater,
                GreenLevels = _worldRules.GreenLevels,
                WhiteLevels = _worldRules.WhiteLevels,

                Blocks = blocks
            };

            // One iteration per column (x, z); 32 columns per batch handed to each worker thread.
            int columnCount = data.ChunkSize * data.ChunkSize;
            JobHandle handle = job.Schedule(columnCount, 32);

            await handle.ToUniTask();

            // Job is guaranteed complete here (ToUniTask called handle.Complete()) —
            // safe to copy out and dispose the native buffer.
            blocks.CopyTo(data.Blocks);
            blocks.Dispose();
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

        public void SetPendingSavedChunks(IEnumerable<(Vector3Int position, BlockType[] blocks)> savedChunks)
        {
            _pendingSavedChunks.Clear();
            foreach (var (position, blocks) in savedChunks)
                _pendingSavedChunks[position] = blocks;
        }

        public IEnumerable<ChunkData> GetModifiedChunks() => _chunkDataDictionary.Values.Where(c => c.ModifiedByThePlayer);
    }
}