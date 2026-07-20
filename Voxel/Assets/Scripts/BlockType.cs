namespace VoxelWorld
{
    /// <summary>
    /// The set of block types that can exist in the world.
    /// Air represents an empty cell (no mesh, no collision).
    /// </summary>
    public enum BlockType : byte
    {
        Air = 0,
        Gray = 1,   // deep layer - rock, slowest to mine
        Green = 2,  // walkable layer - grass, normal mine speed
        White = 3   // high layer - snow, fastest to mine
    }
}
