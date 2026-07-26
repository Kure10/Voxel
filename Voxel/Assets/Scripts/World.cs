using After.Main;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VoxelWorld
{
    public class World : Controller
    {
        [Header("Terrain Generation")]
        public TerrainGenerator TerrainGenerator;

        [Header("Seed")]
        public bool UseRandomSeed = true;
        public int Seed;

        [Inject] private WorldRules _worldRules;
        [Inject] private WorldService _worldService;
        [Inject] private MyEventManager _eventManager;

        //For Button
        public void GenerateWorld() => GenerateWorldAsync().Forget();
        
        private async UniTaskVoid GenerateWorldAsync()
        {
            if (UseRandomSeed)
                Seed = Random.Range(int.MinValue, int.MaxValue);

            System.Random seededRandom = new System.Random(Seed);
            Vector2Int worldOffset = new Vector2Int(seededRandom.Next(-100000, 100000), seededRandom.Next(-100000, 100000));

            _worldService.Configure(TerrainGenerator);
            _worldService.SetWorldOffset(worldOffset);
            _worldService.ClearWorld();

            // Load the origin chunk first so the player has solid ground the instant they spawn.
            await _worldService.LoadChunkAsync(Vector3Int.zero);

            _eventManager.DispatchEvent(EventName.OnWorldGenerated);
        }
    }
}