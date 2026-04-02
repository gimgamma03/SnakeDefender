using System.Collections;
using UnityEngine;

namespace SnakeDefender
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private SnakeEnemy enemyPrefab;
        [SerializeField] private PathRoute route;
        [SerializeField] private GameFlowController gameFlow;
        [Tooltip("One wave = one snake (one head). Body part count is set on the SnakeEnemy prefab.")]
        [SerializeField] private float delayBeforeSpawn;

        private void Start()
        {
            if (gameFlow != null)
            {
                gameFlow.SetTotalEnemyCount(1);
            }

            StartCoroutine(SpawnRoutine());
        }

        private IEnumerator SpawnRoutine()
        {
            if (enemyPrefab == null || route == null)
            {
                yield break;
            }

            if (delayBeforeSpawn > 0f)
            {
                yield return new WaitForSeconds(delayBeforeSpawn);
            }

            SnakeEnemy enemy = Instantiate(enemyPrefab, route.GetWaypointPosition(0), Quaternion.identity);
            enemy.Initialize(route, gameFlow);
        }
    }
}
