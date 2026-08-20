using After.Main;
using Character;
using System.Collections.Generic;
using System.Linq;
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

        // Chunks still waiting to be loaded, nearest-to-player first. Rebuilt every time
        // UpdateStreaming runs (player crosses a chunk boundary); drained gradually — a few
        // at a time — every frame in ProcessLoadQueue, instead of firing all of them at once.
        private readonly Queue<Vector3Int> _pendingLoadQueue = new();

        public override void Initialize()
        {
            base.Initialize();
            _eventManager.AddListener<PlayerAddedEvent>(OnPlayerAdded);
            _eventManager.AddListener(EventName.OnWorldGenerated, ForceRestream);
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

            if (!_hasStreamedOnce || playerChunkIndex != _currentPlayerChunkIndex)
            {
                _currentPlayerChunkIndex = playerChunkIndex;
                _hasStreamedOnce = true;
                UpdateStreaming(playerChunkIndex);
            }

            // Runs every frame regardless of whether streaming re-triggered this frame —
            // this is what actually drains the queue over time instead of all at once.
            ProcessLoadQueue();
        }

        private void UpdateStreaming(Vector3Int centerChunkIndex)
        {
            int radius = _worldRules.ViewDistanceInChunks;
            int colliderRadius = _worldRules.ColliderDistanceInChunks;
            int chunkSize = _worldService.ChunkSize;

            var required = new HashSet<Vector3Int>();
            var pending = new List<(Vector3Int pos, int distance)>();

            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    Vector3Int chunkPos = new Vector3Int(
                        (centerChunkIndex.x + x) * chunkSize, 0,
                        (centerChunkIndex.z + z) * chunkSize);

                    required.Add(chunkPos);

                    int distance = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));

                    if (_worldService.IsChunkLoaded(chunkPos))
                    {
                        // Already loaded — cheap no-op unless the near/far state actually changed.
                        bool needsCollider = distance <= colliderRadius;
                        _worldService.SetChunkColliderAsync(chunkPos, needsCollider).Forget();
                    }
                    else
                    {
                        // Not loaded yet — queue it instead of starting it immediately.
                        pending.Add((chunkPos, distance));
                    }
                }
            }

            // Nearest first — the player should see solid ground right around them before the
            // far edge of the view distance trickles in.
            _pendingLoadQueue.Clear();
            foreach (var (pos, _) in pending.OrderBy(p => p.distance))
                _pendingLoadQueue.Enqueue(pos);

            foreach (Vector3Int loadedChunkPos in new List<Vector3Int>(_worldService.LoadedChunkPositions))
            {
                if (!required.Contains(loadedChunkPos))
                    _worldService.UnloadChunk(loadedChunkPos);
            }
        }

        private void ProcessLoadQueue()
        {
            int colliderRadius = _worldRules.ColliderDistanceInChunks;
            int chunkSize = _worldService.ChunkSize;

            // Keep starting new loads as long as the queue has work AND we're under the
            // concurrency budget. LoadingChunkCount goes up synchronously the instant
            // LoadChunkAsync is called (before its first await), so this loop can't
            // over-shoot the budget within a single frame.
            while (_pendingLoadQueue.Count > 0 &&
                   _worldService.LoadingChunkCount < _worldRules.MaxConcurrentChunkLoads)
            {
                Vector3Int chunkPos = _pendingLoadQueue.Dequeue();

                // Re-evaluate collider need against the player's CURRENT position — they may
                // have kept moving in the frames since this position was queued.
                Vector3Int chunkIndex = new Vector3Int(chunkPos.x / chunkSize, 0, chunkPos.z / chunkSize);
                int distance = Mathf.Max(
                    Mathf.Abs(chunkIndex.x - _currentPlayerChunkIndex.x),
                    Mathf.Abs(chunkIndex.z - _currentPlayerChunkIndex.z));
                bool needsCollider = distance <= colliderRadius;

                // Safe even if this position somehow already loaded/started loading elsewhere —
                // LoadChunkAsync no-ops in that case.
                _worldService.LoadChunkAsync(chunkPos, needsCollider).Forget();
            }
        }

        private void ForceRestream()
        {
            _hasStreamedOnce = false;
        }

        protected override void OnControllerDestroy()
        {
            base.OnControllerDestroy();
            if (_eventManager != null)
            {
                _eventManager.RemoveListener<PlayerAddedEvent>(OnPlayerAdded);
                _eventManager.RemoveListener(EventName.OnWorldGenerated, ForceRestream);
            }
        }
    }
}