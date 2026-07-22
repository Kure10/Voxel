using UnityEngine;

namespace VoxelWorld
{
    [CreateAssetMenu(fileName = "NoiseSettings", menuName = "Data/Noise Settings")]
    public class NoiseSettingsSO : ScriptableObject
    {
        public float NoiseZoom = 0.03f;
        public int Octaves = 4;
        public float Persistance = 0.5f;
        public float RedistributionModifier = 1f;
        public float Exponent = 1f;
    }
}