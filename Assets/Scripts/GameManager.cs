using UnityEngine;
using UnityEngine.Events;

namespace SnakeDefender
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private int totalEnemyCount;
        public int TotalEnemyCount => totalEnemyCount;

        [Header("Events")]
        [SerializeField] private UnityEvent onVictory;
        [SerializeField] private UnityEvent onDefeat;

        [Header("Game Over")]
        [Tooltip("승리/패배 시 Time.timeScale = 0 으로 게임플레이 정지.")]
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

        // Legacy overloads to avoid breaking older components in scene.
        public void NotifyEnemyKilled(EnemyUnit enemy)
        {
            int score = enemy == null ? 1 : enemy.KillScore;
            NotifyEnemyKilled(score);
        }

        public void NotifyEnemyReachedGoal(EnemyUnit enemy)
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

        /// <summary>재시작/메뉴 복귀 등에서 호출. (Time.timeScale은 씬 전환 후에도 유지될 수 있음)</summary>
        public static void ResetTimeScale()
        {
            Time.timeScale = 1f;
        }
    }
}
