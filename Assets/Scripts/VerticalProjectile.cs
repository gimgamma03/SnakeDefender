using UnityEngine;

namespace SnakeDefender
{
    public class VerticalProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 18f;
        [SerializeField] private float maxTravelDistance = 15f;

        private float damage;
        private Vector3 startPosition;
        private bool initialized;

        public void Initialize(float projectileDamage, float travelDistance)
        {
            damage = projectileDamage;
            maxTravelDistance = travelDistance;
            startPosition = transform.position;
            initialized = true;
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            transform.position += Vector3.up * (speed * Time.deltaTime);

            if (Vector3.Distance(startPosition, transform.position) >= maxTravelDistance)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            SnakeEnemySegment segment = other.GetComponent<SnakeEnemySegment>();
            if (segment == null)
            {
                return;
            }

            if (!segment.CanBeDamaged)
            {
                return;
            }

            segment.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
