using System.Collections;
using UnityEngine;

namespace SnakeDefender
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("필수 참조")]
        [SerializeField] private SnakeEnemy enemyPrefab;
        [SerializeField] private PathRoute route;
        [SerializeField] private GameManager gameFlow;

        [Header("선택 참조")]
        [Tooltip("비우면 뱀 프리팹 또는 초기화 기본값의 목표를 사용.")]
        [SerializeField] private Transform finalGoalTarget;

        [Header("스폰 간격")]
        [Tooltip("웨이브당 뱀 한 마리(머리 하나). 몸통 개수는 SnakeEnemy 프리팹에서 설정.")]
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

            yield return new WaitUntil(() =>
            {
                var gm = GameManager.Instance;
                return gm == null || gm.HasGameplayBegun;
            });

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
