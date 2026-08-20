using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxelWorld
{
    /// <summary>
    /// Burst-compiled replacement for WorldService.GenerateVoxels().
    /// One Execute(index) call = one (x, z) column of the chunk. Unity's Job System
    /// splits all columns into batches and runs them across multiple worker threads;
    /// [BurstCompile] then turns this into vectorized native code instead of interpreted C#.
    ///
    /// IMPORTANT: this is a struct, not a class. Jobs get copied to worker threads,
    /// so every field must be a plain value type (int, float, NativeArray...).
    /// You CANNOT put a reference to a MonoBehaviour or ScriptableObject in here
    /// (e.g. NoiseSettingsSO or WorldRules directly) — copy the numbers you need
    /// out of them on the main thread before scheduling the job.
    /// </summary>
    [BurstCompile]
    public struct GenerateVoxelsJob : IJobParallelFor
    {
        // Chunk shape
        public int ChunkSize;
        public int ChunkHeight;

        // Where this chunk sits in world space + the per-world-seed offset,
        // so noise is sampled at the correct absolute coordinates.
        public int WorldPositionX;
        public int WorldPositionZ;
        public int WorldOffsetX;
        public int WorldOffsetY;

        // Copied from NoiseSettingsSO (can't reference the ScriptableObject itself in a job)
        public float NoiseZoom;
        public int Octaves;
        public float Persistance;
        public float RedistributionModifier;
        public float Exponent;

        // Copied from WorldRules
        public int MaxTerrainHeight;
        public int WaterLevel;
        public int GrayLevelsAboveWater;
        public int GreenLevels;
        public int WhiteLevels;

        // We only ever write to this array, never read it back inside the job -> WriteOnly.
        // NativeDisableParallelForRestriction: by default the safety system assumes
        // "1 array slot per iteration index", which would falsely flag this as unsafe,
        // because here 1 iteration (1 column) writes ChunkHeight slots, not 1.
        // We know it's safe because every column writes a disjoint range of indices.
        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<BlockType> Blocks;

        public void Execute(int index)
        {
            int x = index % ChunkSize;
            int z = index / ChunkSize;

            int groundHeight = GetSurfaceHeight(WorldPositionX + x, WorldPositionZ + z);

            for (int y = 0; y < ChunkHeight; y++)
            {
                BlockType voxelType;

                if (y > groundHeight)
                    voxelType = y < WaterLevel ? BlockType.Water : BlockType.Air;
                else
                    voxelType = GetSolidBlockType(y);

                // Same flat-array indexing as Chunk.GetIndexFromPosition — must match
                // exactly, or the mesher will read garbage from the wrong cell.
                int blockIndex = x + ChunkSize * y + ChunkSize * ChunkHeight * z;
                Blocks[blockIndex] = voxelType;
            }
        }

        private int GetSurfaceHeight(int worldX, int worldZ)
        {
            float noiseValue = OctavePerlin(worldX + WorldOffsetX, worldZ + WorldOffsetY);
            noiseValue = math.pow(noiseValue * RedistributionModifier, Exponent);

            int rawHeight = (int)math.round(noiseValue * MaxTerrainHeight);
            return math.clamp(rawHeight, 0, ChunkHeight - 1);
        }

        private float OctavePerlin(float x, float z)
        {
            x *= NoiseZoom;
            z *= NoiseZoom;

            float total = 0f;
            float frequency = 1f;
            float amplitude = 1f;
            float amplitudeSum = 0f;

            for (int i = 0; i < Octaves; i++)
            {
                // Mathf.PerlinNoise isn't Burst-compatible, so we use Unity.Mathematics.noise
                // instead. noise.cnoise (classic Perlin) returns roughly [-1, 1]; Mathf.PerlinNoise
                // returned [0, 1] per call, so we remap each octave the same way to keep the
                // NoiseSettings sliders (zoom/octaves/persistance) behaving the way they used to.
                // NOTE: it's still a different noise algorithm than before, so the actual shape
                // of the terrain will look a bit different even with the same settings/seed —
                // that's expected, not a bug. You'll likely want to re-tune NoiseSettings a bit.
                float sample = noise.cnoise(new float2(x * frequency, z * frequency));
                float sample01 = sample * 0.5f + 0.5f;

                total += sample01 * amplitude;
                amplitudeSum += amplitude;

                amplitude *= Persistance;
                frequency *= 2f;
            }

            return total / amplitudeSum;
        }

        private BlockType GetSolidBlockType(int y)
        {
            // 1:1 port of WorldRules.GetSolidBlockType — duplicated here on purpose,
            // because a Burst job can't call a method on a ScriptableObject.
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
    }
}