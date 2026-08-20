using UnityEngine;

namespace VoxelWorld.Experimental.MarchingCubes
{
    /// <summary>
    /// Colors a Marching Cubes mesh per-vertex, primarily by HEIGHT: brown at the bottom, blending
    /// up through light green into dark green at the top -- the smooth-terrain equivalent of the
    /// cube world's height-band coloring (Gray/Green/White in <c>WorldRules</c>), just continuous
    /// instead of discrete bands. Steep faces (cliffs/overhangs) additionally blend towards rock
    /// regardless of height, so cliff walls don't look like green putty.
    ///
    /// Must run AFTER Mesh.RecalculateNormals() — it reads the mesh's own (already-smoothed, thanks
    /// to <see cref="MarchingCubesMesher"/> welding shared vertices) normals to decide the rock blend.
    ///
    /// Requires a material using the VoxelWorld/Experimental/TerrainVertexColor shader — a normal
    /// URP Lit material ignores vertex colors entirely and will render flat white/gray.
    /// </summary>
    public static class TerrainVertexColorizer
    {
        private static readonly Color BrownColor = new Color(0.36f, 0.26f, 0.15f);
        private static readonly Color LightGreenColor = new Color(0.47f, 0.66f, 0.26f);
        private static readonly Color DarkGreenColor = new Color(0.11f, 0.30f, 0.12f);
        private static readonly Color RockColor = new Color(0.42f, 0.40f, 0.36f);

        /// <param name="heightMin">World/local Y that should map to pure brown (the lowest terrain can go).</param>
        /// <param name="heightMax">World/local Y that should map to pure dark green (the highest terrain can go).</param>
        /// <param name="flatSlope">Slope (0 = flat, 1 = vertical) below which a vertex ignores rock entirely.</param>
        /// <param name="steepSlope">Slope above which a vertex is pure rock, regardless of height.</param>
        public static void ApplyTerrainColors(Mesh mesh, float heightMin, float heightMax, float flatSlope = 0.3f, float steepSlope = 0.65f)
        {
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            var colors = new Color[vertices.Length];

            // heightMin/heightMax are passed in by the caller (BaseHeight / BaseHeight + HeightScale)
            // rather than auto-detected from this mesh's own min/max Y. That matters for
            // MarchingCubesWorld specifically: every chunk shares the same BaseHeight/HeightScale, so
            // every chunk maps a given world height to the exact same color. Auto-detecting per-mesh
            // would normalize each chunk against its OWN local height range instead, and two
            // neighbouring chunks with different local terrain variance would shade the same height
            // differently -- a visible color seam at the chunk border, on top of an otherwise
            // seamless geometry border.
            bool hasRange = heightMax > heightMin;

            for (int i = 0; i < vertices.Length; i++)
            {
                float heightT = hasRange ? Mathf.InverseLerp(heightMin, heightMax, vertices[i].y) : 0f;

                Color heightColor = heightT < 0.5f
                    ? Color.Lerp(BrownColor, LightGreenColor, heightT / 0.5f)
                    : Color.Lerp(LightGreenColor, DarkGreenColor, (heightT - 0.5f) / 0.5f);

                // normal.y close to 1 => facing straight up => flat ground, no rock. Close to 0 (or
                // negative, for overhangs) => steep/vertical => rock face, regardless of height.
                float slope = 1f - Mathf.Clamp01(normals[i].y);
                float rockBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(flatSlope, steepSlope, slope));

                colors[i] = Color.Lerp(heightColor, RockColor, rockBlend);
            }

            mesh.colors = colors;
        }
    }
}