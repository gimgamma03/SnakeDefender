using UnityEngine;
using UnityEngine.Events;

namespace SnakeDefender
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private int totalEnemyCount;
        public int TotalEnemyCount => totalEnemyCount;

        [Header("승리·패배 이벤트")]
        [SerializeField] private UnityEvent onVictory;
        [SerializeField] private UnityEvent onDefeat;

        [Header("종료 시")]
        [Tooltip("승리 또는 패배 시 timeScale을 0으로 설정해 진행을 멈춤.")]
        [SerializeField] private bool pauseTimeOnGameEnd = true;

        private int killedCount;
        private bool finished;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Time.timeScale = 1f;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public bool IsGameFinished => finished;

        public void SetTotalEnemyCount(int count)
        {
            totalEnemyCount = Mathf.Max(0, count);
        }

        public void NotifyEnemyKilled(int score = 1)
        {
            if (finished)
            {
                return;
            }

            killedCount += Mathf.Max(1, score);

            if (killedCount >= totalEnemyCount)
            {
                finished = true;
                PauseGameplayIfNeeded();
                onVictory?.Invoke();
            }
        }

        public void NotifyEnemyReachedGoal()
        {
            if (finished)
            {
                return;
            }

            finished = true;
            PauseGameplayIfNeeded();
            onDefeat?.Invoke();
        }

        public void NotifyPlayerDefeated()
        {
            NotifyEnemyReachedGoal();
        }

        private void PauseGameplayIfNeeded()
        {
            if (pauseTimeOnGameEnd)
            {
                Time.timeScale = 0f;
            }
        }

        // 재시작이나 메뉴 갈 때 부르면 됨. timeScale은 씬 넘어가도 안 풀릴 수 있어서 따로 리셋하는 거임.
        public static void ResetTimeScale()
        {
            Time.timeScale = 1f;
        }
    }
}
