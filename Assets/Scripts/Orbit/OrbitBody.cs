using UnityEngine;

namespace OuterWitness.Orbit
{
    /// <summary>
    /// 轨道数据组件。不包含任何逻辑，仅存储轨道定义。
    /// </summary>
    [DisallowMultipleComponent]
    public class OrbitBody : MonoBehaviour
    {
        [Header("Hierarchy")]
        [Tooltip("围绕旋转的中心天体。若为空，则该物体为星系中心（如恒星）。")]
        public Transform orbitCenter;

        [Header("Shape (Elliptic Parameters)")]
        [Tooltip("轨道长半轴 (a): 决定轨道在 X 方向的最大跨度。")]
        public float semiMajorAxis = 10f;

        [Range(0f, 0.99f)]
        [Tooltip("偏心率 (e): 0 为正圆，接近 1 为极扁的椭圆。\n公式: b = a * sqrt(1 - e^2)")]
        public float eccentricity = 0f;
        
        /// <summary>
        /// 自动计算的短半轴 (b)。
        /// </summary>
        public float SemiMinorAxis 
        {
            get 
            {
                // 实时计算 b 以确保数据一致性
                float b = semiMajorAxis * Mathf.Sqrt(1f - eccentricity * eccentricity);
                return Mathf.Max(0.1f, b);
            }
        }

        [Header("Orientation")]
        [Tooltip("轨道的倾斜方向（轴向）。默认 Vector3.up 表示在 XZ 平面运动。")]
        public Vector3 orbitNormal = Vector3.up;

        [Header("Motion")]
        [Tooltip("角速度 (弧度/秒)")]
        public float angularSpeed = 1f;
        [Tooltip("初始相位 (弧度)")]
        public float initialPhase = 0f;

        // --- 向后兼容与旧数据迁移 ---
        [HideInInspector] [SerializeField] private float semiMinorAxis = -1f; 

        // 内部缓存用于 OrbitSystem 排序
        [HideInInspector] public int dependencyDepth = 0;

        private void OnDrawGizmos()
        {
            // 仅当指定了轨道中心时绘制预览
            if (orbitCenter == null) return;

            DrawOrbitGizmo();
        }

        private void DrawOrbitGizmo()
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.4f); // 半透明黄色
            
            Vector3 centerPos = orbitCenter.position;
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, orbitNormal.normalized);
            float b = SemiMinorAxis;

            int segments = 64; // 采样点数量
            Vector3 lastPoint = Vector3.zero;

            for (int i = 0; i <= segments; i++)
            {
                // 计算当前采样点的局部坐标
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                float x = semiMajorAxis * Mathf.Cos(angle);
                float z = b * Mathf.Sin(angle);
                
                // 应用旋转和中心点偏移
                Vector3 localPos = new Vector3(x, 0, z);
                Vector3 worldPos = centerPos + (rotation * localPos);

                if (i > 0)
                {
                    Gizmos.DrawLine(lastPoint, worldPos);
                }
                lastPoint = worldPos;
            }

            // 绘制一条指向中心点的虚线辅助线（可选，当前使用实线表示关联）
            Gizmos.color = new Color(1f, 1f, 1f, 0.2f);
            Gizmos.DrawLine(transform.position, centerPos);
        }

        private void OnEnable()
        {
            if (OrbitSystem.Instance != null)
                OrbitSystem.Instance.RegisterBody(this);
        }

        private void OnDisable()
        {
            if (OrbitSystem.Instance != null)
                OrbitSystem.Instance.UnregisterBody(this);
        }

        private void OnValidate()
        {
            // 1. 处理旧数据迁移 (仅在检测到旧字段有值时执行一次)
            if (semiMinorAxis > 0)
            {
                float ratio = semiMinorAxis / semiMajorAxis;
                eccentricity = Mathf.Sqrt(Mathf.Abs(1f - ratio * ratio));
                semiMinorAxis = -1f; // 标记迁移完成，不再触发
                Debug.Log($"[OrbitBody] 已自动将旧的短半轴数据转换为偏心率: {eccentricity:F4}", gameObject);
            }

            // 2. 强制约束 a > 0
            semiMajorAxis = Mathf.Max(0.1f, semiMajorAxis);

            // 3. 强制约束 0 <= e < 1
            eccentricity = Mathf.Clamp(eccentricity, 0f, 0.99f);
            
            if (Application.isPlaying && OrbitSystem.Instance != null)
                OrbitSystem.Instance.MarkDirty();
        }
    }
}
