using UnityEngine;

namespace VoxelWorld.UI
{
    public class LoadingSpinner : MonoBehaviour
    {
        [Header("Rotation")]
        public float RotationSpeed = 180f; // degrees per second
        public bool Clockwise = true;

        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void Update()
        {
            float direction = Clockwise ? -1f : 1f;
            _rectTransform.Rotate(0f, 0f, direction * RotationSpeed * Time.deltaTime);
        }
    }
}