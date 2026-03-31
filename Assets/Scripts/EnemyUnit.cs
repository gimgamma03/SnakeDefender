using System.Collections;
using UnityEngine;

namespace SnakeDefender
{
    public class EnemyUnit : MonoBehaviour
    {
        [Header("Base Stats")]
        [SerializeField] private float maxHp = 100f;
        [SerializeField] private float moveSpeed = 2.4f;
        [SerializeField] private int killScore = 1;

        [Header("Snake Links")]
        [SerializeField] private bool isHead;
        [SerializeField] private EnemyUnit linkedHead;

        [Header("Optional Rage")]
        [SerializeField] private bool useRage;
        [SerializeField] private float rageTriggerProgress = 0.65f;
        [SerializeField] private float rageSpeedMultiplier = 1.5f;

        private float currentHp;
        private int waypointIndex;
        private bool isDead;
        private bool reroutingToBody;
        private bool isRaged;
        private float baseSpeed;
        private PathRoute route;
        private GameFlowController gameFlow;

        public bool IsDead => isDead;
        public int KillScore => killScore;

        public void Initialize(PathRoute pathRoute, GameFlowController flow)
        {
            route = pathRoute;
            gameFlow = flow;
            currentHp = maxHp;
            baseSpeed = moveSpeed;
            waypointIndex = 0;
            isDead = false;
            reroutingToBody = false;
            isRaged = false;
        }

        private void Update()
        {
            if (isDead || route == null)
            {
                return;
            }

            if (!reroutingToBody)
            {
                TryApplyRage();
            }

            MoveAlongPath();
        }

        public void TakeDamage(float amount)
        {
            if (isDead)
            {
                return;
            }

            currentHp -= amount;
            if (currentHp <= 0f)
            {
                Die(false);
            }
        }

        public void OnBodyDestroyed(Vector3 bodyPosition)
        {
            if (!isHead || isDead)
            {
                return;
            }

            StartCoroutine(MoveHeadToBody(bodyPosition));
        }

        private IEnumerator MoveHeadToBody(Vector3 targetPosition)
        {
            reroutingToBody = true;
            while (Vector3.Distance(transform.position, targetPosition) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    moveSpeed * Time.deltaTime);
                yield return null;
            }

            reroutingToBody = false;
        }

        private void MoveAlongPath()
        {
            if (waypointIndex >= route.WaypointCount)
            {
                ReachGoal();
                return;
            }

            Vector3 target = route.GetWaypointPosition(waypointIndex);
            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target) <= 0.05f)
            {
                waypointIndex++;
            }
        }

        private void TryApplyRage()
        {
            if (!useRage || isRaged || route.WaypointCount <= 0)
            {
                return;
            }

            float progress = (float)waypointIndex / route.WaypointCount;
            if (progress >= rageTriggerProgress)
            {
                isRaged = true;
                moveSpeed = baseSpeed * rageSpeedMultiplier;
            }
        }

        private void ReachGoal()
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            gameFlow?.NotifyEnemyReachedGoal(this);
            Destroy(gameObject);
        }

        private void Die(bool reachedGoal)
        {
            if (isDead)
            {
                return;
            }

            isDead = true;

            if (!reachedGoal)
            {
                gameFlow?.NotifyEnemyKilled(this);
            }

            if (!isHead && linkedHead != null)
            {
                linkedHead.OnBodyDestroyed(transform.position);
            }

            Destroy(gameObject);
        }
    }
}
