using UnityEngine;

namespace SnakeDefender
{
    public class DefenderTower : MonoBehaviour
    {
        [Header("Projectile")]
        [SerializeField] private VerticalProjectile projectilePrefab;
        [SerializeField] private Transform firePoint;

        [Header("Attack")]
        [SerializeField] private float maxVerticalRange = 15f;
        [SerializeField] private float attackDamage = 15f;
        [SerializeField] private float attackInterval = 0.6f;
        [SerializeField] private LayerMask enemyMask;

        private float attackCooldown;

        private void Update()
        {
            attackCooldown -= Time.deltaTime;

            if (attackCooldown > 0f)
            {
                return;
            }

            SnakeEnemySegment target = FindFirstSegmentOnVerticalLine();
            if (target == null)
            {
                return;
            }

            SpawnProjectile();
            attackCooldown = attackInterval;
        }

        private void SpawnProjectile()
        {
            Debug.Log("SpawnProjectile called");

            if (projectilePrefab == null)
            {
                return;
            }

            Vector3 spawnPos = firePoint == null ? transform.position : firePoint.position;
            VerticalProjectile projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            projectile.Initialize(attackDamage, maxVerticalRange);
        }

        private SnakeEnemySegment FindFirstSegmentOnVerticalLine()
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(
                transform.position,
                Vector2.up,
                maxVerticalRange,
                enemyMask);

            for (int i = 0; i < hits.Length; i++)
            {
                SnakeEnemySegment segment = hits[i].collider.GetComponentInParent<SnakeEnemySegment>();
                if (segment == null || !segment.CanBeDamaged)
                {
                    continue;
                }

                return segment;
            }

            return null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 end = transform.position + (Vector3)(Vector2.up * maxVerticalRange);
            Gizmos.DrawLine(transform.position, end);
        }
    }
}
