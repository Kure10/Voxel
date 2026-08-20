using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

        // Whether this chunk currently has a cooked MeshCollider. Chunks far from the player
        // are visual-only (no collider) — see WorldRules.ColliderDistanceInChunks and
        // WorldService.LoadChunkAsync / SetChunkColliderAsync.
        public bool HasCollider { get; private set; }

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

        private void RenderMesh(MeshData meshData, bool needsCollider)
        {
            _mesh.Clear();

            _mesh.subMeshCount = 2;
            _mesh.vertices = meshData.Vertices.Concat(meshData.WaterMesh.Vertices).ToArray();

            _mesh.SetTriangles(meshData.Triangles.ToArray(), 0);
            _mesh.SetTriangles(meshData.WaterMesh.Triangles.Select(val => val + meshData.Vertices.Count).ToArray(), 1);

            _mesh.uv = meshData.Uv.Concat(meshData.WaterMesh.Uv).ToArray();
            _mesh.RecalculateNormals();

            if (needsCollider)
            {
                // The expensive part: assigning sharedMesh makes PhysX cook a collision mesh
                // (build its internal BVH) right here, synchronously, on the main thread.
                // Only pay for this when the chunk is close enough to the player to matter.
                Mesh collisionMesh = new Mesh();
                collisionMesh.vertices = meshData.ColliderVertices.ToArray();
                collisionMesh.triangles = meshData.ColliderTriangles.ToArray();
                collisionMesh.RecalculateNormals();

                _meshCollider.sharedMesh = collisionMesh;
                HasCollider = true;
            }
            else
            {
                ClearCollider();
            }
        }

        // Dropping a collider is effectively free — no PhysX cooking, just clearing a reference.
        public void ClearCollider()
        {
            _meshCollider.sharedMesh = null;
            HasCollider = false;
        }

        public void UpdateChunk() => UpdateChunk(Chunk.GetChunkMeshData(ChunkData), needsCollider: true);

        public void UpdateChunk(MeshData data) => UpdateChunk(data, needsCollider: true);

        public void UpdateChunk(MeshData data, bool needsCollider)
        {
            RenderMesh(data, needsCollider);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (ShowGizmo)
            {
                if (Application.isPlaying && ChunkData != null)
                {
                    if (Selection.activeObject == gameObject)
                        Gizmos.color = new Color(0, 1, 0, 0.4f);
                    else
                        Gizmos.color = new Color(1, 0, 1, 0.4f);

                    Gizmos.DrawCube(transform.position + new Vector3(ChunkData.ChunkSize / 2f, ChunkData.ChunkHeight / 2f, ChunkData.ChunkSize / 2f),
                        new Vector3(ChunkData.ChunkSize, ChunkData.ChunkHeight, ChunkData.ChunkSize));
                }
            }
        }
#endif
    }
}