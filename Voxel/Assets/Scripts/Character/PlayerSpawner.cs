using UnityEngine;
using After.Main;

namespace VoxelWorld
{
    public class PlayerSpawner : MonoBehaviour
    {
        public World World;
        public GameObject PlayerPrefab;

        [Tooltip("Extra height above ground/water to avoid spawning inside terrain.")]
        public float SpawnHeightOffset = 2f;

        private GameObject _spawnedPlayer;

        public void SpawnPlayer()
        {
            int centerX = (World.MapSizeInChunks * World.ChunkSize) / 2;
            int centerZ = (World.MapSizeInChunks * World.ChunkSize) / 2;

            int groundHeight = World.GetSurfaceHeight(centerX, centerZ);
            int waterLevel = World.WorldRules.WaterLevel;

            int spawnHeight = Mathf.Max(groundHeight, waterLevel) + Mathf.CeilToInt(SpawnHeightOffset);
            Vector3 spawnPosition = new Vector3(centerX, spawnHeight, centerZ);

            if (_spawnedPlayer == null)
            {
                _spawnedPlayer = Instantiate(PlayerPrefab, spawnPosition, Quaternion.identity);
                InitializePlayerControllers(_spawnedPlayer);
            }
            else
            {
                _spawnedPlayer.transform.position = spawnPosition; // re-generating world moves existing player instead of duplicating
            }
        }

        private void InitializePlayerControllers(GameObject playerInstance)
        {
            // Any Controller (PlayerController, future mining/building controllers, etc.)
            // needs [Inject] fields filled and Initialize() called explicitly,
            // since Instantiate() alone doesn't go through the Injector.
            foreach (var controller in playerInstance.GetComponentsInChildren<Controller>(true))
            {
                Injector.Instance.InjectInto(controller);
                controller.Initialize();
            }
        }
    }
}