using Cysharp.Threading.Tasks;
using UnityEngine;
using VoxelWorld;

namespace After.Main
{
    [DefaultExecutionOrder(-10)]
    public class GameInitializer : BaseInitializer
    {
        public BlockDataManager BlockDataManager;
        public World World;
        public TerrainGenerator TerrainGenerator;
        
        protected override void Initialize(GameContext gameContext)
        {
            base.Initialize(gameContext);
            _injector.TryMapManager(BlockDataManager);

            var worldService = _injector.MapOrGetSingleton<VoxelWorld.WorldService>();
            worldService.Configure(TerrainGenerator);

            _injector.TryMapService(new VoxelWorld.SaveService());
        }

        protected override void Awake()
        {
            base.Awake(); // maps BlockDataManager, then injects + Initialize()s every Controller (including World)
            World.GenerateWorld().Forget();
        }
    }
}