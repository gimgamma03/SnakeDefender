using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SnakeDefender
{
    /// <summary>
    /// 몸통 파괴 N개마다 업그레이드 패널 표시. UI는 Upgrade Ui Root 에 패널만 연결.
    /// </summary>
    public class PlayerUpgradeScheduler : MonoBehaviour
    {
        public static PlayerUpgradeScheduler Instance { get; private set; }

        [Header("Tower (플레이어)")]
        [Tooltip("비우면 같은 GameObject 의 DefenderTower 를 사용.")]
        [SerializeField] private DefenderTower tower;

        [Header("Trigger")]
        [Tooltip("몸통 세그먼트를 이 개수만큼 파괴할 때마다 업그레이드 1회 제공.")]
        [SerializeField] private int bodiesDestroyedPerUpgrade = 2;

        [Header("Optional")]
        [Tooltip("비우면 GameManager.Instance 로 판정.")]
        [SerializeField] private GameManager gameManager;
        [Tooltip("체크 시 업그레이드 제안 시점에 Time.timeScale = 0. UI에서 선택 후 ResumeTime 호출 필요.")]
        [SerializeField] private bool pauseTimeWhileUpgradePending;

        [Header("Upgrade UI (이것만 켜고 끔)")]
        [Tooltip("Canvas 전체가 아니라, 3개 버튼을 감싼 패널 하나만 연결.")]
        [SerializeField] private GameObject upgradeUiRoot;
        [Tooltip("총알 수 업그레이드 버튼. 최대 단계면 interactable=false.")]
        [SerializeField] private Button bulletUpgradeButton;
        [Tooltip("버튼 자식: 업 가능할 때 보여줄 오브젝트(예: 공격 문구 TMP 부모).")]
        [SerializeField] private GameObject bulletUpgradeNormalContent;
        [Tooltip("버튼 자식: 최대 단계일 때 보여줄 오브젝트(예: 최대 TMP 부모).")]
        [FormerlySerializedAs("bulletUpgradeMaxLabel")]
        [SerializeField] private GameObject bulletUpgradeMaxContent;

        private int bodiesDestroyedSinceStart;
        private bool upgradePending;
        private CanvasGroup upgradeUiCanvasGroup;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (tower == null)
            {
                tower = GetComponent<DefenderTower>();
            }
        }

        private void Start()
        {
            if (upgradeUiRoot != null)
            {
                upgradeUiCanvasGroup = upgradeUiRoot.GetComponent<CanvasGroup>();
                upgradeUiRoot.SetActive(false);
            }

            RefreshBulletUpgradeButtonInteractable();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>SnakeEnemy 가 몸통 파괴 시 호출.</summary>
        public void RegisterBodySegmentDestroyed()
        {
            if (tower == null)
            {
                return;
            }

            GameManager gm = gameManager != null ? gameManager : GameManager.Instance;
            if (gm != null && gm.IsGameFinished)
            {
                return;
            }

            bodiesDestroyedSinceStart++;
            if (bodiesDestroyedSinceStart % Mathf.Max(1, bodiesDestroyedPerUpgrade) != 0)
            {
                return;
            }

            if (pauseTimeWhileUpgradePending && upgradePending)
            {
                return;
            }

            if (pauseTimeWhileUpgradePending)
            {
                upgradePending = true;
                Time.timeScale = 0f;
            }

            if (upgradeUiRoot != null)
            {
                upgradeUiRoot.SetActive(true);
                RectTransform rt = upgradeUiRoot.transform as RectTransform;
                if (rt != null)
                {
                    rt.SetAsLastSibling();
                }

                // Defensive: if CanvasGroup exists, make sure panel can receive clicks.
                if (upgradeUiCanvasGroup == null)
                {
                    upgradeUiCanvasGroup = upgradeUiRoot.GetComponent<CanvasGroup>();
                }

                if (upgradeUiCanvasGroup != null)
                {
                    upgradeUiCanvasGroup.alpha = 1f;
                    upgradeUiCanvasGroup.interactable = true;
                    upgradeUiCanvasGroup.blocksRaycasts = true;
                }

                RefreshBulletUpgradeButtonInteractable();
            }
        }

        public void ApplyAttackPowerUpgrade()
        {
            tower?.ApplyAttackPowerUpgrade();
            FinishUpgradeChoice();
        }

        public void ApplyAttackSpeedUpgrade()
        {
            tower?.ApplyAttackSpeedUpgrade();
            FinishUpgradeChoice();
        }

        public void ApplyBulletCountUpgrade()
        {
            if (tower == null || !tower.TryApplyBulletCountUpgrade())
            {
                return;
            }

            FinishUpgradeChoice();
        }

        private void RefreshBulletUpgradeButtonInteractable()
        {
            if (tower == null)
            {
                return;
            }

            bool isMaxed = tower.IsBulletCountUpgradeMaxed;
            if (bulletUpgradeButton != null)
            {
                bulletUpgradeButton.interactable = !isMaxed;
            }

            if (bulletUpgradeNormalContent != null)
            {
                bulletUpgradeNormalContent.SetActive(!isMaxed);
            }

            if (bulletUpgradeMaxContent != null)
            {
                bulletUpgradeMaxContent.SetActive(isMaxed);
            }
        }

        public void FinishUpgradeChoice()
        {
            upgradePending = false;
            if (upgradeUiRoot != null)
            {
                upgradeUiRoot.SetActive(false);
            }

            if (pauseTimeWhileUpgradePending)
            {
                GameManager.ResetTimeScale();
            }
        }
    }
}
