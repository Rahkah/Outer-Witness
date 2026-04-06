using System.Collections.Generic;
using UnityEngine;

namespace OuterWitness.Orbit
{
    /// <summary>
    /// 星系轨道核心管理类。统一更新所有天体坐标。
    /// 解决父子依赖顺序、无物理模拟、高性能更新。
    /// </summary>
    public class OrbitSystem : MonoBehaviour
    {
        public static OrbitSystem Instance { get; private set; }

        private List<OrbitBody> _orbitBodies = new List<OrbitBody>();
        private bool _isDirty = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void RegisterBody(OrbitBody body)
        {
            if (!_orbitBodies.Contains(body))
            {
                _orbitBodies.Add(body);
                MarkDirty();
            }
        }

        public void UnregisterBody(OrbitBody body)
        {
            if (_orbitBodies.Remove(body))
            {
                MarkDirty();
            }
        }

        public void MarkDirty() => _isDirty = true;

        private void Update()
        {
            // 1. 如果列表发生了变动，重新计算依赖层级并排序
            if (_isDirty)
            {
                SortOrbitBodies();
                _isDirty = false;
            }

            // 2. 集中统一更新坐标
            float time = Time.time;
            for (int i = 0; i < _orbitBodies.Count; i++)
            {
                UpdateOrbitPosition(_orbitBodies[i], time);
            }
        }

        /// <summary>
        /// 基于层级深度排序，确保恒星更新 -> 行星更新 -> 卫星更新。
        /// 解决“子星更新时父星坐标还没同步”导致的抖动问题。
        /// </summary>
        private void SortOrbitBodies()
        {
            // 计算深度
            foreach (var body in _orbitBodies)
            {
                body.dependencyDepth = CalculateDepth(body);
            }

            // 根据深度从浅到深排序
            _orbitBodies.Sort((a, b) => a.dependencyDepth.CompareTo(b.dependencyDepth));
        }

        private int CalculateDepth(OrbitBody body)
        {
            int depth = 0;
            Transform current = body.orbitCenter;

            while (current != null)
            {
                // 寻找父级是否也是轨道组件，以计算深度
                OrbitBody parentBody = current.GetComponent<OrbitBody>();
                if (parentBody != null)
                {
                    depth++;
                    current = parentBody.orbitCenter;
                }
                else break;
            }
            return depth;
        }

        private void UpdateOrbitPosition(OrbitBody body, float time)
        {
            if (body.orbitCenter == null) return;

            // 1. 获取轨道相位角度：θ = ωt + φ
            float theta = (body.angularSpeed * time) + body.initialPhase;

            // 2. 在 XZ 平面计算椭圆位置（局部空间）
            // 使用重构后的 a (semiMajorAxis) 和 动态计算的 b (SemiMinorAxis)
            float x = body.semiMajorAxis * Mathf.Cos(theta);
            float z = body.SemiMinorAxis * Mathf.Sin(theta);
            Vector3 localPos = new Vector3(x, 0, z);

            // 3. 应用轨道平面的倾斜角
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, body.orbitNormal.normalized);
            Vector3 offset = rotation * localPos;

            // 4. 应用到世界坐标
            body.transform.position = body.orbitCenter.position + offset;
        }
    }
}
