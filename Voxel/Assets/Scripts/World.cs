using System.Collections.Generic;
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

        [Header("Spawn")]
        [Tooltip("Extra pause, after the world (and the spawn neighbourhood) has finished loading, " +
                 "before the player actually spawns and OnWorldGenerated fires. Purely cosmetic — " +
                 "keeps the loading screen up a bit longer instead of snapping away the instant " +
                 "loading happens to finish, which can otherwise feel abrupt.")]
        public float SpawnDelaySeconds = 1f;

        [Inject] private WorldRules _worldRules;
        [Inject] private WorldService _worldService;
        [Inject] private MyEventManager _eventManager;


        public override void Initialize()
        {
            base.Initialize();
            _eventManager.AddListener<LoadWorldEvent>(OnLoadWorldRequested);
        }
        //For Button
        public UniTask GenerateWorld() => GenerateWorldAsync();

        private async UniTask  GenerateWorldAsync()
        {
            if (UseRandomSeed)
                Seed = Random.Range(int.MinValue, int.MaxValue);

            System.Random seededRandom = new System.Random(Seed);
            Vector2Int worldOffset = new Vector2Int(seededRandom.Next(-100000, 100000), seededRandom.Next(-100000, 100000));

            _worldService.Configure(TerrainGenerator);
            _worldService.SetWorldOffset(worldOffset);
            _worldService.ClearWorld();

            // Load a solid neighbourhood around spawn before anyone is allowed to exist in the
            // world. Just the single origin chunk isn't enough — PlayerSpawner spawns at the
            // CENTER of that chunk (see PlayerSpawner.cs), but PlayerChunkStreamer only starts
            // streaming the surrounding chunks once the player already exists, and with
            // MaxConcurrentChunkLoads throttling those neighbours, that gap got long enough for
            // gravity to pull the player through unloaded ground near the chunk's edge.
            await LoadSpawnNeighborhoodAsync();

            // Cosmetic pause — the world is already fully safe to spawn into at this point,
            // this just holds the loading screen up a little longer on purpose.
            if (SpawnDelaySeconds > 0f)
                await UniTask.Delay(System.TimeSpan.FromSeconds(SpawnDelaySeconds));

            _worldService.SetCurrentSeed(Seed);
            _eventManager.DispatchEvent(EventName.OnWorldGenerated);
        }

        // Loads every chunk within ColliderDistanceInChunks of the origin, all with colliders,
        // in parallel (not through PlayerChunkStreamer's throttled queue — this is a small,
        // one-time, must-finish-before-anyone-can-fall-through load, done while the loading
        // screen is up, not something that needs frame-time smoothing).
        private async UniTask LoadSpawnNeighborhoodAsync()
        {
            int radius = _worldRules.ColliderDistanceInChunks;
            int chunkSize = _worldService.ChunkSize;

            var loads = new List<UniTask>();

            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    Vector3Int chunkPos = new Vector3Int(x * chunkSize, 0, z * chunkSize);
                    loads.Add(_worldService.LoadChunkAsync(chunkPos, needsCollider: true));
                }
            }

            await UniTask.WhenAll(loads);
        }

        private void OnLoadWorldRequested(LoadWorldEvent e)
        {
            GenerateLoadWorld(e.Seed).Forget();
        }

        public UniTask GenerateLoadWorld(int seed)
        {
            UseRandomSeed = false;
            Seed = seed;
            return GenerateWorld();
        }

        protected override void OnControllerDestroy()
        {
            base.OnControllerDestroy();
            if (_eventManager != null)
                _eventManager.RemoveListener<LoadWorldEvent>(OnLoadWorldRequested);
        }
    }
}