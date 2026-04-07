using UnityEngine;

namespace OuterWitness.Light
{
    /// <summary>
    /// 简单的星球自转控制，配合光照产生昼夜效果。
    /// </summary>
    public class PlanetRotation : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [SerializeField] private Vector3 _rotationAxis = Vector3.up;
        [SerializeField] private float _degreesPerSecond = 5f;

        private void Update()
        {
            // 简单的局部旋转
            transform.Rotate(_rotationAxis, _degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
