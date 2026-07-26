using After.Main;
using Character;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VoxelWorld
{
    public class PlayerChunkStreamer : Controller
    {
        [Inject] private WorldService _worldService;
        [Inject] private WorldRules _worldRules;
        [Inject] private MyEventManager _eventManager;

        private Character.Character _player;
        private Vector3Int _currentPlayerChunkIndex;
        private bool _hasStreamedOnce;

        public override void Initialize()
        {
            base.Initialize();
            _eventManager.AddListener<PlayerAddedEvent>(OnPlayerAdded);
        }

        private void OnPlayerAdded(PlayerAddedEvent e)
        {
            _player = e.Player;
            _hasStreamedOnce = false;
        }

        private void Update()
        {
            if (_player == null)
                return;

            Vector3Int playerChunkIndex = _worldService.WorldPositionToChunkIndex(_player.Position);

            if (_hasStreamedOnce && playerChunkIndex == _currentPlayerChunkIndex)
                return;

            _currentPlayerChunkIndex = playerChunkIndex;
            _hasStreamedOnce = true;

            UpdateStreaming(playerChunkIndex);
        }

        private void UpdateStreaming(Vector3Int centerChunkIndex)
        {
            int radius = _worldRules.ViewDistanceInChunks;
            int chunkSize = _worldService.ChunkSize;

            var required = new HashSet<Vector3Int>();

            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    Vector3Int chunkPos = new Vector3Int(
                        (centerChunkIndex.x + x) * chunkSize, 0,
                        (centerChunkIndex.z + z) * chunkSize);

                    required.Add(chunkPos);
                    _worldService.LoadChunkAsync(chunkPos).Forget();
                }
            }

            foreach (Vector3Int loadedChunkPos in new List<Vector3Int>(_worldService.LoadedChunkPositions))
            {
                if (!required.Contains(loadedChunkPos))
                    _worldService.UnloadChunk(loadedChunkPos);
            }
        }

        protected override void OnControllerDestroy()
        {
            base.OnControllerDestroy();
            if (_eventManager != null)
                _eventManager.RemoveListener<PlayerAddedEvent>(OnPlayerAdded);
        }
    }
}