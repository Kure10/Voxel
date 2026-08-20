using System;
using UnityEngine;
using VoxelWorld;

namespace VoxelWorld
{
    public static class Chunk
    {
        public static void LoopThroughTheBlocks(ChunkData chunkData, Action<int, int, int> actionToPerform)
        {
            for (int index = 0; index < chunkData.Blocks.Length; index++)
            {
                var position = GetPostitionFromIndex(chunkData, index);
                actionToPerform(position.x, position.y, position.z);
            }
        }

        private static Vector3Int GetPostitionFromIndex(ChunkData chunkData, int index)
        {
            int x = index % chunkData.ChunkSize;
            int y = (index / chunkData.ChunkSize) % chunkData.ChunkHeight;
            int z = index / (chunkData.ChunkSize * chunkData.ChunkHeight);
            return new Vector3Int(x, y, z);
        }

        //in chunk coordinate system
        private static bool InRange(ChunkData chunkData, int axisCoordinate)
        {
            if (axisCoordinate < 0 || axisCoordinate >= chunkData.ChunkSize)
                return false;

            return true;
        }

        //in chunk coordinate system
        private static bool InRangeHeight(ChunkData chunkData, int ycoordinate)
        {
            if (ycoordinate < 0 || ycoordinate >= chunkData.ChunkHeight)
                return false;

            return true;
        }

        public static BlockType GetBlockFromChunkCoordinates(ChunkData chunkData, Vector3Int chunkCoordinates)
        {
            return GetBlockFromChunkCoordinates(chunkData, chunkCoordinates.x, chunkCoordinates.y, chunkCoordinates.z);
        }

        public static BlockType GetBlockFromChunkCoordinates(ChunkData chunkData, int x, int y, int z)
        {
            if (InRange(chunkData, x) && InRangeHeight(chunkData, y) && InRange(chunkData, z))
            {
                int index = GetIndexFromPosition(chunkData, x, y, z);
                return chunkData.Blocks[index];
            }

            // Y is NOT chunked like X/Z — there's no vertical chunk stacking, so ChunkHeight is a
            // permanent world limit, not "not loaded yet". Treating an out-of-height query the same
            // way as a missing horizontal neighbour (BlockType.Nothing -> "don't draw this face")
            // was silently hiding the topmost exposed face of any terrain clamped to the chunk
            // ceiling (see WorldRules' MaxTerrainHeight/ChunkHeight warning) — no visible/collidable
            // "roof" meant players fell straight through wherever terrain generated tall enough to
            // get clamped. Below y=0 gets the symmetric treatment so the world floor is solid too.
            if (y < 0)
                return BlockType.Gray;
            if (y >= chunkData.ChunkHeight)
                return BlockType.Air;

            return chunkData.WorldReference.GetBlockFromChunkCoordinates(
                chunkData.WorldPosition.x + x, chunkData.WorldPosition.y + y, chunkData.WorldPosition.z + z);
        }

        public static void SetBlock(ChunkData chunkData, Vector3Int localPosition, BlockType block)
        {
            if (InRange(chunkData, localPosition.x) && InRangeHeight(chunkData, localPosition.y) &&
                InRange(chunkData, localPosition.z))
            {
                int index = GetIndexFromPosition(chunkData, localPosition.x, localPosition.y, localPosition.z);
                chunkData.Blocks[index] = block;
            }
            else
            {
                throw new Exception("Need to ask World for appropiate chunk");
            }
        }

        public static int AddBlockDamage(ChunkData chunkData, Vector3Int localPosition)
        {
            int index = GetIndexFromPosition(chunkData, localPosition.x, localPosition.y, localPosition.z);
            chunkData.Damage[index]++;
            return chunkData.Damage[index];
        }

        public static void ClearBlockDamage(ChunkData chunkData, Vector3Int localPosition)
        {
            int index = GetIndexFromPosition(chunkData, localPosition.x, localPosition.y, localPosition.z);
            chunkData.Damage[index] = 0;
        }

        private static int GetIndexFromPosition(ChunkData chunkData, int x, int y, int z)
        {
            return x + chunkData.ChunkSize * y + chunkData.ChunkSize * chunkData.ChunkHeight * z;
        }

        public static Vector3Int GetBlockInChunkCoordinates(ChunkData chunkData, Vector3Int pos)
        {
            return new Vector3Int
            {
                x = pos.x - chunkData.WorldPosition.x,
                y = pos.y - chunkData.WorldPosition.y,
                z = pos.z - chunkData.WorldPosition.z
            };
        }

        public static MeshData GetChunkMeshData(ChunkData chunkData)
        {
            MeshData meshData = new MeshData(true);

            LoopThroughTheBlocks(chunkData,
                (x, y, z) => meshData = BlockHelper.GetMeshData(chunkData, x, y, z, meshData,
                    chunkData.Blocks[GetIndexFromPosition(chunkData, x, y, z)]));


            return meshData;
        }
    }
}