using UnityEngine;

namespace OuterWitness.Player
{
    /// <summary>
    /// 背包推进器：
    ///   Shift  → 沿玩家局部 Up 方向推进（上升）
    ///   Ctrl   → 沿玩家局部 Down 方向推进（下降）
    ///   Q / E  → 太空中（无重力场时）左右翻滚调整姿态
    /// 推进消耗 SpaceSuit 燃料；燃料耗尽时无法推进。
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SpaceSuit))]
    [RequireComponent(typeof(GravityController))]
    public class JetpackController : MonoBehaviour
    {
        [Header("Roll Settings")]
        [Tooltip("太空中 Q/E 翻滚速度（度/秒）")]
        [SerializeField] private float rollSpeed = 90f;

        private Rigidbody _rb;
        private SpaceSuit _suit;
        private GravityController _gravity;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _suit = GetComponent<SpaceSuit>();
            _gravity = GetComponent<GravityController>();
        }

        private void FixedUpdate()
        {
            HandleThrust();
            HandleSpaceRoll();
        }

        private void HandleThrust()
        {
            bool thrustUp   = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool thrustDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            if (!thrustUp && !thrustDown) return;

            float force = _suit.ConsumeFuelAndGetForce();
            if (force <= 0f) return;

            Vector3 direction = thrustUp ? transform.up : -transform.up;
            _rb.AddForce(direction * force, ForceMode.Acceleration);
        }

        /// <summary>
        /// 仅在太空中（不在任何星球重力场内）时，Q/E 绕 forward 轴翻滚。
        /// </summary>
        private void HandleSpaceRoll()
        {
            if (_gravity.CurrentPlanet != null) return;

            float roll = 0f;
            if (Input.GetKey(KeyCode.Q)) roll += 1f;
            if (Input.GetKey(KeyCode.E)) roll -= 1f;

            if (Mathf.Approximately(roll, 0f)) return;

            Quaternion rollDelta = Quaternion.AngleAxis(roll * rollSpeed * Time.fixedDeltaTime, transform.forward);
            _rb.MoveRotation(rollDelta * _rb.rotation);
        }
    }
}
