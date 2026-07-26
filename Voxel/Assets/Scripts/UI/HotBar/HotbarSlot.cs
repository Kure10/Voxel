using UnityEngine;

namespace VoxelWorld.UI
{
    public class HotbarSlot : MonoBehaviour
    {
        public BlockType BlockType;
        public GameObject SelectionIndicator;

        public void SetSelected(bool selected)
        {
            SelectionIndicator.SetActive(selected);
        }
    }
}