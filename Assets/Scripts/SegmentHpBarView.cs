using TMPro;
using UnityEngine;

namespace SnakeDefender
{
    /// <summary>
    /// 몸통 위에 붙는 숫자형 HP (슬라이더 없음). 프리팹은 TMP만 두면 됩니다.
    /// </summary>
    public class SegmentHpBarView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;

        [Header("표시")]
        [Tooltip("1000 이상이면 5.5K 형식, 미만이면 정수.")]
        [SerializeField] private bool compactThousands = true;

        public void SetHp(float current)
        {
            if (label == null)
            {
                return;
            }

            label.text = compactThousands ? FormatHpCompact(current) : Mathf.CeilToInt(Mathf.Max(0f, current)).ToString();
        }

        public static string FormatHpCompact(float value)
        {
            float v = Mathf.Max(0f, value);
            if (v >= 1000f)
            {
                return (v / 1000f).ToString("0.0") + "K";
            }

            return Mathf.CeilToInt(v).ToString();
        }
    }
}
