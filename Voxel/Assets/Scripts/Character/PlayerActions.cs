using After.Main;
using UnityEngine;
using VoxelWorld;

namespace Character
{
    public class PlayerActions : Controller
    {
        [Inject] private WorldRules _worldRules;
        [Inject] private WorldService _worldService;
        [Inject] private CoreGameInputsSystem _coreGameInputsSystem;

        [Header("References")]
        public Camera PlayerCamera;

        [Header("Interaction")]
        public float InteractionRange = 6f;
        public BlockType BlockToPlace = BlockType.Gray;

        private Vector3Int? _targetBlockPos;
        private int _currentHits;

        public override void Initialize()
        {
            base.Initialize();
            _coreGameInputsSystem.OnDigPerformed += HandleDigPerformed;
            _coreGameInputsSystem.OnLeftMousePerformed += HandleDigPerformed;
            _coreGameInputsSystem.OnRightMousePerformed += HandleBuildPerformed;
        }
        
        private void HandleBuildPerformed()
        {
            if (!TryRaycastCenterScreen(out RaycastHit hit))
                return;

            Vector3Int placePos = Vector3Int.RoundToInt(hit.point + hit.normal * 0.5f);
            // in HandleBuildPerformed:
            if (placePos.y >= _worldRules.MaxBuildHeight)
                return;
            
            _worldService.TrySetBlockAtWorldPosition(placePos, BlockToPlace);
        }
        
        private void HandleDigPerformed()
        {
            if (!TryRaycastCenterScreen(out RaycastHit hit))
                return;

            Vector3Int blockPos = Vector3Int.RoundToInt(hit.point - hit.normal * 0.5f);
            // in HandleDigPerformed:
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

        private bool TryRaycastCenterScreen(out RaycastHit hit)
        {
            Ray ray = PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            return Physics.Raycast(ray, out hit, InteractionRange);
        }

        protected override void OnControllerDestroy()
        {
            base.OnControllerDestroy();
            if (_coreGameInputsSystem == null)
                return;
            
            _coreGameInputsSystem.OnDigPerformed -= HandleDigPerformed;
            _coreGameInputsSystem.OnLeftMousePerformed -= HandleDigPerformed;
            _coreGameInputsSystem.OnRightMousePerformed -= HandleBuildPerformed;
        }
    }
}