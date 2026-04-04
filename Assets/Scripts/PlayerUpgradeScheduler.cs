using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SnakeDefender
{
    // 몸통 N개 깰 때마다 업그레이드 창 띄우는 거임. UI는 upgradeUiRoot에 패널만 묶어두면 됨.
    public class PlayerUpgradeScheduler : MonoBehaviour
    {
        public static PlayerUpgradeScheduler Instance { get; private set; }

        [Header("타워")]
        [Tooltip("비우면 같은 GameObject의 DefenderTower를 사용.")]
        [SerializeField] private DefenderTower tower;

        [Header("업그레이드 조건")]
        [Tooltip("몸통 세그먼트를 이 횟수만큼 파괴할 때마다 업그레이드 1회.")]
        [SerializeField] private int bodiesDestroyedPerUpgrade = 2;

        [Header("기타")]
        [Tooltip("비우면 GameManager.Instance로 게임 종료 여부를 확인.")]
        [SerializeField] private GameManager gameManager;
        [Tooltip("켜면 업그레이드 대기 중 timeScale 0. 선택 후 FinishUpgradeChoice로 복구.")]
        [SerializeField] private bool pauseTimeWhileUpgradePending;

        [Header("업그레이드 UI")]
        [Tooltip("Canvas 전체가 아니라 버튼 묶음 패널만 연결.")]
        [SerializeField] private GameObject upgradeUiRoot;
        [Tooltip("총알 수 업그레이드 버튼. 최대 단계면 비활성.")]
        [SerializeField] private Button bulletUpgradeButton;
        [Tooltip("업그레이드 가능 시 표시할 자식 오브젝트(문구 등).")]
        [SerializeField] private GameObject bulletUpgradeNormalContent;
        [Tooltip("최대 단계일 때 표시할 자식 오브젝트.")]
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

        // SnakeEnemy에서 몸통 터질 때 부르는 거임.
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

                // CanvasGroup 있으면 알파랑 클릭 받게 맞춰두는 거임.
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
