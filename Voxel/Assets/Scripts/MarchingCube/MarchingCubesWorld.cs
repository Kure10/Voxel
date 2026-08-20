using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace VoxelWorld.Experimental.MarchingCubes
{
    /// <summary>
    /// Step 2: tile multiple Marching Cubes chunks into one small connected world. Still static
    /// (no editing, no streaming, no Job System yet) -- the goal here is just to prove chunks
    /// tile SEAMLESSLY, with no visible cracks/gaps at their shared borders.
    ///
    /// Why this works with zero explicit chunk-to-chunk data sharing: density is a pure function
    /// of WORLD-SPACE position (noise). Two neighbouring chunks independently sampling the same
    /// world-space grid point along their shared edge compute the exact same density value,
    /// because it's the same deterministic function call with the same input -- so the resulting
    /// surface lines up automatically, no synchronization needed.
    ///
    /// This stops being true the moment chunks get their own MUTABLE density data (i.e. once we
    /// add digging/building) -- at that point a dig near a border only touches ONE chunk's copy,
    /// and the neighbour needs to be told about it (the "padding" problem we discussed). That's
    /// a separate, harder step for later -- not needed for a static world like this one.
    /// </summary>
    public class MarchingCubesWorld : MonoBehaviour
    {
        [Header("World")]
        [Tooltip("World is WorldSizeInChunks x WorldSizeInChunks chunks.")]
        public int WorldSizeInChunks = 4;

        [Tooltip("Cells per HORIZONTAL (X/Z) axis, PER CHUNK. Kept smaller than the cube world's chunk " +
                 "size on purpose -- a full-chunk remesh on every dig favours smaller chunks with Marching Cubes.")]
        public int ChunkGridSize = 24;

        [Tooltip("Cells along the VERTICAL (Y) axis, PER CHUNK -- kept SEPARATE from ChunkGridSize on " +
                 "purpose. Must be tall enough to contain the full possible surface height (BaseHeight + " +
                 "HeightScale), or hills taller than this get holes where the column never crosses " +
                 "IsoLevel within the sampled grid.")]
        public int ChunkHeight = 48;

        public float IsoLevel = 0f;

        [Tooltip("Assign a material using the VoxelWorld/Experimental/TerrainVertexColor shader -- " +
                 "a normal Lit material ignores the per-vertex grass/rock colors and renders flat white.")]
        public Material ChunkMaterial;

        [Header("Terrain shape (throwaway)")]
        public float NoiseScale = 0.08f;
        public float HeightScale = 8f;
        public float BaseHeight = 16f;

        private void Start()
        {
            Regenerate();
        }

        [Button("Regenerate World", ButtonSizes.Large)]
        private void Regenerate()
        {
            ClearChunks();

            for (int chunkX = 0; chunkX < WorldSizeInChunks; chunkX++)
            {
                for (int chunkZ = 0; chunkZ < WorldSizeInChunks; chunkZ++)
                {
                    BuildChunk(chunkX, chunkZ);
                }
            }

            Debug.Log($"MarchingCubesWorld: built {transform.childCount} chunks " +
                      $"({WorldSizeInChunks * ChunkGridSize} x {WorldSizeInChunks * ChunkGridSize} world units).");
        }

        private void ClearChunks()
        {
            // Deliberately NOT tracking spawned chunks in a runtime List -- a List field gets reset
            // to empty on every domain reload (script recompile, or entering/exiting Play Mode),
            // while the actual child GameObjects it was tracking stay in the scene. That desync is
            // exactly what caused chunks to "keep the old material": ClearChunks() found nothing to
            // destroy (empty list after a reload), so old chunks -- built back when ChunkMaterial was
            // still unassigned -- just sat there while a second, correctly-materialed set got added
            // on top of them. Reading actual child transforms instead is reload-proof.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject chunk = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(chunk);
                else DestroyImmediate(chunk);
            }
        }

        private void BuildChunk(int chunkX, int chunkZ)
        {
            float[,,] density = BuildDensityField(chunkX, chunkZ);

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            MarchingCubesMesher.GenerateMesh(density, IsoLevel, vertices, triangles);

            var mesh = new Mesh
            {
                indexFormat = vertices.Count > 65000
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            TerrainVertexColorizer.ApplyTerrainColors(mesh, BaseHeight, BaseHeight + HeightScale);
            mesh.RecalculateBounds();

            var chunkObject = new GameObject($"MC_Chunk_{chunkX}_{chunkZ}");
            chunkObject.transform.SetParent(transform);

            // Mesh vertices are in LOCAL (0..ChunkGridSize) space (see BuildDensityField) --
            // position the GameObject in world space instead of baking the offset into every vertex.
            chunkObject.transform.localPosition = new Vector3(chunkX * ChunkGridSize, 0, chunkZ * ChunkGridSize);

            var meshFilter = chunkObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var meshRenderer = chunkObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = ChunkMaterial;
        }

        private float[,,] BuildDensityField(int chunkX, int chunkZ)
        {
            if (chunkX == 0 && chunkZ == 0 && BaseHeight + HeightScale > ChunkHeight)
            {
                Debug.LogWarning($"MarchingCubesWorld: BaseHeight + HeightScale ({BaseHeight + HeightScale}) " +
                                  $"exceeds ChunkHeight ({ChunkHeight}) -- the tallest hills will have holes " +
                                  "where the column never crosses IsoLevel within the sampled grid. Raise ChunkHeight to fix.");
            }

            int pointsXZ = ChunkGridSize + 1;
            int pointsY = ChunkHeight + 1;
            var density = new float[pointsXZ, pointsY, pointsXZ];

            // World-space origin of THIS chunk's grid -- the key to seamless tiling. Grid point
            // (0, _, 0) of chunk (1,0) must land on the exact same world coordinate as grid point
            // (ChunkGridSize, _, 0) of chunk (0,0) -- both compute the same density there, so the
            // two chunks' surfaces connect with no crack.
            int worldOriginX = chunkX * ChunkGridSize;
            int worldOriginZ = chunkZ * ChunkGridSize;

            for (int x = 0; x < pointsXZ; x++)
            {
                for (int z = 0; z < pointsXZ; z++)
                {
                    int worldX = worldOriginX + x;
                    int worldZ = worldOriginZ + z;

                    float surfaceHeight = BaseHeight + Mathf.PerlinNoise(worldX * NoiseScale, worldZ * NoiseScale) * HeightScale;

                    for (int y = 0; y < pointsY; y++)
                    {
                        density[x, y, z] = y - surfaceHeight;
                    }
                }
            }

            return density;
        }
    }
}