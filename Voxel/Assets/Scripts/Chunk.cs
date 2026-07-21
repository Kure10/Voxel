using System;
using UnityEngine;

namespace VoxelWorld
{
    public static class Chunk
    {
        public static MeshData GetChunkMeshData(ChunkData data)
        {
            MeshData meshData = new MeshData(true);

            // fill later


            return meshData;
        }

        public static void LoopThroughTheBlocks(ChunkData chunkData, Action<int, int, int> actionToPerform)
        {
            for (int index = 0; index < chunkData.blocks.Length; index++)
            {
                var position = GetPostitionFromIndex(chunkData, index);
                actionToPerform(position.x, position.y, position.z);
            }
        }
    }
}
