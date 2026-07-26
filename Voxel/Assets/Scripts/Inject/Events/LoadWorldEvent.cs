using After.Main;

namespace VoxelWorld
{
    public class LoadWorldEvent : AbstractEvent
    {
        public int Seed { get; }

        public LoadWorldEvent(int seed)
        {
            Seed = seed;
        }
    }
}