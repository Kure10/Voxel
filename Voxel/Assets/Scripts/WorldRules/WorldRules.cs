using UnityEngine;

namespace VoxelWorld
{
    [CreateAssetMenu(fileName = "WorldRules", menuName = "Data/World Rules")]
    public class WorldRules : ScriptableObject
    {
        [Header("Chunk Dimensions")]
        public int ChunkSize = 16;
        public int ChunkHeight = 36;
        public int MapSizeInChunks = 6;

        [Header("Terrain")]
        [Tooltip("Amplitude the noise scales into — how tall hills get. Independent of ChunkHeight.")]
        public int MaxTerrainHeight = 100;

        [Header("Water")]
        public int WaterLevel = 50;

        [Header("Height Bands (levels above WaterLevel)")]
        public int GrayLevelsAboveWater = 7;
        public int GreenLevels = 20;

        [Header("Dig/Build Limits")]
        public int MinDigHeight = 0;

        public int MaxBuildHeight => ChunkHeight - 1;

        public BlockType GetSolidBlockType(int y)
        {
            int grayMaxHeight = WaterLevel + GrayLevelsAboveWater;
            int greenMaxHeight = grayMaxHeight + GreenLevels;

            if (y <= grayMaxHeight)
                return BlockType.Gray;
            if (y <= greenMaxHeight)
                return BlockType.Green;
            return BlockType.White;
        }
        
        private void OnEnable()
        {
            OnValidate();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (MaxTerrainHeight > ChunkHeight)
            {
                Debug.LogWarning($"WorldRules: MaxTerrainHeight ({MaxTerrainHeight}) exceeds ChunkHeight ({ChunkHeight}). " +
                                 "Terrain will be clamped at the chunk ceiling — no vertical chunk stacking exists yet to hold the overflow.");
            }
        }
#endif
    }
}