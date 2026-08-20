using System.Collections.Generic;
using UnityEngine;

namespace VoxelWorld.Experimental.MarchingCubes
{
    /// <summary>
    /// Plain C# Marching Cubes mesher — deliberately NOT a Burst job yet. The goal of this first
    /// step is to understand/verify the algorithm itself; jobify it later once it's producing
    /// correct meshes (same order we did things for the cube world: correctness first).
    ///
    /// Vertices ARE welded/shared between triangles now (see <see cref="GetOrAddVertex"/>) — two
    /// cells that compute an edge vertex at (numerically) the same position reuse the same vertex
    /// index instead of duplicating it. That's what makes Mesh.RecalculateNormals() produce SMOOTH
    /// shading: a shared vertex's normal is the average of every triangle that touches it, instead
    /// of each triangle keeping its own flat-facing normal.
    /// </summary>
    public static class MarchingCubesMesher
    {
        // Positions are quantized to 1/1000 of a unit before being used as a dictionary key.
        // Two cells computing "the same" edge vertex can land a few ULPs apart (interpolation runs
        // in a different order/direction depending on which cell reaches that edge first), so exact
        // float equality would fail to weld them — rounding first fixes that without visibly moving
        // any vertex.
        private const float WeldPrecision = 1000f;

        public static void GenerateMesh(float[,,] density, float isoLevel, List<Vector3> vertices, List<int> triangles)
        {
            // density has (points) entries per axis -> (points - 1) CELLS per axis.
            int cellsX = density.GetLength(0) - 1;
            int cellsY = density.GetLength(1) - 1;
            int cellsZ = density.GetLength(2) - 1;

            var cornerDensities = new float[8];
            var edgeVertices = new Vector3[12];
            var vertexLookup = new Dictionary<Vector3Int, int>();

            for (int x = 0; x < cellsX; x++)
            {
                for (int y = 0; y < cellsY; y++)
                {
                    for (int z = 0; z < cellsZ; z++)
                    {
                        MarchCell(density, x, y, z, isoLevel, cornerDensities, edgeVertices, vertices, triangles, vertexLookup);
                    }
                }
            }
        }

        private static void MarchCell(
            float[,,] density, int x, int y, int z, float isoLevel,
            float[] cornerDensities, Vector3[] edgeVertices,
            List<Vector3> vertices, List<int> triangles, Dictionary<Vector3Int, int> vertexLookup)
        {
            // 1. Read the density at this cell's 8 corners.
            for (int i = 0; i < 8; i++)
            {
                int cx = x + MarchingCubesTables.CornerOffsets[i, 0];
                int cy = y + MarchingCubesTables.CornerOffsets[i, 1];
                int cz = z + MarchingCubesTables.CornerOffsets[i, 2];
                cornerDensities[i] = density[cx, cy, cz];
            }

            // 2. Pack which corners are "inside" the surface (density < isoLevel) into an 8-bit index.
            //    This 0-255 value is the lookup key into EdgeTable/TriangleTable.
            int cubeIndex = 0;
            for (int i = 0; i < 8; i++)
            {
                if (cornerDensities[i] < isoLevel)
                    cubeIndex |= 1 << i;
            }

            int edgeMask = MarchingCubesTables.EdgeTable[cubeIndex];
            if (edgeMask == 0)
                return; // cell is fully inside or fully outside -- surface doesn't pass through it

            // 3. For every edge the surface crosses, find WHERE along that edge (linear
            //    interpolation between the two corner densities, not just the edge midpoint --
            //    this is what makes the result smooth instead of blocky).
            for (int edge = 0; edge < 12; edge++)
            {
                if ((edgeMask & (1 << edge)) == 0)
                    continue;

                int cornerA = MarchingCubesTables.EdgeConnections[edge, 0];
                int cornerB = MarchingCubesTables.EdgeConnections[edge, 1];

                Vector3 posA = new Vector3(
                    x + MarchingCubesTables.CornerOffsets[cornerA, 0],
                    y + MarchingCubesTables.CornerOffsets[cornerA, 1],
                    z + MarchingCubesTables.CornerOffsets[cornerA, 2]);

                Vector3 posB = new Vector3(
                    x + MarchingCubesTables.CornerOffsets[cornerB, 0],
                    y + MarchingCubesTables.CornerOffsets[cornerB, 1],
                    z + MarchingCubesTables.CornerOffsets[cornerB, 2]);

                edgeVertices[edge] = InterpolateEdge(posA, cornerDensities[cornerA], posB, cornerDensities[cornerB], isoLevel);
            }

            // 4. TriangleTable tells us, for this cubeIndex, which edges to connect into triangles
            //    (up to 5 triangles per cell), terminated by -1.
            for (int i = 0; MarchingCubesTables.TriangleTable[cubeIndex, i] != -1; i += 3)
            {
                Vector3 v0 = edgeVertices[MarchingCubesTables.TriangleTable[cubeIndex, i]];
                Vector3 v1 = edgeVertices[MarchingCubesTables.TriangleTable[cubeIndex, i + 1]];
                Vector3 v2 = edgeVertices[MarchingCubesTables.TriangleTable[cubeIndex, i + 2]];

                // Index order is v0, v2, v1 -- NOT the v0,v1,v2 order TriangleTable lists them in.
                // MarchingCubesTables was transcribed straight from Paul Bourke's reference page,
                // which (like most Marching Cubes write-ups) assumes an OpenGL-style right-handed
                // space with counter-clockwise front faces. Unity is left-handed with CLOCKWISE
                // front faces, so using the table's vertex order as-is builds every triangle facing
                // exactly backwards -- the whole mesh renders "inside out". With Cull Back (the
                // default, and what our custom shader uses) that makes the terrain invisible from
                // above and visible only from underneath/inside, which is exactly what you saw.
                // Swapping any two of the three indices reverses the winding without changing the
                // triangle's shape or position -- that's the fix.
                triangles.Add(GetOrAddVertex(v0, vertices, vertexLookup));
                triangles.Add(GetOrAddVertex(v2, vertices, vertexLookup));
                triangles.Add(GetOrAddVertex(v1, vertices, vertexLookup));
            }
        }

        private static int GetOrAddVertex(Vector3 position, List<Vector3> vertices, Dictionary<Vector3Int, int> vertexLookup)
        {
            var key = new Vector3Int(
                Mathf.RoundToInt(position.x * WeldPrecision),
                Mathf.RoundToInt(position.y * WeldPrecision),
                Mathf.RoundToInt(position.z * WeldPrecision));

            if (vertexLookup.TryGetValue(key, out int existingIndex))
                return existingIndex;

            int newIndex = vertices.Count;
            vertices.Add(position);
            vertexLookup[key] = newIndex;
            return newIndex;
        }

        private static Vector3 InterpolateEdge(Vector3 posA, float densityA, Vector3 posB, float densityB, float isoLevel)
        {
            const float epsilon = 0.00001f;

            if (Mathf.Abs(isoLevel - densityA) < epsilon) return posA;
            if (Mathf.Abs(isoLevel - densityB) < epsilon) return posB;
            if (Mathf.Abs(densityA - densityB) < epsilon) return posA;

            float t = (isoLevel - densityA) / (densityB - densityA);
            return posA + t * (posB - posA);
        }
    }
}