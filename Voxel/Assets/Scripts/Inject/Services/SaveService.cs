using System.Collections.Generic;
using System.IO;
using System.Linq;
using After.Main;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VoxelWorld
{
    public class SaveService : IService
    {
        //[Inject] private World _world;
        [Inject] private WorldService _worldService;
        [Inject] private PlayerService _playerService;
        [Inject] private CoreGameInputsSystem _coreGameInputsSystem;
        [Inject] private MyEventManager _eventManager;

        private const int SaveFormatVersion = 1;
        
        private string _savePath;
        private Vector3? _pendingPlayerPosition;
        public void Init()
        {
            _savePath = Path.Combine(Application.persistentDataPath, "world.save");
            
            _coreGameInputsSystem.OnSaveRequested += HandleSaveRequested;
            _coreGameInputsSystem.OnLoadRequested += HandleLoadRequested;
            _eventManager.AddListener(EventName.OnWorldGenerated, OnWorldGenerated);
        }

        public void Destroy()
        {
            _coreGameInputsSystem.OnSaveRequested -= HandleSaveRequested;
            _coreGameInputsSystem.OnLoadRequested -= HandleLoadRequested;
            _eventManager.RemoveListener(EventName.OnWorldGenerated, OnWorldGenerated);
        }

        private void HandleSaveRequested() => SaveGame().Forget();
        private void HandleLoadRequested() => LoadGame().Forget();

        public async UniTask SaveGame()
        {
            int seed = _worldService.CurrentSeed;
            Vector3 playerPos = _playerService.Players.FirstOrDefault()?.Position ?? Vector3.zero;

            var modifiedChunks = _worldService.GetModifiedChunks()
                .Select(c => (c.WorldPosition, Blocks: (BlockType[])c.Blocks.Clone()))
                .ToList();

            await UniTask.RunOnThreadPool(() =>
            {
                using var stream = File.Create(_savePath); // overwrites any existing save — single slot
                using var writer = new BinaryWriter(stream);

                writer.Write(SaveFormatVersion);
                writer.Write(seed);
                writer.Write(playerPos.x);
                writer.Write(playerPos.y);
                writer.Write(playerPos.z);

                writer.Write(modifiedChunks.Count);

                foreach (var (position, blocks) in modifiedChunks)
                {
                    writer.Write(position.x);
                    writer.Write(position.y);
                    writer.Write(position.z);
                    writer.Write(blocks.Length);
                    foreach (var block in blocks)
                        writer.Write((byte)block);
                }
            });

            Debug.Log($"World saved: {modifiedChunks.Count} modified chunks.");
        }

        public async UniTask LoadGame()
        {
            if (!File.Exists(_savePath))
            {
                Debug.LogWarning("No save file found.");
                return;
            }

            _eventManager.DispatchEvent(EventName.OnWorldLoadStarted);

            using var stream = File.OpenRead(_savePath);
            using var reader = new BinaryReader(stream);

            reader.ReadInt32();
            int savedSeed = reader.ReadInt32();
            Vector3 savedPlayerPos = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

            int chunkCount = reader.ReadInt32();
            var overrides = new List<(Vector3Int, BlockType[])>(chunkCount);

            for (int i = 0; i < chunkCount; i++)
            {
                Vector3Int pos = new Vector3Int(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
                int blockCount = reader.ReadInt32();
                BlockType[] blocks = new BlockType[blockCount];
                for (int b = 0; b < blockCount; b++)
                    blocks[b] = (BlockType)reader.ReadByte();

                overrides.Add((pos, blocks));
            }

            _worldService.SetPendingSavedChunks(overrides);
            _pendingPlayerPosition = savedPlayerPos;

            _eventManager.DispatchEvent(new LoadWorldEvent(savedSeed));

            await UniTask.CompletedTask;
        }
        
        private void OnWorldGenerated()
        {
            if (_pendingPlayerPosition == null)
                return;

            var player = _playerService.Players.FirstOrDefault();
            if (player != null)
                player.transform.position = _pendingPlayerPosition.Value;

            _pendingPlayerPosition = null;

            _eventManager.DispatchEvent(EventName.OnWorldLoadFinished);
        }
    }
}