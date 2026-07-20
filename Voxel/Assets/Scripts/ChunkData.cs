using UnityEngine;

namespace VoxelWorld
{
    public class ChunkData
    {
        public BlockType[] blocks;
        public int ChunkSize = 16;
        public int ChunkHeight = 100;
        public World WorldReference;
        public Vector3Int WorldPosition;

        public bool ModifiedByThePlayer = false;
        
        public ChunkData(World world, Vector3Int position , int size, int height)
        {
            WorldReference = world;
            WorldPosition = position;
            ChunkHeight = height;
            ChunkSize = size;
            blocks = new BlockType[ChunkSize * ChunkSize * ChunkHeight];
        }
        
        
    }
}