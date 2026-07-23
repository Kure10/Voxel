
using After.Main;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-10)]
public abstract class BaseInitializer : MonoBehaviour
{
    protected Injector _injector;
    protected GameContext _gameContext;

    protected virtual void Awake()
    {
        Initialize(GameContext.Instance);
        InitializeControllers();
    }

    protected virtual void Initialize(GameContext gameContext)
    {
        _injector = Injector.Instance;
        _gameContext = gameContext;
        _injector.InjectInto(this);
    }

    protected void InitializeControllers()
    {
        var controllers = new List<Controller>();

        foreach (GameObject rootObject in gameObject.scene.GetRootGameObjects())
            controllers.AddRange(rootObject.GetComponentsInChildren<Controller>(true));

        foreach (Controller controller in controllers)
            _injector.InjectInto(controller);

        foreach (Controller controller in controllers)
            controller.Initialize();
    }

    public virtual bool IsSceneReady()
    {
        return true;
    }
}
