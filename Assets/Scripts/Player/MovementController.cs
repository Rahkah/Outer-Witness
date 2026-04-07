using UnityEngine;

namespace OuterWitness.Player
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(GravityController))]
    [RequireComponent(typeof(PlayerController))]
    public class MovementController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 6f;
        [SerializeField] private float runSpeed = 10f;
        [SerializeField] private float acceleration = 50f;

        [Header("Jump Settings")]
        [SerializeField] private float jumpForce = 5f;

        private Rigidbody _rb;
        private GravityController _gravity;
        private PlayerController _player;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _gravity = GetComponent<GravityController>();
            _player = GetComponent<PlayerController>();
        }

        private void FixedUpdate()
        {
            HandleMovement();
            HandleJump();
        }

        private void HandleMovement()
        {
            Vector2 input = _player.MoveInput;
            
            // 1. 计算当前平面的切线方向
            // transform.forward 和 transform.right 已经由 GravityController 自动对齐了星球法线
            Vector3 targetMoveDir = (transform.forward * input.y + transform.right * input.x).normalized;

            // 2. 目标速度（如果是斜向移动且没有 normalize，input.magnitude 会超过 1）
            float currentSpeed = walkSpeed; // 这里可以根据 Shift 切换 runSpeed
            Vector3 targetVelocity = targetMoveDir * currentSpeed;

            // 3. 计算速度差，并施加力进行调整
            // 仅在水平面（局部平面）内调整速度，保留垂直分量（引力产生的下落速度）
            Vector3 currentVelocity = _rb.velocity;
            
            // 投影到局部平面
            Vector3 localVelocity = currentVelocity - Vector3.Project(currentVelocity, transform.up);
            Vector3 velocityDiff = targetVelocity - localVelocity;

            // 4. 应用加速度
            // 如果不在地面，加速度减半（增强空中控制力，但不像地面那么强）
            float currentAccel = _gravity.IsGrounded ? acceleration : acceleration * 0.5f;
            
            if (targetMoveDir.sqrMagnitude > 0.01f || localVelocity.sqrMagnitude > 0.01f)
            {
                _rb.AddForce(velocityDiff * currentAccel, ForceMode.Acceleration);
            }
        }

        private void HandleJump()
        {
            if (_player.JumpRequest)
            {
                if (_gravity.IsGrounded)
                {
                    // 沿着玩家的局部 Up 方向施加冲量 (Impulse 考虑质量)
                    // 使用 VelocityChange 可以忽略质量，获得更稳定的跳跃高度感
                    _rb.AddForce(transform.up * jumpForce, ForceMode.VelocityChange);
                }

                // 处理完请求后必须清除，防止连续触发
                _player.ConsumeJumpRequest();
            }
        }
    }
}
