using UnityEngine;

namespace VoxelWorld
{
    [CreateAssetMenu(fileName = "WorldRules", menuName = "Data/World Rules")]
    public class WorldRules : ScriptableObject
    {
        [Header("Player")]
        public GameObject CharacterPrefab;
        [Header("Chunk Settings")]
        public GameObject ChunkPrefab;
        public int ChunkSize = 16;
        public int ChunkHeight = 36;

        [Header("Streaming")]
        public int ViewDistanceInChunks = 6;

        [Tooltip("How many chunks may be generating/meshing at the same time. Higher = view distance " +
                 "fills in faster overall, but bigger frame-time spikes when many chunks finish around " +
                 "the same moment (e.g. right after spawn or a big teleport). Lower = smoother frame " +
                 "pacing, slower to fully fill in the view distance. PlayerChunkStreamer enforces this " +
                 "via a nearest-first queue instead of firing every required chunk at once.")]
        public int MaxConcurrentChunkLoads = 4;

        [Header("Collision")]
        [Tooltip("Chunks within this many chunks (Chebyshev/square distance) of the player get a " +
                 "MeshCollider — needed for mining/building raycasts (see PlayerActions.InteractionRange). " +
                 "Chunks further away are visual-only: no PhysX mesh cooking, which is the single most " +
                 "expensive thing that happens per chunk load. Keep this at least " +
                 "ceil(InteractionRange / ChunkSize) — with the default 6m range and a 36-unit chunk, " +
                 "1 is already enough (covers the current chunk + its 8 neighbours).")]
        public int ColliderDistanceInChunks = 1;

        [Header("Terrain")]
        [Tooltip("Amplitude the noise scales into — how tall hills get. Independent of ChunkHeight.")]
        public int MaxTerrainHeight = 100;
        public int MapSizeInChunks = 6;

        [Header("Water")]
        public int WaterLevel = 50;

        [Header("Height Bands (levels above WaterLevel)")]
        public int GrayLevelsAboveWater = 7;
        public int GreenLevels = 20;
        public int WhiteLevels = 3;

        [Header("Dig/Build Limits")]
        public int MinDigHeight = 0;

        public int MaxBuildHeight => ChunkHeight - 1;

        public BlockType GetSolidBlockType(int y)
        {
            int grayMaxHeight = WaterLevel + GrayLevelsAboveWater;
            int greenMaxHeight = grayMaxHeight + GreenLevels;
            int whiteMaxHeight = greenMaxHeight + WhiteLevels;

            if (y <= grayMaxHeight) return BlockType.Gray;
            if (y == grayMaxHeight + 1) return BlockType.GreenFirst;
            if (y == greenMaxHeight) return BlockType.GreenLast;
            if (y <= greenMaxHeight - 1) return BlockType.Green;
            if (y <= whiteMaxHeight) return BlockType.White;
            return BlockType.Ice;
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