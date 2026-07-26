using UnityEngine;

namespace VoxelWorld
{
    [RequireComponent(typeof(MeshRenderer))]
    public class CubeWireframe : MonoBehaviour
    {
        [Header("Shader Properties")]
        [ColorUsage(true, true)] public Color FrameColor = Color.cyan;
        [Range(0.01f, 0.2f)] public float Thickness = 0.05f;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int ThicknessId = Shader.PropertyToID("_Thickness");

        private MeshRenderer _meshRenderer;
        private MaterialPropertyBlock _propertyBlock;

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _propertyBlock = new MaterialPropertyBlock();
            ApplyProperties();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_meshRenderer == null)
                _meshRenderer = GetComponent<MeshRenderer>();

            _propertyBlock ??= new MaterialPropertyBlock();
            ApplyProperties();
        }
#endif

        private void ApplyProperties()
        {
            _meshRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(ColorId, FrameColor);
            _propertyBlock.SetFloat(ThicknessId, Thickness);
            _meshRenderer.SetPropertyBlock(_propertyBlock);
        }

        public void Show(Vector3 worldPosition)
        {
            transform.position = worldPosition;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}