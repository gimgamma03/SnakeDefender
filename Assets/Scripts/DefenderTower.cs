using UnityEngine;

namespace SnakeDefender
{
    public class DefenderTower : MonoBehaviour
    {
        [Header("Projectile")]
        [SerializeField] private VerticalProjectile projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Animator muzzleFlashAnimator;
        [SerializeField] private string muzzleFlashStateName = "muzzle_flash_rifle";
        [SerializeField] private string muzzleFlashTrigger = "Fire";

        [Header("Attack")]
        [SerializeField] private float maxVerticalRange = 15f;
        [SerializeField] private float attackDamage = 15f;
        [SerializeField] private float attackInterval = 0.6f;
        [SerializeField] private LayerMask enemyMask;
        [SerializeField] private int raycastBufferSize = 32;

        [Header("Upgrades (스펙업)")]
        [Tooltip("공격력 업그레이드 1회당 배율 가산 (예: 0.2 = +20%).")]
        [SerializeField] private float attackPowerBonusPerLevel = 0.2f;
        [Tooltip("공격속도 업그레이드 1회당 공격 간격에 곱함 (예: 0.9 = 10% 빨라짐).")]
        [SerializeField] private float attackIntervalScalePerSpeedLevel = 0.9f;
        [Tooltip("총알 수 업 1단계당 좌/우 발사 각도 간격(도). 보통 5~10.")]
        [SerializeField] private float bulletSpreadStepDegrees = 7f;
        [Tooltip("총알 수 업그레이드 최대 단계. 3이면 최대 7발(중앙 1 + 좌우 3쌍).")]
        [SerializeField] private int maxBulletCountUpgradeLevel = 3;

        private int attackPowerLevel;
        private int attackSpeedLevel;
        private int bulletCountUpgradeLevel;

        private float attackCooldown;
        private RaycastHit2D[] raycastHits;

        private void Awake()
        {
            raycastHits = new RaycastHit2D[Mathf.Max(4, raycastBufferSize)];
        }

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

            SpawnProjectiles();
            attackCooldown = GetEffectiveAttackInterval();
        }

        private float GetEffectiveDamage()
        {
            return attackDamage * (1f + attackPowerLevel * attackPowerBonusPerLevel);
        }

        private float GetEffectiveAttackInterval()
        {
            float t = attackInterval;
            for (int i = 0; i < attackSpeedLevel; i++)
            {
                t *= attackIntervalScalePerSpeedLevel;
            }

            return Mathf.Max(0.05f, t);
        }

        public void ApplyAttackPowerUpgrade()
        {
            attackPowerLevel++;
        }

        public void ApplyAttackSpeedUpgrade()
        {
            attackSpeedLevel++;
        }

        public int BulletCountUpgradeLevel => bulletCountUpgradeLevel;
        public int MaxBulletCountUpgradeLevel => maxBulletCountUpgradeLevel;
        public bool IsBulletCountUpgradeMaxed =>
            maxBulletCountUpgradeLevel <= 0 || bulletCountUpgradeLevel >= maxBulletCountUpgradeLevel;

        /// <returns>최대치면 false (단계 증가 없음).</returns>
        public bool TryApplyBulletCountUpgrade()
        {
            if (IsBulletCountUpgradeMaxed)
            {
                return false;
            }

            bulletCountUpgradeLevel++;
            return true;
        }

        public void ApplyBulletCountUpgrade()
        {
            TryApplyBulletCountUpgrade();
        }

        private void SpawnProjectiles()
        {
            if (projectilePrefab == null)
            {
                return;
            }

            Vector3 spawnPos = firePoint == null ? transform.position : firePoint.position;
            float dmg = GetEffectiveDamage();
            float spreadStep = Mathf.Clamp(bulletSpreadStepDegrees, 0f, 45f);

            SpawnOneProjectile(spawnPos, Vector2.up, dmg);
            int sidePairs = Mathf.Clamp(bulletCountUpgradeLevel, 0, Mathf.Max(0, maxBulletCountUpgradeLevel));
            for (int i = 1; i <= sidePairs; i++)
            {
                float angle = spreadStep * i;
                Vector2 leftDir = Quaternion.Euler(0f, 0f, -angle) * Vector3.up;
                Vector2 rightDir = Quaternion.Euler(0f, 0f, angle) * Vector3.up;
                SpawnOneProjectile(spawnPos, leftDir, dmg);
                SpawnOneProjectile(spawnPos, rightDir, dmg);
            }

            TriggerMuzzleFlash();
        }

        private void SpawnOneProjectile(Vector3 spawnPos, Vector2 direction, float damage)
        {
            VerticalProjectile projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            projectile.Initialize(damage, maxVerticalRange, direction);
        }

        private void TriggerMuzzleFlash()
        {
            if (muzzleFlashAnimator == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(muzzleFlashStateName))
            {
                int stateHash = Animator.StringToHash(muzzleFlashStateName);
                if (muzzleFlashAnimator.HasState(0, stateHash))
                {
                    muzzleFlashAnimator.Play(stateHash, 0, 0f);
                    return;
                }
            }

            if (!string.IsNullOrEmpty(muzzleFlashTrigger))
            {
                muzzleFlashAnimator.ResetTrigger(muzzleFlashTrigger);
                muzzleFlashAnimator.SetTrigger(muzzleFlashTrigger);
            }
        }

        private SnakeEnemySegment FindFirstSegmentOnVerticalLine()
        {
            int hitCount = Physics2D.RaycastNonAlloc(
                transform.position,
                Vector2.up,
                raycastHits,
                maxVerticalRange,
                enemyMask);

            float bestDistance = float.MaxValue;
            SnakeEnemySegment best = null;
            for (int i = 0; i < hitCount; i++)
            {
                SnakeEnemySegment segment = raycastHits[i].collider.GetComponentInParent<SnakeEnemySegment>();
                if (segment == null || !segment.CanBeDamaged)
                {
                    continue;
                }

                float d = raycastHits[i].distance;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = segment;
                }
            }

            return best;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 end = transform.position + (Vector3)(Vector2.up * maxVerticalRange);
            Gizmos.DrawLine(transform.position, end);
        }
    }
}
