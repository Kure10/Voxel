using UnityEngine;

namespace VoxelWorld
{
    public class TerrainGenerator : MonoBehaviour
    {
        public NoiseSettingsSO NoiseSettings;

        public int GetSurfaceHeight(int worldX, int worldZ, int chunkHeight, Vector2Int worldOffset)
        {
            float noiseValue = OctavePerlin(worldX + worldOffset.x, worldZ + worldOffset.y);
            noiseValue = Redistribute(noiseValue);
            return Mathf.Clamp(Mathf.RoundToInt(noiseValue * chunkHeight), 0, chunkHeight - 1);
        }

        private float OctavePerlin(float x, float z)
        {
            x *= NoiseSettings.NoiseZoom;
            z *= NoiseSettings.NoiseZoom;

            float total = 0f;
            float frequency = 1f;
            float amplitude = 1f;
            float amplitudeSum = 0f;

            for (int i = 0; i < NoiseSettings.Octaves; i++)
            {
                total += Mathf.PerlinNoise(x * frequency, z * frequency) * amplitude;
                amplitudeSum += amplitude;

                amplitude *= NoiseSettings.Persistance;
                frequency *= 2f;
            }

            return total / amplitudeSum;
        }

        private float Redistribute(float noise)
        {
            return Mathf.Pow(noise * NoiseSettings.RedistributionModifier, NoiseSettings.Exponent);
        }
    }
}