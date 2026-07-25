using After.Main;
using UnityEngine;

namespace VoxelWorld
{
    public class World : Controller
    {
        [Inject] private WorldRules _worldRules;
        [Inject] private WorldService _worldService;
        [Inject] private MyEventManager _eventManager;
        
        [Header("Chunk Settings")]
        public GameObject ChunkPrefab;

        [Header("Terrain Generation")]
        public TerrainGenerator TerrainGenerator;

        [Header("Seed")]
        public bool UseRandomSeed = true;
        public int Seed;
        
        public void GenerateWorld()
        {
            if (UseRandomSeed)
                Seed = Random.Range(int.MinValue, int.MaxValue);

            System.Random seededRandom = new System.Random(Seed);
            Vector2Int worldOffset = new Vector2Int(seededRandom.Next(-100000, 100000), seededRandom.Next(-100000, 100000));

            _worldService.Configure(TerrainGenerator);
            _worldService.SetWorldOffset(worldOffset);
            _worldService.ClearWorld();

            for (int x = 0; x < _worldRules.MapSizeInChunks; x++)
            {
                for (int z = 0; z < _worldRules.MapSizeInChunks; z++)
                {
                    ChunkData data = new ChunkData(_worldRules.ChunkSize, _worldRules.ChunkHeight, _worldService,
                        new Vector3Int(x * _worldRules.ChunkSize, 0, z * _worldRules.ChunkSize));
                    GenerateVoxels(data);
                    _worldService.RegisterChunkData(data);
                }
            }

            foreach (ChunkData data in _worldService.AllChunkData)
            {
                MeshData meshData = Chunk.GetChunkMeshData(data);
                GameObject chunkObject = Instantiate(ChunkPrefab, data.WorldPosition, Quaternion.identity);
                ChunkRenderer chunkRenderer = chunkObject.GetComponent<ChunkRenderer>();
                _worldService.RegisterChunkRenderer(data.WorldPosition, chunkRenderer);
                chunkRenderer.InitializeChunk(data);
                chunkRenderer.UpdateChunk(meshData);
            }
            
            _eventManager.DispatchEvent(EventName.OnWorldGenerated);
        }

        private void GenerateVoxels(ChunkData data)
        {
            for (int x = 0; x < data.ChunkSize; x++)
            {
                for (int z = 0; z < data.ChunkSize; z++)
                {
                    int groundPosition = _worldService.GetSurfaceHeight(data.WorldPosition.x + x, data.WorldPosition.z + z);

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
    }
}