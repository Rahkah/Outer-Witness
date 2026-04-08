using UnityEngine;
using UnityEngine.UI;
using OuterWitness.Player;

namespace OuterWitness.UI
{
    /// <summary>
    /// 太空服状态 HUD：订阅 SpaceSuit 燃料事件，驱动 fuelArc 弧形状态条。
    /// fuelArc 使用 Image.fillAmount，最大填充 0.25（对应 1/4 圆弧 Sprite）。
    /// </summary>
    public class SpaceSuitStatus : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpaceSuit spaceSuit;

        [Header("UI")]
        [Tooltip("燃料弧形状态条 Image（fillAmount 驱动）")]
        [SerializeField] private Image fuelArc;

        private void Start()
        {
            if (spaceSuit == null)
            {
                Debug.LogError("[SpaceSuitStatus] 缺少 SpaceSuit 引用！", this);
                return;
            }

            spaceSuit.OnFuelChanged += UpdateFuelArc;

            // 初始化显示
            UpdateFuelArc(spaceSuit.FuelPercent);
        }

        private void OnDestroy()
        {
            if (spaceSuit != null)
                spaceSuit.OnFuelChanged -= UpdateFuelArc;
        }

        private void UpdateFuelArc(float percentage)
        {
            if (fuelArc == null) return;
            // 与 outer_wilds_clone 保持一致：最大 fillAmount = 0.25（1/4 圆弧）
            fuelArc.fillAmount = percentage / 100f * 0.25f;
        }
    }
}
