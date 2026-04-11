using UnityEngine;
using UnityEngine.InputSystem;

namespace OuterWitness.Player
{
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("相机旋转目标 (CameraTarget)")]
        [SerializeField] private Transform cameraTarget;

        [Header("Settings")]
        [SerializeField] private float mouseSensitivity = 0.2f;
        [SerializeField] private float verticalClampAngle = 80f;

        public Vector2 MoveInput { get; private set; }
        public bool JumpRequest { get; private set; }
        public bool InteractRequest { get; private set; }

        private Vector2 _lookInput;
        private float _pitch; // 仅记录俯仰角

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (cameraTarget != null)
            {
                _pitch = cameraTarget.localEulerAngles.x;
                if (_pitch > 180f) _pitch -= 360f;
            }
            else
            {
                Debug.LogError("[PlayerController] 缺少 CameraTarget 引用！", this);
            }
        }

        public void OnMove(InputValue value)
        {
            MoveInput = value.Get<Vector2>();
        }

        public void OnLook(InputValue value)
        {
            _lookInput = value.Get<Vector2>();
        }

        public void OnJump(InputValue value)
        {
            if (value.isPressed)
            {
                JumpRequest = true;
            }
        }

        public void OnInteract(InputValue value)
        {
            if (value.isPressed)
            {
                InteractRequest = true;
            }
        }

        private void Update()
        {
            HandleRotation();
        }

        /// <summary>
        /// 供外部调用，在处理完跳跃逻辑后清除请求。
        /// </summary>
        public void ConsumeJumpRequest()
        {
            JumpRequest = false;
        }

        /// <summary>
        /// 供外部调用，在处理完交互逻辑后清除请求。
        /// </summary>
        public void ConsumeInteractRequest()
        {
            InteractRequest = false;
        }

        /// <summary>
        /// 切换相机旋转目标（进入/退出飞船时调用）。
        /// </summary>
        public void SetCameraTarget(Transform newTarget)
        {
            cameraTarget = newTarget;
            if (cameraTarget != null)
            {
                _pitch = cameraTarget.localEulerAngles.x;
                if (_pitch > 180f) _pitch -= 360f;
            }
        }

        /// <summary>返回当前 CameraTarget，供外部缓存。</summary>
        public Transform GetCameraTarget() => cameraTarget;

        private void HandleRotation()
        {
            if (cameraTarget == null) return;

            float mouseX = _lookInput.x * mouseSensitivity;
            float mouseY = _lookInput.y * mouseSensitivity;

            // 1. 水平偏航 (Yaw)：围绕玩家当前的 "局部 Up" 旋转
            // 这保证了在任何引力方向下，鼠标左右滑动都是原地转圈
            transform.Rotate(Vector3.up * mouseX, Space.Self);

            // 2. 垂直俯仰 (Pitch)：围绕相机目标的 "局部 Right" 旋转
            _pitch -= mouseY;
            _pitch = Mathf.Clamp(_pitch, -verticalClampAngle, verticalClampAngle);
            cameraTarget.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }
    }
}
