using UnityEngine;
using VoxelWorld;

namespace After.Main
{
    [DefaultExecutionOrder(-10)]
    public class GameInitializer : BaseInitializer
    {
        public BlockDataManager BlockDataManager;
        public World World;

        protected override void Initialize(GameContext gameContext)
        {
            base.Initialize(gameContext);
            _injector.TryMapManager(BlockDataManager);
        }

        protected override void Awake()
        {
            base.Awake(); // maps BlockDataManager, then injects + Initialize()s every Controller (including World)
            World.GenerateWorld();
        }
    }
}