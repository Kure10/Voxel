using UnityEngine;

namespace VoxelWorld
{
    public class ChunkData
    {
        public BlockType[] Blocks;
        public byte[] Damage;
        public int ChunkSize = 16;
        public int ChunkHeight = 100;
        public WorldService WorldReference;
        public Vector3Int WorldPosition;

        public bool ModifiedByThePlayer = false;

        public ChunkData(int chunkSize, int chunkHeight, WorldService worldService, Vector3Int worldPosition)
        {
            ChunkHeight = chunkHeight;
            ChunkSize = chunkSize;
            WorldReference = worldService;
            WorldPosition = worldPosition;
            Blocks = new BlockType[chunkSize * chunkHeight * chunkSize];
            Damage = new byte[chunkSize * chunkHeight * chunkSize];
        }
    }
}