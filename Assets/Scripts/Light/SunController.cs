using UnityEngine;

namespace OuterWitness.Light
{
    /// <summary>
    /// 太阳视觉表现控制器。
    /// 负责管理太阳的自发光材质（Emission）和颜色。
    /// </summary>
    [ExecuteAlways]
    public class SunController : MonoBehaviour
    {
        [Header("Visual Settings")]
        [ColorUsage(true, true)] // 启用 HDR 颜色选择器
        [SerializeField] private Color _sunColor = Color.white;
        [SerializeField] private float _emissionIntensity = 5f;

        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private void OnEnable()
        {
            _renderer = GetComponent<Renderer>();
            _propBlock = new MaterialPropertyBlock();
            UpdateVisuals();
        }

        private void Update()
        {
            // 如果是在编辑器模式下，实时更新以便预览
            if (!Application.isPlaying) UpdateVisuals();
        }

        [ContextMenu("Update Visuals")]
        public void UpdateVisuals()
        {
            if (_renderer == null) return;

            // HDR 最终颜色 = 颜色 * 2^强度
            Color finalColor = _sunColor * Mathf.Pow(2, _emissionIntensity);
            
            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(EmissionColor, finalColor);
            _renderer.SetPropertyBlock(_propBlock);
            
            // 确保材质启用自发光关键字
            if (_renderer.sharedMaterial != null)
            {
                _renderer.sharedMaterial.EnableKeyword("_EMISSION");
                _renderer.sharedMaterial.SetColor(EmissionColor, finalColor);
            }
        }
    }
}
