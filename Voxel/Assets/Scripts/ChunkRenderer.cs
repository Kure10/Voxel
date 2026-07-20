using UnityEngine;
using System.Linq;

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
        
        private void RenderMesh(MeshData meshData)
        {
            _mesh.Clear();
        
            _mesh.subMeshCount = 2;
            _mesh.vertices = meshData.Vertices.Concat(meshData.WaterMesh.Vertices).ToArray();
        
            _mesh.SetTriangles(meshData.Triangles.ToArray(), 0);
            _mesh.SetTriangles(meshData.WaterMesh.Triangles.Select(val => val + meshData.Vertices.Count).ToArray(), 1);
        
            _mesh.uv = meshData.Uv.Concat(meshData.WaterMesh.Uv).ToArray();
            _mesh.RecalculateNormals();
        
            _meshCollider.sharedMesh = null;
            Mesh collisionMesh = new Mesh();
            collisionMesh.vertices = meshData.ColliderVertices.ToArray();
            collisionMesh.triangles = meshData.ColliderTriangles.ToArray();
            collisionMesh.RecalculateNormals();
        
            _meshCollider.sharedMesh = collisionMesh;
        }
    }
}