using After.Main;
using UnityEngine;

namespace Character
{
    public class PlayerActions : Controller
    {
        [Inject] private CoreGameInputsSystem _coreGameInputsSystem;

        public override void Initialize()
        {
            base.Initialize();

            _coreGameInputsSystem.OnMineStarted += HandleMineStarted;
            _coreGameInputsSystem.OnMineCanceled += HandleMineCanceled;
            _coreGameInputsSystem.OnBuildPerformed += HandleBuildPerformed;
        }

        private void HandleMineStarted()
        {
            
        }

        private void HandleMineCanceled()
        {
            
        }

        private void HandleBuildPerformed()
        {
        }

        protected override void OnControllerDestroy()
        {
            base.OnControllerDestroy();
            if (_coreGameInputsSystem == null) return;
            _coreGameInputsSystem.OnMineStarted -= HandleMineStarted;
            _coreGameInputsSystem.OnMineCanceled -= HandleMineCanceled;
            _coreGameInputsSystem.OnBuildPerformed -= HandleBuildPerformed;
        }
    }
}