using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelWorld
{
    [CreateAssetMenu(fileName = "Block Data", menuName = "Data/Block Data")]
    public class BlockDataSO : ScriptableObject
    {
        public float TextureSizeX, TextureSizeY;
        public List<TextureData> TextureDataList;
    }

    [Serializable]
    public class TextureData
    {
        public BlockType BlockType;
        public Vector2Int Up, Down, Side;
        public bool IsSolid = true;
        public bool GeneratesCollider = true;
    }
}