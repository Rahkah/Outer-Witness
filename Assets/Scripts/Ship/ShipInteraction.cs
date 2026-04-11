using UnityEngine;
using Cinemachine;
using OuterWitness.Player;
using OuterWitness.PlayerTools;

namespace OuterWitness.Ship
{
    /// <summary>
    /// 飞船交互入口：检测玩家靠近且对准飞船时显示[E]提示，处理进入/退出飞船逻辑。
    /// 挂载在飞船根 GameObject 上。
    /// </summary>
    public class ShipInteraction : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("玩家 Transform")]
        [SerializeField] private Transform player;
        [Tooltip("飞船驾驶舱相机目标（进入飞船后 VCam 的 Follow / LookAt）")]
        [SerializeField] private Transform shipCameraTarget;
        [Tooltip("场景中的 Virtual Camera")]
        [SerializeField] private CinemachineVirtualCamera virtualCamera;
        [Tooltip("飞船 HUD 根节点")]
        [SerializeField] private GameObject shipHudRoot;
        [Tooltip("玩家 HUD 根节点（进入飞船后隐藏）")]
        [SerializeField] private GameObject playerHudRoot;
        [Tooltip("太空导航仪（进入飞船后隐藏）")]
        [SerializeField] private SpaceNavigator spaceNavigator;
        [Tooltip("提示 UI 根节点（显示[E]进入提示）")]
        [SerializeField] private GameObject interactPromptRoot;
        [Tooltip("飞船碰撞体（用于视线检测，留空则自动获取子节点 Collider）")]
        [SerializeField] private Collider shipCollider;

        [Header("Settings")]
        [Tooltip("触发交互提示的距离")]
        [SerializeField] private float interactRange = 4f;
        [Tooltip("视线检测最大距离")]
        [SerializeField] private float lookRayDistance = 6f;
        [Tooltip("玩家在飞船内的本地座位偏移")]
        [SerializeField] private Vector3 seatLocalOffset = new Vector3(0f, -3f, 0f);

        private PlayerController _playerController;
        private Camera _mainCamera;
        private bool _isInShip;
        private bool _trackSeat;

        // VCam 原始目标缓存
        private Transform _vcamOriginalFollow;
        private Transform _vcamOriginalLookAt;

        // 玩家父节点缓存
        private Transform _originalPlayerParent;

        private void Awake()
        {
            if (player != null)
                _playerController = player.GetComponent<PlayerController>();

            _mainCamera = Camera.main;

            if (shipCollider == null)
                shipCollider = GetComponentInChildren<Collider>();
        }

        private void Start()
        {
            if (shipHudRoot != null) shipHudRoot.SetActive(false);
            if (interactPromptRoot != null) interactPromptRoot.SetActive(false);
        }

        private void Update()
        {
            if (player == null || _playerController == null) return;

            if (!_isInShip)
            {
                bool showPrompt = IsInRangeAndLooking();
                if (interactPromptRoot != null)
                    interactPromptRoot.SetActive(showPrompt);

                if (showPrompt && _playerController.InteractRequest)
                {
                    _playerController.ConsumeInteractRequest();
                    EnterShip();
                }
                else if (_playerController.InteractRequest)
                {
                    _playerController.ConsumeInteractRequest();
                }
            }
            else
            {
                if (_playerController.InteractRequest)
                {
                    _playerController.ConsumeInteractRequest();
                    ExitShip();
                }
            }
        }

        private void LateUpdate()
        {
            if (!_trackSeat || player == null) return;

            // 每帧把玩家世界坐标钉在飞船座位上，跟随飞船移动
            player.position = transform.TransformPoint(seatLocalOffset);
            player.rotation = transform.rotation;
        }

        // ─── 视线检测 ───────────────────────────────────────

        private bool IsInRangeAndLooking()
        {
            if (Vector3.Distance(player.position, transform.position) > interactRange)
                return false;

            if (_mainCamera == null) return false;

            Ray ray = new Ray(_mainCamera.transform.position, _mainCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, lookRayDistance))
                return hit.transform.IsChildOf(transform) || hit.transform == transform;

            return false;
        }

        // ─── 进入 / 退出 ─────────────────────────────────────

        private void EnterShip()
        {
            _isInShip = true;

            // 1. 缓存玩家父节点，脱离原父节点
            _originalPlayerParent = player.parent;
            player.SetParent(null, true);

            // 2. 禁用玩家物理和移动
            SetPlayerComponents(false);

            // 3. 切换 Virtual Camera 的 Follow / LookAt 到飞船驾驶舱目标
            if (virtualCamera != null && shipCameraTarget != null)
            {
                _vcamOriginalFollow = virtualCamera.Follow;
                _vcamOriginalLookAt = virtualCamera.LookAt;
                virtualCamera.Follow = shipCameraTarget;
                virtualCamera.LookAt = shipCameraTarget;
            }

            // 4. 每帧跟随座位
            _trackSeat = true;

            // 5. 切换 HUD
            if (interactPromptRoot != null) interactPromptRoot.SetActive(false);
            if (playerHudRoot != null) playerHudRoot.SetActive(false);
            if (spaceNavigator != null) spaceNavigator.ForceSetHudVisible(false);
            if (shipHudRoot != null) shipHudRoot.SetActive(true);
        }

        private void ExitShip()
        {
            _isInShip = false;
            _trackSeat = false;

            // 1. 恢复玩家父节点
            player.SetParent(_originalPlayerParent, true);

            // 2. 恢复玩家物理和移动
            SetPlayerComponents(true);

            // 3. 恢复 Virtual Camera 的 Follow / LookAt
            if (virtualCamera != null)
            {
                virtualCamera.Follow = _vcamOriginalFollow;
                virtualCamera.LookAt = _vcamOriginalLookAt;
            }

            // 4. 切换 HUD
            if (shipHudRoot != null) shipHudRoot.SetActive(false);
            if (playerHudRoot != null) playerHudRoot.SetActive(true);
            if (spaceNavigator != null) spaceNavigator.ForceSetHudVisible(true);
        }

        private void SetPlayerComponents(bool enabled)
        {
            if (player == null) return;
            var movement = player.GetComponent<MovementController>();
            var jetpack  = player.GetComponent<JetpackController>();
            var gravity  = player.GetComponent<GravityController>();
            var rb       = player.GetComponent<Rigidbody>();

            if (movement != null) movement.enabled = enabled;
            if (jetpack  != null) jetpack.enabled  = enabled;
            if (gravity  != null) gravity.IsPassenger = !enabled;
            if (rb != null)
            {
                rb.isKinematic = !enabled;
                if (!enabled) rb.velocity = Vector3.zero;
            }
        }
    }
}
