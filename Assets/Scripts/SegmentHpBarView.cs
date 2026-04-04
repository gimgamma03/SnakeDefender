using TMPro;
using UnityEngine;

namespace SnakeDefender
{
    // 몸통 위에 숫자로 HP 찍는 용도임. 슬라이더 없음. 프리팹엔 TMP만 있으면 됨.
    public class SegmentHpBarView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;

        [Header("숫자 표시")]
        [Tooltip("1000 이상이면 축약(예: 5.5K), 아니면 정수 표기.")]
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
