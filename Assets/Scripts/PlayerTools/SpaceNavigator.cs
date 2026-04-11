using UnityEngine;
using TMPro;
using OuterWitness.Gravity;
using OuterWitness.Player;

namespace OuterWitness.PlayerTools
{
    /// <summary>
    /// 太空导航仪 HUD
    /// 追踪天体位置，显示距离/速度，并用速度箭头指示相对运动方向。
    /// 逻辑移植自 outer_wilds_clone/SpaceNavigator.cs，适配本项目的 PlanetGravity 系统。
    /// </summary>
    public class SpaceNavigator : MonoBehaviour
    {
        [Header("Suggest Cursor")]
        [SerializeField] private GameObject suggestCursor;
        [SerializeField] private GameObject suggesterCursorTopRightArc;
        [SerializeField] private GameObject suggesterCursorTopLeftArc;
        [SerializeField] private GameObject suggesterCursorBottomRightArc;
        [SerializeField] private GameObject suggesterCursorBottomLeftArc;

        [Header("Lock Cursor (lockCursor 为根节点，lockCursorArcs / VelocityArrows / lockInformation 均为其子节点)")]
        [SerializeField] private GameObject lockCursor;
        [SerializeField] private GameObject lockCursorTopRightArc;
        [SerializeField] private GameObject lockCursorTopLeftArc;
        [SerializeField] private GameObject lockCursorBottomRightArc;
        [SerializeField] private GameObject lockCursorBottomLeftArc;
        [SerializeField] private GameObject topVelocityArrowMask;
        [SerializeField] private GameObject rightVelocityArrowMask;
        [SerializeField] private GameObject bottomVelocityArrowMask;
        [SerializeField] private GameObject leftVelocityArrowMask;
        [SerializeField] private GameObject topVelocityArrow;
        [SerializeField] private GameObject rightVelocityArrow;
        [SerializeField] private GameObject bottomVelocityArrow;
        [SerializeField] private GameObject leftVelocityArrow;

        [Header("Info Text (lockInformation 也是 lockCursor 的子节点)")]
        [SerializeField] private TextMeshProUGUI lockInformation;

        [Header("References")]
        [SerializeField] private Rigidbody player;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private GravityController gravityController;
        [SerializeField] private RectTransform canvasRect;

        [Header("Settings")]
        [Tooltip("距离超过此值时显示 km")]
        [SerializeField] private float kmThreshold = 5000f;
        [Tooltip("光标弧片额外留白（像素）")]
        [SerializeField] private float cursorPadding = 50f;
        [Tooltip("速度箭头 Mask 半尺寸，需与 Mask RectTransform 的半宽一致")]
        [SerializeField] private float arrowMaskHalfSize = 100f;
        [Tooltip("速度达到此值时箭头完全露出（m/s）")]
        [SerializeField] private float arrowMaxSpeed = 20f;
        [Tooltip("速度为 0 时箭头头部刚好露出的初始 PosX（通常为负值，约等于 -(箭头长度/2)）")]
        [SerializeField] private float arrowDefaultPos = -995f;
        [Tooltip("箭头 PosX 上限，超过此值会出 mask 范围")]
        [SerializeField] private float arrowMaxPos = -904f;

        private PlanetGravity _lockedBody;
        private float _previousDistance;
        private bool _hudVisible;

        private void Start()
        {
            suggestCursor.SetActive(false);
            lockCursor.SetActive(false);
            SetHudVisible(true);
        }

        private void Update()
        {
            // Tab 键切换 HUD
            if (Input.GetKeyDown(KeyCode.Tab))
                SetHudVisible(!_hudVisible);

            if (!_hudVisible) return;

            if (Input.GetMouseButtonDown(0))
            {
                if (HasSuggestedPlanet() && !IsLocked())
                    Lock();
                else if (IsLocked())
                    Unlock();
            }
        }

        private void FixedUpdate()
        {
            if (!_hudVisible) return;

            if (!IsLocked())
                TryToSuggestCelestialBody();
            else
                UpdateLock();
        }

        // ─────────────────────────────────────────────
        // HUD 显隐
        // ─────────────────────────────────────────────

        private void SetHudVisible(bool visible)
        {
            _hudVisible = visible;
            if (!visible)
            {
                Unlock();
                suggestCursor.SetActive(false);
            }
        }

        /// <summary>供外部（如飞船交互）强制设置 HUD 显隐。</summary>
        public void ForceSetHudVisible(bool visible) => SetHudVisible(visible);

        // ─────────────────────────────────────────────
        // 建议逻辑
        // ─────────────────────────────────────────────

        private void TryToSuggestCelestialBody()
        {
            PlanetGravity suggested = GetSuggestedCelestialBody();

            if (suggested != null)
            {
                UpdateCursorCoordinates(suggested);
                UpdateCursorSize(suggested);
                suggestCursor.SetActive(true);
            }
            else
            {
                suggestCursor.SetActive(false);
            }
        }

        /// <summary>
        /// 沿相机正前方 RaycastAll，返回第一个命中的非当前星球的 PlanetGravity。
        /// </summary>
        private PlanetGravity GetSuggestedCelestialBody()
        {
            Transform camTransform = playerCamera.transform;
            RaycastHit[] hits = Physics.RaycastAll(camTransform.position, camTransform.forward, playerCamera.farClipPlane);

            foreach (RaycastHit hit in hits)
            {
                PlanetGravity body = hit.transform.GetComponentInParent<PlanetGravity>();
                if (body == null) body = hit.transform.GetComponent<PlanetGravity>();
                if (body == null) continue;

                // 忽略玩家当前所在星球
                if (gravityController != null && body == gravityController.CurrentPlanet)
                    continue;

                return body;
            }

            return null;
        }

        private bool HasSuggestedPlanet() => suggestCursor.activeSelf;

        // ─────────────────────────────────────────────
        // 锁定逻辑
        // ─────────────────────────────────────────────

        private void Lock()
        {
            _lockedBody = GetSuggestedCelestialBody();
            if (_lockedBody == null) return;
            UpdateLock();
            suggestCursor.SetActive(false);
        }

        private void Unlock()
        {
            if (lockCursor != null) lockCursor.SetActive(false);
            _lockedBody = null;
        }

        private bool IsLocked() => _lockedBody != null;

        private void UpdateLock()
        {
            if (IsLockedObjectBehindCamera())
            {
                lockCursor.SetActive(false);
                return;
            }

            lockCursor.SetActive(true);
            UpdateCursorCoordinates(_lockedBody);
            UpdateCursorSize(_lockedBody);
            UpdateLockText();
            UpdateVelocityArrows();
        }

        // ─────────────────────────────────────────────
        // 光标坐标 & 尺寸
        // ─────────────────────────────────────────────

        private void UpdateCursorCoordinates(PlanetGravity body)
        {
            transform.position = GetBodyScreenPosition(body);
        }

        private Vector3 GetBodyScreenPosition(PlanetGravity body)
        {
            Vector3 pos = playerCamera.WorldToScreenPoint(body.transform.position);
            pos.z = pos.z > 0 ? 1f : -1f;
            return pos;
        }

        private bool IsLockedObjectBehindCamera()
        {
            return GetBodyScreenPosition(_lockedBody).z < 0;
        }

        private void UpdateCursorSize(PlanetGravity body)
        {
            float offset = GetCursorPositionAddition(body);

            if (IsLocked())
            {
                lockCursorTopRightArc.transform.localPosition    = new Vector3( offset,  offset, 0);
                lockCursorTopLeftArc.transform.localPosition     = new Vector3(-offset,  offset, 0);
                lockCursorBottomRightArc.transform.localPosition = new Vector3( offset, -offset, 0);
                lockCursorBottomLeftArc.transform.localPosition  = new Vector3(-offset, -offset, 0);
            }
            else
            {
                suggesterCursorTopRightArc.transform.localPosition    = new Vector3( offset,  offset, 0);
                suggesterCursorTopLeftArc.transform.localPosition     = new Vector3(-offset,  offset, 0);
                suggesterCursorBottomRightArc.transform.localPosition = new Vector3( offset, -offset, 0);
                suggesterCursorBottomLeftArc.transform.localPosition  = new Vector3(-offset, -offset, 0);
            }

            float arrowMaskCoord = offset + 80f; // 箭头 mask 放在弧片外侧 80px
            topVelocityArrowMask.transform.localPosition    = new Vector3(0,               arrowMaskCoord, 0);
            bottomVelocityArrowMask.transform.localPosition = new Vector3(0,              -arrowMaskCoord, 0);
            rightVelocityArrowMask.transform.localPosition  = new Vector3( arrowMaskCoord, 0,              0);
            leftVelocityArrowMask.transform.localPosition   = new Vector3(-arrowMaskCoord, 0,              0);

            float infoX = 120f + offset;
            lockInformation.transform.localPosition = new Vector3(infoX, 40f, 0f);
        }

        /// <summary>
        /// 用透视投影公式把天体世界空间半径转换为 Canvas 局部像素偏移。
        /// screenRadius(px) = radius / (dist * tan(fov/2)) * (canvasHeight / 2)
        /// </summary>
        private float GetCursorPositionAddition(PlanetGravity body)
        {
            float radius = body.transform.localScale.x * 0.5f;
            float dist   = (body.transform.position - player.position).magnitude;
            if (dist < 0.01f) return 60f;

            float halfFovRad   = playerCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float canvasHeight = canvasRect != null ? canvasRect.rect.height : playerCamera.pixelHeight;
            float screenRadius = radius / (dist * Mathf.Tan(halfFovRad)) * (canvasHeight * 0.5f);

            return Mathf.Max(screenRadius, 60f) + cursorPadding;
        }

        // ─────────────────────────────────────────────
        // 锁定文字
        // ─────────────────────────────────────────────

        private void UpdateLockText()
        {
            lockInformation.text = $"{_lockedBody.gameObject.name}\n{GetDistanceText()}\n{GetVelocityText()}";
        }

        private string GetDistanceText()
        {
            float dist = (player.position - _lockedBody.transform.position).magnitude;
            return dist > kmThreshold
                ? $"{dist / 1000f:####0}km"
                : $"{dist:####0}m";
        }

        private string GetVelocityText()
        {
            float currentDist = (player.position - _lockedBody.transform.position).magnitude;
            float approachSpeed = (_previousDistance - currentDist) / Time.fixedDeltaTime;
            _previousDistance = currentDist;
            string sign = approachSpeed < 0 ? "-" : "";
            return $"{sign}{Mathf.Abs(approachSpeed):####0}m/s";
        }

        // ─────────────────────────────────────────────
        // 速度箭头
        // ─────────────────────────────────────────────

        private void UpdateVelocityArrows()
        {
            Vector3 vel = player.velocity;

            float rollAngle = player.transform.rotation.eulerAngles.z;
            vel = Quaternion.Euler(0, 0, -rollAngle) * vel;

            // 四个箭头全部沿 X 轴移动（mask 均为横向裁剪）
            rightVelocityArrow.transform.localPosition  = new Vector3(ArrowLocalPos( vel.x), 0, 0);
            leftVelocityArrow.transform.localPosition   = new Vector3(ArrowLocalPos(-vel.x), 0, 0);
            topVelocityArrow.transform.localPosition    = new Vector3(ArrowLocalPos( vel.y), 0, 0);
            bottomVelocityArrow.transform.localPosition = new Vector3(ArrowLocalPos(-vel.y), 0, 0);
        }

        /// <summary>
        /// 速度为 0 时箭头头部刚好露出（arrowDefaultPos），
        /// 速度增大时向正方向移动，线部逐渐滑入 mask 可见区域。
        /// </summary>
        private float ArrowLocalPos(float velocityComponent)
        {
            float t = Mathf.Clamp01(velocityComponent / arrowMaxSpeed);
            t = Mathf.Sqrt(t); // 平方根让低速段更不敏感
            float pos = arrowDefaultPos + t * (arrowMaxPos - arrowDefaultPos);
            return pos;
        }
    }
}
