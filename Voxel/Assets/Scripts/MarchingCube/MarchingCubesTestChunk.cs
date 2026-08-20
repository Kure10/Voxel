using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace VoxelWorld.Experimental.MarchingCubes
{
    /// <summary>
    /// Standalone proof-of-concept for step 1: build a density field, run it through Marching
    /// Cubes, show the result. No chunk streaming, no pooling, no editing, no Job System —
    /// deliberately isolated from the rest of the game (different namespace, doesn't touch or
    /// reference any of the existing Chunk/ChunkData/WorldService code) so it's safe to test
    /// without risking the working cube-based game.
    ///
    /// Usage: create an empty GameObject in the scene, add this component, assign a material using
    /// the VoxelWorld/Experimental/TerrainVertexColor shader on the MeshRenderer (a normal Lit
    /// material ignores the per-vertex grass/rock colors this component generates), press Play (or
    /// just hit the Regenerate button below in the Inspector — it works in Edit Mode too, no need
    /// to enter Play mode to iterate on the noise settings).
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class MarchingCubesTestChunk : MonoBehaviour
    {
        [Header("Grid")]
        [Tooltip("Number of CELLS per horizontal (X/Z) axis. The density grid has (GridSize + 1) points per horizontal axis.")]
        public int GridSize = 32;

        [Tooltip("Number of CELLS along the vertical (Y) axis -- kept SEPARATE from GridSize on purpose. " +
                 "This must be tall enough to contain the full possible surface height (BaseHeight + " +
                 "HeightScale), or the terrain surface never crosses IsoLevel within the sampled column " +
                 "and you get holes/missing chunks of mesh wherever the hills are taller than this.")]
        public int Height = 48;

        [Tooltip("The density threshold that defines the surface. 0 works fine with the height-based " +
                 "density function below (density = worldY - surfaceHeight).")]
        public float IsoLevel = 0f;

        [Header("Terrain shape (throwaway — just enough to see a non-trivial surface)")]
        public float NoiseScale = 0.08f;
        public float HeightScale = 8f;
        public float BaseHeight = 16f;

        // Tracks the mesh we generated so Regenerate can clean up the previous one instead of
        // leaking it — each Mesh holds unmanaged (native) memory that plain C# GC won't reclaim.
        private Mesh _generatedMesh;

        private void Start()
        {
            Regenerate();
        }

        [Button("Regenerate", ButtonSizes.Large)]
        private void Regenerate()
        {
            float[,,] density = BuildDensityField();
            BuildMesh(density);
        }

        private float[,,] BuildDensityField()
        {
            if (BaseHeight + HeightScale > Height)
            {
                Debug.LogWarning($"MarchingCubesTestChunk: BaseHeight + HeightScale ({BaseHeight + HeightScale}) " +
                                  $"exceeds Height ({Height}) -- the tallest hills will have their tops clipped " +
                                  "off / missing (the column never crosses IsoLevel within the sampled grid). " +
                                  "Raise Height to fix.");
            }

            int pointsXZ = GridSize + 1;
            int pointsY = Height + 1;
            var density = new float[pointsXZ, pointsY, pointsXZ];

            for (int x = 0; x < pointsXZ; x++)
            {
                for (int z = 0; z < pointsXZ; z++)
                {
                    float surfaceHeight = BaseHeight + Mathf.PerlinNoise(x * NoiseScale, z * NoiseScale) * HeightScale;

                    for (int y = 0; y < pointsY; y++)
                    {
                        // Sign convention matters here, not just magnitude: MarchingCubesTables'
                        // TriangleTable was built assuming "density < isoLevel => inside/solid",
                        // and the winding order of its triangles (hence which way normals end up
                        // facing) depends on that. Below the surface must come out NEGATIVE:
                        density[x, y, z] = y - surfaceHeight;
                    }
                }
            }

            return density;
        }

        private void BuildMesh(float[,,] density)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            MarchingCubesMesher.GenerateMesh(density, IsoLevel, vertices, triangles);

            // Clean up the previous mesh before building a new one — otherwise every click of
            // Regenerate leaks a Mesh's native GPU-side buffers.
            if (_generatedMesh != null)
            {
                if (Application.isPlaying)
                    Destroy(_generatedMesh);
                else
                    DestroyImmediate(_generatedMesh);
            }

            _generatedMesh = new Mesh
            {
                indexFormat = vertices.Count > 65000
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16
            };

            _generatedMesh.SetVertices(vertices);
            _generatedMesh.SetTriangles(triangles, 0);
            _generatedMesh.RecalculateNormals();
            TerrainVertexColorizer.ApplyTerrainColors(_generatedMesh, BaseHeight, BaseHeight + HeightScale);
            _generatedMesh.RecalculateBounds();

            // sharedMesh, not mesh — .mesh auto-instantiates a copy the moment you touch it in
            // Edit Mode, which Unity itself warns leaks meshes. sharedMesh is the correct choice
            // here since we already own a private, freshly-created Mesh instance ourselves.
            GetComponent<MeshFilter>().sharedMesh = _generatedMesh;

            Debug.Log($"MarchingCubesTestChunk: {vertices.Count} vertices, {triangles.Count / 3} triangles.");
        }
    }
}