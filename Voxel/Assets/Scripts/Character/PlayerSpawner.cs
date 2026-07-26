using UnityEngine;
using After.Main;

namespace VoxelWorld
{
    public class PlayerSpawner : Controller
    {
        [Inject] private MyEventManager _eventManager;
        [Inject] private WorldRules _worldRules;
        [Inject] private WorldService _worldService;

        [Tooltip("Extra height above ground/water to avoid spawning inside terrain.")]
        public float SpawnHeightOffset = 2f;

        private GameObject _spawnedPlayer;

        public override void Initialize()
        {
            base.Initialize();
            _eventManager.AddListener(EventName.OnWorldGenerated, SpawnPlayer);
        }

        private void SpawnPlayer()
        {
            int centerX = 0;
            int centerZ = 0;

            int groundHeight = _worldService.GetSurfaceHeight(centerX, centerZ);
            int waterLevel = _worldRules.WaterLevel;

            int spawnHeight = Mathf.Max(groundHeight, waterLevel) + Mathf.CeilToInt(SpawnHeightOffset);
            Vector3 spawnPosition = new Vector3(centerX, spawnHeight, centerZ);

            if (_spawnedPlayer == null)
            {
                _spawnedPlayer = Instantiate(_worldRules.CharacterPrefab, spawnPosition, Quaternion.identity);
                InitializePlayerControllers(_spawnedPlayer);

                var character = _spawnedPlayer.GetComponentInChildren<Character.Character>();
                if (character != null)
                    _eventManager.DispatchEvent(new Character.PlayerAddedEvent(character));
            }
            else
            {
                _spawnedPlayer.transform.position = spawnPosition; // re-generating world moves existing player instead of duplicating
            }
        }

        protected override void OnControllerDestroy()
        {
            //Todo remove when we implement dead on character.
            if (_eventManager != null)
                _eventManager.RemoveListener(EventName.OnWorldGenerated, SpawnPlayer);

            base.OnControllerDestroy();
        }

        private void InitializePlayerControllers(GameObject playerInstance)
        {
            foreach (var controller in playerInstance.GetComponentsInChildren<Controller>(true))
            {
                Injector.Instance.InjectInto(controller);
                controller.Initialize();
            }
        }
    }
}