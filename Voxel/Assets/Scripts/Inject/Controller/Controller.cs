using UnityEngine;

namespace After.Main
{
    public class Controller : MonoBehaviour, IInjectable
    {
        [Inject] protected Injector _injector;

        // IInjectable:
        public bool Injected { get; set; }

        protected bool _initialized;

        private bool _destroyed;

        [Header("ControllerContainer:")]

        [SerializeField]
        protected string _hierarchicalName;

        public string HierarchicalName
        {
            get => _hierarchicalName;
            set => _hierarchicalName = value;
        }


        [SerializeField]
        protected Controller _parent;

        public Controller Parent
        {
            get => _parent;
            set
            {
                _parent = value;
            }
        }

        protected virtual void Awake()
        {

        }

        public virtual void Initialize()
        {
            if (_initialized)
            {
                Debug.LogError("Initialize already called on controller: " + name);
                return;
            }

            _initialized = true;
        }

        /// <summary>
        /// A method called to refresh the controller. Implement in case the controller needs some refresh functionality.
        /// </summary>
        public virtual void Refresh()
        {

        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
        }

        // public virtual AnimationNode ShowAnimation()
        // {
        //     return new CallbackNode(() => gameObject.SetActive(true));
        // }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Called when the controller is being destroyed.
        /// </summary>
        public virtual void Destroy()
        {
            if (_destroyed)
            {
                return;
            }

            _destroyed = true;

            // Call OnControllerDestroy
            OnControllerDestroy();
        }

        protected void OnDestroy()
        {
            Destroy();
        }

        /// <summary>
        /// Called everytime the controller is destroyed, even if it wasn't active before.
        /// </summary>
        protected virtual void OnControllerDestroy()
        {
           // KillAllTweens(transform);
        }

        // // TODO(major): Find a better way how to do this or don't do this at all (can go though a big tree)
        // private void KillAllTweens(Transform transformToKill)
        // {
        //     DOTween.Kill(transformToKill);
        //     foreach (Transform child in transformToKill)
        //     {
        //         KillAllTweens(child);
        //     }
        // }

        public void SetControllerParent(Controller parent)
        {
            // Initialize controller
            _parent = parent;
            _hierarchicalName = parent.HierarchicalName + "/" + name;
        }
    }
}
