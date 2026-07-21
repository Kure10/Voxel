using System.Collections.Generic;
using UnityEngine;
using VoxelWorld;

namespace VoxelWorld
{
    public class BlockDataManager : MonoBehaviour
    {
        public static float TextureOffset = 0.001f;
        public static float TileSizeX, TileSizeY;

        public static Dictionary<BlockType, TextureData> BlockTextureDataDictionary =
            new Dictionary<BlockType, TextureData>();

        public BlockDataSO TextureData;

        private void Awake()
        {
            foreach (var item in TextureData.TextureDataList)
            {
                if (BlockTextureDataDictionary.ContainsKey(item.BlockType) == false)
                {
                    BlockTextureDataDictionary.Add(item.BlockType, item);
                }
            }

            TileSizeX = TextureData.TextureSizeX;
            TileSizeY = TextureData.TextureSizeY;
        }
    }
}