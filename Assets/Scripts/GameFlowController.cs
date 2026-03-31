using UnityEngine;
using UnityEngine.Events;

namespace SnakeDefender
{
    public class GameFlowController : MonoBehaviour
    {
        [SerializeField] private int totalEnemyCount;

        [Header("Events")]
        [SerializeField] private UnityEvent onVictory;
        [SerializeField] private UnityEvent onDefeat;

        private int killedCount;
        private bool finished;

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
            onDefeat?.Invoke();
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
    }
}
