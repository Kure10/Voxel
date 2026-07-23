using UnityEngine;

namespace After.Main
{
    [DefaultExecutionOrder(-100)]
    public class GameContext : MonoBehaviour
    {
        public static GameContext Instance { get; private set; }

        //  [Header("Global settings:")]

        //[SerializeField] private SoundSettings _soundSettings;

        [Header("Managers:")]
        public MyEventManager MyEventManager;
        public CoreGameInputsSystem InputsSystem;

        private void Awake()
        {
            Injector injector = Injector.Instance;

            // ! Try to map current world
            //  Because if we load world in one scene, then mapping would still work with the old world
            
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;

            DontDestroyOnLoad(gameObject);

            // Turn off v-sync
            QualitySettings.vSyncCount = 0;

            // Disable screen dimming
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            injector.MapAndInjectInto(injector);
            // injector.MapValue<ContextSettings>(_contextSettings);
            
            InputsSystem = injector.TryMapManager(InputsSystem);
            MyEventManager = injector.TryMapManager(MyEventManager);


            injector.TryMapService(new PlayerService());

            //
            // injector.TryMapService(new EconomyService());
            // injector.TryMapService(new SpecialistService());
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            Injector.Instance.DestroyAllServices();
            Instance = null;
        }
    }
}
