using System.Collections;
using UnityEngine;

namespace SnakeDefender
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Required References")]
        [SerializeField] private SnakeEnemy enemyPrefab;
        [SerializeField] private PathRoute route;
        [SerializeField] private GameManager gameFlow;

        [Header("Optional References")]
        [Tooltip("If empty, snake uses its own final goal from prefab/initialize defaults.")]
        [SerializeField] private Transform finalGoalTarget;

        [Header("Tuning")]
        [Tooltip("One wave = one snake (one head). Body part count is set on the SnakeEnemy prefab.")]
        [SerializeField] private float delayBeforeSpawn;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (enemyPrefab == null || route == null || gameFlow == null)
            {
                Debug.LogWarning("[EnemySpawner] Missing required references (Enemy Prefab / Route / Game Flow).", this);
            }
        }
#endif

        private void Start()
        {
            int targetBodyCount = enemyPrefab == null ? 1 : enemyPrefab.BodyCount;
            if (gameFlow != null)
            {
                if (gameFlow.TotalEnemyCount > 0)
                {
                    targetBodyCount = gameFlow.TotalEnemyCount;
                }
                else
                {
                    gameFlow.SetTotalEnemyCount(Mathf.Max(1, targetBodyCount));
                }
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
            int spawnBodyCount = gameFlow != null && gameFlow.TotalEnemyCount > 0
                ? gameFlow.TotalEnemyCount
                : Mathf.Max(1, enemyPrefab.BodyCount);
            enemy.Initialize(route, gameFlow, finalGoalTarget, spawnBodyCount);
        }
    }
}
