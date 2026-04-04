using UnityEngine;
using System.Collections.Generic;

namespace SnakeDefender
{
    public class VerticalProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 18f;
        [SerializeField] private float maxTravelDistance = 15f;

        private float damage;
        private Vector3 startPosition;
        private bool initialized;
        private Vector2 moveDirection = Vector2.up;
        private readonly HashSet<int> hitSegmentIds = new HashSet<int>();
        private Collider2D selfCollider;
        private readonly List<Collider2D> overlapBuffer = new List<Collider2D>(24);
        private static readonly ContactFilter2D SegmentOverlapFilter = ContactFilter2D.noFilter;
        private ProjectilePool pool;

        private void Awake()
        {
            selfCollider = GetComponent<Collider2D>();
        }

        /// <summary>프리팹을 풀에서만 쓸 때 인스턴스 생성 직후 1회 호출됩니다.</summary>
        public void BindPool(ProjectilePool owner)
        {
            pool = owner;
        }

        /// <summary>재사용 전 상태 초기화. 풀 전용.</summary>
        internal void ClearForPool()
        {
            initialized = false;
            hitSegmentIds.Clear();
        }

        private void Despawn()
        {
            if (pool != null)
            {
                pool.Release(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void Initialize(float projectileDamage, float travelDistance, Vector2 direction)
        {
            damage = projectileDamage;
            maxTravelDistance = travelDistance;
            startPosition = transform.position;
            initialized = true;
            hitSegmentIds.Clear();
            moveDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;
            // Align projectile sprite with travel direction.
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            transform.position += (Vector3)(moveDirection * (speed * Time.deltaTime));

            if (Vector3.Distance(startPosition, transform.position) >= maxTravelDistance)
            {
                Despawn();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!initialized)
            {
                return;
            }

            // Resolve every segment whose collider currently overlaps this bullet.
            // (Two body segments in the same overlap region used to only receive one TakeDamage
            // because we destroyed the projectile on the first trigger callback.)
            TryApplyDamageToAllOverlappingSegments();
        }

        private void TryApplyDamageToAllOverlappingSegments()
        {
            if (selfCollider == null)
            {
                selfCollider = GetComponent<Collider2D>();
            }

            if (selfCollider == null)
            {
                return;
            }

            overlapBuffer.Clear();
            int count = Physics2D.OverlapCollider(selfCollider, SegmentOverlapFilter, overlapBuffer);
            if (count <= 0)
            {
                return;
            }

            bool dealtAny = false;
            for (int i = 0; i < overlapBuffer.Count; i++)
            {
                Collider2D col = overlapBuffer[i];
                if (col == null)
                {
                    continue;
                }

                SnakeEnemySegment segment = col.GetComponentInParent<SnakeEnemySegment>();
                if (segment == null || !segment.CanBeDamaged)
                {
                    continue;
                }

                int segmentId = segment.GetInstanceID();
                if (!hitSegmentIds.Add(segmentId))
                {
                    // Same segment can have multiple disc colliders; damage once per projectile.
                    continue;
                }

                segment.TakeDamage(damage);
                dealtAny = true;
            }

            if (dealtAny)
            {
                Despawn();
            }
        }
    }
}
