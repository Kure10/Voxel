using After.Main;
using UnityEngine;

namespace VoxelWorld
{
    public class HotbarSelectionChangedEvent : AbstractEvent
    {
        public BlockType SelectedBlockType { get; }

        public HotbarSelectionChangedEvent(BlockType selectedBlockType)
        {
            SelectedBlockType = selectedBlockType;
        }
    }
}