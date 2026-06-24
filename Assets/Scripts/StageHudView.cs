using TMPro;
using UnityEngine;

namespace SnakeDefender
{
    // 상단 스테이지명·파괴율 표시. TMP는 에디터에서 만들고 아래 슬롯에 연결.
    public class StageHudView : MonoBehaviour
    {
        [Header("스테이지")]
        [SerializeField] private int stageNumber = 1;
        [SerializeField] private string stageLabelFormat = "일반 스테이지 {0}";

        [Header("파괴율")]
        [SerializeField] private string destructionRateFormat = "파괴율 : {0}%";

        [Header("UI (에디터에서 연결)")]
        [SerializeField] private TextMeshProUGUI stageLabel;
        [SerializeField] private TextMeshProUGUI destructionRateLabel;

        private GameManager gameFlow;

        private void OnEnable()
        {
            gameFlow = GameManager.Instance;
            if (gameFlow != null)
            {
                gameFlow.OnBodyDestructionProgressChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (gameFlow != null)
            {
                gameFlow.OnBodyDestructionProgressChanged -= Refresh;
            }
        }

        public void SetStageNumber(int number)
        {
            stageNumber = Mathf.Max(1, number);
            Refresh();
        }

        private void Refresh()
        {
            if (stageLabel != null)
            {
                stageLabel.text = string.Format(stageLabelFormat, stageNumber);
            }

            int percent = gameFlow != null ? gameFlow.DestructionRatePercent : 0;
            if (destructionRateLabel != null)
            {
                destructionRateLabel.text = string.Format(destructionRateFormat, percent);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (stageLabel == null || destructionRateLabel == null)
            {
                Debug.LogWarning("[StageHudView] Stage Label / Destruction Rate Label 연결이 필요합니다.", this);
            }
        }
#endif
    }
}
