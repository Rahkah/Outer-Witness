using UnityEngine;
using System.Collections.Generic;

namespace OuterWitness.Gravity
{
    public class PlanetGravity : MonoBehaviour
    {
        public static List<PlanetGravity> AllPlanets = new List<PlanetGravity>();

        [Header("Gravity Settings")]
        [Tooltip("引力强度（加速度，单位 m/s^2）")]
        public float gravityStrength = 9.81f;
        
        [Tooltip("引力生效范围")]
        public float gravityRange = 100f;
        
        [Tooltip("是否启用距离衰减（平方反比定律）")]
        public bool useFalloff = true;

        [Header("Visualization")]
        public Color gizmoColor = Color.cyan;

        private void OnEnable() => AllPlanets.Add(this);
        private void OnDisable() => AllPlanets.Remove(this);

        /// <summary>
        /// 获取给定位置处的引力向量（指向星球中心）
        /// </summary>
        public Vector3 GetGravity(Vector3 position)
        {
            Vector3 direction = (transform.position - position);
            float distance = direction.magnitude;

            if (distance > gravityRange || distance < 0.01f)
                return Vector3.zero;

            direction.Normalize();

            if (useFalloff)
            {
                // 模拟平方反比定律：F = G * (m1*m2 / r^2)
                // 这里简化处理，以表面引力为基准向上衰减
                float radius = transform.localScale.x * 0.5f; // 假设是球体
                float falloff = (radius * radius) / (distance * distance);
                return direction * gravityStrength * Mathf.Clamp01(falloff);
            }

            return direction * gravityStrength;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, gravityRange);
        }
    }
}
