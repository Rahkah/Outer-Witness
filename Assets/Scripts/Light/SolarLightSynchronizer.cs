using UnityEngine;

namespace OuterWitness.Light
{
    /// <summary>
    /// 星系光照同步器。
    /// 核心逻辑：确保 Directional Light 的旋转方向始终是从太阳射向参考点（如玩家或星球）。
    /// </summary>
    [ExecuteAlways]
    public class SolarLightSynchronizer : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("场景中的太阳物体")]
        [SerializeField] private Transform _sunTransform;
        
        [Tooltip("场景中的主方向光")]
        [SerializeField] private UnityEngine.Light _directionalLight;

        [Tooltip("参考参考物体（通常是玩家）。光照方向会根据此物体与太阳的相对位置动态更新。")]
        [SerializeField] private Transform _referencePoint;

        [Header("Settings")]
        [Tooltip("是否在 LateUpdate 中更新。如果是基于物理移动，LateUpdate 能减少阴影抖动。")]
        [SerializeField] private bool _useLateUpdate = true;

        private void LateUpdate()
        {
            if (_useLateUpdate) SyncLight();
        }

        private void Update()
        {
            if (!_useLateUpdate || !Application.isPlaying) SyncLight();
        }

        public void SyncLight()
        {
            if (_sunTransform == null || _directionalLight == null) return;

            // 1. 获取参考位置（玩家、星球或坐标原点）
            Vector3 refPos = _referencePoint != null ? _referencePoint.position : Vector3.zero;

            // 2. 计算光照行进方向：从太阳中心射向观察者
            Vector3 lightDirection = (refPos - _sunTransform.position).normalized;

            // 3. 将方向应用到 Directional Light 的旋转上
            if (lightDirection.sqrMagnitude > 0.01f)
            {
                _directionalLight.transform.forward = lightDirection;
            }
        }

        /// <summary>
        /// 预留接口：切换参考点（例如当玩家降落到不同星球时）。
        /// </summary>
        public void SetReferencePoint(Transform newPoint)
        {
            _referencePoint = newPoint;
        }
    }
}
