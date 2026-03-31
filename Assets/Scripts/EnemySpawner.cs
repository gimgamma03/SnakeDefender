using System.Collections;
using UnityEngine;

namespace SnakeDefender
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private SnakeEnemy enemyPrefab;
        [SerializeField] private PathRoute route;
        [SerializeField] private GameFlowController gameFlow;
        [SerializeField] private int spawnCount = 10;
        [SerializeField] private float spawnInterval = 1f;

        private void Start()
        {
            if (gameFlow != null)
            {
                gameFlow.SetTotalEnemyCount(spawnCount);
            }

            StartCoroutine(SpawnRoutine());
        }

        private IEnumerator SpawnRoutine()
        {
            if (enemyPrefab == null || route == null)
            {
                yield break;
            }

            for (int i = 0; i < spawnCount; i++)
            {
                SnakeEnemy enemy = Instantiate(enemyPrefab, route.GetWaypointPosition(0), Quaternion.identity);
                enemy.Initialize(route, gameFlow);
                yield return new WaitForSeconds(spawnInterval);
            }
        }
    }
}
