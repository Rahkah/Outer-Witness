using UnityEngine;
using OuterWitness.Gravity;

namespace OuterWitness.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class GravityController : MonoBehaviour
    {
        [Header("Alignment Settings")]
        [Tooltip("对齐星球表面的平滑速度")]
        public float rotationSpeed = 10f;

        [Header("Ground Check")]
        [SerializeField] private float groundCheckDistance = 1.2f;
        [SerializeField] private LayerMask groundLayer;

        public PlanetGravity CurrentPlanet { get; private set; }
        public bool IsGrounded { get; private set; }
        public Vector3 GravityDirection { get; private set; } = Vector3.down;

        private Rigidbody _rb;
        private Vector3 _lastPlanetPosition;
        private Quaternion _lastPlanetRotation;
        private PlanetGravity _lastPlanet;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            
            // 重要：禁用内置重力，并设置物理属性
            _rb.useGravity = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate; // 消除抖动
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            
            // 锁定物理旋转，我们将手动控制（防止被物理引擎由于碰撞导致的乱翻）
            _rb.freezeRotation = true;
        }

        private void FixedUpdate()
        {
            SyncReferenceFrame();
            FindClosestPlanet();
            ApplyGravity();
            GroundCheck();
            HandleAlignment();
            UpdateLastState();
        }

        private void SyncReferenceFrame()
        {
            if (_lastPlanet != null)
            {
                // 计算星球本帧相对于上一物理帧的位移与旋转增量
                Vector3 planetPos = _lastPlanet.transform.position;
                Quaternion planetRot = _lastPlanet.transform.rotation;

                // 1. 旋转增量 (Delta Rotation)
                Quaternion rotDelta = planetRot * Quaternion.Inverse(_lastPlanetRotation);

                // 2. 虚拟父子同步：计算由于星球旋转和位移导致的玩家新位置
                // localOffset 是玩家相对于上一帧星球中心的偏移
                Vector3 localOffset = _rb.position - _lastPlanetPosition;
                
                // 应用旋转增量到该偏移，并加上星球的新中心位置
                Vector3 targetPosition = planetPos + (rotDelta * localOffset);

                // 3. 应用同步
                // 直接更新 Rigidbody 的位置和旋转，使其保持在星球的“虚拟本地空间”
                _rb.position = targetPosition;
                _rb.rotation = rotDelta * _rb.rotation;
            }
        }

        private void UpdateLastState()
        {
            if (CurrentPlanet != null)
            {
                _lastPlanet = CurrentPlanet;
                _lastPlanetPosition = CurrentPlanet.transform.position;
                _lastPlanetRotation = CurrentPlanet.transform.rotation;
            }
            else
            {
                _lastPlanet = null;
            }
        }

        private void FindClosestPlanet()
        {
            PlanetGravity closest = null;
            float minDistance = float.MaxValue;

            foreach (var p in PlanetGravity.AllPlanets)
            {
                float dist = Vector3.Distance(transform.position, p.transform.position);
                if (dist < minDistance && dist <= p.gravityRange)
                {
                    minDistance = dist;
                    closest = p;
                }
            }

            CurrentPlanet = closest;
        }

        private void ApplyGravity()
        {
            if (CurrentPlanet == null)
            {
                GravityDirection = Vector3.down; // 默认方向
                Debug.Log("No planets in range! Gravity disabled.");
                return;
            }

            Vector3 gravityForce = CurrentPlanet.GetGravity(transform.position);
            GravityDirection = gravityForce.normalized;

            // 使用 AddForce 实现物理加速 (Acceleration 忽略质量)
            _rb.AddForce(gravityForce, ForceMode.Acceleration);
        }

        private void GroundCheck()
        {
            // 向引力方向发射射线检测地面
            IsGrounded = Physics.Raycast(transform.position, GravityDirection, groundCheckDistance, groundLayer);
        }

        private void HandleAlignment()
        {
            if (CurrentPlanet == null) return;

            // 计算目标 Up 方向（引力的反方向）
            Vector3 targetUp = -GravityDirection;

            // 核心对齐逻辑：
            // 将当前 rb.rotation 旋转至 targetUp，同时尽量保持 transform.forward
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, targetUp) * _rb.rotation;
            
            // 平滑旋转
            _rb.rotation = Quaternion.Slerp(_rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }
}
