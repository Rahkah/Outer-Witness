using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("必须指定一个子物体作为相机旋转目标，并将 Main Camera 挂载在该物体下")]
    [SerializeField] private Transform cameraTarget;

    [Header("Settings")]
    [SerializeField] private float mouseSensitivity = 0.2f; // 稍微提高一点灵敏度
    [SerializeField] private float verticalClampAngle = 80f;

    public Vector2 MoveInput { get; private set; }

    private Vector2 _lookInput;
    private float _yaw;      // 水平偏航角
    private float _pitch;    // 垂直俯仰角

    private void Start()
    {
        // 1. 锁定鼠标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 2. 初始化角度（读取当前物体的初始旋转值）
        _yaw = transform.localEulerAngles.y;
        
        if (cameraTarget != null)
        {
            _pitch = cameraTarget.localEulerAngles.x;
            if (_pitch > 180f) _pitch -= 360f; // 规范化角度到 -180 到 180
        }
        else
        {
            Debug.LogError("[PlayerController] 缺少 CameraTarget 引用！请在 Inspector 中指定一个子物体。", this);
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

    private void Update()
    {
        HandleRotation();
    }

    private void HandleRotation()
    {
        if (cameraTarget == null) return;

        // 获取鼠标增量
        float mouseX = _lookInput.x * mouseSensitivity;
        float mouseY = _lookInput.y * mouseSensitivity;

        // 1. 水平旋转：作用于整个玩家根节点 (Player)
        _yaw += mouseX;
        transform.localRotation = Quaternion.Euler(0f, _yaw, 0f);

        // 2. 垂直旋转：仅作用于相机目标 (CameraTarget)
        _pitch -= mouseY;
        _pitch = Mathf.Clamp(_pitch, -verticalClampAngle, verticalClampAngle);

        // 强制设置局部旋转，并确保 Y 和 Z 始终为 0
        cameraTarget.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }
}
