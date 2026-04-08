using UnityEngine;

namespace OuterWitness.Player
{
    /// <summary>
    /// 背包推进器：
    ///   Shift  → 沿玩家局部 Up 方向推进（上升）
    ///   Ctrl   → 沿玩家局部 Down 方向推进（下降）
    /// 推进消耗 SpaceSuit 燃料；燃料耗尽时无法推进。
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SpaceSuit))]
    public class JetpackController : MonoBehaviour
    {
        private Rigidbody _rb;
        private SpaceSuit _suit;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _suit = GetComponent<SpaceSuit>();
        }

        private void FixedUpdate()
        {
            bool thrustUp   = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool thrustDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            if (!thrustUp && !thrustDown) return;

            float force = _suit.ConsumeFuelAndGetForce();
            if (force <= 0f) return;

            Vector3 direction = thrustUp ? transform.up : -transform.up;
            _rb.AddForce(direction * force, ForceMode.Acceleration);
        }
    }
}
