using After.Main;
using UnityEngine;
using VoxelWorld;

namespace Character
{
    public class PlayerActions : Controller
    {
        [Inject] private CoreGameInputsSystem _coreGameInputsSystem;
        [Inject] private WorldService _worldService;
        [Inject] private WorldRules _worldRules;
        [Inject] private MyEventManager _eventManager;

        [Header("References")]
        public Camera PlayerCamera;

        [Header("Interaction")]
        public float InteractionRange = 6f;
        
        [Header("Placement Preview")]
        public GameObject PlacementPreviewPrefab;

        private GameObject _placementPreviewInstance;
        
        // matches HotbarController's default slot 0 until the first event arrives
        private BlockType _selectedBlockType = BlockType.Gray; 

        public override void Initialize()
        {
            base.Initialize();

            _coreGameInputsSystem.OnDigPerformed += HandleDigPerformed;
            _coreGameInputsSystem.OnLeftMousePerformed += HandleDigPerformed;
            _coreGameInputsSystem.OnRightMousePerformed += HandleBuildPerformed;
            _eventManager.AddListener<HotbarSelectionChangedEvent>(HandleHotbarSelectionChanged);
            
            _placementPreviewInstance = Instantiate(PlacementPreviewPrefab);
            _placementPreviewInstance.SetActive(false);
        }

        private void Update()
        {
            UpdatePlacementPreview();
        }

        private void UpdatePlacementPreview()
        {
            if (!TryRaycastCenterScreen(out RaycastHit hit))
            {
                _placementPreviewInstance.SetActive(false);
                return;
            }

            Vector3Int placePos = Vector3Int.RoundToInt(hit.point + hit.normal * 0.5f);

            if (placePos.y >= _worldRules.MaxBuildHeight)
            {
                _placementPreviewInstance.SetActive(false);
                return;
            }

            _placementPreviewInstance.transform.position = placePos;
            _placementPreviewInstance.SetActive(true);
        }
        
        private void HandleHotbarSelectionChanged(HotbarSelectionChangedEvent e)
        {
            _selectedBlockType = e.SelectedBlockType;
        }

        private void HandleDigPerformed()
        {
            if (!TryRaycastCenterScreen(out RaycastHit hit))
                return;

            Vector3Int blockPos = Vector3Int.RoundToInt(hit.point - hit.normal * 0.5f);

            if (blockPos.y <= _worldRules.MinDigHeight)
                return;

            BlockType blockType = _worldService.GetBlockAtWorldPosition(blockPos);

            if (blockType == BlockType.Nothing || blockType == BlockType.Air)
                return;

            int currentHits = _worldService.AddBlockDamage(blockPos);
            int hitsToBreak = BlockDataManager.BlockTextureDataDictionary[blockType].HitsToBreak;

            if (currentHits >= hitsToBreak)
            {
                _worldService.TrySetBlockAtWorldPosition(blockPos, BlockType.Air);
                _worldService.ClearBlockDamage(blockPos);
            }
        }

        private void HandleBuildPerformed()
        {
            if (!TryRaycastCenterScreen(out RaycastHit hit))
                return;

            Vector3Int placePos = Vector3Int.RoundToInt(hit.point + hit.normal * 0.5f);

            if (placePos.y >= _worldRules.MaxBuildHeight)
                return;

            // TODO: currently overwrites whatever block already occupies placePos (including solid
            // blocks, not just Air). Consider checking GetBlockAtWorldPosition == Air before placing.
            _worldService.TrySetBlockAtWorldPosition(placePos, _selectedBlockType);
        }

        private bool TryRaycastCenterScreen(out RaycastHit hit)
        {
            Ray ray = PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            return Physics.Raycast(ray, out hit, InteractionRange);
        }

        protected override void OnControllerDestroy()
        {
            base.OnControllerDestroy();

            _eventManager.RemoveListener<HotbarSelectionChangedEvent>(HandleHotbarSelectionChanged);
            
            if (_placementPreviewInstance != null)
                Destroy(_placementPreviewInstance);

            if (_coreGameInputsSystem == null) return;
            _coreGameInputsSystem.OnDigPerformed -= HandleDigPerformed;
            _coreGameInputsSystem.OnLeftMousePerformed -= HandleDigPerformed;
            _coreGameInputsSystem.OnRightMousePerformed -= HandleBuildPerformed;
        }
    }
}