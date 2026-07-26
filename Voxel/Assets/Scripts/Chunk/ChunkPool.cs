using System.Collections.Generic;
using UnityEngine;

namespace VoxelWorld
{
    public class ChunkPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _poolRoot;
        private readonly Stack<ChunkRenderer> _available = new();

        public ChunkPool(GameObject prefab, int prewarmCount)
        {
            _prefab = prefab;
            _poolRoot = new GameObject("ChunkPool").transform;

            for (int i = 0; i < prewarmCount; i++)
                _available.Push(CreateNew());
        }

        private ChunkRenderer CreateNew()
        {
            GameObject obj = Object.Instantiate(_prefab, _poolRoot);
            obj.SetActive(false);
            return obj.GetComponent<ChunkRenderer>();
        }

        public ChunkRenderer Get(Vector3Int worldPosition)
        {
            ChunkRenderer renderer = _available.Count > 0 ? _available.Pop() : CreateNew();
            renderer.transform.SetPositionAndRotation(worldPosition, Quaternion.identity);
            renderer.gameObject.SetActive(true);
            return renderer;
        }

        public void Return(ChunkRenderer renderer)
        {
            renderer.gameObject.SetActive(false);
            renderer.transform.SetParent(_poolRoot);
            _available.Push(renderer);
        }
    }
}