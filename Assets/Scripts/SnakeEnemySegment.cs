using UnityEngine;

namespace SnakeDefender
{
    public class SnakeEnemySegment : MonoBehaviour
    {
        [SerializeField] private float maxHp = 30f;

        private float currentHp;
        private SnakeEnemy owner;
        private bool initialized;
        private bool canBeDamaged;

        public bool CanBeDamaged => canBeDamaged;

        public void Initialize(SnakeEnemy enemyOwner, float hp, bool damageable = true)
        {
            owner = enemyOwner;
            maxHp = hp;
            currentHp = maxHp;
            canBeDamaged = damageable;
            initialized = true;
        }

        public void TakeDamage(float amount)
        {
            if (!initialized || owner == null || !canBeDamaged || amount <= 0f)
            {
                return;
            }

            currentHp -= amount;
            if (currentHp <= 0f)
            {
                owner.OnSegmentDestroyed(this);
            }
        }
    }
}
