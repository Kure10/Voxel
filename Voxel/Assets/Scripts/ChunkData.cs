using UnityEngine;

namespace VoxelWorld
{
    public class ChunkData
    {
        public BlockType[] Blocks;
        public int ChunkSize = 16;
        public int ChunkHeight = 100;
        public World WorldReference;
        public Vector3Int WorldPosition;

        public bool ModifiedByThePlayer = false;
        
        public ChunkData(int chunkSize, int chunkHeight, World world, Vector3Int worldPosition)
        {
            ChunkHeight = chunkHeight;
            ChunkSize = chunkSize;
            WorldReference = world;
            WorldPosition = worldPosition;
            Blocks = new BlockType[chunkSize * chunkHeight * chunkSize];
        }
        
        
    }
}