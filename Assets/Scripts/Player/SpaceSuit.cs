using System;
using UnityEngine;

namespace OuterWitness.Player
{
    /// <summary>
    /// 太空服燃料系统：管理燃料槽，提供推进器接口，并通过事件驱动 UI 更新。
    /// </summary>
    public class SpaceSuit : MonoBehaviour
    {
        [Header("Fuel Settings")]
        [SerializeField] private float maxFuel = 100f;
        [SerializeField] private float fuelDepletionPerSecond = 8f;
        [SerializeField] private float fuelRechargePerSecond = 4f;
        [Tooltip("离地后延迟多少秒才开始回充燃料")]
        [SerializeField] private float rechargeDelay = 1f;

        [Header("Thruster Settings")]
        [SerializeField] private float thrusterForce = 12f;

        private float _currentFuel;
        private float _rechargeTimer;
        private GravityController _gravity;

        public float FuelPercent => _currentFuel / maxFuel * 100f;
        public bool HasFuel => _currentFuel > 0f;

        /// <summary>燃料百分比变化时触发（0~100）</summary>
        public event Action<float> OnFuelChanged;

        private void Awake()
        {
            _currentFuel = maxFuel;
            _gravity = GetComponent<GravityController>();
        }

        private void FixedUpdate()
        {
            // 落地时回充燃料（带延迟）
            if (_gravity != null && _gravity.IsGrounded)
            {
                _rechargeTimer += Time.fixedDeltaTime;
                if (_rechargeTimer >= rechargeDelay && _currentFuel < maxFuel)
                {
                    SetFuel(_currentFuel + fuelRechargePerSecond * Time.fixedDeltaTime);
                }
            }
            else
            {
                _rechargeTimer = 0f;
            }
        }

        /// <summary>
        /// 消耗燃料并返回推进力大小；燃料耗尽时返回 0。
        /// </summary>
        public float ConsumeFuelAndGetForce()
        {
            if (!HasFuel) return 0f;

            _rechargeTimer = 0f;
            SetFuel(_currentFuel - fuelDepletionPerSecond * Time.fixedDeltaTime);
            return thrusterForce;
        }

        /// <summary>外部填充燃料（如加油站）</summary>
        public void Refuel(float amount)
        {
            SetFuel(_currentFuel + amount);
        }

        private void SetFuel(float value)
        {
            float clamped = Mathf.Clamp(value, 0f, maxFuel);
            if (Mathf.Approximately(clamped, _currentFuel)) return;
            _currentFuel = clamped;
            OnFuelChanged?.Invoke(FuelPercent);
        }
    }
}
