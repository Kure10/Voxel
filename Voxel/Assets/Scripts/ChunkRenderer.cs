using UnityEngine;

namespace VoxelWorld
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class ChunkRenderer : MonoBehaviour
    {
        private MeshFilter _meshFilter;
        private MeshCollider _meshCollider;
        private Mesh _mesh;
        public bool ShowGizmo = false;

        public ChunkData ChunkData { get; private set; }

        public bool ModifiedByThePlayer
        {
            get { return ChunkData.ModifiedByThePlayer; }
            set { ChunkData.ModifiedByThePlayer = value; }
        }

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshCollider = GetComponent<MeshCollider>();
            _mesh = _meshFilter.mesh;
        }
        
        public void InitializeChunk(ChunkData chunkData)
        {
            ChunkData = chunkData;
        }
        
        
        
    }
}