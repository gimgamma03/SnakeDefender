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

        // 풀 쓸 거면 만들자마자 한 번만 BindPool 호출하면 됨.
        public void BindPool(ProjectilePool owner)
        {
            pool = owner;
        }

        // 풀에 넣기 전에 상태만 비우는 용도임.
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
            // 날아가는 방향이랑 스프라이트 맞추려고 회전시키는 거임.
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

            // 겹쳐있는 몸통 콜라이더 전부 찾아서 맞춤. 예전엔 첫 트리거에서 총알 없애서 한쪽만 맞았음.
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
                    // 디스크 콜라이더 여러 개면 같은 세그먼트가 중복으로 들어올 수 있어서 한 발당 한 번만 맞추는 거임.
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
