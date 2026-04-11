using UnityEngine;
using TMPro;

namespace OuterWitness.UI
{
    /// <summary>
    /// 飞船驾驶舱 HUD：显示速度、高度等飞行数据。
    /// 挂载在飞船 HUD Canvas 的根节点上。
    /// </summary>
    public class ShipHUD : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("飞船 Rigidbody（用于读取速度）")]
        [SerializeField] private Rigidbody shipRigidbody;

        [Header("UI Elements")]
        [Tooltip("速度显示文本")]
        [SerializeField] private TextMeshProUGUI speedText;
        [Tooltip("退出提示文本（按E退出）")]
        [SerializeField] private TextMeshProUGUI exitPromptText;

        private void OnEnable()
        {
            if (exitPromptText != null)
                exitPromptText.text = "[E] 离开飞船";
        }

        private void Update()
        {
            if (shipRigidbody != null && speedText != null)
            {
                float speed = shipRigidbody.velocity.magnitude;
                speedText.text = $"速度  {speed:F1} m/s";
            }
        }
    }
}
