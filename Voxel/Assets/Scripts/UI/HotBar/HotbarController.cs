using After.Main;
using UnityEngine;
using UnityEngine.UI;

namespace VoxelWorld.UI
{
    public class HotbarController : Controller
    {
        [Inject] private CoreGameInputsSystem _coreGameInputsSystem;
        [Inject] private MyEventManager _eventManager;

        public HotbarSlot[] Slots;
        
        private int _selectedIndex;

        public override void Initialize()
        {
            base.Initialize();

            _coreGameInputsSystem.OnHotbarPrevious += SelectPrevious;
            _coreGameInputsSystem.OnHotbarNext += SelectNext;
            
            UpdateSelectionVisuals();
            _eventManager.DispatchEvent(new HotbarSelectionChangedEvent(Slots[_selectedIndex].BlockType));
        }

        private void SelectPrevious()
        {
            if (_selectedIndex <= 0)
                return;

            _selectedIndex--;
            UpdateSelectionVisuals();
            _eventManager.DispatchEvent(new HotbarSelectionChangedEvent(Slots[_selectedIndex].BlockType));
        }

        private void SelectNext()
        {
            if (_selectedIndex >= Slots.Length - 1)
                return;

            _selectedIndex++;
            UpdateSelectionVisuals();
            _eventManager.DispatchEvent(new HotbarSelectionChangedEvent(Slots[_selectedIndex].BlockType));
        }

        private void UpdateSelectionVisuals()
        {
            for (int i = 0; i < Slots.Length; i++)
                Slots[i].SetSelected(i == _selectedIndex);
        }

        protected override void OnControllerDestroy()
        {
            base.OnControllerDestroy();

            if (_coreGameInputsSystem != null)
            {
                _coreGameInputsSystem.OnHotbarPrevious -= SelectPrevious;
                _coreGameInputsSystem.OnHotbarNext -= SelectNext;
            }
        }
    }
}