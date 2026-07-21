using UnityEngine;

namespace VoxelWorld
{
    [CreateAssetMenu(fileName = "WorldRules", menuName = "Data/World Rules")]
    public class WorldRules : ScriptableObject
    {
        [Header("Water")]
        public int WaterLevel = 50;

        [Header("Height Bands (levels above WaterLevel)")]
        public int GrayLevelsAboveWater = 7;   // gray continues this many levels above water
        public int GreenLevels = 20;           // then green for this many levels
        // anything above that -> White

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
    }
}